using BizAnalytics.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizAnalytics.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260410103000_RemoveIntelligentReportModuleArtifacts")]
public partial class RemoveIntelligentReportModuleArtifacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "ReportAnalyses";
            DROP TABLE IF EXISTS "ReportFields";
            DROP TABLE IF EXISTS "ReportTableRows";
            DROP TABLE IF EXISTS "ReportTables";
            DROP TABLE IF EXISTS "ReportDocuments";

            DELETE FROM "__EFMigrationsHistory"
            WHERE "MigrationId" IN (
                '20260408121418_AddIntelligentReportModule',
                '20260408153000_AddReportDocumentDisplayName'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReportDocuments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                FileType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ParsedText = table.Column<string>(type: "text", nullable: true),
                ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ClassificationConfidence = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                OriginalSourceName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportDocuments_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReportAnalyses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                MetricsJson = table.Column<string>(type: "text", nullable: false),
                InsightsJson = table.Column<string>(type: "text", nullable: false),
                NarrativeText = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportAnalyses", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportAnalyses_ReportDocuments_ReportDocumentId",
                    column: x => x.ReportDocumentId,
                    principalTable: "ReportDocuments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReportFields",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalFieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedFieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Value = table.Column<string>(type: "text", nullable: true),
                ValueType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportFields", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportFields_ReportDocuments_ReportDocumentId",
                    column: x => x.ReportDocumentId,
                    principalTable: "ReportDocuments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReportTables",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ColumnsJson = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportTables", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportTables_ReportDocuments_ReportDocumentId",
                    column: x => x.ReportDocumentId,
                    principalTable: "ReportDocuments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReportTableRows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportTableId = table.Column<Guid>(type: "uuid", nullable: false),
                RowJson = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportTableRows", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportTableRows_ReportTables_ReportTableId",
                    column: x => x.ReportTableId,
                    principalTable: "ReportTables",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReportAnalyses_ReportDocumentId_CreatedAt",
            table: "ReportAnalyses",
            columns: new[] { "ReportDocumentId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ReportDocuments_OrganizationId_UploadedAt",
            table: "ReportDocuments",
            columns: new[] { "OrganizationId", "UploadedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ReportFields_ReportDocumentId",
            table: "ReportFields",
            column: "ReportDocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_ReportTableRows_ReportTableId",
            table: "ReportTableRows",
            column: "ReportTableId");

        migrationBuilder.CreateIndex(
            name: "IX_ReportTables_ReportDocumentId",
            table: "ReportTables",
            column: "ReportDocumentId");

        migrationBuilder.Sql("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260408121418_AddIntelligentReportModule', '9.0.2'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260408121418_AddIntelligentReportModule'
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260408153000_AddReportDocumentDisplayName', '9.0.2'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260408153000_AddReportDocumentDisplayName'
            );
            """);
    }
}
