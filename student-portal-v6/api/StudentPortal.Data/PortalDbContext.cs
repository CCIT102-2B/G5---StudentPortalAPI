using Microsoft.EntityFrameworkCore;
using StudentPortal.Data.Models;

namespace StudentPortal.Data;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<AnnouncementRecord> Announcements => Set<AnnouncementRecord>();
    public DbSet<AnnouncementRead> AnnouncementReads => Set<AnnouncementRead>();
    public DbSet<StudentTransaction> Transactions => Set<StudentTransaction>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<AssessedFee> AssessedFees => Set<AssessedFee>();

    public async Task<int?> FindStudentIdAsync(string? studentId)
    {
        var key = (studentId ?? "").Trim().ToLower();
        if (key.Length == 0) return null;
        return await Students
            .Where(s => s.StudentId.ToLower() == key)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Student>().HasIndex(s => s.StudentId).IsUnique();
        b.Entity<AnnouncementRecord>().ToTable("Announcements");

        b.Entity<AnnouncementRead>().HasKey(r => new { r.StudentId, r.AnnouncementId });
        b.Entity<AnnouncementRead>().HasOne<Student>().WithMany().HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AnnouncementRead>().HasOne<AnnouncementRecord>().WithMany().HasForeignKey(r => r.AnnouncementId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<StudentTransaction>().HasOne<Student>().WithMany().HasForeignKey(t => t.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Grade>().HasOne<Student>().WithMany().HasForeignKey(g => g.StudentId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<ScheduleEntry>().HasOne<Student>().WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AssessedFee>().HasOne<Student>().WithMany().HasForeignKey(f => f.StudentId).OnDelete(DeleteBehavior.Cascade);
    }
}