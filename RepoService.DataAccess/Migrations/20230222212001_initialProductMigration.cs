using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepoService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class initialProductMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    PackageCode = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    ProductVersion = table.Column<string>(type: "text", nullable: false),
                    ProductCode = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeCode = table.Column<Guid>(type: "uuid", nullable: false),
                    IsX64 = table.Column<bool>(type: "boolean", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: false),
                    ARPCONTACT = table.Column<string>(type: "text", nullable: false),
                    ARPHELPLINK = table.Column<string>(type: "text", nullable: false),
                    ARPURLINFOABOUT = table.Column<string>(type: "text", nullable: false),
                    ARPURLUPDATEINFO = table.Column<string>(type: "text", nullable: false),
                    ARPHELPTELEPHONE = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.PackageCode);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
