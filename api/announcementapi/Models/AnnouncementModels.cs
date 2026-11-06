namespace AnnouncementAPI.Models;

public record AnnouncementRecord
{
    public int Id { get; set; }
    public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string Category { get; set; } = "General";
    public bool Pinned { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string AudienceType { get; set; } = "Everyone"; // Everyone, Program, Student
    public string AudienceValue { get; set; } = "";
    public int? RelatedLostFoundItemId { get; set; }
}

public record AnnouncementListResponse(bool Success, string Message, List<AnnouncementRecord> Announcements);
public record CreateAnnouncementRequest(string Title, string Body, string Category, bool Pinned, string AudienceType, string AudienceValue, int? RelatedLostFoundItemId);
