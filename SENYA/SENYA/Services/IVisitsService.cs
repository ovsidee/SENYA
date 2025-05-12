using SENYA.Models;
using SENYA.Models.DTO;

namespace SENYA.Services;

public interface IVisitsService
{
    Task<VisitCreateResult> CreateVisitAsync(VisitCreateDto dto, CancellationToken cancellationToken);
}