namespace StudentPortal.Data.Models;

public class Student
{
    public int Id { get; set; }
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Program { get; set; } = "Bachelor of Science in Information Technology";
    public int? YearLevel { get; set; }
    public string PasswordHash { get; set; } = "";
}

public class AnnouncementRecord
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Category { get; set; } = "";
    public string Date { get; set; } = "";
    public bool Pinned { get; set; }
}

public class AnnouncementRead
{
    public int StudentId { get; set; }
    public int AnnouncementId { get; set; }
}

public class StudentTransaction
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Tuition Fee";
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
}

public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string SchoolYear { get; set; } = "2026-2027";
    public string Semester { get; set; } = "first";
    public string SubjectCode { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string Instructor { get; set; } = "";
    public double Units { get; set; }
    public double? Prelim { get; set; }
    public double? Midterm { get; set; }
    public double? Finals { get; set; }
    public double? FinalRating { get; set; }
}

// Feature 03: one weekly class meeting. Day is "Mon".."Sat".
public class ScheduleEntry
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string SubjectCode { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string Instructor { get; set; } = "";
    public string Room { get; set; } = "";
    public string Day { get; set; } = "Mon";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

// Feature 04: a fee the school assessed (a charge on the statement).
public class AssessedFee
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
}