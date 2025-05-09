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

    public async Task<ServiceResult> RegisterClientForTrip(int clientId, int tripId) 
    {
        var checkClient = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        var tripParticipantCount = """
                                   SELECT T.IdTrip, T.MaxPeople, COUNT(CT.IdClient) as "participantCount"
                                   FROM Trip T
                                   INNER JOIN Client_Trip CT on T.IdTrip = CT.IdTrip
                                   WHERE CT.IdTrip = @IdTrip
                                   GROUP BY T.IdTrip, T.MaxPeople;
                                   """;
        var alreadySignedUpClient = "SELECT 1 FROM Client_Trip WHERE Client_Trip.IdClient = @IdClient AND Client_Trip.IdTrip = @IdTrip;";
        var insertNewEntry = """
                             INSERT INTO Client_Trip(IdClient, IdTrip, RegisteredAt, PaymentDate) 
                             VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL);
                             """;

        using (var conn = new SqlConnection(DatabaseUtil.GetConnectionString()))
        {
            await conn.OpenAsync();
            var transaction = await conn.BeginTransactionAsync() as SqlTransaction;

            if (transaction == null)
            {
                throw new ConstraintException("transaction is null");
            }

            try
            {
                await using var cmd = new SqlCommand(checkClient, conn, transaction);
                cmd.Parameters.AddWithValue("@IdClient", clientId);

                var clientCheck = await cmd.ExecuteScalarAsync();
                if (clientCheck == null)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResult(false, "Client does not exist.");
                }
                
                
                await using var cmdSignedUp = new SqlCommand(alreadySignedUpClient, conn, transaction);
                cmdSignedUp.Parameters.AddWithValue("@IdClient", clientId);
                cmdSignedUp.Parameters.AddWithValue("@IdTrip", tripId);
                
                var signedUpCheck = await cmdSignedUp.ExecuteScalarAsync();
                if (signedUpCheck != null)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResult(false, "Client is already signed up for that trip.");
                }
                
                
                await using var cmdTripCheck = new SqlCommand(tripParticipantCount, conn, transaction);
                cmdTripCheck.Parameters.AddWithValue("@IdTrip", tripId);
                
                await using var tripCheck = await cmdTripCheck.ExecuteReaderAsync();

                int currTripMaxPeople;
                int currTripParticipantCount;
                
                if (await tripCheck.ReadAsync()) 
                {
                    currTripMaxPeople = tripCheck.GetInt32("MaxPeople");
                    currTripParticipantCount = tripCheck.GetInt32("ParticipantCount");
                    tripCheck.Close();
                } 
                else
                {
                    tripCheck.Close();
                    await transaction.RollbackAsync();
                    return new ServiceResult(false, $"Trip with id={tripId} does not exist.");
                }


                if (currTripParticipantCount + 1 > currTripMaxPeople)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResult(false, "Trip has already reached the maximum number of participants.");
                }
                
                
                await using var cmdInsert = new SqlCommand(insertNewEntry, conn, transaction);
                cmdInsert.Parameters.AddWithValue("@IdClient", clientId);
                cmdInsert.Parameters.AddWithValue("@IdTrip", tripId);
                cmdInsert.Parameters.AddWithValue("@RegisteredAt", DateTime.Now.ToString("yyyyMMdd"));
                
                await cmdInsert.ExecuteNonQueryAsync();
                

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                conn.Close();
            }
        }

        return new ServiceResult(true, "Successfully registered client.");
    }

    public async Task<ServiceResult> UnregisterClientFromTrip(int clientId, int tripId)
    {
        var alreadySignedUpClient = "SELECT 1 FROM Client_Trip WHERE Client_Trip.IdClient = @IdClient AND Client_Trip.IdTrip = @IdTrip;";
        var deleteEntry = """
                          DELETE FROM Client_Trip
                          WHERE Client_Trip.IdClient = @IdClient AND Client_Trip.IdTrip = @IdTrip;
                          """;

        using (var conn = new SqlConnection(DatabaseUtil.GetConnectionString()))
        {
            await conn.OpenAsync();
            var transaction = await conn.BeginTransactionAsync() as SqlTransaction;

            if (transaction == null)
            {
                throw new ConstraintException("transaction is null");
            }

            try
            {
                await using var cmdSignedUp = new SqlCommand(alreadySignedUpClient, conn, transaction);
                cmdSignedUp.Parameters.AddWithValue("@IdClient", clientId);
                cmdSignedUp.Parameters.AddWithValue("@IdTrip", tripId);
                
                var signedUpCheck = await cmdSignedUp.ExecuteScalarAsync();
                if (signedUpCheck == null)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResult(false, "Client is not signed up for that trip.");
                }
                
                await using var cmd = new SqlCommand(deleteEntry, conn, transaction);
                cmd.Parameters.AddWithValue("@IdClient", clientId);
                cmd.Parameters.AddWithValue("@IdTrip", tripId);
                
                await cmd.ExecuteNonQueryAsync();
                await transaction.CommitAsync();

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                conn.Close();
            }
        }
        
        return new ServiceResult(true, "Successfully unregistered client.");
    }
    
}