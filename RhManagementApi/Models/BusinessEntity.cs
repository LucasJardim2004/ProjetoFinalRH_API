using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhManagementApi.Models
{
    [Table("BusinessEntity", Schema = "Person")]
    public class BusinessEntity
    {
        [Key]
        public int BusinessEntityID { get; set; }  // identity
    }

}
