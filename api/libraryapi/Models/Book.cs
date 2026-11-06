namespace LibraryAPI.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Isbn { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Publisher { get; set; } = "";
    public int? PublishedYear { get; set; }
    public string ShortDetails { get; set; } = "";
    public int TotalCopies { get; set; } = 1;
    public int AvailableCopies { get; set; } = 1;
    public string Shelf { get; set; } = "";
    public string DateAdded { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
}
