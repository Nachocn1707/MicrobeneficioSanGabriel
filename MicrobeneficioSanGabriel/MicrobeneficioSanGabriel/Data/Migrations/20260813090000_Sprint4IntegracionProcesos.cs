using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MicrobeneficioSanGabriel.Data;

#nullable disable

namespace MicrobeneficioSanGabriel.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260813090000_Sprint4IntegracionProcesos")]
    public partial class Sprint4IntegracionProcesos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PedidoEstadoHistoriales]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PedidoEstadoHistoriales]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PedidoEstadoHistoriales] PRIMARY KEY,
        [PedidoId] int NOT NULL,
        [EstadoAnterior] nvarchar(40) NULL,
        [EstadoNuevo] nvarchar(40) NOT NULL,
        [FechaCambio] datetime2 NOT NULL,
        [UsuarioId] nvarchar(450) NULL,
        [UsuarioNombre] nvarchar(180) NOT NULL,
        [Rol] nvarchar(80) NOT NULL,
        [Observacion] nvarchar(300) NULL,
        CONSTRAINT [FK_PedidoEstadoHistoriales_Pedidos_PedidoId]
            FOREIGN KEY([PedidoId]) REFERENCES [dbo].[Pedidos]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_PedidoEstadoHistoriales_PedidoId_FechaCambio]
        ON [dbo].[PedidoEstadoHistoriales]([PedidoId], [FechaCambio]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Trazabilidades]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Trazabilidades', N'EsAutomatico') IS NULL
BEGIN
    ALTER TABLE [dbo].[Trazabilidades]
        ADD [EsAutomatico] bit NOT NULL CONSTRAINT [DF_Trazabilidades_EsAutomatico] DEFAULT(0);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Trazabilidades]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[Producciones]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[Trazabilidades]
        ([LoteId], [ProduccionId], [Etapa], [FechaRegistro], [Responsable], [Observacion], [EsAutomatico])
    SELECT p.[LoteId], p.[Id], p.[TipoProceso], p.[FechaProduccion], N'Sistema',
           CONCAT(N'Migrado automáticamente desde la producción #', p.[Id], N' (', p.[TipoProceso], N').'), 1
    FROM [dbo].[Producciones] p
    WHERE p.[Estado] = N'Completado'
      AND p.[TipoProceso] IN (N'Lavado', N'Secado', N'Tostado', N'Molido', N'Empaque')
      AND NOT EXISTS (SELECT 1 FROM [dbo].[Trazabilidades] t WHERE t.[ProduccionId] = p.[Id] AND t.[EsAutomatico] = 1)
      AND (
            p.[TipoProceso] = N'Lavado'
         OR (p.[TipoProceso] = N'Secado' AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Lavado'))
         OR (p.[TipoProceso] = N'Tostado'
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Lavado')
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Secado'))
         OR (p.[TipoProceso] = N'Molido'
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Lavado')
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Secado')
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Tostado'))
         OR (p.[TipoProceso] = N'Empaque'
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Lavado')
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Secado')
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Tostado')
             AND EXISTS (SELECT 1 FROM [dbo].[Producciones] a WHERE a.[LoteId] = p.[LoteId] AND a.[Estado] = N'Completado' AND a.[TipoProceso] = N'Molido'))
          );
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[RegistrosFinancieros]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'OrigenTipo') IS NULL
        ALTER TABLE [dbo].[RegistrosFinancieros] ADD [OrigenTipo] nvarchar(40) NULL;

    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'OrigenId') IS NULL
        ALTER TABLE [dbo].[RegistrosFinancieros] ADD [OrigenId] int NULL;

    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'EsAutomatico') IS NULL
        ALTER TABLE [dbo].[RegistrosFinancieros]
            ADD [EsAutomatico] bit NOT NULL CONSTRAINT [DF_RegistrosFinancieros_EsAutomatico] DEFAULT(0);

    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'Destinatario') IS NULL
        ALTER TABLE [dbo].[RegistrosFinancieros] ADD [Destinatario] nvarchar(180) NULL;

    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'ProductoNombre') IS NULL
        ALTER TABLE [dbo].[RegistrosFinancieros] ADD [ProductoNombre] nvarchar(120) NULL;

    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'CantidadKg') IS NULL
        ALTER TABLE [dbo].[RegistrosFinancieros] ADD [CantidadKg] decimal(18,2) NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_RegistrosFinancieros_OrigenTipo_OrigenId'
          AND [object_id] = OBJECT_ID(N'[dbo].[RegistrosFinancieros]'))
    BEGIN
        CREATE INDEX [IX_RegistrosFinancieros_OrigenTipo_OrigenId]
            ON [dbo].[RegistrosFinancieros]([OrigenTipo], [OrigenId]);
    END
