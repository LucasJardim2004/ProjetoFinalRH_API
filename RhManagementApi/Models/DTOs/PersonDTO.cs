using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.DTOs
{
    public class PersonDTO
    {
    /// <summary>
    /// Human beings involved with AdventureWorks: employees, customer contacts, and vendor contacts.
    /// </summary>
    [Table("Person", Schema = "Person")]
    public partial class Person
    {
        /// <summary>
        /// Primary key for Person records.
        /// </summary>
        [Key]
        public int BusinessEntityID { get; set; }

        /// <summary>
        /// First name of the person.
        /// </summary>
        [Required]
        public string FirstName { get; set; } = null!;

        /// <summary>
        /// Middle name or middle initial of the person.
        /// </summary>
        public string? MiddleName { get; set; }

        /// <summary>
        /// Last name of the person.
        /// </summary>
        [Required]
        public string LastName { get; set; } = null!;
    }
    }
}