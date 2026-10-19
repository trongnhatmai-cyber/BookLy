using System.ComponentModel.DataAnnotations;

namespace BookLy_Project.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Passwordd { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? StudentCode { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Cac khoa ngoai lien ket
        public ICollection<BorrowRequest> BorrowRequests { get; set; }
            = new List<BorrowRequest>();

        public ICollection<BorrowRequest> ApprovedRequests { get; set; }
            = new List<BorrowRequest>();

        public ICollection<Favorite> Favorites { get; set; }
            = new List<Favorite>();
    }
}
