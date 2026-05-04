-- ============================================
-- BotiApp - Script de Base de Datos
-- ============================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BotiApp')
BEGIN
    CREATE DATABASE BotiApp;
END
GO

USE BotiApp;
GO

-- ============================================
-- TABLAS DE EMPLEADOS
-- ============================================

CREATE TABLE [dbo].[Emp_Tipos_Usuario] (
    [Id_Tipo_Usuario] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Tipo_Usuario] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Emp_Tipos_Usuario] PRIMARY KEY ([Id_Tipo_Usuario])
);
GO

CREATE TABLE [dbo].[Emp_Empleado] (
    [Id_Empleado] INT IDENTITY(1,1) NOT NULL,
    [Nombres_Empleado] NVARCHAR(80) NOT NULL,
    [Apellido_1] NVARCHAR(50) NOT NULL,
    [Apellido_2] NVARCHAR(50) NULL,
    [Rut] INT NOT NULL,
    [Fono] NVARCHAR(50) NULL,
    [Correo] NVARCHAR(150) NULL,
    [Fecha_Ingreso] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Emp_Empleado] PRIMARY KEY ([Id_Empleado])
);
GO

CREATE TABLE [dbo].[Emp_Usuario] (
    [Id_Usuario] INT IDENTITY(1,1) NOT NULL,
    [Id_Empleado] INT NOT NULL,
    [Id_Tipo_Usuario] INT NOT NULL,
    [Nombre_Usuario] NVARCHAR(50) NOT NULL,
    [Clave_Usuario] NVARCHAR(300) NOT NULL,
    [Estado] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Emp_Usuario] PRIMARY KEY ([Id_Usuario]),
    CONSTRAINT [UQ_Id_Empleado] UNIQUE ([Id_Empleado])
);
GO

CREATE TABLE [dbo].[Emp_Accesos_Usuario] (
    [Id_Acceso] INT IDENTITY(1,1) NOT NULL,
    [Id_Usuario] INT NOT NULL,
    [Codigo_Acceso] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Emp_Accesos_Usuario] PRIMARY KEY ([Id_Acceso])
);
GO

-- ============================================
-- TABLAS DE PRODUCTOS
-- ============================================

CREATE TABLE [dbo].[Pro_Tipos_Productos] (
    [Id_Tipo_Producto] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Tipo_Producto] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Bta_Tipos_Productos] PRIMARY KEY ([Id_Tipo_Producto])
);
GO

CREATE TABLE [dbo].[Pro_Marcas] (
    [Id_Marca] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Marca] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Bta_Marcas] PRIMARY KEY ([Id_Marca])
);
GO

CREATE TABLE [dbo].[Pro_Productos] (
    [Id_Producto] INT IDENTITY(1,1) NOT NULL,
    [Id_Tipo_Producto] INT NOT NULL,
    [Id_Marca] INT NOT NULL,
    [Nombre_Producto] NVARCHAR(100) NOT NULL,
    [Descripción] NVARCHAR(150) NULL,
    [Precio] INT NOT NULL,
    [Stock] INT NOT NULL,
    [Estado] BIT NOT NULL DEFAULT 1,
    [Imagen] VARBINARY(MAX) NULL,
    [Codigo] NVARCHAR(300) NULL,
    [Fecha_Ingreso] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Bta_Productos] PRIMARY KEY ([Id_Producto])
);
GO

CREATE TABLE [dbo].[Pro_Productos_Retornables] (
    [Id_Producto] INT NOT NULL,
    [Monto_Deposito] INT NOT NULL,
    CONSTRAINT [PK_Pro_Productos_Retornables] PRIMARY KEY ([Id_Producto])
);
GO

CREATE TABLE [dbo].[Pro_Proveedores] (
    [Id_Proveedor] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Proveedor] NVARCHAR(100) NOT NULL,
    [Rut_Proveedor] NVARCHAR(20) NULL,
    [Fono] NVARCHAR(50) NULL,
    [Correo] NVARCHAR(150) NULL,
    [Direccion] NVARCHAR(200) NULL,
    [Estado] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Bta_Proveedores] PRIMARY KEY ([Id_Proveedor])
);
GO

