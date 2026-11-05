using AccountAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data.Models;

namespace AccountAPI.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private static readonly string[] AllowedTypes = { "Tuition Fee", "Non-School Fee" };
    private readonly PortalDbContext _db;

    public TransactionsController(PortalDbContext db) => _db = db;

    private static object Shape(StudentTransaction t) => new
    {
        id = t.Id,
        date = t.Date.ToString("yyyy-MM-dd"),
        description = t.Description,
        type = t.Type,
        amount = t.Amount,
        notes = t.Notes
    };

    // GET api/transactions?studentId=2026-0001
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? studentId)
    {
        var id = await _db.FindStudentIdAsync(studentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        var rows = await _db.Transactions
            .Where(t => t.StudentId == id)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .ToListAsync();

        return Ok(new { transactions = rows.Select(Shape) });
    }

    // POST api/transactions
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] TransactionRequest req)
    {
        var id = await _db.FindStudentIdAsync(req.StudentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        var description = (req.Description ?? "").Trim();
        if (description.Length == 0)
            return BadRequest(new { message = "Description is required." });
        if (req.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });
        if (!AllowedTypes.Contains(req.Type))
            return BadRequest(new { message = "Invalid transaction type." });
        if (!DateOnly.TryParseExact(req.Date, "yyyy-MM-dd", out var date))
            return BadRequest(new { message = "Invalid date." });

        var tx = new StudentTransaction
        {
            StudentId = id.Value,
            Description = description,
            Amount = Math.Round(req.Amount, 2),
            Type = req.Type!,
            Date = date,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        return Ok(new { transaction = Shape(tx) });
    }

    // DELETE api/transactions/5?studentId=2026-0001
    [HttpDelete("{txId:int}")]
    public async Task<IActionResult> Delete(int txId, [FromQuery] string? studentId)
    {
        var id = await _db.FindStudentIdAsync(studentId);
        if (id == null) return NotFound(new { message = "Student not found." });

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == txId && t.StudentId == id);
        if (tx == null) return NotFound(new { message = "Transaction not found." });

        _db.Transactions.Remove(tx);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}