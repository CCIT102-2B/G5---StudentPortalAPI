namespace AccountAPI.Models;

public record CreateAccountRequest(string StudentId, string FirstName, string? MiddleName, string LastName, string Email, string Program, string YearLevel, string Password);
public record LoginRequest(string StudentId, string Password);
public record StudentProfile(string StudentId, string Name, string Program, string Email, string YearLevel);
public record AccountResponse(bool Success, string Message, StudentProfile? Student);

public class AccountRecord
{
    public string StudentId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Program { get; set; } = "";
    public string YearLevel { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
}
