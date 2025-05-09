using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;
using Tutorial8.Utils;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = """
                         SELECT Trip.IdTrip, Trip.Name, Description, DateFrom, DateTo, MaxPeople, C.IdCountry as "countryId", C.Name as "countryName"
                         FROM Trip
                         LEFT JOIN Country_Trip CT on Trip.IdTrip = CT.IdTrip
                         LEFT JOIN Country C on C.IdCountry = CT.IdCountry;
                         """;
        
        using (SqlConnection conn = new SqlConnection(DatabaseUtil.GetConnectionString()))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int? currTripId = null;
                TripDTO? curr = null;

                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetInt32("IdTrip");

                    if (currTripId != idOrdinal)
                    {
                        if (curr != null) trips.Add(curr);
                        
                        curr = new TripDTO()
                        {
                            Id = idOrdinal,
                            Name = reader.GetString("Name"),
                            Description = reader.GetString("Description"),
                            DateFrom = reader.GetDateTime("DateFrom"),
                            DateTo = reader.GetDateTime("DateTo"),
                            MaxPeople = reader.GetInt32("MaxPeople"),
                            Countries = new List<CountryDTO>()
                        };
                        
                        currTripId = idOrdinal;
                    }


                    if (curr == null) throw new ConstraintException("not possible state");
                    
                    curr.Countries.Add(new CountryDTO()
                    {
                        Name = reader.GetString("countryName"),
                        IdCountry = reader.GetInt32("countryId"),
                    });
                    
                }

                if (curr != null) trips.Add(curr);
            }
        }
        

        return trips;
    }
}