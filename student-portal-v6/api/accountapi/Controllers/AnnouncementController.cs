using AccountAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data.Models;
using System.Globalization;

namespace AccountAPI.Controllers;

[ApiController]
[Route("api/announcements")]
public class AnnouncementController : ControllerBase
{
    private static readonly string[] Categories = { "Academic", "Finance", "Events", "Reminders" };

    // Posting and deleting are for staff only. Change this key before sharing the project,
    // and send it in the X-Admin-Key header. Reading announcements needs no key.
    private const string AdminKey = "change-this-staff-key";

    private readonly PortalDbContext _db;
    public AnnouncementController(PortalDbContext db) => _db = db;

    private bool IsStaff(string? key) => key != null && key == AdminKey;

    [HttpGet]
    public async Task<ActionResult<AnnouncementListResponse>> GetAll([FromQuery] string? category)
    {
        IQueryable<AnnouncementRecord> items = _db.Announcements;
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var c = category.Trim().ToLower();
            items = items.Where(a => a.Category.ToLower() == c);
        }
        var sorted = await items.OrderByDescending(a => a.Pinned).ThenByDescending(a => a.Date).ToListAsync();
        return Ok(new AnnouncementListResponse(true, "OK", sorted));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AnnouncementResponse>> GetOne(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        return item == null
            ? NotFound(new AnnouncementResponse(false, "Announcement not found.", null))
            : Ok(new AnnouncementResponse(true, "OK", item));
    }

    [HttpPost]
    public async Task<ActionResult<AnnouncementResponse>> Create([FromHeader(Name = "X-Admin-Key")] string? adminKey, CreateAnnouncementRequest request)
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

        var item = new AnnouncementRecord { Title = title, Body = body, Category = category, Date = date, Pinned = request.Pinned };
        _db.Announcements.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new AnnouncementResponse(true, "Announcement posted.", item));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<AnnouncementResponse>> Delete([FromHeader(Name = "X-Admin-Key")] string? adminKey, int id)
    {
        if (!IsStaff(adminKey))
            return Unauthorized(new AnnouncementResponse(false, "Only staff can delete announcements.", null));

        var item = await _db.Announcements.FindAsync(id);
        if (item == null)
            return NotFound(new AnnouncementResponse(false, "Announcement not found.", null));

        _db.Announcements.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new AnnouncementResponse(true, "Announcement deleted.", item));
    }
}