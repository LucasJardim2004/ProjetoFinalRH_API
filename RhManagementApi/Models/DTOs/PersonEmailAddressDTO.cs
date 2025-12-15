using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.DTOs
{
    public class PersonEmailAddressDTO
    {
        public int? BusinessEntityID {get;set;}

        public int? EmailAddressID {get;set;}
        
        public string? EmailAddress {get;set;}
    }
}