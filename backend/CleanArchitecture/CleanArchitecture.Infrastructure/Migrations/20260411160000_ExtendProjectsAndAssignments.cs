using System;
using CleanArchitecture.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260411160000_ExtendProjectsAndAssignments")]
    public partial class ExtendProjectsAndAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Projects",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "ProjectAssignments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.Sql("UPDATE ProjectAssignments SET AssignedAtUtc = Created");

            migrationBuilder.AddColumn<string>(
                name: "AssignedByUserId",
                table: "ProjectAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_AssignedByUserId",
                table: "ProjectAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_ProjectId_UserId_ActiveUnique",
                table: "ProjectAssignments",
                columns: new[] { "ProjectId", "UserId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_AspNetUsers_AssignedByUserId",
                table: "ProjectAssignments",
                column: "AssignedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_AspNetUsers_AssignedByUserId",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignments_ProjectId_UserId_ActiveUnique",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignments_AssignedByUserId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Code",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Projects");
        }
    }
}
