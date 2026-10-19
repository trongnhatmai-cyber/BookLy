using System.ComponentModel.DataAnnotations;

namespace BookLy_Project.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public int? CategoryId { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? TotalQuantity { get; set; }
        public int? AvailableQuantity { get; set; }
        public bool? IsDeleted { get; set; }

        // Khoa ngoai den Category
        public Category? Category { get; set; }

        public ICollection<BorrowDetail> BorrowDetails { get; set; }
            = new List<BorrowDetail>();

        public ICollection<Favorite> Favorites { get; set; }
            = new List<Favorite>();
    }
}
