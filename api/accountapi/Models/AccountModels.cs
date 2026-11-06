namespace AccountAPI.Models;

public record CreateAccountRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Program,
    string YearLevel,
    string Password);

public record LoginRequest(string StudentId, string Password);
public record AdminLoginRequest(string Username, string Password);

public record StudentProfile(
    string StudentId,
    string Name,
    string Program,
    string Email,
    string YearLevel,
    string ClassSection = "",
    string Role = "Student");

public record AccountResponse(bool Success, string Message, StudentProfile? Student);

public record AdminResponse(bool Success, string Message, string Username, string Role = "Admin");

public record UpdateStudentRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Program,
    string YearLevel,
    string ClassSection);

public record TransactionRequest(
    string StudentId,
    string Program,
    string ClassSection,
    string Description,
    decimal Amount,
    string Type,
    string Date,
    string? Notes);

public record TransactionRecord(
    int Id,
    string StudentId,
    string Program,
    string ClassSection,
    string Description,
    decimal Amount,
    string Type,
    string Date,
    string Notes);

public record GradeRecord(  //Placeholder model
    int Id,
    string StudentId,
    string Program,
    string ClassSection,
    string CourseCode,
    string CourseName,
    string Semester,
    decimal Grade);

public class AccountRecord
{
    public string StudentId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Program { get; set; } = "";
    public string YearLevel { get; set; } = "";
    public string ClassSection { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
}
