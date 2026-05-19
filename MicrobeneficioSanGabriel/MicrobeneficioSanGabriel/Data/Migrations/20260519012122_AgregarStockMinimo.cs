using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarStockMinimo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockMinimo",
                table: "Productos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Productos");
        }
    }
}
