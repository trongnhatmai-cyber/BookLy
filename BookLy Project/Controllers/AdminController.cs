using BookLy_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookLy_Project.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.totalBooks = _context.Books.Count(b => b.IsDeleted != true);
            ViewBag.borrowed = _context.Books.Count(b => b.AvailableQuantity < b.TotalQuantity);
            ViewBag.totalUser = _context.Users.Count();
            ViewBag.expired = _context.BorrowRequests.Count(b => b.Status == "Expired");
            return View();
        }
        //book manager
        public IActionResult BookManager()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var books = _context.Books
               .Include(b => b.Category)
               .Where(b => b.IsDeleted != true)
               .ToList();
            return View(books);
        }
        //Edit book
        public IActionResult EditBook(int id)
        {
            var book = _context.Books.FirstOrDefault(b=>b.BookId == id);

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CategoryId",
                "CategoryName",
                book.CategoryId
            );
            if (book == null)
                return NotFound();

            return View("BookEdit",book);
        }

        [HttpPost]
        public IActionResult Edit(Book book)
        {
            var oldBook = _context.Books.Find(book.BookId);

            if (oldBook == null)
            {
                return NotFound();
            }

            oldBook.Title = book.Title;
            oldBook.Author = book.Author;
            oldBook.CategoryId = book.CategoryId;
            oldBook.Description = book.Description;
            oldBook.ImageUrl = book.ImageUrl;
            oldBook.TotalQuantity = book.TotalQuantity;
            oldBook.AvailableQuantity = book.AvailableQuantity;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        // Create book
        // Hiển thị trang thêm sách
        [HttpGet]
        public IActionResult CreateBook()
        {
            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CategoryId",
                "CategoryName"
            );

            return View();
        }

        // Xử lý thêm sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBook(Book book)
        {
            if (book.AvailableQuantity > book.TotalQuantity)
            {
                ModelState.AddModelError("", "Số lượng còn không được lớn hơn tổng số lượng.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    _context.Categories.ToList(),
                    "CategoryId",
                    "CategoryName",
                    book.CategoryId
                );

                return View(book);
            }

            book.IsDeleted = false;

            _context.Books.Add(book);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        // Category Manager
        // Trang quản lý thể loại
        public IActionResult CategoryManager()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        // Hiển thị trang thêm thể loại
        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }

        // Xử lý thêm thể loại
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError("CategoryName", "Vui lòng nhập tên thể loại.");
                return View(category);
            }

            _context.Categories.Add(category);
            _context.SaveChanges();

            TempData["Success"] = "Thêm thể loại thành công!";

            return RedirectToAction("CategoryManager");
        }

        // Xóa thể loại
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            // Không xóa nếu thể loại đang có sách
            if (_context.Books.Any(b => b.CategoryId == id))
            {
                TempData["Error"] = "Thể loại này đang có sách, không thể xóa.";
                return RedirectToAction("CategoryManager");
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = "Xóa thể loại thành công!";
            return RedirectToAction("CategoryManager");
        }
        // Hiển thị trang sửa
        [HttpGet]
        public IActionResult EditCategory(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // Lưu tên thể loại
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCategory(Category category)
        {
            var oldCategory = _context.Categories.Find(category.CategoryId);

            if (oldCategory == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError("CategoryName", "Vui lòng nhập tên thể loại.");
                return View(category);
            }

            oldCategory.CategoryName = category.CategoryName.Trim();

            _context.SaveChanges();

            TempData["Success"] = "Cập nhật thể loại thành công!";

            return RedirectToAction("CategoryManager");
        }

        // Trang quản lý mượn trả
        public IActionResult BorrowManager()
        {
            var requests = _context.BorrowRequests
                .Include(r => r.User)
                .Include(r => r.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            return View(requests);
        }


        // Hiển thị trang tạo đơn mượn
        [HttpGet]
        public IActionResult CreateBorrow()
        {
            LoadBorrowData();
            return View();
        }

        // Lưu đơn mượn tại thư viện
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBorrow(int userId, int bookId, int quantity)
        {
            var student = _context.Users.FirstOrDefault(u =>
                u.UserId == userId && u.Role == "Student");

            var book = _context.Books.FirstOrDefault(b =>
                b.BookId == bookId && b.IsDeleted != true);

            if (student == null || book == null || quantity <= 0)
            {
                TempData["Error"] = "Thông tin học sinh hoặc sách không hợp lệ.";
                LoadBorrowData();
                return View();
            }

            if ((book.AvailableQuantity ?? 0) < quantity)
            {
                TempData["Error"] = "Sách không đủ số lượng.";
                LoadBorrowData();
                return View();
            }

            var request = new BorrowRequest
            {
                UserId = userId,
                RequestedAt = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                Status = "Approved"
            };

            var detail = new BorrowDetail
            {
                BookId = bookId,
                Quantity = quantity
            };

            request.BorrowDetails.Add(detail);

            book.AvailableQuantity -= quantity;

            _context.BorrowRequests.Add(request);
            _context.SaveChanges();

            TempData["Success"] = "Tạo đơn mượn thành công!";
            return RedirectToAction("BorrowManager");
        }

        // Lấy danh sách học sinh và sách
        private void LoadBorrowData()
        {
            ViewBag.Students = new SelectList(
                _context.Users
                    .Where(u => u.Role == "Student")
                    .ToList(),
                "UserId",
                "FullName"
            );

            ViewBag.Books = new SelectList(
                _context.Books
                    .Where(b => b.IsDeleted != true
                             && b.AvailableQuantity > 0)
                    .ToList(),
                "BookId",
                "Title"
            );
        }

        // Trang quản lý người dùng
        public IActionResult UserManager()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // Khóa hoặc mở khóa tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserStatus(int id)
        {
            var user = _context.Users.Find(id);

            if (user == null)
                return NotFound();

            if (user.Status == "Active")
                user.Status = "Locked";
            else
                user.Status = "Active";

            _context.SaveChanges();

            return RedirectToAction("UserManager");
        }
        // Hiển thị trang tạo người dùng
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // Xử lý tạo người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(User user)
        {
            if (_context.Users.Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                ModelState.AddModelError("Username", "Vui lòng nhập tên đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(user.Passwordd))
            {
                ModelState.AddModelError("Passwordd", "Vui lòng nhập mật khẩu.");
            }

            if (!ModelState.IsValid)
                return View(user);


            user.CreatedAt = DateTime.Now;
            user.Status = "Active";

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("UserManager");
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Shared/Profile.cshtml", user);
        }

        //change password
        [HttpGet]
        public IActionResult ChangePassword(int? userId)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.UserId = userId ?? HttpContext.Session.GetInt32("UserId");

            return View("~/Views/Shared/ChangePassword.cshtml");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(
    int userId,
    string newPassword,
    string confirmPassword)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                ViewBag.UserId = userId;
                return View("~/Views/Shared/ChangePassword.cshtml");
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.UserId = userId;
                return View("~/Views/Shared/ChangePassword.cshtml");
            }

            var user = _context.Users.Find(userId);

            if (user == null)
                return NotFound();

            user.Passwordd = newPassword;
            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công.";

            if (userId == HttpContext.Session.GetInt32("UserId"))
                return RedirectToAction("Profile");

            return RedirectToAction("Index", "Admin");
        }
        // duyệt sách mượn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveBorrow(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var request = _context.BorrowRequests
                .FirstOrDefault(b => b.BorrowId == id);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
            {
                TempData["Error"] = "Đơn này không còn ở trạng thái chờ duyệt!";
                return RedirectToAction("BorrowManager");
            }

            request.Status = "Approved";
            request.ApprovedBy = HttpContext.Session.GetInt32("UserId");
            request.DueDate = DateTime.Now.AddDays(14);

            _context.SaveChanges();

            TempData["Success"] = "Đã duyệt đơn mượn sách!";
            return RedirectToAction("BorrowManager");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnBorrow(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var request = _context.BorrowRequests
                .Include(b => b.BorrowDetails)
                .FirstOrDefault(b => b.BorrowId == id);

            if (request == null)
                return NotFound();

            if (request.Status != "Approved" &&
                request.Status != "Overdue")
            {
                TempData["Error"] = "Đơn này không thể xác nhận trả sách!";
                return RedirectToAction("BorrowManager");
            }

            foreach (var detail in request.BorrowDetails)
            {
                var book = _context.Books
                    .FirstOrDefault(b => b.BookId == detail.BookId);

                if (book != null)
                    book.AvailableQuantity = (book.AvailableQuantity ?? 0) + 1;
            }

            request.Status = "Returned";
            _context.SaveChanges();

            TempData["Success"] = "Đã xác nhận trả sách!";
            return RedirectToAction("BorrowManager");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectBorrow(int id)
        {
            // 1. Kiểm tra quyền Admin
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Tìm đơn mượn
            var request = _context.BorrowRequests
                .Include(b => b.BorrowDetails)
                .FirstOrDefault(b => b.BorrowId == id);

            if (request == null)
            {
                return NotFound();
            }

            // 3. Chỉ từ chối đơn đang chờ duyệt
            if (request.Status != "Pending")
            {
                TempData["Error"] = "Chỉ có thể từ chối đơn đang chờ duyệt!";
                return RedirectToAction("BorrowManager");
            }

            // 4. Hoàn lại số lượng sách
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

            // 5. Cập nhật trạng thái
            request.Status = "Rejected";

            _context.SaveChanges();

            TempData["Success"] = "Đã từ chối đơn mượn sách!";

            return RedirectToAction("BorrowManager");
        }
    }
}
