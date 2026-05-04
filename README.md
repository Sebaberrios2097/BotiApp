# BotiApp

Sistema ERP/POS (Punto de Venta) web desarrollado en ASP.NET Core.

## Requisitos

- .NET 10.0 SDK
- SQL Server
- Visual Studio 2022 o VS Code (opcional)

## Configuración

### Base de datos

1. Ejecutar el script de creación de tablas:
   ```bash
   sqlcmd -S TU_SERVIDOR -i scripts/BotiApp_Schema.sql
   ```
   O desde SQL Server Management Studio, abrir y ejecutar `scripts/BotiApp_Schema.sql`.

2. Actualizar la cadena de conexión en `BotiApp/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=BotiApp;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Preparar la aplicación

```bash
cd BotiApp
dotnet restore
```

## Ejecución

```bash
cd BotiApp
dotnet run
```

Para desarrollo con recarga activa:

```bash
dotnet watch
```

## Características

- **Ventas**: Gestión de caja, catálogo de productos, historial de ventas, métodos de pago, boletas
- **Productos**: Administración de productos, promociones, ofertas, marcas, productos retornables
- **Compras**: Gestión de proveedores, órdenes de compra, estados de orden
- **Empleados**: Administración de empleados, usuarios y control de accesos
- **Auditoría**: Registro de cambios en productos

## Estructura del proyecto

```
BotiApp/
├── BotiApp/              # Proyecto principal (ASP.NET Core)
│   ├── Areas/            # Módulos de la aplicación
│   ├── Controllers/      # Controladores principales
│   ├── Models/           # ViewModels
│   └── Helpers/          # Utilidades
└── Infraestructura/      # Capa de datos
    ├── Entities/         # Modelos de EF Core
    ├── Repositories/     # Repositorios
    └── Context/          # DbContext
```

## Tecnologías

- ASP.NET Core 10
- Entity Framework Core 10
- SQL Server
- Patrón Repository