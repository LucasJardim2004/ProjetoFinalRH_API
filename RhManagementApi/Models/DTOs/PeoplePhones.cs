using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.Models
{
    public class PeoplePhones
    {
        [Key]
        public int PhoneNumber {get;set;}
    }
}