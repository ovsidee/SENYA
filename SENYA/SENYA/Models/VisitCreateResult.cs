namespace SENYA.Models;

public enum VisitCreateResult
{
    Success,
    AlreadyExists,
    ClientNotFound,
    MechanicNotFound,
    ServiceNotFound,
    InvalidData
}