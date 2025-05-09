using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    // private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    // private readonly string _connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";
    private readonly string _connectionString = "Data Source=127.0.0.1\\db-mssql,1433;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = """
                         SELECT Trip.IdTrip, Trip.Name, Description, DateFrom, DateTo, MaxPeople, C.IdCountry as "countryId", C.Name as "countryName"
                         FROM Trip
                         LEFT JOIN s30500.Country_Trip CT on Trip.IdTrip = CT.IdTrip
                         LEFT JOIN s30500.Country C on C.IdCountry = CT.IdCountry;
                         """;
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
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