using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GaneshEnterprises.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BagSize = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BagSize", "Description", "IsAvailable", "Name", "Price", "StockQuantity" },
                values: new object[,]
                {
                    { 1, 1, "Durable lightweight Low-Density Polyethylene carry bag with punch handle. Suitable for pharmacy, cosmetics, and small retail items.", true, "Small LDPE Carry Bag (10x12)", 1.25m, 15000 },
                    { 2, 1, "High-Density Polyethylene pouch bag designed for provisions, dry goods, and hardware items.", true, "Small HDPE Grocery Pouch (8x10)", 0.90m, 25000 },
                    { 3, 2, "Standard commercial D-cut shopping bag with high tensile strength for apparel stores, supermarkets, and general retail.", true, "Medium D-Cut Shopping Bag (14x16)", 2.40m, 18000 },
                    { 4, 2, "Comfortable soft loop handle plastic bag offering excellent load bearing for departmental stores.", true, "Medium Loop-Handle Carry Bag (12x15)", 2.10m, 12500 },
                    { 5, 3, "Heavy-gauge reinforced plastic carrier bag engineered for bulk clothing, textiles, and wholesale merchandise.", true, "Large Heavy-Duty Carrier Bag (18x24)", 4.50m, 8000 },
                    { 6, 3, "Extra-large heavy-duty plastic packaging bag used for industrial liners, parcel dispatch, and bulk supply storage.", true, "Large Industrial Packaging Liner (24x30)", 6.20m, 5000 }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "ContactNumber", "CustomerName", "OrderDate", "ProductId", "Quantity", "Status" },
                values: new object[,]
                {
                    { 1, "9876543210", "Metro Retail Mart", new DateTime(2026, 10, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 2000, "Completed" },
                    { 2, "9823456789", "City Chemist Wholesale", new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 5000, "Processing" },
                    { 3, "9811223344", "Apex Garments & Fabrics", new DateTime(2026, 10, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, 1500, "Pending" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductId",
                table: "Orders",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
