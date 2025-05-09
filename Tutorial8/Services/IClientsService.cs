using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<ClientTripsDTO> GetTripsFor(int clientId);
    Task<ClientDTO> CreateClient(NewClientDTO newClientDto);
    Task RegisterClientForTrip(int clientId, int tripId);
    Task UnregisterClientFromTrip(int clientId, int tripId);
}