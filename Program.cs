using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRACTICA_NRO_2_TEORIA_20262.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// IMPORTANTE: .AddRoles<IdentityRole>() es necesario para el Panel de Analista
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
    
builder.Services.AddControllersWithViews();

// --- CONFIGURACIONES AGREGADAS (Redis, Sesión y WebSockets) ---

// 1. Configurar Redis Cache (Pregunta 4)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "BancoApp_";
});

// 2. Configurar Sesiones (Pregunta 4)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Configurar WebSockets con SignalR (Pregunta 6)
builder.Services.AddSignalR();

// 4. Configurar RabbitMQ Consumer (Pregunta 7)
builder.Services.AddHostedService<PRACTICA_NRO_2_TEORIA_20262.Services.NotificacionesConsumerService>();
// --------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ACTIVAR LA SESIÓN (Debe ir entre UseRouting y UseAuthorization)
app.UseSession(); 

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// RUTA DEL HUB DE WEBSOCKETS (Pregunta 6)
app.MapHub<PRACTICA_NRO_2_TEORIA_20262.Hubs.SolicitudesHub>("/hubs/solicitudes");

app.MapRazorPages()
    .WithStaticAssets();

app.Run();