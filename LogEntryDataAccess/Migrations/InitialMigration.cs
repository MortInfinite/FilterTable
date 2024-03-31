using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogEntryDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "LogEntries",
                columns: new[] { "Id", "Category", "EventId", "Exception", "LogLevel", "Message", "Payload", "PayloadType", "TimeStamp" },
                values: new object[,]
                {
                    { 1, "Disk space", 1, null, "Warning", "Drive is low on disk space", "17TB remaining", "Text", new DateTime(2024, 3, 31, 11, 35, 1, 671, DateTimeKind.Local).AddTicks(5744) },
                    { 2, "Disk space", 1, null, "Warning", "Drive is still low on disk space", "16TB remaining", "Text", new DateTime(2024, 3, 31, 12, 35, 1, 671, DateTimeKind.Local).AddTicks(5829) },
                    { 3, "Disk space", 1, null, "Warning", "Drive is very low on disk space", "6GB remaining", "Text", new DateTime(2024, 3, 31, 13, 35, 1, 671, DateTimeKind.Local).AddTicks(5847) },
                    { 4, "Disk space", 1, null, "Error", "Drive is out of disk space", "127B remaining", "Text", new DateTime(2024, 3, 31, 14, 35, 1, 671, DateTimeKind.Local).AddTicks(5862) },
                    { 5, "Memory", 2, null, "Information", "High memory usage", "180GB used", "Text", new DateTime(2024, 3, 31, 10, 35, 1, 671, DateTimeKind.Local).AddTicks(5925) },
                    { 6, "Memory", 2, null, "Information", "High memory usage", "189GB used", "Text", new DateTime(2024, 3, 31, 11, 35, 1, 671, DateTimeKind.Local).AddTicks(5945) },
                    { 7, "Memory", 2, null, "Information", "High memory usage", "204GB used", "Text", new DateTime(2024, 3, 31, 12, 35, 1, 671, DateTimeKind.Local).AddTicks(5961) },
                    { 8, "Memory", 2, null, "Warning", "Very high memory usage", "780GB used", "Text", new DateTime(2024, 3, 31, 13, 35, 1, 671, DateTimeKind.Local).AddTicks(5976) },
                    { 9, "Temperature", 3, null, "Information", "High CPU temperature", "50C", "Text", new DateTime(2024, 3, 31, 10, 35, 1, 671, DateTimeKind.Local).AddTicks(5991) },
                    { 10, "Temperature", 3, null, "Information", "High CPU temperature", "65C", "Text", new DateTime(2024, 3, 31, 11, 35, 1, 671, DateTimeKind.Local).AddTicks(6008) },
                    { 11, "Temperature", 3, null, "Information", "High CPU temperature", "69C (nice)", "Text", new DateTime(2024, 3, 31, 12, 35, 1, 671, DateTimeKind.Local).AddTicks(6024) },
                    { 12, "Temperature", 3, null, "Warning", "Very CPU temperature", "99C", "Text", new DateTime(2024, 3, 31, 13, 35, 1, 671, DateTimeKind.Local).AddTicks(6038) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEntries");
        }
    }
}
