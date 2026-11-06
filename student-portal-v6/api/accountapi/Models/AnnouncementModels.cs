using StudentPortal.Data.Models;

namespace AccountAPI.Models;

// Body of POST api/announcements
public record CreateAnnouncementRequest(string? Title, string? Body, string? Category, string? Date, bool Pinned);

public record AnnouncementListResponse(bool Success, string Message, List<AnnouncementRecord> Announcements);
public record AnnouncementResponse(bool Success, string Message, AnnouncementRecord? Announcement);