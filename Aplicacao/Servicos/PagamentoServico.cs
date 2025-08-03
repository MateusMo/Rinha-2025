using Aplicacao.ServicosExternos;
using Dominio.Dtos;
using Dominio.Enumeradores;
using Infraestrutura.CacheRepositorio;
using System.Text.Json;

namespace Aplicacao.Servicos;

public class PagamentoServico : IPagamentoServico
{
    private readonly IPagamentoServicoExterno _pagamentoServicoExterno;
    private readonly ICache _cache;

    public PagamentoServico(IPagamentoServicoExterno pagamentoServicoExterno, ICache cache)
    {
        _pagamentoServicoExterno = pagamentoServicoExterno;
        _cache = cache;
    }

    public async Task RealizaPagamento(PostPagamentoDto pagamento)
    {
        var dataRequisicao = DateTime.Now;
        bool pagamentoRealizado = false;
        ServicoEnum servicoUtilizado = ServicoEnum.Default;

        bool defaultEstaDisponivel = await _pagamentoServicoExterno.VerificaDisponibilidadeDefault();

        if (defaultEstaDisponivel)
        {
            pagamentoRealizado = await _pagamentoServicoExterno.RealizaPagamentoDefault(pagamento);
            servicoUtilizado = ServicoEnum.Default;
        }
        else
        {
            pagamentoRealizado = await _pagamentoServicoExterno.RealizaPagamentoFallback(pagamento);
            servicoUtilizado = ServicoEnum.Fallback;
        }

        if (!pagamentoRealizado)
        {
            pagamentoRealizado = await _pagamentoServicoExterno.RealizaPagamentoFallback(pagamento);
            servicoUtilizado = ServicoEnum.Fallback;
        }

        await GravaPagamento(pagamento, dataRequisicao, servicoUtilizado);
    }

    public async Task<GetSumarioPagamentoDto> ObterSumario(DateTime? from = null, DateTime? to = null)
    {
        var sumario = new GetSumarioPagamentoDto();
        
        // Como estamos usando cache simples, vamos buscar todos os pagamentos
        // Em produção, você usaria um banco de dados com consultas otimizadas
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

        sumario.Default.TotalRequests = defaultPayments.Count();
        sumario.Default.TotalAmount = defaultPayments.Sum(p => p.Valor);
        
        sumario.Fallback.TotalRequests = fallbackPayments.Count();
        sumario.Fallback.TotalAmount = fallbackPayments.Sum(p => p.Valor);

        return sumario;
    }

    private async Task GravaPagamento(PostPagamentoDto pagamento, DateTime dataRequisicao, ServicoEnum servico)
    {
        var domain = PostPagamentoDto.ToDomain(pagamento, dataRequisicao, servico);
        await _cache.SetAsync(domain.Correlacao.ToString(), domain, 1440); // 24 horas
        
        // Manter lista de IDs para facilitar recuperação
        var listaIds = await _cache.GetAsync<List<string>>("pagamentos_ids") ?? new List<string>();
        listaIds.Add(domain.Correlacao.ToString());
        await _cache.SetAsync("pagamentos_ids", listaIds, 1440);
    }

    private async Task<List<Dominio.Dominios.Pagamento>> ObterTodosPagamentos()
    {
        var listaIds = await _cache.GetAsync<List<string>>("pagamentos_ids") ?? new List<string>();
        var pagamentos = new List<Dominio.Dominios.Pagamento>();

        foreach (var id in listaIds)
        {
            var pagamento = await _cache.GetAsync<Dominio.Dominios.Pagamento>(id);
            if (pagamento != null)
                pagamentos.Add(pagamento);
        }

        return pagamentos;
    }
}