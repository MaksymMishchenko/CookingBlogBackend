using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PostApiService.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "PostId", "Content", "CreateAt", "Title" },
                values: new object[,]
                {
                    { 1, "This is the content of the first post.", new DateTime(2024, 10, 6, 20, 56, 0, 545, DateTimeKind.Local).AddTicks(9048), "First Post" },
                    { 2, "This is the content of the second post.", new DateTime(2024, 10, 6, 20, 56, 0, 545, DateTimeKind.Local).AddTicks(9142), "Second Post" }
                });

            migrationBuilder.InsertData(
                table: "Comments",
                columns: new[] { "CommentId", "Author", "Content", "CreatedAt", "PostId" },
                values: new object[,]
                {
                    { 1, "John Doe", "Great post!", new DateTime(2024, 10, 6, 20, 56, 0, 545, DateTimeKind.Local).AddTicks(9958), 1 },
                    { 2, "Jane Doe", "I totally agree with this!", new DateTime(2024, 10, 6, 20, 56, 0, 545, DateTimeKind.Local).AddTicks(9979), 1 },
                    { 3, "Alice", "This is a comment on the second post.", new DateTime(2024, 10, 6, 20, 56, 0, 545, DateTimeKind.Local).AddTicks(9986), 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 2);
        }
    }
}
