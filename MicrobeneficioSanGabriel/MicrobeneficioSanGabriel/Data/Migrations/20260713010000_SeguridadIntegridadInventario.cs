using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MicrobeneficioSanGabriel.Data;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    /// <summary>
    /// Refuerza la propiedad de pedidos, la trazabilidad de inventario,
    /// la concurrencia y el manejo decimal de kilogramos.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260713010000_SeguridadIntegridadInventario")]
    public partial class SeguridadIntegridadInventario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las columnas nuevas se crean en comandos separados. SQL Server compila
            // cada lote antes de ejecutarlo; si se agrega y se consulta una columna
            // dentro del mismo lote, puede producir "Invalid column name".
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Pedidos]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Pedidos', N'ClienteId') IS NULL
        ALTER TABLE [dbo].[Pedidos] ADD [ClienteId] nvarchar(450) NULL;

    IF COL_LENGTH(N'dbo.Pedidos', N'InventarioAplicado') IS NULL
        ALTER TABLE [dbo].[Pedidos] ADD [InventarioAplicado] bit NOT NULL CONSTRAINT [DF_Pedidos_InventarioAplicado] DEFAULT(0);

    IF COL_LENGTH(N'dbo.Pedidos', N'RowVersion') IS NULL
        ALTER TABLE [dbo].[Pedidos] ADD [RowVersion] rowversion NOT NULL;

    ALTER TABLE [dbo].[Pedidos] ALTER COLUMN [Cantidad] decimal(18,2) NOT NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Pedidos]', N'U') IS NOT NULL
