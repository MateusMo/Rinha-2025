using Aplicacao.ServicosExternos;
using Dominio.Dtos;
using Dominio.Enumeradores;
using Infraestrutura.CacheRepositorio;
using System.Collections.Concurrent;

namespace Aplicacao.Servicos;

public class PagamentoServico : IPagamentoServico
{
    private readonly IPagamentoServicoExterno _pagamentoServicoExterno;
    private readonly ICache _cache;
    private readonly ConcurrentDictionary<ServicoEnum, (int Count, decimal Amount)> _memoryCounters = new();

    public PagamentoServico(IPagamentoServicoExterno pagamentoServicoExterno, ICache cache)
    {
        _pagamentoServicoExterno = pagamentoServicoExterno;
        _cache = cache;
        
        // Inicializar contadores
        _memoryCounters.TryAdd(ServicoEnum.Default, (0, 0m));
        _memoryCounters.TryAdd(ServicoEnum.Fallback, (0, 0m));
    }
    public async Task RealizaPagamento(PostPagamentoDto pagamento)
    {
        var dataRequisicao = DateTime.UtcNow;
        ServicoEnum servicoUtilizado = ServicoEnum.Default;
        bool sucesso = false;

        // Estratégia otimizada: tentar default primeiro, se falhar tentar fallback
        var defaultHealth = await _pagamentoServicoExterno.VerificaDisponibilidadeDefault();

        if (defaultHealth)
        {
            sucesso = await _pagamentoServicoExterno.RealizaPagamentoDefault(pagamento);
            servicoUtilizado = ServicoEnum.Default;
        }

        if (!sucesso)
        {
            sucesso = await _pagamentoServicoExterno.RealizaPagamentoFallback(pagamento);
            servicoUtilizado = ServicoEnum.Fallback;
        }

        if (sucesso)
        {
            // Atualizar contadores em memória - CORRIGIDO
            _memoryCounters.AddOrUpdate(servicoUtilizado, 
                (1, pagamento.Amount),
                (key, existingValue) => (existingValue.Count + 1, existingValue.Amount + pagamento.Amount));

            // Gravar no cache de forma assíncrona (fire-and-forget)
            _ = Task.Run(async () => 
            {
                try
                {
                    await GravaPagamento(pagamento, dataRequisicao, servicoUtilizado);
                }
                catch
                {
                    // Ignore errors in background task
                }
            });
        }
    }

    public async Task<GetSumarioPagamentoDto> ObterSumario(DateTime? from = null, DateTime? to = null)
    {
        // Se não há filtro por data, usar contadores em memória
        if (from == null && to == null)
        {
            // CORRIGIDO - Usar variáveis explícitas para evitar problema do compilador
            var defaultExists = _memoryCounters.TryGetValue(ServicoEnum.Default, out var defStats);
            var fallbackExists = _memoryCounters.TryGetValue(ServicoEnum.Fallback, out var fallStats);
            
            var defaultStats = defaultExists ? defStats : (Count: 0, Amount: 0m);
            var fallbackStats = fallbackExists ? fallStats : (Count: 0, Amount: 0m);

            return new GetSumarioPagamentoDto
            {
                Default = new ProcessorSummary 
                { 
                    TotalRequests = defaultStats.Count, 
                    TotalAmount = defaultStats.Amount 
                },
                Fallback = new ProcessorSummary 
                { 
                    TotalRequests = fallbackStats.Count, 
                    TotalAmount = fallbackStats.Amount 
                }
            };
        }

        // Para consultas com filtro de data, usar cache
        return await ObterSumarioComFiltro(from, to);
    }

    private async Task<GetSumarioPagamentoDto> ObterSumarioComFiltro(DateTime? from, DateTime? to)
    {
        // Implementação otimizada com batch get do Redis
        var cacheKey = $"summary_{from?.Ticks ?? 0}_{to?.Ticks ?? 0}";
        var cachedSummary = await _cache.GetAsync<GetSumarioPagamentoDto>(cacheKey);
        
        if (cachedSummary != null)
            return cachedSummary;

        // Se não está em cache, calcular (isso será lento, mas é edge case)
        var pagamentos = await ObterTodosPagamentos();
        
        if (from.HasValue || to.HasValue)
        {
            pagamentos = pagamentos.Where(p => 
                (!from.HasValue || p.RequisitadoEm >= from.Value) &&
                (!to.HasValue || p.RequisitadoEm <= to.Value)
            ).ToList();
        }

        var defaultPayments = pagamentos.Where(p => p.Servico == ServicoEnum.Default);
        var fallbackPayments = pagamentos.Where(p => p.Servico == ServicoEnum.Fallback);

        var summary = new GetSumarioPagamentoDto
        {
            Default = new ProcessorSummary
            {
                TotalRequests = defaultPayments.Count(),
                TotalAmount = defaultPayments.Sum(p => p.Valor)
            },
            Fallback = new ProcessorSummary
            {
                TotalRequests = fallbackPayments.Count(),
                TotalAmount = fallbackPayments.Sum(p => p.Valor)
            }
        };

        // Cache por 30 segundos
        await _cache.SetAsync(cacheKey, summary, 1);
        return summary;
    }

    private async Task GravaPagamento(PostPagamentoDto pagamento, DateTime dataRequisicao, ServicoEnum servico)
    {
        try
        {
            var domain = PostPagamentoDto.ToDomain(pagamento, dataRequisicao, servico);
            await _cache.SetAsync(domain.Correlacao.ToString(), domain, 1440);
            
            // Manter lista de IDs de forma mais eficiente
            var listaIds = await _cache.GetAsync<HashSet<string>>("pagamentos_ids") ?? new HashSet<string>();
            listaIds.Add(domain.Correlacao.ToString());
            await _cache.SetAsync("pagamentos_ids", listaIds, 1440);
        }
        catch
        {
            // Log error but don't throw
        }
    }

    private async Task<List<Dominio.Dominios.Pagamento>> ObterTodosPagamentos()
    {
        try
        {
            var listaIds = await _cache.GetAsync<HashSet<string>>("pagamentos_ids") ?? new HashSet<string>();
            var pagamentos = new List<Dominio.Dominios.Pagamento>();

            // Processar em lotes para melhor performance
            var tasks = listaIds.Select(async id =>
            {
                var pagamento = await _cache.GetAsync<Dominio.Dominios.Pagamento>(id);
                return pagamento;
            });

            var results = await Task.WhenAll(tasks);
            return results.Where(p => p != null).ToList();
        }
        catch
        {
            return new List<Dominio.Dominios.Pagamento>();
        }
    }
}