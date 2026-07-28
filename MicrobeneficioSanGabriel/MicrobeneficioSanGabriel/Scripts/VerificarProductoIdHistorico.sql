-- Verifica que las columnas usadas para historial acepten NULL.
SELECT
    t.name AS Tabla,
    c.name AS Columna,
    c.is_nullable AS PermiteNull
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name IN (N'Pedidos', N'MovimientosInventario', N'Producciones')
  AND c.name = N'ProductoId'
ORDER BY t.name;

-- Verifica los registros históricos cuyo producto ya fue eliminado.
SELECT Id, ProductoId, ProductoNombre, TipoMovimiento, Cantidad, FechaMovimiento
FROM MovimientosInventario
WHERE ProductoId IS NULL
ORDER BY FechaMovimiento DESC;

SELECT Id, ProductoId, ProductoNombre, PrecioUnitario, Cantidad, FechaPedido
FROM Pedidos
WHERE ProductoId IS NULL
ORDER BY FechaPedido DESC;
