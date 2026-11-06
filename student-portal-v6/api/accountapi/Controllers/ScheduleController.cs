using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data.Models;

namespace AccountAPI.Controllers;

public record ScheduleRequest(
    string? StudentId, string? SubjectCode, string? SubjectName,
    string? Instructor, string? Room, string? Day, string? Start, string? End);

[ApiController]
[Route("api/schedule")]
public class ScheduleController : ControllerBase
{
    private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    private readonly PortalDbContext _db;

    public ScheduleController(PortalDbContext db) => _db = db;

    private static object Shape(ScheduleEntry e) => new
    {
        id = e.Id,
        subjectCode = e.SubjectCode,
        subjectName = e.SubjectName,
        instructor = e.Instructor,
        room = e.Room,
        day = e.Day,
        start = e.StartTime.ToString("HH:mm"),
        end = e.EndTime.ToString("HH:mm")
    };

    // GET api/schedule?studentId=s2024101547
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? studentId)
    {
        var id = await _db.FindStudentIdAsync(studentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        var rows = await _db.ScheduleEntries.Where(e => e.StudentId == id).ToListAsync();
        var sorted = rows
            .OrderBy(e => Array.IndexOf(Days, e.Day))
            .ThenBy(e => e.StartTime)
            .Select(Shape);

        return Ok(new { entries = sorted });
    }

    // POST api/schedule
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] ScheduleRequest req)
    {
        var id = await _db.FindStudentIdAsync(req.StudentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        var code = (req.SubjectCode ?? "").Trim();
        var subject = (req.SubjectName ?? "").Trim();
        var instructor = (req.Instructor ?? "").Trim();
        var room = (req.Room ?? "").Trim();

        if (code.Length == 0 || subject.Length == 0)
            return BadRequest(new { message = "Subject code and subject name are required." });
        if (code.Length > 20 || subject.Length > 100 || instructor.Length > 100 || room.Length > 30)
            return BadRequest(new { message = "One of the fields is too long." });

        var day = Days.FirstOrDefault(d => d.Equals((req.Day ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        if (day == null)
            return BadRequest(new { message = "Choose a day from Monday to Saturday." });

        if (!TimeOnly.TryParseExact(req.Start, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
            !TimeOnly.TryParseExact(req.End, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            return BadRequest(new { message = "Start and end times must look like 08:00." });
        if (end <= start)
            return BadRequest(new { message = "End time must be after the start time." });

        // Two classes overlap when each one starts before the other ends.
        var clash = await _db.ScheduleEntries.FirstOrDefaultAsync(e =>
            e.StudentId == id && e.Day == day && start < e.EndTime && end > e.StartTime);
        if (clash != null)
            return Conflict(new
            {
                message = $"That time overlaps with {clash.SubjectCode} on {day} ({clash.StartTime:HH:mm}-{clash.EndTime:HH:mm})."
            });

        var entry = new ScheduleEntry
        {
            StudentId = id.Value,
            SubjectCode = code,
            SubjectName = subject,
            Instructor = instructor,
            Room = room,
            Day = day,
            StartTime = start,
            EndTime = end
        };
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new { entry = Shape(entry) });
    }

    // DELETE api/schedule/5?studentId=s2024101547
    [HttpDelete("{entryId:int}")]
    public async Task<IActionResult> Delete(int entryId, [FromQuery] string? studentId)
    {
        var id = await _db.FindStudentIdAsync(studentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        var entry = await _db.ScheduleEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.StudentId == id);
        if (entry == null) return NotFound(new { message = "Class not found." });

        _db.ScheduleEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}