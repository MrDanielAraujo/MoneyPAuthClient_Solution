// Program.cs
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var defaultCulture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// Adicione serviços ao contêiner.
builder.Services.AddControllers(); // Ou AddEndpointsApiExplorer() se usar Minimal APIs
builder.Services.AddEndpointsApiExplorer();

// Register the Swagger generator
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
});

builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
    builder.WithExposedHeaders("Content-Disposition");
}));

var app = builder.Build();

// Configure o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("corsapp");

app.UseHttpsRedirection();

// Mapeie os endpoints (Minimal APIs) ou Controllers
app.MapControllers(); // Para APIs baseadas em Controllers

app.Run();
