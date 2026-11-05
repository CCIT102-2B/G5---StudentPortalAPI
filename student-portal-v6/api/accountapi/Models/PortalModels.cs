namespace AccountAPI.Models;

// What the page sends when adding a transaction.
public class TransactionRequest
{
    public string? StudentId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? Type { get; set; }
    public string? Date { get; set; }   // "yyyy-MM-dd"
    public string? Notes { get; set; }
}