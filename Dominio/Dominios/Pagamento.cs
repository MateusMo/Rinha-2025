using System.Runtime.InteropServices.JavaScript;
using Dominio.Enumeradores;

namespace Dominio.Dominios;

public class Pagamento
{
    public Guid Correlacao { get; set; }
    public decimal Valor { get; set; }
    public ServicoEnum Servico { get; set; }
    public DateTime ProcessadoEm { get; set; }
    public DateTime RequisitadoEm { get; set; }
}