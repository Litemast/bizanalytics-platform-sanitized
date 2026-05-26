using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRecordSourceFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                table: "SalesRecords",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceFileName",
                table: "SalesRecords");
        }
    }
}
