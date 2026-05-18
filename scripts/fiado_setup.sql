-- ═══════════════════════════════════════════════════════════════════
-- SCRIPT: Sistema de Fiados — BotiApp
-- ═══════════════════════════════════════════════════════════════════

-- 1. Estado 4: Fiado
IF NOT EXISTS (SELECT 1 FROM Ven_Estados_Boletas WHERE Id_Estado_Boleta = 4)
BEGIN
    SET IDENTITY_INSERT Ven_Estados_Boletas ON;
    INSERT INTO Ven_Estados_Boletas (Id_Estado_Boleta, Nombre_Estado_Boleta)
    VALUES (4, 'Fiado');
    SET IDENTITY_INSERT Ven_Estados_Boletas OFF;
END;

-- 2. Tabla clientes fiado
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Fia_Clientes')
BEGIN
    CREATE TABLE Fia_Clientes (
        Id_Cliente      INT          IDENTITY(1,1) NOT NULL,
        Rut             INT          NOT NULL,          -- solo número, sin DV
        Nombres         NVARCHAR(80) NOT NULL,
        Apellido1       NVARCHAR(60) NOT NULL,
        Apellido2       NVARCHAR(60) NULL,
        Telefono        NVARCHAR(20) NULL,
        Observaciones   NVARCHAR(300) NULL,
        Saldo_A_Favor   INT          NOT NULL DEFAULT 0, -- crédito no aplicado
        Estado          BIT          NOT NULL DEFAULT 1,
        Fecha_Registro  DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_Fia_Clientes PRIMARY KEY (Id_Cliente),
        CONSTRAINT UQ_Fia_Clientes_Rut UNIQUE (Rut)
    );
END;

-- 3. Tabla abonos (histórico puro)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Fia_Abonos')
BEGIN
    CREATE TABLE Fia_Abonos (
        Id_Abono        INT          IDENTITY(1,1) NOT NULL,
        Id_Cliente      INT          NOT NULL,
        Id_Usuario      INT          NOT NULL,          -- quien registró el abono (cajero/admin)
        Id_Metodo_Pago  INT          NOT NULL,          -- método de pago del abono
        Monto           INT          NOT NULL,
        Fecha           DATETIME     NOT NULL DEFAULT GETDATE(),
        Observaciones   NVARCHAR(300) NULL,
        CONSTRAINT PK_Fia_Abonos PRIMARY KEY (Id_Abono),
        CONSTRAINT FK_Fia_Abonos_Cliente FOREIGN KEY (Id_Cliente)
            REFERENCES Fia_Clientes (Id_Cliente),
        CONSTRAINT FK_Fia_Abonos_Usuario FOREIGN KEY (Id_Usuario)
            REFERENCES Emp_Usuario (Id_Usuario),
        CONSTRAINT FK_Fia_Abonos_MetodoPago FOREIGN KEY (Id_Metodo_Pago)
            REFERENCES Ven_Metodos_Pago (Id_Metodo_Pago)
    );
END;

-- 3b. Agregar Id_Metodo_Pago a Fia_Abonos si ya existe la tabla sin esa columna
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Fia_Abonos')
   AND NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('Fia_Abonos') AND name = 'Id_Metodo_Pago')
BEGIN
    ALTER TABLE Fia_Abonos ADD Id_Metodo_Pago INT NOT NULL DEFAULT 1;
    ALTER TABLE Fia_Abonos
        ADD CONSTRAINT FK_Fia_Abonos_MetodoPago FOREIGN KEY (Id_Metodo_Pago)
            REFERENCES Ven_Metodos_Pago (Id_Metodo_Pago);
END;

-- 4. FK de boleta → cliente fiado
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Id_Cliente_Fiado'
)
BEGIN
    ALTER TABLE Ven_Boletas
        ADD Id_Cliente_Fiado INT NULL;

    ALTER TABLE Ven_Boletas
        ADD CONSTRAINT FK_Ven_Boletas_Fia_Clientes
            FOREIGN KEY (Id_Cliente_Fiado) REFERENCES Fia_Clientes (Id_Cliente);
END;
