using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceEducationRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EducationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisWorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Grade = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AverageScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SourceFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationRecords_AnalysisWorkspaces_AnalysisWorkspaceId",
                        column: x => x.AnalysisWorkspaceId,
                        principalTable: "AnalysisWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EducationRecords_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisWorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Period = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Expenses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialRecords_AnalysisWorkspaces_AnalysisWorkspaceId",
                        column: x => x.AnalysisWorkspaceId,
                        principalTable: "AnalysisWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialRecords_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationRecords_AnalysisWorkspaceId",
                table: "EducationRecords",
                column: "AnalysisWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationRecords_OrganizationId",
                table: "EducationRecords",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecords_AnalysisWorkspaceId",
                table: "FinancialRecords",
                column: "AnalysisWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecords_OrganizationId",
                table: "FinancialRecords",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationRecords");

            migrationBuilder.DropTable(
                name: "FinancialRecords");
        }
    }
}
