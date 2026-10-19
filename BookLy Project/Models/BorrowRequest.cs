using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookLy_Project.Models
{
    public class BorrowRequest
    {
        [Key]
        public int BorrowId { get; set; }

        public int? UserId { get; set; }
        public DateTime? RequestedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Status { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public User? Approver { get; set; }

        public ICollection<BorrowDetail> BorrowDetails { get; set; }
            = new List<BorrowDetail>();
    }
}