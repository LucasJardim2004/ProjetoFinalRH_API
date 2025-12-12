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
        /// Primary type of person: SC = Store Contact, IN = Individual (retail) customer, SP = Sales person,
        /// EM = Employee (non-sales), VC = Vendor contact, GC = General contact
        /// </summary>
        [Required]
        public string PersonType { get; set; } = null!;

        /// <summary>
        /// 0 = Western style name order; 1 = Eastern style name order.
        /// </summary>
        public bool NameStyle { get; set; }

        /// <summary>
        /// A courtesy title. For example, Mr. or Ms.
        /// </summary>
        public string? Title { get; set; }

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

        /// <summary>
        /// Surname suffix. For example, Sr. or Jr.
        /// </summary>
        public string? Suffix { get; set; }

        /// <summary>
        /// Email promotions preference.
        /// </summary>
        public int EmailPromotion { get; set; }

        /// <summary>
        /// Additional contact information stored in XML format.
        /// </summary>
        public string? AdditionalContactInfo { get; set; }

        /// <summary>
        /// Personal information collected for sales analysis.
        /// </summary>
        public string? Demographics { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record.
        /// </summary>
        public Guid rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }
    }
}