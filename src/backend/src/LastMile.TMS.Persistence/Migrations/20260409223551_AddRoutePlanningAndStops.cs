using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace LastMile.TMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutePlanningAndStops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlannedDistanceMeters",
                table: "Routes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlannedDurationSeconds",
                table: "Routes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<LineString>(
                name: "PlannedPath",
                table: "Routes",
                type: "geometry(LineString,4326)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ZoneId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    RecipientLabel = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Street1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Street2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    State = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StopLocation = table.Column<Point>(type: "geometry(Point,4326)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteStopParcels",
                columns: table => new
                {
                    ParcelsId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStopParcels", x => new { x.ParcelsId, x.RouteStopId });
                    table.ForeignKey(
                        name: "FK_RouteStopParcels_Parcels_ParcelsId",
                        column: x => x.ParcelsId,
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteStopParcels_RouteStops_RouteStopId",
                        column: x => x.RouteStopId,
                        principalTable: "RouteStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ZoneId",
                table: "Routes",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStopParcels_RouteStopId",
                table: "RouteStopParcels",
                column: "RouteStopId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_Sequence",
                table: "RouteStops",
                columns: new[] { "RouteId", "Sequence" },
                unique: true);

            // Default ZoneId was Guid.Empty; existing routes must point at a real zone before FK is added.
            migrationBuilder.Sql(
                """
                UPDATE "Routes" r
                SET "ZoneId" = d."ZoneId"
                FROM "Drivers" d
                WHERE r."DriverId" = d."Id";

                UPDATE "Routes"
                SET "ZoneId" = (SELECT z."Id" FROM "Zones" AS z ORDER BY z."Id" LIMIT 1)
                WHERE "ZoneId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Zones_ZoneId",
                table: "Routes",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Zones_ZoneId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "RouteStopParcels");

            migrationBuilder.DropTable(
                name: "RouteStops");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ZoneId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "PlannedDistanceMeters",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "PlannedDurationSeconds",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "PlannedPath",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "Routes");
        }
    }
}
