namespace Tutorial8.Models.DTOs;

public class ClientTripsDTO
{
    public ClientDTO Client { get; set; } // TODO: Don't print nulls when using not all fields in responses
    public List<TripWithPaymentInfoDTO> Trips { get; set; }
}

public class TripWithPaymentInfoDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime RegisteredAt { get; set; }
    public List<CountryDTO> Countries { get; set; }
}