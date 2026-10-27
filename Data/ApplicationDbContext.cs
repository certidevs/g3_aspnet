using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Models;

namespace G3AspNet.Data;

/// <summary>
/// Contexto de EF Core: configura SQLite y reúne las tablas de la aplicación.
/// Las entidades del dominio del proyecto se añadirán aquí.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>EF Core recibe estas opciones desde Program.cs para conectar con SQLite.</summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>Tabla de vehículos gestionados en el taller.</summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

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

        modelBuilder.Entity<Vehicle>(entity =>
        {
            // Matrícula única
            entity.HasIndex(v => v.LicensePlate)
                  .IsUnique();

            // Relación N Vehicle -> 1 ApplicationUser (Propietario / Customer)
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(v => v.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}