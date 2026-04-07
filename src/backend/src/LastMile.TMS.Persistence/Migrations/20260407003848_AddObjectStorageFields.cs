using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastMile.TMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectStorageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "SourceFile",
                table: "ParcelImports",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "SourceFileKey",
                table: "ParcelImports",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoKey",
                table: "DeliveryConfirmations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImageKey",
                table: "DeliveryConfirmations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceFileKey",
                table: "ParcelImports");

            migrationBuilder.DropColumn(
                name: "PhotoKey",
                table: "DeliveryConfirmations");

            migrationBuilder.DropColumn(
                name: "SignatureImageKey",
                table: "DeliveryConfirmations");

            migrationBuilder.AlterColumn<byte[]>(
                name: "SourceFile",
                table: "ParcelImports",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }
    }
}
