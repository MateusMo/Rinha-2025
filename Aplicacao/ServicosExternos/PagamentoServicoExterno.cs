using System.Text;
using System.Text.Json;
using Dominio.Dtos;
using System.Collections.Concurrent;

namespace Aplicacao.ServicosExternos;

public class PagamentoServicoExterno : IPagamentoServicoExterno
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, (bool IsAvailable, DateTime LastCheck)> _serviceHealth = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PagamentoServicoExterno(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> VerificaDisponibilidadeDefault()
    {
        const string key = "default";
        if (_serviceHealth.TryGetValue(key, out var health) && 
            health.LastCheck.AddSeconds(5) > DateTime.UtcNow)
        {
            return health.IsAvailable;
        }

        bool isAvailable = await CheckServiceHealthInternal("http://payment-processor-default:8080");
        _serviceHealth.AddOrUpdate(key, (isAvailable, DateTime.UtcNow), (k, v) => (isAvailable, DateTime.UtcNow));
        
        return isAvailable;
    }

    public async Task<bool> RealizaPagamentoDefault(PostPagamentoDto pagamento)
    {
        return await RealizaPagamento("http://payment-processor-default:8080/payments", pagamento);
    }

    public async Task<bool> RealizaPagamentoFallback(PostPagamentoDto pagamento)
    {
        return await RealizaPagamento("http://payment-processor-fallback:8080/payments", pagamento);
    }

    private async Task<bool> CheckServiceHealthInternal(string baseUrl)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{baseUrl}/payments/service-health");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<HealthResponse>(content, JsonOptions);
                return health?.Failing == false;
            }
        }
        catch
        {
            // Log error if needed
        }
        return false;
    }

    private async Task<bool> RealizaPagamento(string url, PostPagamentoDto pagamento)
    {
        try
        {
            var payload = new
            {
                correlationId = pagamento.CorrelationId,
                amount = pagamento.Amount,
                requestedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class HealthResponse
{
    public bool Failing { get; set; }
    public int MinResponseTime { get; set; }
}