using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostApiService.Migrations
{
    /// <inheritdoc />
    public partial class MakePreviewImageAltRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreviewImageAlt",
                table: "Posts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Default alt text",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreviewImageAlt",
                table: "Posts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "Default alt text");
        }
    }
}
