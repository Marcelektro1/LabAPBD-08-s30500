using Tutorial8.Models.DTOs;
using Tutorial8.Utils;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<ClientTripsDTO> GetTripsFor(int clientId);
    Task<ClientDTO> CreateClient(NewClientDTO newClientDto);
    Task<ServiceResult> RegisterClientForTrip(int clientId, int tripId);
    Task<ServiceResult> UnregisterClientFromTrip(int clientId, int tripId);
}