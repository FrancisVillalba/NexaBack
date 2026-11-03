using System.Linq.Expressions;
using NexaBack.Infrastructure.Data.Context.Pagos.Entity;
using NexaBack.Infrastructure.Data.Dtos.Pagos;
namespace NexaBack.Repository.Pagos;
internal static class PagoProyeccion
{
    internal static readonly Expression<Func<Pago, PagoDto>> Dto = c => new(c.Id, "PAG-" + c.Id.ToString().PadLeft(6, '0'),
        c.CompraId, "OC-" + c.CompraId.ToString().PadLeft(6, '0'), c.Importe, c.FechaPago, c.MedioPago, c.Referencia,
        c.Observaciones, c.Estado, c.Version, c.AnuladoEn, c.MotivoAnulacion, c.CreadoEn, c.CreadoPor, c.ModificadoEn, c.ModificadoPor);
}
