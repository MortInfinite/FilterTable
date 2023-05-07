using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilterTableExample.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MyDataTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyDataTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MyDataTypes",
                columns: new[] { "Id", "Description", "ExpirationDate", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Yellow item used to measure for scale", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Banana", 4.00m },
                    { 2, "The forbidden fruit. Keep away from Eve", new DateTime(1, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Apple", 2.00m },
                    { 3, "Adjust things that are out of alignment", null, "Wrench", 14.00m },
                    { 4, "More than one piece", new DateTime(2100, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Paper", 7.00m },
                    { 5, "PacMan likes this, a lot!", new DateTime(2023, 2, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Cheese", 19.25m }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyDataTypes");
        }
    }
}
