using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PostApiService.Migrations
{
    /// <inheritdoc />
    public partial class ChangePostImageIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PostImages",
                table: "PostImages");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PostImages");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PostImages",
                type: "integer",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostImages",
                table: "PostImages",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PostImages",
                table: "PostImages");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PostImages");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "PostImages",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostImages",
                table: "PostImages",
                column: "Id");
        }
    }
}