END
");

            // Crea una línea inicial para pedidos existentes que todavía no tienen historial.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PedidoEstadoHistoriales]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[Pedidos]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PedidoEstadoHistoriales]
        ([PedidoId], [EstadoAnterior], [EstadoNuevo], [FechaCambio], [UsuarioId], [UsuarioNombre], [Rol], [Observacion])
    SELECT p.[Id], NULL, COALESCE(NULLIF(p.[Estado], N''), N'Pendiente'), p.[FechaPedido], p.[ClienteId],
           COALESCE(NULLIF(p.[ClienteNombre], N''), N'Sistema'),
           CASE WHEN p.[ClienteId] IS NULL THEN N'Sistema' ELSE N'Cliente' END,
           N'Estado inicial migrado automáticamente.'
    FROM [dbo].[Pedidos] p
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[PedidoEstadoHistoriales] h WHERE h.[PedidoId] = p.[Id]
    );
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[RegistrosFinancieros]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_RegistrosFinancieros_OrigenTipo_OrigenId' AND [object_id] = OBJECT_ID(N'[dbo].[RegistrosFinancieros]'))
        DROP INDEX [IX_RegistrosFinancieros_OrigenTipo_OrigenId] ON [dbo].[RegistrosFinancieros];

    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'CantidadKg') IS NOT NULL ALTER TABLE [dbo].[RegistrosFinancieros] DROP COLUMN [CantidadKg];
    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'ProductoNombre') IS NOT NULL ALTER TABLE [dbo].[RegistrosFinancieros] DROP COLUMN [ProductoNombre];
    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'Destinatario') IS NOT NULL ALTER TABLE [dbo].[RegistrosFinancieros] DROP COLUMN [Destinatario];

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [name] = N'DF_RegistrosFinancieros_EsAutomatico')
        ALTER TABLE [dbo].[RegistrosFinancieros] DROP CONSTRAINT [DF_RegistrosFinancieros_EsAutomatico];
    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'EsAutomatico') IS NOT NULL ALTER TABLE [dbo].[RegistrosFinancieros] DROP COLUMN [EsAutomatico];
    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'OrigenId') IS NOT NULL ALTER TABLE [dbo].[RegistrosFinancieros] DROP COLUMN [OrigenId];
    IF COL_LENGTH(N'dbo.RegistrosFinancieros', N'OrigenTipo') IS NOT NULL ALTER TABLE [dbo].[RegistrosFinancieros] DROP COLUMN [OrigenTipo];
END

IF OBJECT_ID(N'[dbo].[Trazabilidades]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [name] = N'DF_Trazabilidades_EsAutomatico')
        ALTER TABLE [dbo].[Trazabilidades] DROP CONSTRAINT [DF_Trazabilidades_EsAutomatico];
    IF COL_LENGTH(N'dbo.Trazabilidades', N'EsAutomatico') IS NOT NULL ALTER TABLE [dbo].[Trazabilidades] DROP COLUMN [EsAutomatico];
END

IF OBJECT_ID(N'[dbo].[PedidoEstadoHistoriales]', N'U') IS NOT NULL
    DROP TABLE [dbo].[PedidoEstadoHistoriales];
");
        }
    }
}