CREATE TABLE [dbo].[Pro_Proveedores_Dias_Entrega] (
    [Id_Dia_Entrega] INT IDENTITY(1,1) NOT NULL,
    [Id_Proveedor] INT NOT NULL,
    [Dia_Semana] INT NOT NULL,
    CONSTRAINT [PK_Bta_Proveedores_Dias_Entrega] PRIMARY KEY ([Id_Dia_Entrega])
);
GO

CREATE TABLE [dbo].[Pro_Proveedores_Productos] (
    [Id_Proveedor_Producto] INT IDENTITY(1,1) NOT NULL,
    [Id_Proveedor] INT NOT NULL,
    [Id_Producto] INT NOT NULL,
    [Precio_Compra] INT NOT NULL,
    [Estado] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Bta_Proveedores_Productos] PRIMARY KEY ([Id_Proveedor_Producto])
);
GO

-- ============================================
-- TABLAS DE PROMOCIONES
-- ============================================

CREATE TABLE [dbo].[Pro_Promocion] (
    [Id_Promocion] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Promocion] NVARCHAR(100) NOT NULL,
    [Descripcion] NVARCHAR(300) NULL,
    [Fecha_Inicio] DATETIME NOT NULL,
    [Fecha_Fin] DATETIME NOT NULL,
    [Descuento_Porcentaje] INT NULL,
    [Descuento_Monto] INT NULL,
    [Estado] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Bta_Promocion] PRIMARY KEY ([Id_Promocion])
);
GO

CREATE TABLE [dbo].[Pro_Promocion_Grupo] (
    [Id_Grupo] INT IDENTITY(1,1) NOT NULL,
    [Id_Promocion] INT NOT NULL,
    [Nombre_Grupo] NVARCHAR(50) NOT NULL,
    [Es_Excluyente] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK__Pro_Prom__ACDDD97846347211] PRIMARY KEY ([Id_Grupo])
);
GO

CREATE TABLE [dbo].[Pro_Promocion_Detalle] (
    [Id_Promocion_Detalle] INT IDENTITY(1,1) NOT NULL,
    [Id_Promocion] INT NOT NULL,
    [Id_Producto] INT NOT NULL,
    [Id_Grupo] INT NULL,
    CONSTRAINT [PK_Bta_Promocion_Detalle] PRIMARY KEY ([Id_Promocion_Detalle])
);
GO

CREATE TABLE [dbo].[Pro_Oferta_Producto] (
    [Id_Oferta_Producto] INT IDENTITY(1,1) NOT NULL,
    [Id_Producto] INT NOT NULL,
    [Precio_Oferta] INT NOT NULL,
    [Fecha_Inicio] DATETIME NOT NULL,
    [Fecha_Fin] DATETIME NOT NULL,
    CONSTRAINT [PK_Bta_Oferta_Producto] PRIMARY KEY ([Id_Oferta_Producto])
);
GO

-- ============================================
-- TABLAS DE VENTAS
-- ============================================

CREATE TABLE [dbo].[Ven_Estados_Boletas] (
    [Id_Estado_Boleta] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Estado] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Bta_Estados_Boletas] PRIMARY KEY ([Id_Estado_Boleta])
);
GO

CREATE TABLE [dbo].[Ven_Metodos_Pago] (
    [Id_Metodo_Pago] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Metodo] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Bta_Metodos_Pago] PRIMARY KEY ([Id_Metodo_Pago])
);
GO

CREATE TABLE [dbo].[Ven_Boletas] (
    [Id_Boleta] INT IDENTITY(1,1) NOT NULL,
    [Id_Vendedor] INT NOT NULL,
    [Id_Cajero] INT NULL,
    [Id_Estado_Boleta] INT NOT NULL,
    [Fecha_Emision] DATETIME NULL,
    [Monto_Total] INT NOT NULL,
    [Fecha_Pago] DATETIME NULL,
    CONSTRAINT [PK_Bta_Boletas] PRIMARY KEY ([Id_Boleta])
);
GO

CREATE TABLE [dbo].[Ven_Boleta_Detalle] (
    [Id_Boleta_Detalle] INT IDENTITY(1,1) NOT NULL,
    [Id_Boleta] INT NOT NULL,
    [Id_Producto] INT NOT NULL,
    [Cantidad] INT NOT NULL,
    [Precio_Unitario] INT NOT NULL,
    [Subtotal] INT NOT NULL,
    CONSTRAINT [PK_Bta_Boleta_Detalle] PRIMARY KEY ([Id_Boleta_Detalle])
);
GO

