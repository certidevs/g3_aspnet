using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Models;

namespace G3AspNet.Data;

/// <summary>
/// Contexto de EF Core: configura SQLite y reúne las tablas de la aplicación.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>EF Core recibe estas opciones desde Program.cs para conectar con SQLite.</summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Repair> Repairs => Set<Repair>();
    public DbSet<RepairPart> RepairParts => Set<RepairPart>();

    /// <summary>Tabla de vehículos gestionados en el taller.</summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<SparePart> SpareParts => Set<SparePart>();

    /// <summary>Configura las columnas añadidas a Identity y las entidades del dominio.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100);
            entity.Property(user => user.AvatarFileName).HasMaxLength(260);
            entity.Property(user => user.CreatedAt).IsRequired();
        });

        // ---- Relaciones del dominio (regla: Cascade solo si el hijo no tiene sentido sin el padre) ----

        modelBuilder.Entity<Vehicle>(entity =>
        {
            // Matrícula única
            entity.HasIndex(v => v.LicensePlate)
                  .IsUnique();

            // Vehicle -> Owner (ApplicationUser): Restrict, no se borra una cuenta que tiene vehículos.
            entity.HasOne(v => v.Owner)
                  .WithMany(user => user.Vehicles)
                  .HasForeignKey(v => v.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Repair>(entity =>
        {
            // Repair -> Vehicle: Restrict, las reparaciones son historial y no se pierden al borrar el vehículo.
            entity.HasOne(repair => repair.Vehicle)
                  .WithMany(v => v.Repairs)
                  .HasForeignKey(repair => repair.VehicleId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Repair -> Mechanic (ApplicationUser): Restrict, no se borra una cuenta con reparaciones asignadas.
            entity.HasOne(repair => repair.Mechanic)
                  .WithMany(user => user.AssignedRepairs)
                  .HasForeignKey(repair => repair.MechanicId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // RepairPart es la tabla intermedia entre Repair y SparePart (N:M con datos propios:
        // cantidad y precio unitario en el momento del uso).
        modelBuilder.Entity<RepairPart>(entity =>
        {
            // RepairPart -> Repair: Cascade, una línea de pieza no tiene sentido sin su reparación.
            entity.HasOne(part => part.Repair)
                  .WithMany(repair => repair.RepairParts)
                  .HasForeignKey(part => part.RepairId)
                  .OnDelete(DeleteBehavior.Cascade);

            // RepairPart -> SparePart: Restrict, un recambio ya usado en reparaciones es historial y no se borra.
            entity.HasOne(part => part.SparePart)
                  .WithMany(sparePart => sparePart.RepairParts)
                  .HasForeignKey(part => part.SparePartId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // La referencia del fabricante identifica el recambio: no se repite.
        modelBuilder.Entity<SparePart>()
            .HasIndex(sparePart => sparePart.Reference)
            .IsUnique();
    }
}