BEGIN
    UPDATE p
       SET p.[ClienteId] = u.[Id]
      FROM [dbo].[Pedidos] p
      INNER JOIN [dbo].[AspNetUsers] u
              ON LOWER(LTRIM(RTRIM(p.[ClienteCorreo]))) = LOWER(LTRIM(RTRIM(u.[Email])))
     WHERE p.[ClienteId] IS NULL
       AND p.[ClienteCorreo] IS NOT NULL;

    -- Las versiones anteriores ya descontaban inventario al completar el pedido.
    UPDATE [dbo].[Pedidos]
       SET [InventarioAplicado] = 1
     WHERE [Estado] = N'Completado';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ClienteId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
        CREATE INDEX [IX_Pedidos_ClienteId] ON [dbo].[Pedidos]([ClienteId]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Pedidos_AspNetUsers_ClienteId')
        ALTER TABLE [dbo].[Pedidos] WITH CHECK
        ADD CONSTRAINT [FK_Pedidos_AspNetUsers_ClienteId]
            FOREIGN KEY([ClienteId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Productos]', N'U') IS NOT NULL
BEGIN
    -- SQL Server no permite cambiar el tipo de una columna mientras tenga
    -- una restricción DEFAULT asociada. Los nombres de esas restricciones
    -- son generados por SQL Server y cambian entre bases de datos, por eso
    -- se localizan y restauran dinámicamente.
    DECLARE @StockDefaultName sysname = NULL;
    DECLARE @StockDefaultDefinition nvarchar(max) = NULL;
    DECLARE @StockMinimoDefaultName sysname = NULL;
    DECLARE @StockMinimoDefaultDefinition nvarchar(max) = NULL;
    DECLARE @Sql nvarchar(max);

    SELECT
        @StockDefaultName = dc.[name],
        @StockDefaultDefinition = dc.[definition]
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.[object_id] = dc.[parent_object_id]
       AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Productos]')
      AND c.[name] = N'Stock';

    SELECT
        @StockMinimoDefaultName = dc.[name],
        @StockMinimoDefaultDefinition = dc.[definition]
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.[object_id] = dc.[parent_object_id]
       AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Productos]')
      AND c.[name] = N'StockMinimo';

    IF @StockDefaultName IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE [dbo].[Productos] DROP CONSTRAINT ' + QUOTENAME(@StockDefaultName) + N';';
        EXEC sys.sp_executesql @Sql;
    END

    IF @StockMinimoDefaultName IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE [dbo].[Productos] DROP CONSTRAINT ' + QUOTENAME(@StockMinimoDefaultName) + N';';
        EXEC sys.sp_executesql @Sql;
    END

    ALTER TABLE [dbo].[Productos] ALTER COLUMN [Stock] decimal(18,2) NOT NULL;
    ALTER TABLE [dbo].[Productos] ALTER COLUMN [StockMinimo] decimal(18,2) NOT NULL;

    IF @StockDefaultName IS NOT NULL AND @StockDefaultDefinition IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE [dbo].[Productos] ADD CONSTRAINT ' + QUOTENAME(@StockDefaultName)
                 + N' DEFAULT ' + @StockDefaultDefinition + N' FOR [Stock];';
        EXEC sys.sp_executesql @Sql;
    END

    IF @StockMinimoDefaultName IS NOT NULL AND @StockMinimoDefaultDefinition IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE [dbo].[Productos] ADD CONSTRAINT ' + QUOTENAME(@StockMinimoDefaultName)
                 + N' DEFAULT ' + @StockMinimoDefaultDefinition + N' FOR [StockMinimo];';
        EXEC sys.sp_executesql @Sql;
    END

    IF COL_LENGTH(N'dbo.Productos', N'RowVersion') IS NULL
        ALTER TABLE [dbo].[Productos] ADD [RowVersion] rowversion NOT NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[MovimientosInventario]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[MovimientosInventario] ALTER COLUMN [Cantidad] decimal(18,2) NOT NULL;

    IF COL_LENGTH(N'dbo.MovimientosInventario', N'OrigenTipo') IS NULL
        ALTER TABLE [dbo].[MovimientosInventario] ADD [OrigenTipo] nvarchar(40) NULL;

    IF COL_LENGTH(N'dbo.MovimientosInventario', N'OrigenId') IS NULL
        ALTER TABLE [dbo].[MovimientosInventario] ADD [OrigenId] int NULL;

    IF COL_LENGTH(N'dbo.MovimientosInventario', N'EsAutomatico') IS NULL
        ALTER TABLE [dbo].[MovimientosInventario] ADD [EsAutomatico] bit NOT NULL CONSTRAINT [DF_MovimientosInventario_EsAutomatico] DEFAULT(0);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[MovimientosInventario]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[MovimientosInventario]
       SET [EsAutomatico] = 1,
           [OrigenTipo] = N'Produccion',
           [OrigenId] = TRY_CONVERT(int, SUBSTRING([Observacion], CHARINDEX(N'#', [Observacion]) + 1, 20))
     WHERE [Observacion] LIKE N'Entrada automática por producción #%';

    UPDATE [dbo].[MovimientosInventario]
       SET [EsAutomatico] = 1,
           [OrigenTipo] = N'Pedido',
           [OrigenId] = TRY_CONVERT(int, SUBSTRING([Observacion], CHARINDEX(N'#', [Observacion]) + 1, 20))
     WHERE [Observacion] LIKE N'Salida automática por pedido #%';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MovimientosInventario_OrigenTipo_OrigenId' AND object_id = OBJECT_ID(N'[dbo].[MovimientosInventario]'))
        CREATE INDEX [IX_MovimientosInventario_OrigenTipo_OrigenId]
            ON [dbo].[MovimientosInventario]([OrigenTipo], [OrigenId]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Lotes]', N'U') IS NOT NULL
    ALTER TABLE [dbo].[Lotes] ALTER COLUMN [PesoKg] decimal(18,2) NOT NULL;

IF OBJECT_ID(N'[dbo].[Producciones]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Producciones] ALTER COLUMN [CantidadProcesadaKg] decimal(18,2) NOT NULL;
    ALTER TABLE [dbo].[Producciones] ALTER COLUMN [CantidadResultanteKg] decimal(18,2) NOT NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Fincas]', N'U') IS NOT NULL
