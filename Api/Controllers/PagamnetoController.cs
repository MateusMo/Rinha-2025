using Aplicacao.Servicos;
using Dominio.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class PagamentoController : ControllerBase
{
    private readonly IPagamentoServico _pagamentoServico;

    public PagamentoController(IPagamentoServico pagamentoServico)
    {
        _pagamentoServico = pagamentoServico;
    }

    [HttpPost("/payments")]
    public async Task<IActionResult> PostPagamento([FromBody] PostPagamentoDto pagamento)
    {
        await _pagamentoServico.RealizaPagamento(pagamento);
        return Ok();
    }

    [HttpGet("/payments-summary")]
    public async Task<IActionResult> GetSumario([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var sumario = await _pagamentoServico.ObterSumario(from, to);
        return Ok(sumario);
    }
}