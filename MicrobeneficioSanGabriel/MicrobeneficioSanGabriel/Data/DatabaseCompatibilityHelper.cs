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
");
        }
    }
}

