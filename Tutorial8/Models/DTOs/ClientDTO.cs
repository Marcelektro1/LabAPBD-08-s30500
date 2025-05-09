using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;

public class ClientDTO
{
    public int IdClient { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
}

public class NewClientDTO
{
    [Required]
    [StringLength(120)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(120)]
    public string LastName { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(120)]
    public string Email { get; set; }
    
    [Required]
    [Phone]
    [StringLength(120)]
    public string Telephone { get; set; }
    
    [Required]
    [StringLength(120)]
    public string Pesel { get; set; }
    
}
