using System.Linq.Expressions;
using NexaBack.Infrastructure.Data.Context.Cobros.Entity;
using NexaBack.Infrastructure.Data.Dtos.Cobros;
namespace NexaBack.Repository.Cobros;
internal static class CobroProyeccion
{
    internal static readonly Expression<Func<Cobro, CobroDto>> Dto = c => new(c.Id, "COB-" + c.Id.ToString().PadLeft(6, '0'),
        c.VentaId, "VTA-" + c.VentaId.ToString().PadLeft(6, '0'), c.Importe, c.FechaCobro, c.MedioPago, c.Referencia,
        c.Observaciones, c.Estado, c.Version, c.AnuladoEn, c.MotivoAnulacion, c.CreadoEn, c.CreadoPor, c.ModificadoEn, c.ModificadoPor);
}
