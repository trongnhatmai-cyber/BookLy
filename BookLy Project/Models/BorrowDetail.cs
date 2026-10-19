using System.ComponentModel.DataAnnotations.Schema;

namespace BookLy_Project.Models
{
    public class BorrowDetail
    {
        public int BorrowDetailId { get; set; }
        public int? BorrowId { get; set; }
        public int? BookId { get; set; }
        public int? Quantity { get; set; }

        [ForeignKey("BorrowId")]
        public BorrowRequest? BorrowRequest { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }
    }
}
