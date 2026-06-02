using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposPagoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoPago",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pedidos");
        }
    }
}
