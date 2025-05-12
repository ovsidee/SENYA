using Microsoft.AspNetCore.Mvc;
using SENYA.Models;
using SENYA.Models.DTO;
using SENYA.Services;

namespace SENYA.Controllers;


[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private readonly IVisitsService _visitsService;

    public VisitsController(IVisitsService visitsService)
    {
        _visitsService = visitsService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateVisit([FromBody] VisitCreateDto visitDto, CancellationToken cancellationToken)
    {
        var result = await _visitsService.CreateVisitAsync(visitDto, cancellationToken);

        return result switch
        {
            VisitCreateResult.AlreadyExists => Conflict($"Visit with ID {visitDto.VisitId} already exists."),
            VisitCreateResult.ClientNotFound => NotFound($"Client with ID {visitDto.ClientId} not found."),
            VisitCreateResult.MechanicNotFound => NotFound($"Mechanic with license number '{visitDto.MechanicLicenceNumber}' not found."),
            VisitCreateResult.ServiceNotFound => BadRequest("One or more services do not exist."),
            VisitCreateResult.InvalidData => BadRequest("Invalid visit data."),
            VisitCreateResult.Success => CreatedAtAction(nameof(CreateVisit), new { id = visitDto.VisitId }, null),
            _ => StatusCode(500, "An unknown error occurred.")
        };
    }
}