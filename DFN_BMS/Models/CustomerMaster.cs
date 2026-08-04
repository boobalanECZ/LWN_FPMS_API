using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFN_BMS.Models
{
    [Table("CUSTOMER_MASTER")]
    public class CustomerMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string CustomerCode { get; set; }   // manually entered by the user, e.g. CUS-0001

        [Required]
        [MaxLength(150)]
        public string CustomerName { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerDivision { get; set; }

        [Required]
        [MaxLength(20)]
        public string MobileNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string EmailId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}