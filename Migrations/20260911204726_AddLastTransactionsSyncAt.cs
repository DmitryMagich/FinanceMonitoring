using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceCalculator.Migrations
{
    /// <inheritdoc />
    public partial class AddLastTransactionsSyncAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastTransactionsSyncAt",
                table: "Accounts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTransactionsSyncAt",
                table: "Accounts");
        }
    }
}
