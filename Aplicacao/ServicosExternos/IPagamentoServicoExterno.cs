using Dominio.Dtos;

namespace Aplicacao.ServicosExternos;

public interface IPagamentoServicoExterno
{
    Task<bool> VerificaDisponibilidadeDefault();
    Task<bool> RealizaPagamentoDefault(PostPagamentoDto pagamento);
    Task<bool> RealizaPagamentoFallback(PostPagamentoDto pagamento);
}