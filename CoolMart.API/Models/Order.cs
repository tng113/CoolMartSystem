using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoolMart.API.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        [StringLength(30)]
        public string OrderCode { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string CashierUsername { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public int TotalItems { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        public int RewardPoints { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}