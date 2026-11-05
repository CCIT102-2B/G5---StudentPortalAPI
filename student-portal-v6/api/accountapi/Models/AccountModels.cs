namespace AccountAPI.Models;

public record CreateAccountRequest(string StudentId, string FirstName, string? MiddleName, string LastName, string Email, string Program, string YearLevel, string Password);
public record LoginRequest(string StudentId, string Password);
public record StudentProfile(string StudentId, string Name, string Program, string Email, string YearLevel);
public record AccountResponse(bool Success, string Message, StudentProfile? Student);