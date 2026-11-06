using Feature03API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Feature03API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private static readonly object Gate = new();
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "items.json");
    private const string AdminKey = "Dragonborn:Fusrodah";

    private static List<Item> Load()
    {
        lock (Gate)
        {
            if (!System.IO.File.Exists(FilePath)) return new();
            try { return JsonSerializer.Deserialize<List<Item>>(System.IO.File.ReadAllText(FilePath)) ?? new(); }
            catch { return new(); }
        }
    }

    private static void Save(List<Item> items) =>
        System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true }));

    private bool IsAdmin(string? key) => string.Equals(key, AdminKey, StringComparison.Ordinal);

    [HttpGet]
    public ActionResult<IEnumerable<Item>> GetItems([FromQuery] string? studentId, [FromQuery] string? status)
    {
        IEnumerable<Item> items = Load();
        if (!string.IsNullOrWhiteSpace(studentId))
            items = items.Where(i => i.ReportedByStudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            items = items.Where(i => i.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        return Ok(items.OrderByDescending(i => i.Id));
    }

    [HttpPost]
    public ActionResult<Item> CreateItem(Item item)
    {
        lock (Gate)
        {
            var items = Load();
            item.Id = items.Count == 0 ? 1 : items.Max(i => i.Id) + 1;
            item.DateReported = string.IsNullOrWhiteSpace(item.DateReported) ? DateTime.Now.ToString("yyyy-MM-dd") : item.DateReported;
            item.Status = string.IsNullOrWhiteSpace(item.Status) ? "Unclaimed" : item.Status;
            items.Add(item);
            Save(items);
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
        }
    }

    [HttpGet("{id}")]
    public ActionResult<Item> GetItem(int id)
    {
        var item = Load().FirstOrDefault(i => i.Id == id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateItem(int id, Item updated, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized();
        lock (Gate)
        {
            var items = Load();
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();
            updated.Id = id;
            items[items.IndexOf(item)] = updated;
            Save(items);
            return Ok(updated);
        }
    }

    [HttpPost("{id}/notify")]
    public IActionResult NotifyStudent(int id, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized();
        var item = Load().FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();
        if (string.IsNullOrWhiteSpace(item.ReportedByStudentId)) return BadRequest("This item has no student number attached.");

        // The frontend calls Announcement API with the same admin key and creates the actual announcement.
        return Ok(new { itemId = item.Id, itemName = item.ItemName, studentId = item.ReportedByStudentId, program = item.Program, message = "Ready to create a personal missing-item announcement." });
    }
}
