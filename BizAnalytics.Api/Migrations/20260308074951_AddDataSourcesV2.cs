using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSourcesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataSource_Organizations_OrganizationId",
                table: "DataSource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataSource",
                table: "DataSource");

            migrationBuilder.RenameTable(
                name: "DataSource",
                newName: "DataSources");

            migrationBuilder.RenameIndex(
                name: "IX_DataSource_OrganizationId",
                table: "DataSources",
                newName: "IX_DataSources_OrganizationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataSources",
                table: "DataSources",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DataSources_Organizations_OrganizationId",
                table: "DataSources",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataSources_Organizations_OrganizationId",
                table: "DataSources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataSources",
                table: "DataSources");

            migrationBuilder.RenameTable(
                name: "DataSources",
                newName: "DataSource");

            migrationBuilder.RenameIndex(
                name: "IX_DataSources_OrganizationId",
                table: "DataSource",
                newName: "IX_DataSource_OrganizationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataSource",
                table: "DataSource",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DataSource_Organizations_OrganizationId",
                table: "DataSource",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
