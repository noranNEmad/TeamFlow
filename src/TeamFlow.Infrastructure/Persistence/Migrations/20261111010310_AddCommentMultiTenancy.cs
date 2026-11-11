using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_comments_OrganizationId",
                table: "comments",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_comments_OrganizationId",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "comments");
        }
    }
}
