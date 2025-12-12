using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.Models
{
    public class PeopleEmailAdresses
    {
        [Key]
        public int BusinessEntityID {get;set;}

        [Key]
        public int EmailAddressID {get;set;}
        
        [Required]
        public string EmailAddress {get;set;}

        [Required]
        public DateTime ModifiedDate {get;set;}


    }
}