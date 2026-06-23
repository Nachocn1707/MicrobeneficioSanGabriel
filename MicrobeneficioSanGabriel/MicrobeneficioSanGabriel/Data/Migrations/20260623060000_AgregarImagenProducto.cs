using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    public partial class AgregarImagenProducto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Productos', N'ImagenUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [ImagenUrl] nvarchar(250) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Productos', N'ImagenUrl') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Productos] DROP COLUMN [ImagenUrl];
END
");
        }
    }
}
