using AccountAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;

namespace AccountAPI.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private static readonly object Gate = new();
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "accounts.json");

    private static List<AccountRecord> Load()
    {
        lock (Gate)
        {
            if (!System.IO.File.Exists(FilePath)) return new();
            try { return JsonSerializer.Deserialize<List<AccountRecord>>(System.IO.File.ReadAllText(FilePath)) ?? new(); }
            catch { return new(); }
        }
    }

    private static void Save(List<AccountRecord> accounts)
    {
        lock (Gate)
            System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    private static bool VerifyPassword(string password, AccountRecord account)
    {
        try
        {
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(account.PasswordSalt), 100000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(account.PasswordHash));
        }
        catch { return false; }
    }

    private static StudentProfile Profile(AccountRecord a) => new(
        a.StudentId,
        $"{a.LastName.ToUpper()}, {a.FirstName.ToUpper()}{(string.IsNullOrWhiteSpace(a.MiddleName) ? "" : " " + a.MiddleName.ToUpper())}",
        a.Program, a.Email, a.YearLevel);

    [HttpPost("create")]
    public ActionResult<AccountResponse> Create(CreateAccountRequest request)
    {
        string studentId = (request.StudentId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Program) || string.IsNullOrWhiteSpace(request.YearLevel) ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new AccountResponse(false, "Complete all required fields. Password must be at least 6 characters.", null));

        lock (Gate)
        {
            var accounts = Load();
            // Student numbers are unique, ignoring capitalization and surrounding spaces.
            if (accounts.Any(a => string.Equals(a.StudentId.Trim(), studentId, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new AccountResponse(false, "That student number is already registered.", null));

            var password = HashPassword(request.Password);
            var account = new AccountRecord
            {
                StudentId = studentId,
                FirstName = request.FirstName.Trim(), MiddleName = request.MiddleName?.Trim() ?? "", LastName = request.LastName.Trim(),
                Email = request.Email.Trim(), Program = request.Program.Trim(), YearLevel = request.YearLevel.Trim(),
                PasswordHash = password.Hash, PasswordSalt = password.Salt
            };
            accounts.Add(account);
            Save(accounts);
            return Ok(new AccountResponse(true, "Account created successfully.", Profile(account)));
        }
    }

    [HttpPost("login")]
    public ActionResult<AccountResponse> Login(LoginRequest request)
    {
        string studentId = (request.StudentId ?? "").Trim();
        var account = Load().FirstOrDefault(a => string.Equals(a.StudentId.Trim(), studentId, StringComparison.OrdinalIgnoreCase));
        if (account != null && VerifyPassword(request.Password ?? "", account))
            return Ok(new AccountResponse(true, "Login successful.", Profile(account)));
        return Unauthorized(new AccountResponse(false, "Invalid student number or password.", null));
    }
}
