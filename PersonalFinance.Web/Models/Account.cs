namespace PersonalFinance.Web.Models;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Checking";   // keep simple string for UI phase
    public decimal Balance { get; set; }
    public string? Institution { get; set; }
    public string? Notes { get; set; }
}