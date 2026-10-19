using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookLy_Project.Models
{
    public class Favorite
    {
        public int FavoriteId { get; set; }
        public int? UserId { get; set; }
        public int? BookId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }
    }
}
