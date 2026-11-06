using AnnouncementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnnouncementAPI.Controllers;

[ApiController]
[Route("api/announcements")]
public class AnnouncementController : ControllerBase
{
    private static readonly object Gate = new();
    private const string AdminKey = "Dragonborn:Fusrodah";
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "announcements.json");

    private static List<AnnouncementRecord> Load()
    {
        lock (Gate)
        {
            if (!System.IO.File.Exists(FilePath)) return new();
            try { return JsonSerializer.Deserialize<List<AnnouncementRecord>>(System.IO.File.ReadAllText(FilePath)) ?? new(); }
            catch { return new(); }
        }
    }

    private static void Save(List<AnnouncementRecord> announcements)
    {
        lock (Gate)
            System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(announcements, new JsonSerializerOptions { WriteIndented = true }));
    }

    private bool IsAdmin(string? key) => string.Equals(key, AdminKey, StringComparison.Ordinal);

    [HttpGet]
    public ActionResult<AnnouncementListResponse> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? studentId,
        [FromQuery] string? program)
    {
        IEnumerable<AnnouncementRecord> items = Load();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            items = items.Where(a => a.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(studentId) || !string.IsNullOrWhiteSpace(program))
        {
            items = items.Where(a =>
                a.AudienceType.Equals("Everyone", StringComparison.OrdinalIgnoreCase) ||
                (a.AudienceType.Equals("Program", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(program) && a.AudienceValue.Equals(program.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                (a.AudienceType.Equals("Student", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(studentId) && a.AudienceValue.Equals(studentId.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        var sorted = items.OrderByDescending(a => a.Pinned).ThenByDescending(a => a.Date).ToList();
        return Ok(new AnnouncementListResponse(true, "OK", sorted));
    }

    [HttpPost]
    public ActionResult<AnnouncementRecord> Create(CreateAnnouncementRequest request, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body)) return BadRequest("Title and body are required.");

        lock (Gate)
        {
            var records = Load();
            var record = new AnnouncementRecord
            {
                Id = records.Count == 0 ? 1 : records.Max(a => a.Id) + 1,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(),
                Pinned = request.Pinned,
                Title = request.Title.Trim(),
                Body = request.Body.Trim(),
                AudienceType = string.IsNullOrWhiteSpace(request.AudienceType) ? "Everyone" : request.AudienceType.Trim(),
                AudienceValue = request.AudienceValue?.Trim() ?? "",
                RelatedLostFoundItemId = request.RelatedLostFoundItemId
            };
            records.Add(record);
            Save(records);
            return Ok(record);
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized();
        lock (Gate)
        {
            var records = Load();
            var removed = records.RemoveAll(a => a.Id == id);
            if (removed == 0) return NotFound();
            Save(records);
            return NoContent();
        }
    }
}
