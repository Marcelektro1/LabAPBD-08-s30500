using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly ITripsService _tripsService;
        private readonly IClientsService _clientsService;

        public ClientsController(ITripsService tripsService, IClientsService clientsService)
        {
            _tripsService = tripsService;
            _clientsService = clientsService;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] NewClientDTO newClientDto)
        {
            try
            {
                var created = await _clientsService.CreateClient(newClientDto);
                return CreatedAtAction(nameof(GetClientTrips), new { id = created.IdClient }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            ClientTripsDTO res;
            try
            {
                res = await _clientsService.GetTripsFor(id);
            }
            catch (ArgumentException argumentException)
            {
                return NotFound(argumentException.Message);
            }

            if (res.Trips.Count == 0)
            {
                return NotFound(res);
            }
            
            return Ok(res);
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> CreateClientTripRegistration(int id, int tripId)
        {
            var res = await _clientsService.RegisterClientForTrip(id, tripId);

            if (!res.Success)
            {
                return BadRequest(res.Message);
            }
            
            return Created();
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientTripRegistration(int id, int tripId)
        {
            var res = await _clientsService.UnregisterClientFromTrip(id, tripId);

            if (!res.Success)
            {
                return BadRequest(res.Message);
            }
            
            return NoContent();
        }
    }
}
