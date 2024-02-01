using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AuntieDot.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuntieDot_States",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopifyShopDomain = table.Column<string>(nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(nullable: false),
                    Token = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuntieDot_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuntieDot_Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopifyAccessToken = table.Column<string>(nullable: true),
                    ShopifyShopDomain = table.Column<string>(nullable: true),
                    ShopifyShopId = table.Column<long>(nullable: false),
                    ShopifyChargeId = table.Column<long>(nullable: true),
                    BillingOn = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuntieDot_Users", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuntieDot_States");

            migrationBuilder.DropTable(
                name: "AuntieDot_Users");
        }
    }
}
