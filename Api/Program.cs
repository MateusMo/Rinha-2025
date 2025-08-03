// ============== Program.cs CORRIGIDO ==============
using Aplicacao.Servicos;
using Aplicacao.ServicosExternos;
using Infraestrutura.CacheRepositorio;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });

// HttpClient otimizado SIMPLES (sem Resilience que está causando problema)
builder.Services.AddHttpClient<IPagamentoServicoExterno, PagamentoServicoExterno>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.Add("User-Agent", "PaymentProcessor/1.0");
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Redis otimizado
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "PaymentProcessor";
});

// Registrar serviços
builder.Services.AddSingleton<ICache, Cache>();
builder.Services.AddSingleton<IPagamentoServicoExterno, PagamentoServicoExterno>();
builder.Services.AddScoped<IPagamentoServico, PagamentoServico>();

// Configurações de performance CORRIGIDAS
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.Http2.MaxStreamsPerConnection = 1000;
});

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();