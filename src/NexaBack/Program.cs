using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NexaBack.Infrastructure.Persistence;
using NexaBack;
using NexaBack.Common;
using NexaBack.Infrastructure.Security;
using NexaBack.Infrastructure.Documentation;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Archivo privado local: no se versiona ni se publica. Evita depender del perfil de IIS Express.
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
    // Mantener la prioridad de las variables de entorno y de los argumentos de ejecución.
    builder.Configuration.AddEnvironmentVariables();
    if (args.Length > 0) builder.Configuration.AddCommandLine(args);
}

// Add services to the container.

builder.Services.AddControllers();
if (builder.Environment.IsDevelopment()) builder.Services.AddNexaSwagger();
builder.Services.AddBusinessServices();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
builder.Services.AddNexaAuthentication(builder.Configuration);
builder.Services.AddDbContext<UsuariosDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("NexaDB")
        ?? throw new InvalidOperationException("Configurá ConnectionStrings:NexaDB en Secretos de usuario o en el entorno.");
    options.UseNpgsql(connectionString);
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "NEXA Backend API v1");
        options.DocumentTitle = "NEXA · Swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
