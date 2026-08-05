namespace PersonalFinance.Core.Dtos.Transactions;

public class BankStatementImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BankStatementImportPreviewRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string InferredType { get; set; } = "";
}