BEGIN
    ;WITH repetidas AS
    (
        SELECT [Id], ROW_NUMBER() OVER(PARTITION BY [ProductorId], LOWER(LTRIM(RTRIM([Nombre]))) ORDER BY [Id]) AS rn
        FROM [dbo].[Fincas]
    )
    UPDATE f
       SET f.[Nombre] = LEFT(f.[Nombre], 88) + N' #' + CONVERT(nvarchar(10), f.[Id])
      FROM [dbo].[Fincas] f
      INNER JOIN repetidas r ON r.[Id] = f.[Id]
     WHERE r.rn > 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Fincas_ProductorId_Nombre' AND object_id = OBJECT_ID(N'[dbo].[Fincas]'))
        CREATE UNIQUE INDEX [IX_Fincas_ProductorId_Nombre]
            ON [dbo].[Fincas]([ProductorId], [Nombre]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[NotificacionesUsuarios]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificacionesUsuarios]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_NotificacionesUsuarios] PRIMARY KEY,
        [UsuarioId] nvarchar(450) NOT NULL,
        [Clave] nvarchar(200) NOT NULL,
        [FechaDescartada] datetime2 NOT NULL,
        CONSTRAINT [FK_NotificacionesUsuarios_AspNetUsers_UsuarioId]
            FOREIGN KEY([UsuarioId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_NotificacionesUsuarios_UsuarioId_Clave]
        ON [dbo].[NotificacionesUsuarios]([UsuarioId], [Clave]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Facturas]', N'U') IS NOT NULL
BEGIN
    ;WITH duplicadas AS
    (
        SELECT [Id], ROW_NUMBER() OVER(PARTITION BY [PedidoId] ORDER BY [Id]) AS rn
        FROM [dbo].[Facturas]
        WHERE [EstadoPago] <> N'Anulada'
    )
    UPDATE f
       SET f.[EstadoPago] = N'Anulada',
           f.[Observacion] = CONCAT(COALESCE(f.[Observacion] + N' ', N''), N'Anulada automáticamente por duplicidad histórica del pedido.')
      FROM [dbo].[Facturas] f
      INNER JOIN duplicadas d ON d.[Id] = f.[Id]
     WHERE d.rn > 1;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Facturas_PedidoId' AND object_id = OBJECT_ID(N'[dbo].[Facturas]'))
        DROP INDEX [IX_Facturas_PedidoId] ON [dbo].[Facturas];

    CREATE UNIQUE INDEX [IX_Facturas_PedidoId]
        ON [dbo].[Facturas]([PedidoId])
        WHERE [EstadoPago] <> N'Anulada';
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[NotificacionesUsuarios]', N'U') IS NOT NULL
    DROP TABLE [dbo].[NotificacionesUsuarios];

IF OBJECT_ID(N'[dbo].[Facturas]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Facturas_PedidoId' AND object_id = OBJECT_ID(N'[dbo].[Facturas]'))
        DROP INDEX [IX_Facturas_PedidoId] ON [dbo].[Facturas];
    CREATE INDEX [IX_Facturas_PedidoId] ON [dbo].[Facturas]([PedidoId]);
END

IF OBJECT_ID(N'[dbo].[Fincas]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Fincas_ProductorId_Nombre' AND object_id = OBJECT_ID(N'[dbo].[Fincas]'))
    DROP INDEX [IX_Fincas_ProductorId_Nombre] ON [dbo].[Fincas];

IF OBJECT_ID(N'[dbo].[MovimientosInventario]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MovimientosInventario_OrigenTipo_OrigenId' AND object_id = OBJECT_ID(N'[dbo].[MovimientosInventario]'))
        DROP INDEX [IX_MovimientosInventario_OrigenTipo_OrigenId] ON [dbo].[MovimientosInventario];

    UPDATE [dbo].[MovimientosInventario] SET [Cantidad] = ROUND([Cantidad], 0);
    ALTER TABLE [dbo].[MovimientosInventario] ALTER COLUMN [Cantidad] int NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_MovimientosInventario_EsAutomatico')
        ALTER TABLE [dbo].[MovimientosInventario] DROP CONSTRAINT [DF_MovimientosInventario_EsAutomatico];
    IF COL_LENGTH(N'dbo.MovimientosInventario', N'EsAutomatico') IS NOT NULL
        ALTER TABLE [dbo].[MovimientosInventario] DROP COLUMN [EsAutomatico];
    IF COL_LENGTH(N'dbo.MovimientosInventario', N'OrigenId') IS NOT NULL
        ALTER TABLE [dbo].[MovimientosInventario] DROP COLUMN [OrigenId];
    IF COL_LENGTH(N'dbo.MovimientosInventario', N'OrigenTipo') IS NOT NULL
        ALTER TABLE [dbo].[MovimientosInventario] DROP COLUMN [OrigenTipo];
END

IF OBJECT_ID(N'[dbo].[Producciones]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Producciones] ALTER COLUMN [CantidadProcesadaKg] float NOT NULL;
    ALTER TABLE [dbo].[Producciones] ALTER COLUMN [CantidadResultanteKg] float NOT NULL;
END

IF OBJECT_ID(N'[dbo].[Lotes]', N'U') IS NOT NULL
    ALTER TABLE [dbo].[Lotes] ALTER COLUMN [PesoKg] float NOT NULL;

IF OBJECT_ID(N'[dbo].[Productos]', N'U') IS NOT NULL
BEGIN
    DECLARE @DownStockDefaultName sysname = NULL;
    DECLARE @DownStockDefaultDefinition nvarchar(max) = NULL;
    DECLARE @DownStockMinimoDefaultName sysname = NULL;
    DECLARE @DownStockMinimoDefaultDefinition nvarchar(max) = NULL;
    DECLARE @DownSql nvarchar(max);

    SELECT
        @DownStockDefaultName = dc.[name],
        @DownStockDefaultDefinition = dc.[definition]
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.[object_id] = dc.[parent_object_id]
       AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Productos]')
      AND c.[name] = N'Stock';

    SELECT
        @DownStockMinimoDefaultName = dc.[name],
        @DownStockMinimoDefaultDefinition = dc.[definition]
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.[object_id] = dc.[parent_object_id]
       AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Productos]')
      AND c.[name] = N'StockMinimo';

    IF @DownStockDefaultName IS NOT NULL
    BEGIN
        SET @DownSql = N'ALTER TABLE [dbo].[Productos] DROP CONSTRAINT ' + QUOTENAME(@DownStockDefaultName) + N';';
        EXEC sys.sp_executesql @DownSql;
    END

    IF @DownStockMinimoDefaultName IS NOT NULL
    BEGIN
        SET @DownSql = N'ALTER TABLE [dbo].[Productos] DROP CONSTRAINT ' + QUOTENAME(@DownStockMinimoDefaultName) + N';';
        EXEC sys.sp_executesql @DownSql;
    END

    UPDATE [dbo].[Productos] SET [Stock] = ROUND([Stock], 0), [StockMinimo] = ROUND([StockMinimo], 0);
    ALTER TABLE [dbo].[Productos] ALTER COLUMN [Stock] int NOT NULL;
    ALTER TABLE [dbo].[Productos] ALTER COLUMN [StockMinimo] int NOT NULL;

    IF @DownStockDefaultName IS NOT NULL AND @DownStockDefaultDefinition IS NOT NULL
    BEGIN
        SET @DownSql = N'ALTER TABLE [dbo].[Productos] ADD CONSTRAINT ' + QUOTENAME(@DownStockDefaultName)
                     + N' DEFAULT ' + @DownStockDefaultDefinition + N' FOR [Stock];';
        EXEC sys.sp_executesql @DownSql;
    END

    IF @DownStockMinimoDefaultName IS NOT NULL AND @DownStockMinimoDefaultDefinition IS NOT NULL
    BEGIN
        SET @DownSql = N'ALTER TABLE [dbo].[Productos] ADD CONSTRAINT ' + QUOTENAME(@DownStockMinimoDefaultName)
                     + N' DEFAULT ' + @DownStockMinimoDefaultDefinition + N' FOR [StockMinimo];';
        EXEC sys.sp_executesql @DownSql;
    END

    IF COL_LENGTH(N'dbo.Productos', N'RowVersion') IS NOT NULL
        ALTER TABLE [dbo].[Productos] DROP COLUMN [RowVersion];
