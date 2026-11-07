namespace BookLy_Project.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int BorrowedBooks { get; set; }
        public int TotalUsers { get; set; }
        public int ExpiredRequests { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime SelectedDate { get; set; }
        public List<DailyBorrowStat> DailyBorrowStats { get; set; } = new();
        public int TotalBorrowedInWeek => DailyBorrowStats.Sum(x => x.BookCount);
    }

    public class DailyBorrowStat
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }
}
