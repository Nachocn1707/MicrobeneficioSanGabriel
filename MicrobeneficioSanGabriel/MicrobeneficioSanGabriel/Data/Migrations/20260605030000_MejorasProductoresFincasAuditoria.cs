using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MicrobeneficioSanGabriel.Data;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260605030000_MejorasProductoresFincasAuditoria")]
    public partial class MejorasProductoresFincasAuditoria : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normaliza datos existentes antes de reducir el tamaño de las columnas.
            // Esto evita que la migración falle por guiones, espacios o valores demasiado largos.
            migrationBuilder.Sql(@"
                UPDATE Productores
                SET Cedula = LEFT(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(Cedula)), '-', ''), ' ', ''), '.', ''), 12),
                    Telefono = REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(Telefono)), '-', ''), ' ', ''), '.', '');
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Lotes_Productores_ProductorId",
                table: "Lotes");

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Productores",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Productores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Cedula",
                table: "Productores",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Canton",
                table: "Productores",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Por definir");

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Productores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionExacta",
                table: "Productores",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "Por definir");

            migrationBuilder.AddColumn<string>(
                name: "Distrito",
                table: "Productores",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Por definir");

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
                table: "Productores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Por definir");

            migrationBuilder.AddColumn<int>(
                name: "FincaId",
                table: "Lotes",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoLote",
                table: "Lotes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Observacion",
                table: "Lotes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RegistroId = table.Column<int>(type: "int", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fincas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductorId = table.Column<int>(type: "int", nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Canton = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Distrito = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DireccionExacta = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fincas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fincas_Productores_ProductorId",
                        column: x => x.ProductorId,
                        principalTable: "Productores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                UPDATE Productores
                SET DireccionExacta = CASE
                        WHEN Direccion IS NULL OR LTRIM(RTRIM(Direccion)) = '' THEN 'Por definir'
                        ELSE Direccion
                    END,
                    Provincia = 'Por definir',
                    Canton = 'Por definir',
                    Distrito = 'Por definir';

                INSERT INTO Fincas
                    (Nombre, ProductorId, Provincia, Canton, Distrito, DireccionExacta, Activa, FechaRegistro)
                SELECT
                    CASE
                        WHEN Finca IS NULL OR LTRIM(RTRIM(Finca)) = '' THEN 'Finca principal'
                        ELSE Finca
                    END,
                    Id,
                    Provincia,
                    Canton,
                    Distrito,
                    DireccionExacta,
                    1,
                    ISNULL(FechaRegistro, GETDATE())
                FROM Productores;

                UPDATE l
                SET FincaId = f.Id
                FROM Lotes l
                CROSS APPLY (
                    SELECT TOP 1 Id
                    FROM Fincas
                    WHERE ProductorId = l.ProductorId
                    ORDER BY Id
                ) f;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "FincaId",
                table: "Lotes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT Cedula
                    FROM Productores
                    GROUP BY Cedula
                    HAVING COUNT(*) > 1
                )
                    THROW 51001, 'Existen productores con cédulas duplicadas. Corrija los datos antes de aplicar la migración.', 1;

                IF EXISTS (
                    SELECT LOWER(Correo)
                    FROM Productores
                    WHERE Correo IS NOT NULL AND LTRIM(RTRIM(Correo)) <> ''
                    GROUP BY LOWER(Correo)
                    HAVING COUNT(*) > 1
                )
                    THROW 51002, 'Existen productores con correos duplicados. Corrija los datos antes de aplicar la migración.', 1;

                IF EXISTS (
                    SELECT CodigoLote
                    FROM Lotes
                    GROUP BY CodigoLote
                    HAVING COUNT(*) > 1
                )
                    THROW 51003, 'Existen códigos de lote duplicados. Corrija los datos antes de aplicar la migración.', 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Productores_Cedula",
                table: "Productores",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productores_Correo",
                table: "Productores",
                column: "Correo",
                unique: true,
                filter: "[Correo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_CodigoLote",
                table: "Lotes",
                column: "CodigoLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_FincaId",
                table: "Lotes",
                column: "FincaId");

            migrationBuilder.CreateIndex(
                name: "IX_Fincas_ProductorId",
                table: "Fincas",
                column: "ProductorId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Fecha",
                table: "Auditorias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Modulo",
                table: "Auditorias",
                column: "Modulo");

            migrationBuilder.AddForeignKey(
                name: "FK_Lotes_Fincas_FincaId",
                table: "Lotes",
                column: "FincaId",
                principalTable: "Fincas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lotes_Productores_ProductorId",
                table: "Lotes",
                column: "ProductorId",
                principalTable: "Productores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Lotes_Fincas_FincaId", table: "Lotes");
            migrationBuilder.DropForeignKey(name: "FK_Lotes_Productores_ProductorId", table: "Lotes");

            migrationBuilder.DropTable(name: "Auditorias");
            migrationBuilder.DropTable(name: "Fincas");

            migrationBuilder.DropIndex(name: "IX_Productores_Cedula", table: "Productores");
            migrationBuilder.DropIndex(name: "IX_Productores_Correo", table: "Productores");
            migrationBuilder.DropIndex(name: "IX_Lotes_CodigoLote", table: "Lotes");
            migrationBuilder.DropIndex(name: "IX_Lotes_FincaId", table: "Lotes");

            migrationBuilder.DropColumn(name: "Canton", table: "Productores");
            migrationBuilder.DropColumn(name: "Correo", table: "Productores");
            migrationBuilder.DropColumn(name: "DireccionExacta", table: "Productores");
            migrationBuilder.DropColumn(name: "Distrito", table: "Productores");
            migrationBuilder.DropColumn(name: "Provincia", table: "Productores");
            migrationBuilder.DropColumn(name: "FincaId", table: "Lotes");

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Productores",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Productores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Cedula",
                table: "Productores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoLote",
                table: "Lotes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Observacion",
                table: "Lotes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lotes_Productores_ProductorId",
                table: "Lotes",
                column: "ProductorId",
                principalTable: "Productores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
