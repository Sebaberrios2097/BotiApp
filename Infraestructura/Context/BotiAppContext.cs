using System;
using System.Collections.Generic;
using Infraestructura.Entities.BotiApp;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Context;

public partial class BotiAppContext : DbContext
{
    public BotiAppContext(DbContextOptions<BotiAppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AudProProductos> AudProProductos { get; set; }

    public virtual DbSet<ComEstadosOrdenCompra> ComEstadosOrdenCompra { get; set; }

    public virtual DbSet<ComOrdenCompra> ComOrdenCompra { get; set; }

    public virtual DbSet<ComOrdenDetalle> ComOrdenDetalle { get; set; }

    public virtual DbSet<EmpAccesosUsuario> EmpAccesosUsuario { get; set; }

    public virtual DbSet<EmpEmpleado> EmpEmpleado { get; set; }

    public virtual DbSet<EmpTiposUsuario> EmpTiposUsuario { get; set; }

    public virtual DbSet<EmpUsuario> EmpUsuario { get; set; }

    public virtual DbSet<ProMarcas> ProMarcas { get; set; }

    public virtual DbSet<ProOfertaProducto> ProOfertaProducto { get; set; }

    public virtual DbSet<ProProductos> ProProductos { get; set; }

    public virtual DbSet<ProProductosRetornables> ProProductosRetornables { get; set; }

    public virtual DbSet<ProPromocion> ProPromocion { get; set; }

    public virtual DbSet<ProPromocionDetalle> ProPromocionDetalle { get; set; }

    public virtual DbSet<ProPromocionGrupo> ProPromocionGrupo { get; set; }

    public virtual DbSet<ProProveedores> ProProveedores { get; set; }

    public virtual DbSet<ProProveedoresDiasEntrega> ProProveedoresDiasEntrega { get; set; }

    public virtual DbSet<ProProveedoresProductos> ProProveedoresProductos { get; set; }

    public virtual DbSet<ProTiposProductos> ProTiposProductos { get; set; }

    public virtual DbSet<VenBoletaDetalle> VenBoletaDetalle { get; set; }

    public virtual DbSet<VenBoletas> VenBoletas { get; set; }

    public virtual DbSet<VenEstadosBoletas> VenEstadosBoletas { get; set; }

    public virtual DbSet<VenMetodosPago> VenMetodosPago { get; set; }

    public virtual DbSet<VenMetodosPagoBoleta> VenMetodosPagoBoleta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AudProProductos>(entity =>
        {
            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.AudProProductos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Aud_Pro_Productos_Pro_Productos");
        });

        modelBuilder.Entity<ComOrdenCompra>(entity =>
        {
            entity.HasOne(d => d.IdEstadoOrdenCompraNavigation).WithMany(p => p.ComOrdenCompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Com_Orden_Compra_Com_Estados_Orden_Compra");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.ComOrdenCompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Com_Orden_Compra_Bta_Proveedores");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ComOrdenCompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Com_Orden_Compra_Emp_Usuario");
        });

        modelBuilder.Entity<ComOrdenDetalle>(entity =>
        {
            entity.HasOne(d => d.IdOrdenCompraNavigation).WithMany(p => p.ComOrdenDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Com_Orden_Detalle_Com_Orden_Compra");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.ComOrdenDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Com_Orden_Detalle_Bta_Proveedores");

            entity.HasOne(d => d.IdProveedorProductoNavigation).WithMany(p => p.ComOrdenDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Com_Orden_Detalle_Pro_Proveedores_Productos");
        });

        modelBuilder.Entity<EmpAccesosUsuario>(entity =>
        {
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.EmpAccesosUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Emp_Accesos_Usuario_Emp_Usuario");
        });

        modelBuilder.Entity<EmpUsuario>(entity =>
        {
            entity.HasOne(d => d.IdEmpleadoNavigation).WithOne(p => p.EmpUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Emp_Usuario_Emp_Empleado1");

            entity.HasOne(d => d.IdTipoUsuarioNavigation).WithMany(p => p.EmpUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Emp_Usuario_Emp_Tipos_Usuario");
        });

        modelBuilder.Entity<ProMarcas>(entity =>
        {
            entity.HasKey(e => e.IdMarca).HasName("PK_Bta_Marcas");
        });

        modelBuilder.Entity<ProOfertaProducto>(entity =>
        {
            entity.HasKey(e => e.IdOfertaProducto).HasName("PK_Bta_Oferta_Producto");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ProOfertaProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Oferta_Producto_Pro_Productos");
        });

        modelBuilder.Entity<ProProductos>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK_Bta_Productos");

            entity.Property(e => e.FechaIngreso)
                .HasComment("Columna que indica la fecha de ingreso del producto al sistema.")
                .HasDefaultValueSql("(getdate())", "DF_Pro_Productos_Fecha_Ingreso");

            entity.HasOne(d => d.IdMarcaNavigation).WithMany(p => p.ProProductos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Productos_Pro_Marcas");

            entity.HasOne(d => d.IdTipoProductoNavigation).WithMany(p => p.ProProductos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Productos_Pro_Tipos_Productos");
        });

        modelBuilder.Entity<ProProductosRetornables>(entity =>
        {
            entity.HasOne(d => d.IdProductoNavigation).WithOne(p => p.ProProductosRetornables)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Productos_Retornables_Pro_Productos");
        });

        modelBuilder.Entity<ProPromocion>(entity =>
        {
            entity.HasKey(e => e.IdPromocion).HasName("PK_Bta_Promocion");
        });

        modelBuilder.Entity<ProPromocionDetalle>(entity =>
        {
            entity.HasKey(e => e.IdPromocionDetalle).HasName("PK_Bta_Promocion_Detalle");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.ProPromocionDetalle).HasConstraintName("FK_PromoDetalle_Grupo");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ProPromocionDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Promocion_Detalle_Pro_Productos");

            entity.HasOne(d => d.IdPromocionNavigation).WithMany(p => p.ProPromocionDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Promocion_Detalle_Pro_Promocion");
        });

        modelBuilder.Entity<ProPromocionGrupo>(entity =>
        {
            entity.HasKey(e => e.IdGrupo).HasName("PK__Pro_Prom__ACDDD97846347211");

            entity.Property(e => e.EsExcluyente).HasDefaultValue(true);

            entity.HasOne(d => d.IdPromocionNavigation).WithMany(p => p.ProPromocionGrupo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromoGrupo_Promo");
        });

        modelBuilder.Entity<ProProveedores>(entity =>
        {
            entity.HasKey(e => e.IdProveedor).HasName("PK_Bta_Proveedores");
            entity.Property(e => e.Estado).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProProveedoresDiasEntrega>(entity =>
        {
            entity.HasKey(e => e.IdDiaEntrega).HasName("PK_Bta_Proveedores_Dias_Entrega");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.ProProveedoresDiasEntrega)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Proveedores_Dias_Entrega_Pro_Proveedores");
        });

        modelBuilder.Entity<ProProveedoresProductos>(entity =>
        {
            entity.HasKey(e => e.IdProveedorProducto).HasName("PK_Bta_Proveedores_Productos");

            entity.Property(e => e.Estado).HasComment("Refleja el estado del proveedor con el producto, indicando si el proveedor sigue encargado de entregar ese producto o no.");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ProProveedoresProductos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Proveedores_Productos_Pro_Productos");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.ProProveedoresProductos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pro_Proveedores_Productos_Pro_Proveedores");
        });

        modelBuilder.Entity<ProTiposProductos>(entity =>
        {
            entity.HasKey(e => e.IdTipoProducto).HasName("PK_Bta_Tipos_Productos");
        });

        modelBuilder.Entity<VenBoletaDetalle>(entity =>
        {
            entity.HasKey(e => e.IdBoletaDetalle).HasName("PK_Bta_Boleta_Detalle");

            entity.HasOne(d => d.IdBoletaNavigation).WithMany(p => p.VenBoletaDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bta_Boleta_Detalle_Bta_Boletas");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.VenBoletaDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bta_Boleta_Detalle_Bta_Productos");
        });

        modelBuilder.Entity<VenBoletas>(entity =>
        {
            entity.HasKey(e => e.IdBoleta).HasName("PK_Bta_Boletas");

            entity.HasOne(d => d.IdEstadoBoletaNavigation).WithMany(p => p.VenBoletas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bta_Boletas_Bta_Estados_Boletas");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.VenBoletasVendedor)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bta_Boletas_Emp_Vendedor");

            entity.HasOne(d => d.IdCajeroNavigation).WithMany(p => p.VenBoletasCajero)
                .HasForeignKey(d => d.IdCajero)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VenEstadosBoletas>(entity =>
        {
            entity.HasKey(e => e.IdEstadoBoleta).HasName("PK_Bta_Estados_Boletas");
        });

        modelBuilder.Entity<VenMetodosPago>(entity =>
        {
            entity.HasKey(e => e.IdMetodoPago).HasName("PK_Bta_Metodos_Pago");
        });

        modelBuilder.Entity<VenMetodosPagoBoleta>(entity =>
        {
            entity.HasKey(e => e.IdMetodoPagoBoleta).HasName("PK_Bta_Metodos_Pago_Boleta");

            entity.HasOne(d => d.IdBoletaNavigation).WithMany(p => p.VenMetodosPagoBoleta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bta_Metodos_Pago_Boleta_Bta_Boletas");

            entity.HasOne(d => d.IdMetodoPagoNavigation).WithMany(p => p.VenMetodosPagoBoleta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bta_Metodos_Pago_Boleta_Bta_Metodos_Pago");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
