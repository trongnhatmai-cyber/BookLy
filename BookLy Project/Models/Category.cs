using System.ComponentModel.DataAnnotations;

namespace BookLy_Project.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public ICollection<Book> Books { get; set; }
            = new List<Book>();
    }
}
