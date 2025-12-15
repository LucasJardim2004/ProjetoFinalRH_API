using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.DTOs
{
    public class PersonEmailAddressDTO
    {
        [Key]
        public int BusinessEntityID {get;set;}
        
        [Required]
        public string EmailAddress {get;set;}
    }
}