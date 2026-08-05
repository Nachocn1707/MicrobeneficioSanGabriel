using Microsoft.EntityFrameworkCore;

namespace MicrobeneficioSanGabriel.Data
{
    public static class DatabaseCompatibilityHelper
    {
        public static async Task EnsureLatestSchemaAsync(ApplicationDbContext context)
        {
            if (!context.Database.IsSqlServer())
            {
                return;
            }

            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Productos]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Productos', N'ImagenUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [ImagenUrl] nvarchar(250) NULL;
END

IF OBJECT_ID(N'[dbo].[Lotes]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Lotes] SET [CodigoLote] = N'' WHERE [CodigoLote] IS NULL;
    UPDATE [dbo].[Lotes] SET [Estado] = N'Pendiente' WHERE [Estado] IS NULL;
END

IF OBJECT_ID(N'[dbo].[Productores]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Productores] SET [Nombre] = N'Sin Nombre' WHERE [Nombre] IS NULL;
    UPDATE [dbo].[Productores] SET [Cedula] = N'' WHERE [Cedula] IS NULL;
    UPDATE [dbo].[Productores] SET [Telefono] = N'' WHERE [Telefono] IS NULL;
    UPDATE [dbo].[Productores] SET [Provincia] = N'' WHERE [Provincia] IS NULL;
    UPDATE [dbo].[Productores] SET [Canton] = N'' WHERE [Canton] IS NULL;
    UPDATE [dbo].[Productores] SET [Distrito] = N'' WHERE [Distrito] IS NULL;
    UPDATE [dbo].[Productores] SET [DireccionExacta] = N'' WHERE [DireccionExacta] IS NULL;
    UPDATE [dbo].[Productores] SET [Direccion] = N'' WHERE [Direccion] IS NULL;
END

IF OBJECT_ID(N'[dbo].[Fincas]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Fincas] SET [Nombre] = N'Sin Nombre' WHERE [Nombre] IS NULL;
    UPDATE [dbo].[Fincas] SET [Provincia] = N'' WHERE [Provincia] IS NULL;
    UPDATE [dbo].[Fincas] SET [Canton] = N'' WHERE [Canton] IS NULL;
    UPDATE [dbo].[Fincas] SET [Distrito] = N'' WHERE [Distrito] IS NULL;
    UPDATE [dbo].[Fincas] SET [DireccionExacta] = N'' WHERE [DireccionExacta] IS NULL;
END

IF OBJECT_ID(N'[dbo].[Producciones]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Producciones] SET [TipoProceso] = N'' WHERE [TipoProceso] IS NULL;
    UPDATE [dbo].[Producciones] SET [Estado] = N'Completado' WHERE [Estado] IS NULL;
END

IF OBJECT_ID(N'[dbo].[Lotes]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[Fincas]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Lotes] SET [FincaId] = NULL WHERE [FincaId] IS NOT NULL AND [FincaId] NOT IN (SELECT [Id] FROM [dbo].[Fincas]);
END

IF OBJECT_ID(N'[dbo].[Lotes]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[Productores]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Lotes] SET [ProductorId] = NULL WHERE [ProductorId] IS NOT NULL AND [ProductorId] NOT IN (SELECT [Id] FROM [dbo].[Productores]);
END

IF OBJECT_ID(N'[dbo].[Fincas]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[Productores]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Fincas] SET [ProductorId] = NULL WHERE [ProductorId] IS NOT NULL AND [ProductorId] NOT IN (SELECT [Id] FROM [dbo].[Productores]);
END
");
        }
    }
}

