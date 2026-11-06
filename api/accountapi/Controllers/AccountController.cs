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
    private static string TransactionsPath => Path.Combine(AppContext.BaseDirectory, "transactions.json");
  
    // Demo administrator requested for this school project.
    private const string AdminUsername = "Dragonborn";
    private const string AdminPassword = "Fusrodah";

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

    private static List<TransactionRecord> LoadTransactions()
    {
        if (!System.IO.File.Exists(TransactionsPath)) return new();
        try { return JsonSerializer.Deserialize<List<TransactionRecord>>(System.IO.File.ReadAllText(TransactionsPath)) ?? new(); }
        catch { return new(); }
    }

    private static void SaveTransactions(List<TransactionRecord> records) =>
        System.IO.File.WriteAllText(TransactionsPath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));

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
        a.Program, a.Email, a.YearLevel, a.ClassSection, "Student");

    private static string GenerateStudentNumber(List<AccountRecord> accounts)
    {
        // S + current year + current student-count sequence + five-digit unique suffix.
        // Example shape: S20261901850.
        int year = DateTime.Now.Year;
        int count = accounts.Count + 1;
        return $"S{year}{count:D2}{RandomNumberGenerator.GetInt32(10000, 100000):D5}";
    }

    [HttpPost("create")]
    public ActionResult<AccountResponse> Create(CreateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Program) ||
            string.IsNullOrWhiteSpace(request.YearLevel) || string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 6)
            return BadRequest(new AccountResponse(false, "Complete all required fields. Password must be at least 6 characters.", null));

        lock (Gate)
        {
            var accounts = Load();
            string studentId = GenerateStudentNumber(accounts);
            while (accounts.Any(a => a.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase)))
            {
                // The sequence is derived from the count, so this only matters if records were manually edited.
                studentId = $"S{DateTime.Now.Year}{accounts.Count + 1:D2}{RandomNumberGenerator.GetInt32(10000, 100000):D5}";
            }

            var password = HashPassword(request.Password);
            var account = new AccountRecord
            {
                StudentId = studentId,
                FirstName = request.FirstName.Trim(),
                MiddleName = request.MiddleName?.Trim() ?? "",
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                Program = request.Program.Trim(),
                YearLevel = request.YearLevel.Trim(),
                ClassSection = "",
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt
            };
            accounts.Add(account);
            Save(accounts);
            return Ok(new AccountResponse(true, $"Account created successfully. Your student number is {studentId}.", Profile(account)));
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

    [HttpPost("admin-login")]
    public ActionResult<AdminResponse> AdminLogin(AdminLoginRequest request)
    {
        if (string.Equals(request.Username?.Trim(), AdminUsername, StringComparison.OrdinalIgnoreCase) && request.Password == AdminPassword)
            return Ok(new AdminResponse(true, "Administrator login successful.", AdminUsername));
        return Unauthorized(new AdminResponse(false, "Invalid administrator username or password.", ""));
    }

    [HttpGet("students")]
    public ActionResult<IEnumerable<StudentProfile>> Students()
    {
        return Ok(Load().Select(Profile).OrderBy(s => s.StudentId));
    }

    [HttpPut("students/{studentId}")]
    public ActionResult<AccountResponse> UpdateStudent(string studentId, UpdateStudentRequest request, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (adminKey != $"{AdminUsername}:{AdminPassword}") return Unauthorized();
        lock (Gate)
        {
            var accounts = Load();
            var account = accounts.FirstOrDefault(a => a.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase));
            if (account == null) return NotFound(new AccountResponse(false, "Student not found.", null));

            account.FirstName = request.FirstName.Trim();
            account.MiddleName = request.MiddleName?.Trim() ?? "";
            account.LastName = request.LastName.Trim();
            account.Email = request.Email.Trim();
            account.Program = request.Program.Trim();
            account.YearLevel = request.YearLevel.Trim();
            account.ClassSection = request.ClassSection?.Trim() ?? "";
            Save(accounts);
            return Ok(new AccountResponse(true, "Student updated successfully.", Profile(account)));
        }
    }

    [HttpPost("transactions")]
    public ActionResult<TransactionRecord> AddTransaction(TransactionRequest request, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (adminKey != $"{AdminUsername}:{AdminPassword}") return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.StudentId) || string.IsNullOrWhiteSpace(request.Program) ||
            string.IsNullOrWhiteSpace(request.ClassSection) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Student, program, class, and description are required.");

        lock (Gate)
        {
            var records = LoadTransactions();
            var record = new TransactionRecord(
                records.Count == 0 ? 1 : records.Max(x => x.Id) + 1,
                request.StudentId.Trim(), request.Program.Trim(), request.ClassSection.Trim(), request.Description.Trim(),
                request.Amount, request.Type.Trim(), request.Date.Trim(), request.Notes?.Trim() ?? "");
            records.Add(record);
            SaveTransactions(records);
            return Ok(record);
        }
    }

    [HttpGet("transactions/student/{studentId}")]
    public ActionResult<IEnumerable<TransactionRecord>> StudentTransactions(string studentId) =>
        Ok(LoadTransactions().Where(t => t.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase)).OrderByDescending(t => t.Date));

    [HttpGet("transactions")]
    public ActionResult<IEnumerable<TransactionRecord>> Transactions([FromQuery] string? program, [FromQuery] string? classSection)
    {
        IEnumerable<TransactionRecord> records = LoadTransactions();
        if (!string.IsNullOrWhiteSpace(program)) records = records.Where(t => t.Program.Equals(program, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(classSection)) records = records.Where(t => t.ClassSection.Equals(classSection, StringComparison.OrdinalIgnoreCase));
        return Ok(records.OrderByDescending(t => t.Date));
    }

  [HttpGet("grades/student/{studentId}")]
public IActionResult StudentGrades(string studentId)
{
    // Placeholder
    return Ok(Array.Empty<GradeRecord>());
}
