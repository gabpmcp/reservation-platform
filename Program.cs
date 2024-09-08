using static ReservationPlatform.Business.Decisions;
using static ReservationPlatform.Helpers.KafkaConsumer;
using static ReservationPlatform.CQRS.Commands;
using static ReservationPlatform.Validations.Validations;
using ReservationPlatform.CQRS;
using ReservationPlatform.Helpers;
using ReservationPlatform.Utils;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Immutable;
using Prometheus;
using Serilog;
using Serilog.Sinks.Loki;
using static ReservationPlatform.Helpers.RedisCache;
using static ReservationPlatform.Business.Result;

var builder = WebApplication.CreateBuilder(args);

// Configura Serilog para usar Loki
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Escribe también en la consola
    .WriteTo.LokiHttp("http://loki.default.svc.cluster.local:3100") // URL de Loki
    .CreateLogger();

builder.Host.UseSerilog(); // Usar Serilog como logger

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "http://localhost:5000"; // Reemplaza con tu proveedor de identidad
    options.Audience = "api1"; // Reemplaza con el público que utiliza tu API
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Rutas de métricas
app.UseRouting();
app.UseHttpMetrics(); // Métricas automáticas de HTTP

// Endpoint para Prometheus
app.MapMetrics(); // Expone las métricas en /metrics

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var eventsProducer = CommandHandler(
    GetAggregate(),
    ProduceMessage<Event>("localhost:9092", "events-topic")(JsonSerialization.Serialize),
    ProduceMessage<Failure>("localhost:9092", "errors-topic")(JsonSerialization.Serialize),
    SetState
);

var eventsConsumer = ConsumeEventsWithState(eventsProducer, JsonSerialization.Deserialize<Command>()(UnsupportedCommand));

// En el contexto de una aplicación que se está iniciando
// await eventsConsumer("reservation-events", "localhost:9092", "reservation-event-consumer-group");

app.MapPost("/command", [Authorize] (HttpContext context, JsonElement jsonRequest) =>
{
    var kind = jsonRequest.GetProperty("kind").GetString();
    var data = jsonRequest.GetProperty("data").EnumerateObject()
        .ToImmutableDictionary(prop => prop.Name, prop => (object)prop.Value.ToString());

    // Validar el diccionario de entrada antes de convertirlo en un comando
    var validationResult = ValidateSchema(kind ?? string.Empty, data, CommandSchema);

    // Si la validación falla, retornar un BadRequest con los errores
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.ErrorMessages);
    }

    // Si la validación pasa, crear el comando
    var command = BuildCommand(data);

    // Producir
    eventsProducer(data.Get<string>("userId"), command);

    return Results.Ok(command);
});

app.Run();
