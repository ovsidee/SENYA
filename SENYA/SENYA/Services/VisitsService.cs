using Microsoft.Data.SqlClient;
using SENYA.Models;
using SENYA.Models.DTO;

namespace SENYA.Services;

public class VisitsService : IVisitsService
{
    private readonly IConfiguration _configuration;
    
    public VisitsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<VisitCreateResult> CreateVisitAsync(VisitCreateDto dto, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("ConnectionString"));
        await con.OpenAsync(cancellationToken);

        // 1. Check visit ID 
        await using (var checkVisitCmd = new SqlCommand("SELECT COUNT(1) FROM Visit WHERE visit_id = @VisitId", con))
        {
            checkVisitCmd.Parameters.AddWithValue("@VisitId", dto.VisitId);
            
            var exists = (int)await checkVisitCmd.ExecuteScalarAsync(cancellationToken) > 0;
            
            if (exists)
                return VisitCreateResult.AlreadyExists;
        }

        // 2. Check client ID
        await using (var checkClientCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE client_id = @ClientId", con))
        {
            checkClientCmd.Parameters.AddWithValue("@ClientId", dto.ClientId);
            
            var exists = (int)await checkClientCmd.ExecuteScalarAsync(cancellationToken) > 0;
            
            if (!exists)
                return VisitCreateResult.ClientNotFound;
        }

        // 3. get mechanic ID 
        int? mechanicId = null;
        await using (var getMechanicCmd = new SqlCommand("SELECT mechanic_id FROM Mechanic WHERE licence_number = @LicenceNumber", con))
        {
            getMechanicCmd.Parameters.AddWithValue("@LicenceNumber", dto.MechanicLicenceNumber);
            
            var result = await getMechanicCmd.ExecuteScalarAsync(cancellationToken);
            
            if (result == null)
                return VisitCreateResult.MechanicNotFound;
            mechanicId = (int)result;
        }

        // 4. check service names 
        var serviceIds = new Dictionary<string, int>();
        foreach (var service in dto.Services)
        {
            await using var checkServiceCmd = new SqlCommand("SELECT service_id FROM Service WHERE name = @Name", con);
            checkServiceCmd.Parameters.AddWithValue("@Name", service.ServiceName);
            var result = await checkServiceCmd.ExecuteScalarAsync(cancellationToken);
            
            if (result == null)
                return VisitCreateResult.ServiceNotFound;

            serviceIds[service.ServiceName] = (int)result;
        }

        // 5. Insert into Visit 
        await using (var insertVisitCmd = new SqlCommand(@"
            INSERT INTO Visit (visit_id, client_id, mechanic_id, date)
            VALUES (@VisitId, @ClientId, @MechanicId, @Date)", con))
        {
            insertVisitCmd.Parameters.AddWithValue("@VisitId", dto.VisitId);
            insertVisitCmd.Parameters.AddWithValue("@ClientId", dto.ClientId);
            insertVisitCmd.Parameters.AddWithValue("@MechanicId", mechanicId!.Value);
            insertVisitCmd.Parameters.AddWithValue("@Date", DateTime.Now);

            await insertVisitCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 6. Insert into Visit_Service 
        foreach (var service in dto.Services)
        {
            int serviceId = serviceIds[service.ServiceName];

            await using var insertVisitServiceCmd = new SqlCommand(@"
                INSERT INTO Visit_Service (visit_id, service_id, service_fee)
                VALUES (@VisitId, @ServiceId, @Fee)", con);
            insertVisitServiceCmd.Parameters.AddWithValue("@VisitId", dto.VisitId);
            insertVisitServiceCmd.Parameters.AddWithValue("@ServiceId", serviceId);
            insertVisitServiceCmd.Parameters.AddWithValue("@Fee", service.ServiceFee);

            await insertVisitServiceCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        return VisitCreateResult.Success;
    }

}