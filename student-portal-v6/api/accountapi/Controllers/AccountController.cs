using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AccountAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudentPortal.Data;
using StudentPortal.Data.Models;

namespace AccountAPI.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private const int Iterations = 600_000; // OWASP guidance for PBKDF2-HMAC-SHA256
    private static readonly string DummyHash = HashPassword("dummy-password");

    private readonly PortalDbContext _db;
    public AccountController(PortalDbContext db) => _db = db;

    // Stored in Students.PasswordHash as "iterations.salt.hash" (base64 parts).
    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        try
        {
            var parts = stored.Split('.');
            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expected = Convert.FromBase64String(parts[2]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }

    // The Student table has one Name column and a numeric YearLevel, so convert at the edges.
    private static int? ParseYear(string value)
    {
        var m = Regex.Match(value ?? "", @"\d+");
        return m.Success && int.TryParse(m.Value, out int y) ? y : null;
    }

    private static StudentProfile Profile(Student s) =>
        new(s.StudentId, s.Name, s.Program, s.Email, s.YearLevel?.ToString() ?? "");

    [HttpPost("create")]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request)
    {
        string studentId = (request.StudentId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Program) || string.IsNullOrWhiteSpace(request.YearLevel) ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new AccountResponse(false, "Complete all required fields. Password must be at least 6 characters.", null));

        // Student numbers are unique, ignoring capitalization and surrounding spaces.
        string key = studentId.ToLower();
        if (await _db.Students.AnyAsync(s => s.StudentId.ToLower() == key))
            return Conflict(new AccountResponse(false, "That student number is already registered.", null));

        string middle = request.MiddleName?.Trim() ?? "";
        var student = new Student
        {
            StudentId = studentId,
            Name = $"{request.LastName.Trim().ToUpper()}, {request.FirstName.Trim().ToUpper()}{(middle == "" ? "" : " " + middle.ToUpper())}",
            Email = request.Email.Trim(),
            Program = request.Program.Trim(),
            YearLevel = ParseYear(request.YearLevel),
            PasswordHash = HashPassword(request.Password)
        };

        _db.Students.Add(student);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two sign-ups with the same number at the same moment.
            return Conflict(new AccountResponse(false, "That student number is already registered.", null));
        }

        return Ok(new AccountResponse(true, "Account created successfully.", Profile(student)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccountResponse>> Login(LoginRequest request)
    {
        string key = (request.StudentId ?? "").Trim().ToLower();
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId.ToLower() == key);

        // Hash even when the student doesn't exist so timing doesn't reveal valid student numbers.
        bool ok = VerifyPassword(request.Password ?? "", student?.PasswordHash ?? DummyHash);
        if (student != null && ok)
            return Ok(new AccountResponse(true, "Login successful.", Profile(student)));

        return Unauthorized(new AccountResponse(false, "Invalid student number or password.", null));
    }
}