CREATE TABLE [dbo].[Ven_Metodos_Pago_Boleta] (
    [Id_Metodo_Pago_Boleta] INT IDENTITY(1,1) NOT NULL,
    [Id_Boleta] INT NOT NULL,
    [Id_Metodo_Pago] INT NOT NULL,
    [Monto] INT NOT NULL,
    CONSTRAINT [PK_Bta_Metodos_Pago_Boleta] PRIMARY KEY ([Id_Metodo_Pago_Boleta])
);
GO

-- ============================================
-- TABLAS DE COMPRAS
-- ============================================

CREATE TABLE [dbo].[Com_Estados_Orden_Compra] (
    [Id_Estado_Orden_Compra] INT IDENTITY(1,1) NOT NULL,
    [Nombre_Estado] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Com_Estados_Orden_Compra] PRIMARY KEY ([Id_Estado_Orden_Compra])
);
GO

CREATE TABLE [dbo].[Com_Orden_Compra] (
    [Id_Orden_Compra] INT IDENTITY(1,1) NOT NULL,
    [Id_Proveedor] INT NOT NULL,
    [Id_Usuario] INT NOT NULL,
    [Id_Estado_Orden_Compra] INT NOT NULL,
    [Fecha_Creacion] DATETIME NOT NULL DEFAULT GETDATE(),
    [Fecha_Recepcion] DATETIME NULL,
    [Total] INT NOT NULL,
    CONSTRAINT [PK_Com_Orden_Compra] PRIMARY KEY ([Id_Orden_Compra])
);
GO

CREATE TABLE [dbo].[Com_Orden_Detalle] (
    [Id_Orden_Detalle] INT IDENTITY(1,1) NOT NULL,
    [Id_Orden_Compra] INT NOT NULL,
    [Id_Proveedor] INT NOT NULL,
    [Id_Proveedor_Producto] INT NOT NULL,
    [Cantidad] INT NOT NULL,
    [Precio_Unitario] INT NOT NULL,
    [Subtotal] INT NOT NULL,
    [Cantidad_Recibida] INT NULL,
    CONSTRAINT [PK_Com_Orden_Detalle] PRIMARY KEY ([Id_Orden_Detalle])
);
GO

-- ============================================
-- TABLAS DE AUDITORÍA
-- ============================================

CREATE TABLE [dbo].[Aud_Pro_Productos] (
    [Id_Auditoria] INT IDENTITY(1,1) NOT NULL,
    [Id_Producto] INT NOT NULL,
    [Campo_Modificado] NVARCHAR(50) NOT NULL,
    [Valor_Anterior] NVARCHAR(MAX) NULL,
    [Valor_Nuevo] NVARCHAR(MAX) NULL,
    [Fecha_Cambio] DATETIME NOT NULL DEFAULT GETDATE(),
    [Id_Usuario] INT NULL,
    CONSTRAINT [PK_Aud_Pro_Productos] PRIMARY KEY ([Id_Auditoria])
);
GO

-- ============================================
-- FOREIGN KEYS
-- ============================================

-- Empleados
ALTER TABLE [dbo].[Emp_Usuario] ADD CONSTRAINT [FK_Emp_Usuario_Emp_Empleado1]
    FOREIGN KEY ([Id_Empleado]) REFERENCES [dbo].[Emp_Empleado]([Id_Empleado]);

ALTER TABLE [dbo].[Emp_Usuario] ADD CONSTRAINT [FK_Emp_Usuario_Emp_Tipos_Usuario]
    FOREIGN KEY ([Id_Tipo_Usuario]) REFERENCES [dbo].[Emp_Tipos_Usuario]([Id_Tipo_Usuario]);

ALTER TABLE [dbo].[Emp_Accesos_Usuario] ADD CONSTRAINT [FK_Emp_Accesos_Usuario_Emp_Usuario]
    FOREIGN KEY ([Id_Usuario]) REFERENCES [dbo].[Emp_Usuario]([Id_Usuario]);

