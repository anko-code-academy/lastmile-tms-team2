using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastMile.TMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteAssignmentAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouteAssignmentAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    PreviousDriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousDriverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NewDriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewDriverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreviousVehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousVehiclePlate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewVehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewVehiclePlate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteAssignmentAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteAssignmentAuditEntries_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteAssignmentAuditEntries_RouteId_ChangedAt",
                table: "RouteAssignmentAuditEntries",
                columns: new[] { "RouteId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteAssignmentAuditEntries");
        }
    }
}
