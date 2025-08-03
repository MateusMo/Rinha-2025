// Dominio/Dtos/PostPagamentoDto.cs
using Dominio.Dominios;
using Dominio.Enumeradores;

namespace Dominio.Dtos;

public class PostPagamentoDto
{
    public Guid CorrelationId { get; set; }
    public decimal Amount { get; set; }

    public static Pagamento ToDomain(PostPagamentoDto pagamento, DateTime dataRequisicao, ServicoEnum servico)
    {
        return new Pagamento()
        {
            Correlacao = pagamento.CorrelationId, 
            ProcessadoEm = DateTime.Now,
            RequisitadoEm = dataRequisicao,
            Servico = servico,
            Valor = pagamento.Amount
        };
    }
}