END

IF OBJECT_ID(N'[dbo].[Pedidos]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Pedidos_AspNetUsers_ClienteId')
        ALTER TABLE [dbo].[Pedidos] DROP CONSTRAINT [FK_Pedidos_AspNetUsers_ClienteId];
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ClienteId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
        DROP INDEX [IX_Pedidos_ClienteId] ON [dbo].[Pedidos];

    UPDATE [dbo].[Pedidos] SET [Cantidad] = ROUND([Cantidad], 0);
    ALTER TABLE [dbo].[Pedidos] ALTER COLUMN [Cantidad] int NOT NULL;

    IF COL_LENGTH(N'dbo.Pedidos', N'RowVersion') IS NOT NULL
        ALTER TABLE [dbo].[Pedidos] DROP COLUMN [RowVersion];
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Pedidos_InventarioAplicado')
        ALTER TABLE [dbo].[Pedidos] DROP CONSTRAINT [DF_Pedidos_InventarioAplicado];
    IF COL_LENGTH(N'dbo.Pedidos', N'InventarioAplicado') IS NOT NULL
        ALTER TABLE [dbo].[Pedidos] DROP COLUMN [InventarioAplicado];
    IF COL_LENGTH(N'dbo.Pedidos', N'ClienteId') IS NOT NULL
        ALTER TABLE [dbo].[Pedidos] DROP COLUMN [ClienteId];
END
");
        }
    }
}
