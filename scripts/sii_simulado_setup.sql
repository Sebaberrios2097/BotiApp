-- ═══════════════════════════════════════════════════════════════════
-- SCRIPT: SII Simulado para Boleta Afecta (39) — BotiApp
-- ═══════════════════════════════════════════════════════════════════

-- 1) Tabla de secuencia de folios simulados por tipo DTE
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ven_Sii_Folios')
BEGIN
    CREATE TABLE Ven_Sii_Folios (
        Id_Folio_Secuencia INT IDENTITY(1,1) NOT NULL,
        Tipo_Dte           INT NOT NULL,
        Folio_Actual       INT NOT NULL,
        Folio_Inicial      INT NOT NULL,
        Folio_Final        INT NOT NULL,
        Actualizado_En     DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_Ven_Sii_Folios PRIMARY KEY (Id_Folio_Secuencia)
    );

    CREATE UNIQUE INDEX UX_Ven_Sii_Folios_Tipo_Dte ON Ven_Sii_Folios (Tipo_Dte);
END;

-- 2) Columnas SII en boletas
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Tipo_Dte_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Tipo_Dte_Sii INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Folio_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Folio_Sii INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Estado_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Estado_Sii NVARCHAR(30) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'TrackId_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD TrackId_Sii NVARCHAR(60) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Fecha_Envio_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Fecha_Envio_Sii DATETIME NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Monto_Neto_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Monto_Neto_Sii INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Monto_Iva_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Monto_Iva_Sii INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Monto_Exento_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Monto_Exento_Sii INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Xml_Dte_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Xml_Dte_Sii NVARCHAR(MAX) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Mensaje_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Mensaje_Sii NVARCHAR(300) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Ven_Boletas') AND name = 'Intentos_Envio_Sii')
BEGIN
    ALTER TABLE Ven_Boletas ADD Intentos_Envio_Sii INT NULL;
END;

-- 3) Semilla secuencia para tipo 39 (boleta afecta)
IF NOT EXISTS (SELECT 1 FROM Ven_Sii_Folios WHERE Tipo_Dte = 39)
BEGIN
    INSERT INTO Ven_Sii_Folios (Tipo_Dte, Folio_Actual, Folio_Inicial, Folio_Final, Actualizado_En)
    VALUES (39, 0, 1, 99999999, GETDATE());
END;
