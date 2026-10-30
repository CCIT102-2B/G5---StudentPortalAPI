using AnnouncementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnnouncementAPI.Controllers;

[ApiController]
[Route("api/announcements")]
public class AnnouncementController : ControllerBase
{
    private static readonly object Gate = new();
    private static readonly string[] Categories = { "Academic", "Finance", "Events", "Reminders" };

    // Posting and deleting are for staff only. Change this key before sharing the project,
    // and send it in the X-Admin-Key header. Reading announcements needs no key.
    private const string AdminKey = "change-this-staff-key";

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "announcements.json");

    private static List<AnnouncementRecord> Load()
    {
        lock (Gate)
        {
            if (!System.IO.File.Exists(FilePath))
            {
                var seed = SeedData();
                Save(seed);
                return seed;
            }
            try { return JsonSerializer.Deserialize<List<AnnouncementRecord>>(System.IO.File.ReadAllText(FilePath)) ?? new(); }
            catch { return new(); }
        }
    }

    private static void Save(List<AnnouncementRecord> announcements)
    {
        lock (Gate)
            System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(announcements, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Sample announcements created the first time the API runs.
    private static List<AnnouncementRecord> SeedData() => new()
    {
        new() { Id = 1, Date = "2026-10-28", Category = "Academic",  Pinned = true,  Title = "Midterm grades are now posted", Body = "Midterm grades for this semester are available on the Student Portal page. If a grade looks wrong, send a request from Grade Requests." },
        new() { Id = 2, Date = "2026-10-26", Category = "Finance",   Pinned = false, Title = "Final tuition installment is due November 15", Body = "Pay at the cashier, then record the payment under General Transactions so your balance stays accurate." },
        new() { Id = 3, Date = "2026-10-24", Category = "Events",    Pinned = false, Title = "Foundation Week schedule is out", Body = "Classes follow a shortened schedule from November 3 to 7. Check your department board for room changes." },
        new() { Id = 4, Date = "2026-10-22", Category = "Reminders", Pinned = false, Title = "Enrollment for next semester opens November 9", Body = "Check your schedule for overlaps before you add subjects. The maximum load is 24 units." },
        new() { Id = 5, Date = "2026-10-19", Category = "Academic",  Pinned = false, Title = "Library hours extended during finals", Body = "The library stays open until 9:00 PM on weekdays during the finals period." },
        new() { Id = 6, Date = "2026-10-15", Category = "Events",    Pinned = false, Title = "Student council candidacy filing", Body = "Interested students can file their candidacy at the student affairs office before the deadline." }
    };

    private bool IsStaff(string? key) => key != null && key == AdminKey;

    // GET api/announcements            -> all announcements
    // GET api/announcements?category=Finance -> one category
    [HttpGet]
    public ActionResult<AnnouncementListResponse> GetAll([FromQuery] string? category)
    {
        IEnumerable<AnnouncementRecord> items = Load();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            items = items.Where(a => a.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));

        // Pinned first, then newest first.
        var sorted = items.OrderByDescending(a => a.Pinned).ThenByDescending(a => a.Date).ToList();
        return Ok(new AnnouncementListResponse(true, "OK", sorted));
    }

    // GET api/announcements/3
    [HttpGet("{id:int}")]
    public ActionResult<AnnouncementResponse> GetOne(int id)
    {
        var item = Load().FirstOrDefault(a => a.Id == id);
        return item == null
            ? NotFound(new AnnouncementResponse(false, "Announcement not found.", null))
            : Ok(new AnnouncementResponse(true, "OK", item));
    }

    // POST api/announcements   (staff only)
    [HttpPost]
    public ActionResult<AnnouncementResponse> Create([FromHeader(Name = "X-Admin-Key")] string? adminKey, CreateAnnouncementRequest request)
    {
        if (!IsStaff(adminKey))
            return Unauthorized(new AnnouncementResponse(false, "Only staff can post announcements.", null));

        string title = (request.Title ?? "").Trim();
        string body = (request.Body ?? "").Trim();
        string? category = Categories.FirstOrDefault(c => c.Equals((request.Category ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

        if (title.Length == 0 || body.Length == 0 || category == null)
            return BadRequest(new AnnouncementResponse(false, $"Title, body and a category ({string.Join(", ", Categories)}) are required.", null));

        string date = DateTime.Today.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(request.Date))
        {
            if (!DateTime.TryParseExact(request.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return BadRequest(new AnnouncementResponse(false, "Date must look like 2026-11-05.", null));
            date = parsed.ToString("yyyy-MM-dd");
        }

        lock (Gate)
        {
            var announcements = Load();
            var item = new AnnouncementRecord
            {
                Id = announcements.Count == 0 ? 1 : announcements.Max(a => a.Id) + 1,
                Title = title, Body = body, Category = category, Date = date, Pinned = request.Pinned
            };
            announcements.Add(item);
            Save(announcements);
            return Ok(new AnnouncementResponse(true, "Announcement posted.", item));
        }
    }

    // DELETE api/announcements/3   (staff only)
    [HttpDelete("{id:int}")]
    public ActionResult<AnnouncementResponse> Delete([FromHeader(Name = "X-Admin-Key")] string? adminKey, int id)
    {
        if (!IsStaff(adminKey))
            return Unauthorized(new AnnouncementResponse(false, "Only staff can delete announcements.", null));

        lock (Gate)
        {
            var announcements = Load();
            var item = announcements.FirstOrDefault(a => a.Id == id);
            if (item == null)
                return NotFound(new AnnouncementResponse(false, "Announcement not found.", null));
            announcements.Remove(item);
            Save(announcements);
            return Ok(new AnnouncementResponse(true, "Announcement deleted.", item));
        }
    }
}