using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.Models
{
    [Table("Person", Schema = "Person")]
    public partial class Person
    {
        [Key]
        public int BusinessEntityID { get; set; }

        [Required]
        public string PersonType { get; set; } = null!;

        public bool NameStyle { get; set; } = false;

        public string? Title { get; set; }

        [Required]
        public string FirstName { get; set; } = null!;

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = null!;

        public string? Suffix { get; set; }

        public int EmailPromotion { get; set; } = 0;

        public string? AdditionalContactInfo { get; set; }
        public string? Demographics { get; set; }

        // REQUIRED by AdventureWorks schema
        public Guid rowguid { get; set; } = Guid.NewGuid();
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}
