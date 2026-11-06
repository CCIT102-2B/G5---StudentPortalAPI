using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data.Models;

namespace AccountAPI.Controllers;

[ApiController]
[Route("api/statement")]
public class StatementController : ControllerBase
{
    private readonly PortalDbContext _db;
    public StatementController(PortalDbContext db) => _db = db;

    // DEMO DATA: placeholder fees added the first time a student opens the statement.
    private static readonly (string Description, decimal Amount)[] DemoFees =
    {
        ("Tuition (24 units)", 28800.00m),
        ("Miscellaneous fees", 6500.00m),
        ("Laboratory fees", 3200.00m),
        ("Registration fee", 500.00m)
    };

    // GET api/statement?studentId=s2024101547
    // Charges = AssessedFees. Payments = this student's "Tuition Fee" transactions.
    // "Non-School Fee" transactions are reported separately and do not change the balance.
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? studentId)
    {
        var id = await _db.FindStudentIdAsync(studentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        if (!await _db.AssessedFees.AnyAsync(f => f.StudentId == id))
        {
            var assessedOn = new DateOnly(2026, 8, 10);
            foreach (var (description, amount) in DemoFees)
                _db.AssessedFees.Add(new AssessedFee { StudentId = id.Value, Description = description, Amount = amount, Date = assessedOn });
            await _db.SaveChangesAsync();
        }

        var fees = await _db.AssessedFees.Where(f => f.StudentId == id).ToListAsync();
        var payments = await _db.Transactions.Where(t => t.StudentId == id && t.Type == "Tuition Fee").ToListAsync();
        var nonSchool = await _db.Transactions.Where(t => t.StudentId == id && t.Type == "Non-School Fee").SumAsync(t => t.Amount);

        // Charges and payments in one list, oldest first (charges before payments on the same day).
        var lines = fees
            .Select(f => new { f.Date, Order = 0, f.Id, f.Description, Kind = "Charge", Charge = f.Amount, Payment = 0m })
            .Concat(payments.Select(p => new { p.Date, Order = 1, p.Id, p.Description, Kind = "Payment", Charge = 0m, Payment = p.Amount }))
            .OrderBy(l => l.Date).ThenBy(l => l.Order).ThenBy(l => l.Id)
            .ToList();

        decimal running = 0;
        var ledger = new List<object>();
        foreach (var l in lines)
        {
            running += l.Charge - l.Payment;
            ledger.Add(new
            {
                date = l.Date.ToString("yyyy-MM-dd"),
                description = l.Description,
                kind = l.Kind,
                charge = l.Charge,
                payment = l.Payment,
                balance = running
            });
        }

        var assessed = fees.Sum(f => f.Amount);
        var paid = payments.Sum(p => p.Amount);

        return Ok(new
        {
            ledger,
            totals = new { assessed, paid, balance = assessed - paid, nonSchool }
        });
    }
}