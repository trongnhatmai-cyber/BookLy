using BookLy_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;

namespace BookLy_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var data = _context.Books.Where(b=>b.IsDeleted != true);
            return View(data);
        }

        [HttpPost]
        public IActionResult Login(User user)
        {
            // Tìm tài khoản theo Name
            var findUser = _context.Users
                .FirstOrDefault(u => u.Username == user.Username);

            // Không tìm thấy tài khoản
            if (findUser == null)
            {
                TempData["LoginFail"] = "Sai tài khoản hoặc mật khẩu !!!";
                TempData["OpenLogin"] = "true";

                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra mật khẩu
            if (findUser.Passwordd != user.Passwordd)
            {
                TempData["LoginFail"] = "Sai tài khoản hoặc mật khẩu !!!";
                TempData["OpenLogin"] = "true";

                return RedirectToAction("Index", "Home");
            }

            // Đăng nhập thành công
            HttpContext.Session.SetInt32("UserId", findUser.UserId);
            HttpContext.Session.SetString("Name", findUser.Username);
            HttpContext.Session.SetString("Role", findUser.Role);

            // Nếu là Admin
            if (findUser.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            // Nếu là User
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index","Home");
            }

            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Shared/Profile.cshtml", user);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Index");

            return View("~/Views/Shared/ChangePassword.cshtml");
        }
        //Change Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(
            string newPassword,
            string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View("~/Views/Shared/ChangePassword.cshtml");
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View("~/Views/Shared/ChangePassword.cshtml");
            }

            var user = _context.Users.Find(userId.Value);

            if (user == null)
                return NotFound();

            user.Passwordd = newPassword;
            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công.";

            return RedirectToAction("Profile");
        }
        //thông tin sách
        public IActionResult Details(int id)
        {
            var user = HttpContext.Session.GetInt32("UserId");
            var book = _context.Books
                .Include(b => b.Category)
                .FirstOrDefault(b => b.BookId == id && b.IsDeleted != true);
            ViewBag.list = _context.Books.Where(b => b.Category == book.Category && b.IsDeleted != true);
            ViewBag.Favorite = user != null &&  
                _context.Favorites.Any(f =>
                    f.BookId == book.BookId &&
                    f.UserId == user.Value);
            if (book == null)
                return NotFound();

            return View(book);
        }
        //yêu thích sách
        [HttpPost]
        public IActionResult ToggleFavorite(int id)
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["OpenLogin"] = "true";
                return RedirectToAction("Index");
            }

            // Tìm sách
            var book = _context.Books
                .FirstOrDefault(b => b.BookId == id && b.IsDeleted != true);

            if (book == null)
            {
                return NotFound();
            }

            // Kiểm tra sách đã được yêu thích chưa
            var favorite = _context.Favorites
                .FirstOrDefault(f => f.UserId == userId.Value
                                  && f.BookId == id);

            if (favorite == null)
            {
                // Chưa yêu thích thì thêm vào
                favorite = new Favorite
                {
                    UserId = userId.Value,
                    BookId = id
                };

                _context.Favorites.Add(favorite);
            }
            else
            {
                // Đã yêu thích thì xóa đi
                _context.Favorites.Remove(favorite);
            }

            _context.SaveChanges();

            return RedirectToAction("Details", new { id = id });
        }
        // trang sách yêu thích
        public IActionResult Favorities()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["OpenLogin"] = "true";
                return RedirectToAction("Index");
            }

            var books = _context.Favorites
                .Where(f => f.UserId == userId.Value)
                .Include(f => f.Book)
                .ThenInclude(b => b.Category)
                .Select(f => f.Book)
                .Where(b => b != null && b.IsDeleted != true)
                .ToList();

            return View(books);
        }
        public IActionResult BorrowHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["OpenLogin"] = "true";
                return RedirectToAction("Index");
            }

            var history = _context.BorrowRequests
                .Where(b => b.UserId == userId.Value)
                .Include(b => b.BorrowDetails)
                .ThenInclude(d => d.Book)
                .OrderByDescending(b => b.RequestedAt)
                .ToList();

            return View(history);
        }

        // Mượn - Trả
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Borrow(int id)
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["OpenLogin"] = "true";
                return RedirectToAction("Index");
            }

            // 2. Đếm số đơn đang hoạt động
            var activeCount = _context.BorrowRequests.Count(b =>
                b.UserId == userId.Value &&
                (b.Status == "Pending" ||
                 b.Status == "Approved" ||
                 b.Status == "Overdue"));

            if (activeCount >= 3)
            {
                TempData["Error"] = "Bạn chỉ được mượn tối đa 3 cuốn sách cùng lúc!";
                return RedirectToAction("Details", new { id = id });
            }

            // 3. Tìm sách
            var book = _context.Books.FirstOrDefault(b =>
                b.BookId == id && b.IsDeleted != true);

            if (book == null)
            {
                return NotFound();
            }

            // 4. Kiểm tra số lượng sách
            if (book.AvailableQuantity == null ||
                book.AvailableQuantity <= 0)
            {
                TempData["Error"] = "Sách đã hết!";
                return RedirectToAction("Details", new { id = id });
            }

            // 5. Tạo đơn mượn, mỗi đơn chỉ có 1 cuốn
            var request = new BorrowRequest
            {
                UserId = userId.Value,
                RequestedAt = DateTime.Now,
                Status = "Pending"
            };

            request.BorrowDetails.Add(new BorrowDetail
            {
                BookId = book.BookId,
                Quantity = 1
            });

            // 6. Giữ chỗ sách
            book.AvailableQuantity--;

            _context.BorrowRequests.Add(request);
            _context.SaveChanges();

            TempData["Success"] = "Đã giữ chỗ sách! Vui lòng chờ Admin duyệt.";

            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelBorrow(int id)
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Tìm đơn mượn của chính học sinh đó
            var request = _context.BorrowRequests
                .Include(b => b.BorrowDetails)
                .FirstOrDefault(b =>
                    b.BorrowId == id &&
                    b.UserId == userId.Value);

            if (request == null)
            {
                return NotFound();
            }

            // 3. Chỉ được hủy đơn đang chờ duyệt
            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "Chỉ có thể thu hồi đơn đang chờ duyệt!";

                return RedirectToAction("BorrowHistory");
            }

            // 4. Cộng lại sách vào kho
            foreach (var detail in request.BorrowDetails)
            {
                var book = _context.Books
                    .FirstOrDefault(b => b.BookId == detail.BookId);

                if (book != null)
                {
                    book.AvailableQuantity =
                        (book.AvailableQuantity ?? 0) + 1;
                }
            }

            // 5. Đổi trạng thái đơn
            request.Status = "Cancelled";

            _context.SaveChanges();

            TempData["Success"] = "Đã thu hồi đơn mượn thành công!";

            return RedirectToAction("BorrowHistory");
        }
    }
}
