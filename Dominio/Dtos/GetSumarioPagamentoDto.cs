namespace Dominio.Dtos;

public class GetSumarioPagamentoDto
{
    public ProcessorSummary Default { get; set; } = new();
    public ProcessorSummary Fallback { get; set; } = new();
}

public class ProcessorSummary
{
    public int TotalRequests { get; set; }
    public decimal TotalAmount { get; set; }
}