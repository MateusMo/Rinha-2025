// Aplicacao/ServicosExternos/PagamentoServicoExterno.cs
using System.Text;
using System.Text.Json;
using Dominio.Dtos;

namespace Aplicacao.ServicosExternos;

public class PagamentoServicoExterno : IPagamentoServicoExterno
{
    private readonly HttpClient _httpClient;
    private DateTime _ultimaVezQueADisponibilidadeFoiVerificada = DateTime.MinValue;
    private bool _ultimoStatusBooleanoDeDisponibilidade = false;

    public PagamentoServicoExterno(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> VerificaDisponibilidadeDefault()
    {
        if (_ultimaVezQueADisponibilidadeFoiVerificada.AddSeconds(5) > DateTime.Now)
            return _ultimoStatusBooleanoDeDisponibilidade;

        try
        {
            var response = await _httpClient.GetAsync("http://payment-processor-default:8080/payments/service-health");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                _ultimoStatusBooleanoDeDisponibilidade = !health.Failing;
            }
            else
            {
                _ultimoStatusBooleanoDeDisponibilidade = false;
            }
        }
        catch
        {
            _ultimoStatusBooleanoDeDisponibilidade = false;
        }

        _ultimaVezQueADisponibilidadeFoiVerificada = DateTime.Now;
        return _ultimoStatusBooleanoDeDisponibilidade;
    }

    public async Task<bool> RealizaPagamentoDefault(PostPagamentoDto pagamento)
    {
        return await RealizaPagamento("http://payment-processor-default:8080/payments", pagamento);
    }

    public async Task<bool> RealizaPagamentoFallback(PostPagamentoDto pagamento)
    {
        return await RealizaPagamento("http://payment-processor-fallback:8080/payments", pagamento);
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

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
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