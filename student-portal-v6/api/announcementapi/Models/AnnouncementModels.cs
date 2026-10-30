namespace AnnouncementAPI.Models;

// One announcement as it is stored in announcements.json and sent to the frontend.
public class AnnouncementRecord
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Category { get; set; } = "";   // Academic, Finance, Events or Reminders
    public string Date { get; set; } = "";       // yyyy-MM-dd
    public bool Pinned { get; set; }
}

// Body of POST api/announcements
public record CreateAnnouncementRequest(string? Title, string? Body, string? Category, string? Date, bool Pinned);

// Responses (same style as the Account API: Success + Message + data)
public record AnnouncementListResponse(bool Success, string Message, List<AnnouncementRecord> Announcements);
public record AnnouncementResponse(bool Success, string Message, AnnouncementRecord? Announcement);