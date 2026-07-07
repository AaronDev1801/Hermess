using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using Hermess.Hubs;
using Hermess.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

//Casi me vuelvo loco por este examen, peleando con supabase
//y el RLS, solo Dios y yo sabemos que paso aqui, porque copilot perdio la cabeza al intentarlo.
//Para que se de cuenta de la seriedad, yo no suelo comentar mi codigo,
//pero ahora si lo hice por necesidad de orden en el backtracking.
//ojala les guste.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//Configuración de Supabase
builder.Services.AddSingleton<Supabase.Client>(sp =>
{
    var url = builder.Configuration["Supabase:Url"];
    var key = builder.Configuration["Supabase:ServiceRoleKey"]; //Use la servicerolekey para saltarme el RLS
    return new Supabase.Client(url!, key!);
});

//CORS (permite peticiones del frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5031",
            "https://localhost:7031",
            "http://localhost:5000",
            "https://localhost:5001"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // Necesario para SignalR WebSockets
    });
});

//Testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//SignalR
builder.Services.AddSignalR();

//Controladores
builder.Services.AddControllers();

// Autenticación JWT con Supabase (Dolor de cabeza mas grande)
var supabaseJwtSecret = builder.Configuration["Supabase:JwtSecret"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://pdyeacbfznurpewcabmr.supabase.co/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(supabaseJwtSecret ?? "")
            )
        };

        //Permite que SignalR extraiga el token del query string (WebSockets)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

//Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Servir frontend desde wwwroot (ANTES del routing)
app.UseDefaultFiles();
app.UseStaticFiles();

// Orden correcto del pipeline
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

//Endpoint de prueba (Ya no sabia cual era la razon del error, si no llegaba a APi u otra cosa)
app.MapGet("/ping", () => "Hermess backend activo ✅")
   .WithName("Ping")
   .WithOpenApi();

//Controladores
app.MapControllers();

//SignalR Hub
app.MapHub<ChatHub>("/chatHub");

app.Run();
