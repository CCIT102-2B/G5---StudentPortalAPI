using LibraryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LibraryAPI.Controllers;

[ApiController]
[Route("api/library/books")]
public class BooksController : ControllerBase
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private const string AdminKey = "Dragonborn:Fusrodah";
    private const int MaxDetailsLength = 500;

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "books.json");

    // ---------- storage helpers ----------

    private static List<Book> Load()
    {
        lock (Gate)
        {
            if (!System.IO.File.Exists(FilePath))
            {
                // First run: create the file with a few sample books so the library tab is not empty.
                var seed = SeedBooks();
                Save(seed);
                return seed;
            }
            try { return JsonSerializer.Deserialize<List<Book>>(System.IO.File.ReadAllText(FilePath)) ?? new(); }
            catch { return new(); }
        }
    }

    private static void Save(List<Book> books) =>
        System.IO.File.WriteAllText(FilePath, JsonSerializer.Serialize(books, JsonOptions));

    private static List<Book> SeedBooks()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        return new()
        {
            new Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Publisher = "Prentice Hall", PublishedYear = 2008, Category = "Technology", ShortDetails = "A guide to writing readable, maintainable code, covering naming, functions, comments, and refactoring.", TotalCopies = 3, AvailableCopies = 3, Shelf = "T-04", DateAdded = today },
            new Book { Id = 2, Title = "The Pragmatic Programmer", Author = "Andrew Hunt, David Thomas", Publisher = "Addison-Wesley", PublishedYear = 1999, Category = "Technology", ShortDetails = "Practical advice on software craftsmanship, debugging, testing, and working as a professional developer.", TotalCopies = 2, AvailableCopies = 2, Shelf = "T-05", DateAdded = today },
            new Book { Id = 3, Title = "Introduction to Algorithms", Author = "Thomas H. Cormen et al.", Publisher = "MIT Press", PublishedYear = 2009, Category = "Textbook", ShortDetails = "The standard reference on algorithm design and analysis, with proofs and worked examples.", TotalCopies = 2, AvailableCopies = 2, Shelf = "R-01", DateAdded = today },
            new Book { Id = 4, Title = "Computer Networking: A Top-Down Approach", Author = "James F. Kurose, Keith W. Ross", Publisher = "Pearson", PublishedYear = 2016, Category = "Textbook", ShortDetails = "An introduction to computer networks, starting from applications and working down to the physical layer.", TotalCopies = 2, AvailableCopies = 1, Shelf = "T-11", DateAdded = today }
        };
    }

    private static bool IsAdmin(string? key) => string.Equals(key, AdminKey, StringComparison.Ordinal);

    private static string Clean(string? value, string fallback = "") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static bool Matches(string? text, string term) =>
        (text ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase);

    private static Book Normalize(Book source) => new()
    {
        Title = Clean(source.Title),
        Author = Clean(source.Author),
        Isbn = Clean(source.Isbn),
        Category = Clean(source.Category, "General"),
        Publisher = Clean(source.Publisher),
        PublishedYear = source.PublishedYear,
        ShortDetails = Clean(source.ShortDetails),
        TotalCopies = source.TotalCopies,
        Shelf = Clean(source.Shelf)
    };

    private static string? Validate(Book book)
    {
        if (book.Title.Length == 0) return "Title is required.";
        if (book.Title.Length > 200) return "Title must be 200 characters or fewer.";
        if (book.Author.Length == 0) return "Author is required.";
        if (book.TotalCopies < 1 || book.TotalCopies > 999) return "Total copies must be between 1 and 999.";
        if (book.PublishedYear is int year && (year < 1450 || year > 2100)) return "Year published must be between 1450 and 2100.";
        if (book.ShortDetails.Length > MaxDetailsLength) return $"Short details must be {MaxDetailsLength} characters or fewer.";
        return null;
    }

    // ---------- public endpoints (students and admin) ----------

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetBooks([FromQuery] string? search, [FromQuery] string? category)
    {
        IEnumerable<Book> books = Load();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            books = books.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            books = books.Where(b =>
                Matches(b.Title, term) || Matches(b.Author, term) || Matches(b.Isbn, term) ||
                Matches(b.Category, term) || Matches(b.Publisher, term));
        }

        return Ok(books.OrderBy(b => b.Title));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetBook(int id)
    {
        var book = Load().FirstOrDefault(b => b.Id == id);
        if (book == null) return NotFound(new { message = "Book not found." });
        return Ok(book);
    }

    // ---------- admin endpoints ----------

    [HttpPost]
    public IActionResult CreateBook([FromBody] Book input, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized(new { message = "Administrator access is required." });

        var book = Normalize(input);
        var error = Validate(book);
        if (error != null) return BadRequest(new { message = error });

        lock (Gate)
        {
            var books = Load();
            book.Id = books.Count == 0 ? 1 : books.Max(b => b.Id) + 1;
            book.AvailableCopies = book.TotalCopies;
            book.DateAdded = DateTime.Now.ToString("yyyy-MM-dd");
            books.Add(book);
            Save(books);
            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateBook(int id, [FromBody] Book input, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized(new { message = "Administrator access is required." });

        var updated = Normalize(input);
        var error = Validate(updated);
        if (error != null) return BadRequest(new { message = error });

        lock (Gate)
        {
            var books = Load();
            var existing = books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return NotFound(new { message = "Book not found." });

            // Copies currently checked out stay checked out when the total changes.
            int onLoan = Math.Max(0, existing.TotalCopies - existing.AvailableCopies);
            if (updated.TotalCopies < onLoan)
                return BadRequest(new { message = $"Total copies cannot be lower than the {onLoan} copy(ies) currently checked out." });

            updated.Id = id;
            updated.DateAdded = existing.DateAdded;
            updated.AvailableCopies = updated.TotalCopies - onLoan;
            books[books.IndexOf(existing)] = updated;
            Save(books);
            return Ok(updated);
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteBook(int id, [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (!IsAdmin(adminKey)) return Unauthorized(new { message = "Administrator access is required." });

        lock (Gate)
        {
            var books = Load();
            var existing = books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return NotFound(new { message = "Book not found." });
            if (existing.AvailableCopies < existing.TotalCopies)
                return BadRequest(new { message = "Return all copies before deleting this book." });

            books.Remove(existing);
            Save(books);
            return NoContent();
        }
    }

    [HttpPost("{id:int}/checkout")]
    public IActionResult CheckOut(int id, [FromHeader(Name = "X-Admin-Key")] string? adminKey) =>
        ChangeAvailability(id, adminKey, -1);

    [HttpPost("{id:int}/return")]
    public IActionResult ReturnCopy(int id, [FromHeader(Name = "X-Admin-Key")] string? adminKey) =>
        ChangeAvailability(id, adminKey, 1);

    private IActionResult ChangeAvailability(int id, string? adminKey, int change)
    {
        if (!IsAdmin(adminKey)) return Unauthorized(new { message = "Administrator access is required." });

        lock (Gate)
        {
            var books = Load();
            var book = books.FirstOrDefault(b => b.Id == id);
            if (book == null) return NotFound(new { message = "Book not found." });

            int next = book.AvailableCopies + change;
            if (next < 0) return BadRequest(new { message = "No copies are available to check out." });
            if (next > book.TotalCopies) return BadRequest(new { message = "All copies are already on the shelf." });

            book.AvailableCopies = next;
            Save(books);
            return Ok(book);
        }
    }
}
