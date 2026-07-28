using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MicrobeneficioSanGabriel.Data;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260728030000_PermitirEliminarProductosConHistorial")]
    public partial class PermitirEliminarProductosConHistorial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Productos_ProductoId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Producciones_Productos_ProductoId",
                table: "Producciones");

            migrationBuilder.AddColumn<string>(
                name: "ProductoNombre",
                table: "Producciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioUnitario",
                table: "Pedidos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductoNombre",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductoNombre",
                table: "MovimientosInventario",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE pe
                SET pe.ProductoNombre = pr.Nombre,
                    pe.PrecioUnitario = pr.Precio
                FROM Pedidos pe
                INNER JOIN Productos pr ON pr.Id = pe.ProductoId;

                UPDATE mi
                SET mi.ProductoNombre = pr.Nombre
                FROM MovimientosInventario mi
                INNER JOIN Productos pr ON pr.Id = mi.ProductoId;

                UPDATE pd
                SET pd.ProductoNombre = pr.Nombre
                FROM Producciones pd
                INNER JOIN Productos pr ON pr.Id = pd.ProductoId;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "Pedidos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "MovimientosInventario",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Productos_ProductoId",
                table: "Pedidos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Producciones_Productos_ProductoId",
                table: "Producciones",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Productos_ProductoId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Producciones_Productos_ProductoId",
                table: "Producciones");

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Pedidos WHERE ProductoId IS NULL)
                   OR EXISTS (SELECT 1 FROM MovimientosInventario WHERE ProductoId IS NULL)
                BEGIN
                    THROW 51000, 'No se puede revertir la migración porque existen registros históricos de productos eliminados.', 1;
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "MovimientosInventario",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ProductoNombre",
                table: "Producciones");

            migrationBuilder.DropColumn(
                name: "PrecioUnitario",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ProductoNombre",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ProductoNombre",
                table: "MovimientosInventario");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Productos_ProductoId",
                table: "Pedidos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Producciones_Productos_ProductoId",
                table: "Producciones",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id");
        }
    }
}
