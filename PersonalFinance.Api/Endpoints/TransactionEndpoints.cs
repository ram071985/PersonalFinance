using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Transactions;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", async (ITransactionService service, int? page, int? pageSize) =>
        {
            if (page is null && pageSize is null)
                return Results.Ok(await service.GetAllAsync());

            return Results.Ok(await service.GetPagedAsync(page ?? 1, pageSize ?? 20));
        });

        group.MapGet("/recent", async (ITransactionService service, int count = 10) =>
            Results.Ok(await service.GetRecentAsync(count)));

        group.MapPost("/import/csv", async (
            HttpRequest http,
            ITransactionService service) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { message = "Expected multipart form data." });

            var form = await http.ReadFormAsync();
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { message = "CSV file is required (field name: file)." });

            if (!int.TryParse(form["accountId"], out var accountId) || accountId <= 0)
                return Results.BadRequest(new { message = "accountId is required." });

            int? expenseCat = int.TryParse(form["expenseCategoryId"], out var e) && e > 0 ? e : null;
            int? incomeCat = int.TryParse(form["incomeCategoryId"], out var i) && i > 0 ? i : null;

            await using var stream = file.OpenReadStream();
            var result = await service.ImportBankStatementAsync(
                accountId, expenseCat, incomeCat, stream, file.FileName);

            return Results.Ok(result);
        }).DisableAntiforgery();

        group.MapGet("/export/csv", async (ITransactionService service) =>
        {
            var items = (await service.GetAllAsync()).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,Date,Type,Amount,Description,Account,Category,Notes");
            foreach (var tx in items)
            {
                static string Esc(string? s)
                {
                    var value = (s ?? string.Empty).Replace("\"", "\"\"");
                    return $"\"{value}\"";
                }

                sb.AppendLine(string.Join(",",
                    tx.Id,
                    tx.Date.ToString("yyyy-MM-dd"),
                    tx.Type,
                    tx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Esc(tx.Description),
                    Esc(tx.AccountName),
                    Esc(tx.CategoryName),
                    Esc(tx.Notes)));
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return Results.File(bytes, "text/csv", $"transactions-{DateTime.UtcNow:yyyyMMdd}.csv");
        });

        group.MapGet("/account/{accountId:int}", async (int accountId, ITransactionService service) =>
            Results.Ok(await service.GetByAccountIdAsync(accountId)));

        group.MapGet("/{id:int}", async (int id, ITransactionService service) =>
        {
            var tx = await service.GetByIdAsync(id);
            return tx is null ? Results.NotFound() : Results.Ok(tx);
        });

        group.MapPost("/", async (CreateTransactionRequest transaction, ITransactionService service) =>
        {
            var created = await service.CreateAsync(transaction);
            return Results.Created($"/api/transactions/{created.Id}", created);
        }).Validate<CreateTransactionRequest>();

        group.MapPut("/{id:int}", async (int id, UpdateTransactionRequest input, ITransactionService service) =>
        {
            var result = await service.UpdateAsync(id, input);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { message = result.Error });
        }).Validate<UpdateTransactionRequest>();

        group.MapDelete("/{id:int}", async (int id, ITransactionService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
