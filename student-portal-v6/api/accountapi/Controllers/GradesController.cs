using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data.Models;

namespace AccountAPI.Controllers;

[ApiController]
[Route("api/grades")]
public class GradesController : ControllerBase
{
    private readonly PortalDbContext _db;

    public GradesController(PortalDbContext db) => _db = db;

    // PLACEHOLDER DATA: fictional subjects. A student with no grades yet gets these once,
    // so the page isn't empty. When real grades are entered into the Grades table, delete
    // this array and the seeding block inside List().
    private static readonly (string Sem, string Code, string Name, string Instructor, double Units, double P, double M, double F, double R)[] Demo =
    {
        ("first",  "CS101",   "Introduction to Computing",  "A. Rivera",     3, 91, 93, 94, 93),
        ("first",  "MATH110", "College Algebra",            "L. Mendoza",    3, 88, 90, 92, 90),
        ("first",  "ENG105",  "Academic Communication",     "C. Santos",     3, 94, 92, 95, 94),
        ("first",  "HUM120",  "Art and Society",            "R. Flores",     3, 90, 91, 93, 91),
        ("first",  "PE101",   "Movement and Wellness",      "D. Cruz",       2, 95, 96, 94, 95),
        ("second", "CS120",   "Programming Fundamentals",   "M. Villanueva", 3, 92, 94, 95, 94),
        ("second", "STAT115", "Introductory Statistics",    "E. Garcia",     3, 89, 91, 93, 91),
        ("second", "ENG120",  "Professional Communication", "N. Reyes",      3, 93, 94, 94, 94),
        ("second", "SOC110",  "Culture and Community",      "P. Lim",        3, 91, 90, 92, 91),
        ("second", "PE102",   "Fitness and Recreation",     "D. Cruz",       2, 96, 95, 97, 96),
    };

    // GET api/grades?studentId=2026-0001
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? studentId)
    {
        var id = await _db.FindStudentIdAsync(studentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        // Seed placeholder grades once for a student who has none.
        if (!await _db.Grades.AnyAsync(g => g.StudentId == id))
        {
            _db.Grades.AddRange(Demo.Select(d => new Grade
            {
                StudentId = id.Value,
                SchoolYear = "2026-2027",
                Semester = d.Sem,
                SubjectCode = d.Code,
                SubjectName = d.Name,
                Instructor = d.Instructor,
                Units = d.Units,
                Prelim = d.P,
                Midterm = d.M,
                Finals = d.F,
                FinalRating = d.R
            }));
            await _db.SaveChangesAsync();
        }

        var grades = await _db.Grades
            .AsNoTracking()
            .Where(g => g.StudentId == id)
            .OrderBy(g => g.SchoolYear).ThenBy(g => g.Semester).ThenBy(g => g.Id)
            .Select(g => new
            {
                id = g.Id,
                schoolYear = g.SchoolYear,
                semester = g.Semester,
                subjectCode = g.SubjectCode,
                subjectName = g.SubjectName,
                instructor = g.Instructor,
                units = g.Units,
                prelim = g.Prelim,
                midterm = g.Midterm,
                finals = g.Finals,
                finalRating = g.FinalRating
            })
            .ToListAsync();

        return Ok(new { grades });
    }
}