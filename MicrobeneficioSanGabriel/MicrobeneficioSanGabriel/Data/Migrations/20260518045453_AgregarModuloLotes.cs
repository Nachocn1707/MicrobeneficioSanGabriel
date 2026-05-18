using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModuloLotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoLote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductorId = table.Column<int>(type: "int", nullable: false),
                    PesoKg = table.Column<double>(type: "float", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lotes_Productores_ProductorId",
                        column: x => x.ProductorId,
                        principalTable: "Productores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_ProductorId",
                table: "Lotes",
                column: "ProductorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lotes");
        }
    }
}
