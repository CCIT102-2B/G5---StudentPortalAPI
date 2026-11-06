using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StudentPortal.Data.Models;

namespace StudentPortal.Data;

public static class PortalDatabase
{
    public static string ConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Portal")
        ?? throw new InvalidOperationException(
            "Database connection string not found. Set the ConnectionStrings__Portal environment variable.");

    // The ONE place that chooses the database provider.
    public static void Configure(DbContextOptionsBuilder options) =>
        options.UseNpgsql(ConnectionString());

    // Safe to call from every API: applies migrations, then adds sample data only if empty.
    public static void Initialize(PortalDbContext db)
    {
        db.Database.Migrate();

        if (!db.Announcements.Any())
        {
            db.Announcements.AddRange(
                new AnnouncementRecord { Date = "2026-10-28", Category = "Academic",  Pinned = true,  Title = "Midterm grades are now posted", Body = "Midterm grades for this semester are available on the Student Portal page. If a grade looks wrong, send a request from Grade Requests." },
                new AnnouncementRecord { Date = "2026-10-26", Category = "Finance",   Pinned = false, Title = "Final tuition installment is due November 15", Body = "Pay at the cashier, then record the payment under General Transactions so your balance stays accurate." },
                new AnnouncementRecord { Date = "2026-10-24", Category = "Events",    Pinned = false, Title = "Foundation Week schedule is out", Body = "Classes follow a shortened schedule from November 3 to 7. Check your department board for room changes." },
                new AnnouncementRecord { Date = "2026-10-22", Category = "Reminders", Pinned = false, Title = "Enrollment for next semester opens November 9", Body = "Check your schedule for overlaps before you add subjects. The maximum load is 24 units." },
                new AnnouncementRecord { Date = "2026-10-19", Category = "Academic",  Pinned = false, Title = "Library hours extended during finals", Body = "The library stays open until 9:00 PM on weekdays during the finals period." },
                new AnnouncementRecord { Date = "2026-10-15", Category = "Events",    Pinned = false, Title = "Student council candidacy filing", Body = "Interested students can file their candidacy at the student affairs office before the deadline." });
            db.SaveChanges();
        }
    }
}

// Lets `dotnet ef` create migrations straight from this project.
public class PortalDbContextFactory : IDesignTimeDbContextFactory<PortalDbContext>
{
    public PortalDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PortalDbContext>();
        PortalDatabase.Configure(builder);
        return new PortalDbContext(builder.Options);
    }
}