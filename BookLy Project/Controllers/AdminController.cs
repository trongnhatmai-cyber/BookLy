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
        public IActionResult Index(string? weekOf)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            if (!DateTime.TryParseExact(
                    weekOf, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var selectedDate))
            {
                selectedDate = DateTime.Today;
            }

            selectedDate = selectedDate.Date;
            // Tuần bắt đầu từ thứ Hai và kết thúc vào Chủ nhật.
            var weekStart = selectedDate.AddDays(-(((int)selectedDate.DayOfWeek + 6) % 7));
            var weekEnd = weekStart.AddDays(7);

            // Đếm số cuốn sách thực tế được mượn (tổng Quantity trong BorrowDetails),
            // không phải số phiếu mượn.
            var dailyBorrowRows = _context.BorrowDetails
                .AsNoTracking()
                .Where(d => d.BorrowRequest != null
                    && d.BorrowRequest.RequestedAt.HasValue
                    && d.BorrowRequest.RequestedAt.Value >= weekStart
                    && d.BorrowRequest.RequestedAt.Value < weekEnd
                    && (d.BorrowRequest.Status == "Approved"
                        || d.BorrowRequest.Status == "Overdue"
                        || d.BorrowRequest.Status == "Returned"))
                .Select(d => new
                {
                    RequestedAt = d.BorrowRequest!.RequestedAt!.Value,
                    Quantity = d.Quantity ?? 0
                })
                .ToList();

            var dailyStats = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = weekStart.AddDays(offset);
                    return new DailyBorrowStat
                    {
                        Date = day,
                        DayLabel = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" }[offset],
                        BookCount = dailyBorrowRows
                            .Where(x => x.RequestedAt.Date == day)
                            .Sum(x => x.Quantity)
                    };
                })
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalBooks = _context.Books.Count(b => b.IsDeleted != true),
                BorrowedBooks = _context.Books.Count(b => b.IsDeleted != true
                    && b.AvailableQuantity < b.TotalQuantity),
                TotalUsers = _context.Users.Count(),
                ExpiredRequests = _context.BorrowRequests.Count(b =>
                    b.Status == "Expired" ||
                    b.Status == "Overdue" ||
                    (b.Status == "Approved" && b.DueDate.HasValue && b.DueDate.Value < DateTime.Now)),
                WeekStart = weekStart,
                SelectedDate = selectedDate,
                DailyBorrowStats = dailyStats
            };

            return View(model);
        }
        // Danh sách sách
        public IActionResult BookManager()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            var books = _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Where(b => b.IsDeleted != true)
                .OrderBy(b => b.Title)
                .ToList();

            return View(books);
        }

        // Hiển thị form chỉnh sửa
        [HttpGet]
        public IActionResult EditBook(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            var book = _context.Books.FirstOrDefault(b => b.BookId == id && b.IsDeleted != true);
            if (book == null)
                return NotFound();

            ViewBag.Categories = new SelectList(
                _context.Categories.AsNoTracking().ToList(),
                "CategoryId", "CategoryName", book.CategoryId);

            return View("BookEdit", book);
        }

        // Lưu chỉnh sửa. Tên action phải trùng với asp-action trong form BookEdit.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditBook(Book book)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            var oldBook = _context.Books.FirstOrDefault(b =>
                b.BookId == book.BookId && b.IsDeleted != true);

            if (oldBook == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(book.Title))
                ModelState.AddModelError(nameof(book.Title), "Vui lòng nhập tên sách.");

            if (book.TotalQuantity < 0 || book.AvailableQuantity < 0
                || book.AvailableQuantity > book.TotalQuantity)
                ModelState.AddModelError(nameof(book.AvailableQuantity),
                    "Số lượng còn phải từ 0 đến tổng số lượng.");

            if (book.CategoryId.HasValue
                && !_context.Categories.Any(c => c.CategoryId == book.CategoryId.Value))
                ModelState.AddModelError(nameof(book.CategoryId), "Thể loại không tồn tại.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    _context.Categories.AsNoTracking().ToList(),
                    "CategoryId", "CategoryName", book.CategoryId);
                return View("BookEdit", book);
            }

            oldBook.Title = book.Title!.Trim();
            oldBook.Author = book.Author?.Trim();
            oldBook.CategoryId = book.CategoryId;
            oldBook.Description = book.Description;
            oldBook.ImageUrl = book.ImageUrl;
            oldBook.TotalQuantity = book.TotalQuantity;
            oldBook.AvailableQuantity = book.AvailableQuantity;

            _context.SaveChanges();
            TempData["Success"] = "Cập nhật thông tin sách thành công.";
            return RedirectToAction(nameof(BookManager));
        }

        // Hiển thị form thêm sách
        [HttpGet]
        public IActionResult CreateBook()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.Categories = new SelectList(
                _context.Categories.AsNoTracking().ToList(),
                "CategoryId", "CategoryName");
            return View();
        }

        // Lưu sách mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBook(Book book)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(book.Title))
                ModelState.AddModelError(nameof(book.Title), "Vui lòng nhập tên sách.");

            if (book.TotalQuantity < 0 || book.AvailableQuantity < 0
                || book.AvailableQuantity > book.TotalQuantity)
                ModelState.AddModelError(nameof(book.AvailableQuantity),
                    "Số lượng còn phải từ 0 đến tổng số lượng.");

            if (book.CategoryId.HasValue
                && !_context.Categories.Any(c => c.CategoryId == book.CategoryId.Value))
                ModelState.AddModelError(nameof(book.CategoryId), "Thể loại không tồn tại.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    _context.Categories.AsNoTracking().ToList(),
                    "CategoryId", "CategoryName", book.CategoryId);
                return View(book);
            }

            book.Title = book.Title!.Trim();
            book.Author = book.Author?.Trim();
            book.IsDeleted = false;
            _context.Books.Add(book);
            _context.SaveChanges();

            TempData["Success"] = "Thêm sách thành công.";
            return RedirectToAction(nameof(BookManager));
        }

        // Xóa mềm để không làm hỏng lịch sử mượn/trả có liên kết với sách.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBook(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            var book = _context.Books.FirstOrDefault(b => b.BookId == id && b.IsDeleted != true);
            if (book == null)
                return NotFound();

            var hasActiveBorrow = _context.BorrowDetails.Any(d =>
                d.BookId == id && d.BorrowRequest != null
                && (d.BorrowRequest.Status == "Pending"
                    || d.BorrowRequest.Status == "Approved"
                    || d.BorrowRequest.Status == "Overdue"));

            if (hasActiveBorrow)
            {
                TempData["Error"] = "Không thể xóa sách đang nằm trong phiếu mượn chưa hoàn tất.";
                return RedirectToAction(nameof(BookManager));
            }

            book.IsDeleted = true;
            _context.SaveChanges();
            TempData["Success"] = "Đã xóa sách khỏi danh mục.";
            return RedirectToAction(nameof(BookManager));
        }

        // Category Manager
        // Trang quản lý thể loại
        public IActionResult CategoryManager()
        {
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Index", "Home");
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Index", "Home");
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
            if (HttpContext.Session.GetString("Role") != "Admin") return RedirectToAction("Index", "Home");
            var users = _context.Users.ToList();
            return View(users);
        }

        // Thực hiện thao tác hàng loạt với tài khoản học sinh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkUserAction(List<int> selectedIds, string action)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Home");

            if (selectedIds == null || selectedIds.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một học sinh.";
                return RedirectToAction(nameof(UserManager));
            }

            // Chỉ xử lý tài khoản học sinh, không cho thao tác hàng loạt lên Admin.
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var users = _context.Users
                .Where(u => selectedIds.Contains(u.UserId)
                    && u.Role == "Student"
                    && u.UserId != currentUserId)
                .ToList();

            if (users.Count == 0)
            {
                TempData["Error"] = "Không có học sinh hợp lệ được chọn.";
                return RedirectToAction(nameof(UserManager));
            }

            if (action == "Lock" || action == "Unlock")
            {
                foreach (var user in users)
                    user.Status = action == "Lock" ? "Locked" : "Active";

                _context.SaveChanges();
                TempData["Success"] = $"Đã {(action == "Lock" ? "khóa" : "mở khóa")} {users.Count} tài khoản học sinh.";
            }
            else if (action == "Delete")
            {
                int deleted = 0;
                int skipped = 0;

                foreach (var user in users)
                {
                    bool hasBorrowHistory = _context.BorrowRequests.Any(r =>
                        r.UserId == user.UserId || r.ApprovedBy == user.UserId);
                    bool hasFavorites = _context.Favorites.Any(f => f.UserId == user.UserId);

                    // Giữ lại tài khoản có dữ liệu liên quan để không làm mất lịch sử.
                    if (hasBorrowHistory || hasFavorites)
                    {
                        skipped++;
                        continue;
                    }

                    _context.Users.Remove(user);
                    deleted++;
                }

                _context.SaveChanges();
                TempData[deleted > 0 ? "Success" : "Error"] =
                    $"Đã xóa {deleted} tài khoản. Bỏ qua {skipped} tài khoản vì đang có lịch sử mượn sách hoặc sách yêu thích.";
            }
            else
            {
                TempData["Error"] = "Thao tác không hợp lệ.";
            }

            return RedirectToAction(nameof(UserManager));
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
                    book.AvailableQuantity = (book.AvailableQuantity ?? 0) + (detail.Quantity ?? 1);
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
