using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AnalysisWorkspaceId",
                table: "SalesRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnalysisWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisWorkspaces_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRecords_AnalysisWorkspaceId",
                table: "SalesRecords",
                column: "AnalysisWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisWorkspaces_OrganizationId",
                table: "AnalysisWorkspaces",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesRecords_AnalysisWorkspaces_AnalysisWorkspaceId",
                table: "SalesRecords",
                column: "AnalysisWorkspaceId",
                principalTable: "AnalysisWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesRecords_AnalysisWorkspaces_AnalysisWorkspaceId",
                table: "SalesRecords");

            migrationBuilder.DropTable(
                name: "AnalysisWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_SalesRecords_AnalysisWorkspaceId",
                table: "SalesRecords");

            migrationBuilder.DropColumn(
                name: "AnalysisWorkspaceId",
                table: "SalesRecords");
        }
    }
}
