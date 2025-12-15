using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.DTOs
{
    public class PersonEmailAdressDTO
    {
        [Key]
        public int BusinessEntityID {get;set;}
        
        [Required]
        public string EmailAddress {get;set;}
    }
}