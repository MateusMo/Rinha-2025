using Aplicacao.Servicos;
using Aplicacao.ServicosExternos;
using Infraestrutura.CacheRepositorio;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient configurado para alta performance
builder.Services.AddHttpClient<IPagamentoServicoExterno, PagamentoServicoExterno>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configuração do Redis (mais simples que Memcached para Docker)
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});

// Registrar serviços
builder.Services.AddScoped<ICache, Cache>();
builder.Services.AddScoped<IPagamentoServico, PagamentoServico>();
builder.Services.AddScoped<IPagamentoServicoExterno, PagamentoServicoExterno>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();