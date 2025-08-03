using Dominio.Dtos;

namespace Aplicacao.Servicos;

public interface IPagamentoServico
{
    Task RealizaPagamento(PostPagamentoDto pagamento);
    Task<GetSumarioPagamentoDto> ObterSumario(DateTime? from = null, DateTime? to = null);
}