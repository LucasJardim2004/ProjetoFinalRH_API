using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.Models
{
    public class PersonPhoneDTO
    {
        
        public int? BusinessEntityID {get;set;}
        
        public string? PhoneNumber {get;set;}
        public int? PhoneNumberTypeID {get;set;}
    }
}