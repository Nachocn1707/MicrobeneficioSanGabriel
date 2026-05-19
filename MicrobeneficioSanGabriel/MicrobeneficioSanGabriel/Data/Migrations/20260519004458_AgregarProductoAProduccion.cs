using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProductoAProduccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "Producciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_ProductoId",
                table: "Producciones",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Producciones_Productos_ProductoId",
                table: "Producciones",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Producciones_Productos_ProductoId",
                table: "Producciones");

            migrationBuilder.DropIndex(
                name: "IX_Producciones_ProductoId",
                table: "Producciones");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "Producciones");
        }
    }
}
