using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.Models
{
    public class PeoplePhones
    {
        [Key]
        public int BusinessEntityID {get;set;}

        [Key]
        public int PhoneNumber {get;set;}

        [Key]
        public int PhoneNumberTypeID {get;set;}

        public DateTime ModifiedDate {get;set;}
    }
}