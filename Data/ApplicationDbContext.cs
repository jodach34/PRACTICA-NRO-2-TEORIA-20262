using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PRACTICA_NRO_2_TEORIA_20262.Models;

namespace PRACTICA_NRO_2_TEORIA_20262.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Nuestras tablas
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<SolicitudCredito> Solicitudes { get; set; }
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Restricción EF Core: Un cliente solo puede tener una solicitud Pendiente (Estado = 0)
        builder.Entity<SolicitudCredito>()
            .HasIndex(s => s.ClienteId)
            .IsUnique()
            .HasFilter("\"Estado\" = 0");

        // --- DATOS INICIALES (SEED) ---

        // 1. Crear Rol Analista
        var analistaRoleId = "rol-analista-id";
        builder.Entity<IdentityRole>().HasData(new IdentityRole
        {
            Id = analistaRoleId,
            Name = "Analista",
            NormalizedName = "ANALISTA"
        });

        // 2. Crear Usuarios (1 Analista y 2 Clientes)
        var hasher = new PasswordHasher<IdentityUser>();
        var analistaId = "user-analista-id";
        var cliente1Id = "user-cliente1-id";
        var cliente2Id = "user-cliente2-id";

        builder.Entity<IdentityUser>().HasData(
            new IdentityUser { Id = analistaId, UserName = "analista@banco.com", NormalizedUserName = "ANALISTA@BANCO.COM", Email = "analista@banco.com", NormalizedEmail = "ANALISTA@BANCO.COM", EmailConfirmed = true, PasswordHash = hasher.HashPassword(null, "Password123!") },
            new IdentityUser { Id = cliente1Id, UserName = "cliente1@banco.com", NormalizedUserName = "CLIENTE1@BANCO.COM", Email = "cliente1@banco.com", NormalizedEmail = "CLIENTE1@BANCO.COM", EmailConfirmed = true, PasswordHash = hasher.HashPassword(null, "Password123!") },
            new IdentityUser { Id = cliente2Id, UserName = "cliente2@banco.com", NormalizedUserName = "CLIENTE2@BANCO.COM", Email = "cliente2@banco.com", NormalizedEmail = "CLIENTE2@BANCO.COM", EmailConfirmed = true, PasswordHash = hasher.HashPassword(null, "Password123!") }
        );

        // 3. Asignar Rol al Analista
        builder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string> { RoleId = analistaRoleId, UserId = analistaId }
        );

        // 4. Crear 2 Clientes
        builder.Entity<Cliente>().HasData(
            new Cliente { Id = 1, UsuarioId = cliente1Id, IngresosMensuales = 2000m, Activo = true },
            new Cliente { Id = 2, UsuarioId = cliente2Id, IngresosMensuales = 3000m, Activo = true }
        );

        // 5. Crear 2 Solicitudes (Una Pendiente y Una Aprobada)
    builder.Entity<SolicitudCredito>().HasData(
        new SolicitudCredito { Id = 1, ClienteId = 1, MontoSolicitado = 5000m, FechaSolicitud = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), Estado = EstadoSolicitud.Pendiente },
        new SolicitudCredito { Id = 2, ClienteId = 2, MontoSolicitado = 1000m, FechaSolicitud = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc), Estado = EstadoSolicitud.Aprobado }
    );
    }
}