-- Productos
ALTER TABLE [dbo].[Pro_Productos] ADD CONSTRAINT [FK_Pro_Productos_Pro_Marcas]
    FOREIGN KEY ([Id_Marca]) REFERENCES [dbo].[Pro_Marcas]([Id_Marca]);

ALTER TABLE [dbo].[Pro_Productos] ADD CONSTRAINT [FK_Pro_Productos_Pro_Tipos_Productos]
    FOREIGN KEY ([Id_Tipo_Producto]) REFERENCES [dbo].[Pro_Tipos_Productos]([Id_Tipo_Producto]);

ALTER TABLE [dbo].[Pro_Productos_Retornables] ADD CONSTRAINT [FK_Pro_Productos_Retornables_Pro_Productos]
    FOREIGN KEY ([Id_Producto]) REFERENCES [dbo].[Pro_Productos]([Id_Producto]);

ALTER TABLE [dbo].[Pro_Proveedores_Dias_Entrega] ADD CONSTRAINT [FK_Pro_Proveedores_Dias_Entrega_Pro_Proveedores]
    FOREIGN KEY ([Id_Proveedor]) REFERENCES [dbo].[Pro_Proveedores]([Id_Proveedor]);

ALTER TABLE [dbo].[Pro_Proveedores_Productos] ADD CONSTRAINT [FK_Pro_Proveedores_Productos_Pro_Productos]
    FOREIGN KEY ([Id_Producto]) REFERENCES [dbo].[Pro_Productos]([Id_Producto]);

ALTER TABLE [dbo].[Pro_Proveedores_Productos] ADD CONSTRAINT [FK_Pro_Proveedores_Productos_Pro_Proveedores]
    FOREIGN KEY ([Id_Proveedor]) REFERENCES [dbo].[Pro_Proveedores]([Id_Proveedor]);

-- Promociones
ALTER TABLE [dbo].[Pro_Promocion_Grupo] ADD CONSTRAINT [FK_PromoGrupo_Promo]
    FOREIGN KEY ([Id_Promocion]) REFERENCES [dbo].[Pro_Promocion]([Id_Promocion]);

ALTER TABLE [dbo].[Pro_Promocion_Detalle] ADD CONSTRAINT [FK_Pro_Promocion_Detalle_Pro_Promocion]
    FOREIGN KEY ([Id_Promocion]) REFERENCES [dbo].[Pro_Promocion]([Id_Promocion]);

ALTER TABLE [dbo].[Pro_Promocion_Detalle] ADD CONSTRAINT [FK_Pro_Promocion_Detalle_Pro_Productos]
    FOREIGN KEY ([Id_Producto]) REFERENCES [dbo].[Pro_Productos]([Id_Producto]);

ALTER TABLE [dbo].[Pro_Promocion_Detalle] ADD CONSTRAINT [FK_PromoDetalle_Grupo]
    FOREIGN KEY ([Id_Grupo]) REFERENCES [dbo].[Pro_Promocion_Grupo]([Id_Grupo]);

ALTER TABLE [dbo].[Pro_Oferta_Producto] ADD CONSTRAINT [FK_Pro_Oferta_Producto_Pro_Productos]
    FOREIGN KEY ([Id_Producto]) REFERENCES [dbo].[Pro_Productos]([Id_Producto]);

-- Ventas
ALTER TABLE [dbo].[Ven_Boletas] ADD CONSTRAINT [FK_Bta_Boletas_Bta_Estados_Boletas]
    FOREIGN KEY ([Id_Estado_Boleta]) REFERENCES [dbo].[Ven_Estados_Boletas]([Id_Estado_Boleta]);

ALTER TABLE [dbo].[Ven_Boletas] ADD CONSTRAINT [FK_Bta_Boletas_Emp_Vendedor]
    FOREIGN KEY ([Id_Vendedor]) REFERENCES [dbo].[Emp_Usuario]([Id_Usuario]);

ALTER TABLE [dbo].[Ven_Boleta_Detalle] ADD CONSTRAINT [FK_Bta_Boleta_Detalle_Bta_Boletas]
    FOREIGN KEY ([Id_Boleta]) REFERENCES [dbo].[Ven_Boletas]([Id_Boleta]);

