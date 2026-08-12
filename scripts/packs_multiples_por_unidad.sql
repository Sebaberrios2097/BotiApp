-- ═══════════════════════════════════════════════════════════════════
-- SCRIPT: Permitir varios packs sobre la misma unidad base — BotiApp
-- ═══════════════════════════════════════════════════════════════════
-- Pro_Producto_Pack tenía un UNIQUE sobre Id_Producto_Unidad que impedía
-- registrar, por ejemplo, un pack de 6 y otro de 12 del mismo producto.
-- La relación correcta es 1 unidad → N packs (cada pack sigue apuntando a
-- una sola unidad, eso no cambia).

-- 1. Quitar la restricción UNIQUE si todavía existe
IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_Pro_Producto_Pack_Unidad'
             AND parent_object_id = OBJECT_ID('Pro_Producto_Pack'))
BEGIN
    ALTER TABLE Pro_Producto_Pack DROP CONSTRAINT UQ_Pro_Producto_Pack_Unidad;
END;

-- En algunas bases pudo crearse como índice único en vez de constraint
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'UQ_Pro_Producto_Pack_Unidad'
             AND object_id = OBJECT_ID('Pro_Producto_Pack'))
BEGIN
    DROP INDEX UQ_Pro_Producto_Pack_Unidad ON Pro_Producto_Pack;
END;

-- 2. Índice NO único para que las búsquedas por unidad sigan siendo rápidas
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Pro_Producto_Pack_Unidad'
                 AND object_id = OBJECT_ID('Pro_Producto_Pack'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pro_Producto_Pack_Unidad
        ON Pro_Producto_Pack (Id_Producto_Unidad);
END;

-- 3. Un producto sigue teniendo como máximo UNA definición de pack:
--    esa dirección de la relación no cambia y conviene garantizarla.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_Pro_Producto_Pack_Producto'
                 AND object_id = OBJECT_ID('Pro_Producto_Pack'))
   AND NOT EXISTS (SELECT Id_Producto_Pack_Producto
                   FROM Pro_Producto_Pack
                   GROUP BY Id_Producto_Pack_Producto
                   HAVING COUNT(*) > 1)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Pro_Producto_Pack_Producto
        ON Pro_Producto_Pack (Id_Producto_Pack_Producto);
END;
