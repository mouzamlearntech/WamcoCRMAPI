using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Domain.Migrations
{
    /// <inheritdoc />
    public partial class addedshippingaddresstosalesorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippingPrice",
                table: "SalesOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingPrice",
                table: "SalesOrders");
        }
    }
}