ALTER TABLE [dbo].[Ven_Boleta_Detalle] ADD CONSTRAINT [FK_Bta_Boleta_Detalle_Bta_Productos]
    FOREIGN KEY ([Id_Producto]) REFERENCES [dbo].[Pro_Productos]([Id_Producto]);

ALTER TABLE [dbo].[Ven_Metodos_Pago_Boleta] ADD CONSTRAINT [FK_Bta_Metodos_Pago_Boleta_Bta_Boletas]
    FOREIGN KEY ([Id_Boleta]) REFERENCES [dbo].[Ven_Boletas]([Id_Boleta]);

ALTER TABLE [dbo].[Ven_Metodos_Pago_Boleta] ADD CONSTRAINT [FK_Bta_Metodos_Pago_Boleta_Bta_Metodos_Pago]
    FOREIGN KEY ([Id_Metodo_Pago]) REFERENCES [dbo].[Ven_Metodos_Pago]([Id_Metodo_Pago]);

-- Compras
ALTER TABLE [dbo].[Com_Orden_Compra] ADD CONSTRAINT [FK_Com_Orden_Compra_Com_Estados_Orden_Compra]
    FOREIGN KEY ([Id_Estado_Orden_Compra]) REFERENCES [dbo].[Com_Estados_Orden_Compra]([Id_Estado_Orden_Compra]);

ALTER TABLE [dbo].[Com_Orden_Compra] ADD CONSTRAINT [FK_Com_Orden_Compra_Bta_Proveedores]
    FOREIGN KEY ([Id_Proveedor]) REFERENCES [dbo].[Pro_Proveedores]([Id_Proveedor]);

ALTER TABLE [dbo].[Com_Orden_Compra] ADD CONSTRAINT [FK_Com_Orden_Compra_Emp_Usuario]
    FOREIGN KEY ([Id_Usuario]) REFERENCES [dbo].[Emp_Usuario]([Id_Usuario]);

ALTER TABLE [dbo].[Com_Orden_Detalle] ADD CONSTRAINT [FK_Com_Orden_Detalle_Com_Orden_Compra]
    FOREIGN KEY ([Id_Orden_Compra]) REFERENCES [dbo].[Com_Orden_Compra]([Id_Orden_Compra]);

ALTER TABLE [dbo].[Com_Orden_Detalle] ADD CONSTRAINT [FK_Com_Orden_Detalle_Bta_Proveedores]
    FOREIGN KEY ([Id_Proveedor]) REFERENCES [dbo].[Pro_Proveedores]([Id_Proveedor]);

ALTER TABLE [dbo].[Com_Orden_Detalle] ADD CONSTRAINT [FK_Com_Orden_Detalle_Pro_Proveedores_Productos]
    FOREIGN KEY ([Id_Proveedor_Producto]) REFERENCES [dbo].[Pro_Proveedores_Productos]([Id_Proveedor_Producto]);

-- Auditoría
ALTER TABLE [dbo].[Aud_Pro_Productos] ADD CONSTRAINT [FK_Aud_Pro_Productos_Pro_Productos]
    FOREIGN KEY ([Id_Producto]) REFERENCES [dbo].[Pro_Productos]([Id_Producto]);
GO

-- ============================================
-- DATOS INICIALES
-- ============================================

-- Tipos de usuario
INSERT INTO [dbo].[Emp_Tipos_Usuario] ([Nombre_Tipo_Usuario]) VALUES
('Administrador'),
('Cajero'),
('Vendedor'),
('Encargado de compras');

-- Estados de boletas
INSERT INTO [dbo].[Ven_Estados_Boletas] ([Nombre_Estado]) VALUES
('Pendiente'),
('Pagada'),
('Anulada'),
('Cancelada');

-- Métodos de pago
INSERT INTO [dbo].[Ven_Metodos_Pago] ([Nombre_Metodo]) VALUES
('Efectivo'),
('Débito'),
('Crédito'),
('Transferencia');

-- Estados de orden de compra
INSERT INTO [dbo].[Com_Estados_Orden_Compra] ([Nombre_Estado]) VALUES
('Pendiente'),
('Aprobada'),
('Recibida'),
('Cancelada');

GO

PRINT 'Base de datos BotiApp creada exitosamente';
GO