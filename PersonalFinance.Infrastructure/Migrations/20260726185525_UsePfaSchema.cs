using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsePfaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pfa");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transactions",
                newSchema: "pfa");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Categories",
                newSchema: "pfa");

            migrationBuilder.RenameTable(
                name: "Budgets",
                newName: "Budgets",
                newSchema: "pfa");

            migrationBuilder.RenameTable(
                name: "Accounts",
                newName: "Accounts",
                newSchema: "pfa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "pfa",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "Categories",
                schema: "pfa",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "Budgets",
                schema: "pfa",
                newName: "Budgets");

            migrationBuilder.RenameTable(
                name: "Accounts",
                schema: "pfa",
                newName: "Accounts");
        }
    }
}
