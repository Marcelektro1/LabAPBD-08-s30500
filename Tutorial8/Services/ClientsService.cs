using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;
using Tutorial8.Utils;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    public async Task<ClientTripsDTO> GetTripsFor(int clientId)
    {
        var trips = new List<TripWithPaymentInfoDTO>();

        ClientDTO? clientDto = null;
        string command = """
                         SELECT C.IdClient, C.FirstName as "clientFirstName", C.LastName as "clientLastName", 
                                T.IdTrip, T.Name, Description, DateFrom, DateTo, MaxPeople,
                                CTR.PaymentDate, CTR.RegisteredAt,
                                CO.IdCountry as "countryId", CO.Name as "countryName"
                         FROM Client C
                              LEFT JOIN Client_Trip CTR on C.IdClient = CTR.IdClient
                              LEFT JOIN Trip T on CTR.IdTrip = T.IdTrip
                              LEFT JOIN Country_Trip COTR on T.IdTrip = COTR.IdTrip
                              LEFT JOIN Country CO on CO.IdCountry = COTR.IdCountry
                         WHERE C.IdClient = @IdClient;
                         """;
        
        using (SqlConnection conn = new SqlConnection(DatabaseUtil.GetConnectionString()))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();
            
            cmd.Parameters.AddWithValue("@IdClient", clientId);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int? currTripId = null;
                TripWithPaymentInfoDTO? curr = null;
                
                while (await reader.ReadAsync())
                {
                    if (clientDto == null)
                    {
                        if (reader.IsDBNull("IdClient"))
                        {
                            clientDto = null;
                            break;
                        }

                        clientDto = new ClientDTO()
                        {
                            IdClient = reader.GetInt32("IdClient"),
                            FirstName = reader.GetString("clientFirstName"),
                            LastName = reader.GetString("clientLastName")
                        };
                    }

                    if (reader.IsDBNull("IdTrip"))
                    {
                        continue; // no trip, so skip! :D
                    }
                    
                    
                    int idOrdinal = reader.GetInt32("IdTrip");

                    if (currTripId != idOrdinal)
                    {
                        if (curr != null) trips.Add(curr);
                        
                        curr = new TripWithPaymentInfoDTO()
                        {
                            Id = idOrdinal,
                            Name = reader.GetString("Name"),
                            Description = reader.GetString("Description"),
                            DateFrom = reader.GetDateTime("DateFrom"),
                            DateTo = reader.GetDateTime("DateTo"),
                            MaxPeople = reader.GetInt32("MaxPeople"),
                            PaymentDate = DateTime.ParseExact(
                                reader.GetInt32("PaymentDate").ToString(),
                                "yyyyMMdd",
                                CultureInfo.InvariantCulture
                                ),
                            RegisteredAt = DateTime.ParseExact(
                                reader.GetInt32("RegisteredAt").ToString(),
                                "yyyyMMdd",
                                CultureInfo.InvariantCulture
                            ),
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

        if (clientDto == null)
        {
            throw new ArgumentException("No client for provided Id exists.");
        }

        return new ClientTripsDTO()
        {
            Client = clientDto,
            Trips = trips
        };
    }

    public async Task<ClientDTO> CreateClient(NewClientDTO newClientDto)
    {
        ClientDTO? clientDto;

        string createQuery = """
                             INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) 
                             VALUES ('@FirstName', '@LastName', '@Email', '@Telephone', '@Pesel');
                             SELECT SCOPE_IDENTITY();
                             """;

        using (SqlConnection conn = new SqlConnection(DatabaseUtil.GetConnectionString()))
        using (SqlCommand cmd = new SqlCommand(createQuery, conn))
        {
            await conn.OpenAsync();

            cmd.Parameters.AddWithValue("@FirstName", newClientDto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", newClientDto.LastName);
            cmd.Parameters.AddWithValue("@Email", newClientDto.Email);
            cmd.Parameters.AddWithValue("@Telephone", newClientDto.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", newClientDto.Pesel);

            var res = await cmd.ExecuteScalarAsync();
            
            if (res == null)
                throw new ConstraintException("could not create new client.");

            var resInt = Convert.ToInt32(res);

            clientDto = new ClientDTO()
            {
                IdClient = resInt,
                FirstName = newClientDto.FirstName,
                LastName = newClientDto.LastName,
                Email = newClientDto.Email,
                Telephone = newClientDto.Telephone,
                Pesel = newClientDto.Pesel,

            };
            
        }

        return clientDto;
    }

    public Task RegisterClientForTrip(int clientId, int tripId)
    {
        throw new NotImplementedException();
    }

    public Task UnregisterClientFromTrip(int clientId, int tripId)
    {
        throw new NotImplementedException();
    }
}