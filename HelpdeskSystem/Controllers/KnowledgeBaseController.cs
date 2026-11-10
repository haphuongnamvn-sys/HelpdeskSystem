using HelpdeskSystem.Filters;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Repositories;
using Microsoft.AspNet.Identity;

namespace HelpdeskSystem.Controllers
{
    [AuthorizeRoles("EndUser", "Manager", "Technician")]
    public class KnowledgeBaseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public KnowledgeBaseController()
        {
            _unitOfWork = new UnitOfWork(new ApplicationDbContext());
        }

        public KnowledgeBaseController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: /KnowledgeBase
        [AllowAnonymous]
        public ActionResult Index(string search, int? categoryId)
        {
            EnsureSeedData();

            var query = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .Where(a => a.IsVisible)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(s) || a.Content.ToLower().Contains(s));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(a => a.CategoryId == categoryId.Value);
            }

            var articles = query.OrderByDescending(a => a.CreatedDate).ToList();

            var allVisible = _unitOfWork.KnowledgeBaseArticles.GetAll().Where(a => a.IsVisible).ToList();
            ViewBag.ArticleCountsByCategory = allVisible.GroupBy(a => a.CategoryId).ToDictionary(g => g.Key, g => g.Count());
            ViewBag.TotalArticlesCount = allVisible.Count;

            ViewBag.Search = search;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.Categories = _unitOfWork.Categories.GetAll().Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
            ViewBag.PopularArticles = allVisible.OrderByDescending(a => a.ViewCount).Take(5).ToList();

            return View(articles);
        }

        private void EnsureSeedData()
        {
            try
            {
                var existing = _unitOfWork.KnowledgeBaseArticles.GetAll().ToList();
                if (!existing.Any())
                {
                    var categories = _unitOfWork.Categories.GetAll().Where(c => c.IsActive).ToList();
                    if (!categories.Any()) return;

                    int catHardware = categories.FirstOrDefault(c => c.Name.Contains("cứng"))?.CategoryId ?? categories.First().CategoryId;
                    int catNetwork = categories.FirstOrDefault(c => c.Name.Contains("Mạng"))?.CategoryId ?? categories.First().CategoryId;
                    int catAccount = categories.FirstOrDefault(c => c.Name.Contains("khoản"))?.CategoryId ?? categories.First().CategoryId;
                    int catSoftware = categories.FirstOrDefault(c => c.Name.Contains("mềm"))?.CategoryId ?? categories.First().CategoryId;

                    var defaultArticles = new List<KnowledgeBase>
                    {
                        new KnowledgeBase
                        {
                            Title = "Hướng dẫn kết nối máy in văn phòng qua mạng LAN hoặc Wi-Fi",
                            CategoryId = catHardware,
                            ViewCount = 42,
                            IsVisible = true,
                            CreatedDate = DateTime.Now.AddDays(-10),
                            Content = @"<p>Khi gặp vấn đề không tìm thấy máy in văn phòng trên máy tính của bạn, hãy thực hiện theo các bước sau:</p>
<ol>
    <li><strong>Kiểm tra nguồn và dây mạng:</strong> Đảm bảo máy in đã bật nguồn, đèn tín hiệu mạng (cổng LAN) sáng xanh hoặc màn hình máy in hiển thị biểu tượng Wi-Fi kết nối tốt.</li>
    <li><strong>Kiểm tra địa chỉ IP máy in:</strong> In trang cấu hình mạng (Network Configuration Report) từ màn hình menu của máy in để lấy địa chỉ IP (ví dụ: <code>192.168.1.200</code>).</li>
    <li><strong>Thêm máy in trên Windows:</strong>
        <ul>
            <li>Vào <em>Settings</em> &rarr; <em>Devices</em> &rarr; <em>Printers & scanners</em>.</li>
            <li>Bấm <strong>Add a printer or scanner</strong>.</li>
            <li>Nếu Windows không tự tìm thấy, chọn <em>The printer that I want isn't listed</em> &rarr; Chọn <em>Add a printer using an IP address or hostname</em>.</li>
            <li>Nhập địa chỉ IP máy in và bấm <em>Next</em> để hoàn tất cài đặt driver.</li>
        </ul>
    </li>
</ol>
<p>Nếu vẫn không in được, vui lòng khởi động lại dịch vụ Print Spooler (vào Services &rarr; tìm Print Spooler &rarr; Restart) hoặc gửi phiếu yêu cầu hỗ trợ.</p>"
                        },
                        new KnowledgeBase
                        {
                            Title = "Cách xử lý sự cố mạng Internet bị chập chờn hoặc không truy cập được",
                            CategoryId = catNetwork,
                            ViewCount = 68,
                            IsVisible = true,
                            CreatedDate = DateTime.Now.AddDays(-7),
                            Content = @"<p>Mạng Internet bị gián đoạn gây ảnh hưởng đến công việc. Dưới đây là các bước tự kiểm tra nhanh:</p>
<ol>
    <li><strong>Kiểm tra kết nối dây cáp hoặc Wi-Fi:</strong>
        <ul>
            <li>Nếu dùng dây LAN: Rút đầu cáp ra và cắm lại thật chặt, kiểm tra xem đèn cổng mạng ở máy tính có nháy sáng không.</li>
            <li>Nếu dùng Wi-Fi: Tắt kết nối Wi-Fi trên máy tính trong 10 giây và bật lại, hoặc kết nối thử vào mạng 5GHz của công ty.</li>
        </ul>
    </li>
    <li><strong>Làm mới địa chỉ IP bằng Command Prompt:</strong>
        <p>Mở cửa sổ <em>cmd</em> (Command Prompt) và chạy 3 lệnh sau:</p>
        <pre class='bg-light p-2 border'>ipconfig /release
ipconfig /renew
ipconfig /flushdns</pre>
    </li>
    <li><strong>Khởi động lại Switch bàn làm việc:</strong> Nếu bàn làm việc có bộ chia mạng mini (Switch), hãy rút nguồn điện khoảng 15 giây và cắm lại.</li>
</ol>
<p>Nếu toàn bộ phòng ban đều mất kết nối, đây là sự cố diện rộng, vui lòng liên hệ trực tiếp hotline IT hoặc tạo phiếu ưu tiên Cao.</p>"
                        },
                        new KnowledgeBase
                        {
                            Title = "Quy trình mở khóa tài khoản nội bộ và đổi mật khẩu an toàn",
                            CategoryId = catAccount,
                            ViewCount = 95,
                            IsVisible = true,
                            CreatedDate = DateTime.Now.AddDays(-5),
                            Content = @"<p>Để đảm bảo an toàn thông tin, hệ thống tự động khóa tạm thời tài khoản nếu nhập sai mật khẩu quá 5 lần liên tiếp.</p>
<h5>1. Khóa tạm thời 5 phút:</h5>
<p>Nếu bị khóa do gõ nhầm, hệ thống sẽ tự động mở lại sau <strong>5 phút</strong>. Vui lòng chờ hết thời gian này và kiểm tra kỹ phím <em>Caps Lock</em> hoặc bộ gõ tiếng Việt trước khi thử lại.</p>
<h5>2. Quên mật khẩu hoàn toàn:</h5>
<p>Nếu bạn không thể nhớ mật khẩu của mình:</p>
<ul>
    <li>Liên hệ trực tiếp quản trị viên hệ thống qua hotline hoặc email hỗ trợ IT.</li>
    <li>Gửi kèm mã nhân viên và họ tên để IT cấp lại mật khẩu mặc định tạm thời.</li>
    <li>Sau khi đăng nhập được, bạn <strong>bắt buộc</strong> phải vào mục <em>Hồ sơ &rarr; Đổi mật khẩu</em> để thiết lập mật khẩu cá nhân mới.</li>
</ul>"
                        },
                        new KnowledgeBase
                        {
                            Title = "Hướng dẫn cài đặt và cấu hình phần mềm họp trực tuyến Microsoft Teams / Zoom",
                            CategoryId = catSoftware,
                            ViewCount = 34,
                            IsVisible = true,
                            CreatedDate = DateTime.Now.AddDays(-3),
                            Content = @"<p>Hướng dẫn chuẩn bị thiết bị micro, webcam và phần mềm trước các cuộc họp quan trọng:</p>
<ol>
    <li><strong>Cấp quyền truy cập Microphone và Camera trên Windows:</strong>
        <p>Vào <em>Settings &rarr; Privacy & security &rarr; Microphone / Camera</em>. Đảm bảo công tắc <em>Let apps access your microphone/camera</em> đang ở trạng thái <strong>On</strong>.</p>
    </li>
    <li><strong>Kiểm tra thiết bị âm thanh trên Teams / Zoom:</strong>
        <p>Trong ứng dụng, vào mục <em>Settings &rarr; Devices</em>:</p>
        <ul>
            <li>Mục Audio: Chọn đúng tai nghe (Headset) bạn đang sử dụng.</li>
            <li>Bấm <strong>Make a test call</strong> (trên Teams) hoặc <strong>Test Mic/Speaker</strong> (trên Zoom) để nghe lại giọng nói của mình.</li>
        </ul>
    </li>
</ol>"
                        }
                    };

                    foreach (var a in defaultArticles)
                    {
                        _unitOfWork.KnowledgeBaseArticles.Add(a);
                    }
                    _unitOfWork.SaveChanges();
                }
                else
                {
                    bool updated = false;
                    foreach (var a in existing.Where(x => !x.IsVisible))
                    {
                        a.IsVisible = true;
                        updated = true;
                    }
                    if (updated)
                    {
                        _unitOfWork.SaveChanges();
                    }
                }
            }
            catch
            {
                // Fallback gracefully if database table is not writable
            }
        }

        // GET: /KnowledgeBase/Details/5
        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            var article = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .FirstOrDefault(a => a.ArticleId == id);

            if (article == null) return HttpNotFound();

            // Increment view count
            article.ViewCount++;
            _unitOfWork.SaveChanges();

            // User rating status
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var userRating = _unitOfWork.ArticleRatings
                    .Find(r => r.ArticleId == id && r.UserId == userId)
                    .FirstOrDefault();
                ViewBag.UserRating = userRating != null ? (bool?)userRating.IsHelpful : null;
            }

            // Related articles
            ViewBag.RelatedArticles = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Where(a => a.IsVisible && a.CategoryId == article.CategoryId && a.ArticleId != id)
                .OrderByDescending(a => a.ViewCount)
                .Take(4)
                .ToList();

            return View(article);
        }

        // POST: /KnowledgeBase/RateArticle (UC09 - 2.5.4: Đánh giá bài viết hữu ích)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RateArticle(int id, bool isHelpful)
        {
            var article = _unitOfWork.KnowledgeBaseArticles.GetById(id);
            if (article == null) return HttpNotFound();

            var userId = User.Identity.GetUserId();
            var existing = _unitOfWork.ArticleRatings
                .Find(r => r.ArticleId == id && r.UserId == userId)
                .FirstOrDefault();

            if (existing == null)
            {
                _unitOfWork.ArticleRatings.Add(new ArticleRating
                {
                    ArticleId = id,
                    UserId = userId,
                    IsHelpful = isHelpful,
                    CreatedDate = DateTime.Now
                });
                if (isHelpful) article.HelpfulCount++;
                else article.NotHelpfulCount++;
            }
            else if (existing.IsHelpful != isHelpful)
            {
                if (existing.IsHelpful) article.HelpfulCount = Math.Max(0, article.HelpfulCount - 1);
                else article.NotHelpfulCount = Math.Max(0, article.NotHelpfulCount - 1);

                existing.IsHelpful = isHelpful;
                existing.CreatedDate = DateTime.Now;

                if (isHelpful) article.HelpfulCount++;
                else article.NotHelpfulCount++;
            }

            _unitOfWork.SaveChanges();
            TempData["Success"] = "Cảm ơn bạn đã đánh giá mức độ hữu ích của bài viết!";
            return RedirectToAction("Details", new { id = id });
        }

        // GET: /KnowledgeBase/Manage
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult Manage()
        {
            var articles = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.CreatedDate)
                .ToList();

            return View(articles);
        }

        // GET: /KnowledgeBase/Create
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult Create()
        {
            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().Where(c => c.IsActive).OrderBy(c => c.Name).ToList(), "CategoryId", "Name");
            return View(new KnowledgeBase { IsVisible = true });
        }

        // POST: /KnowledgeBase/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult Create(KnowledgeBase model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedById = User.Identity.GetUserId();
                model.CreatedDate = DateTime.Now;
                model.ViewCount = 0;

                _unitOfWork.KnowledgeBaseArticles.Add(model);
                _unitOfWork.SaveChanges();

                TempData["Success"] = "Đã xuất bản bài viết hướng dẫn mới!";
                return RedirectToAction("Manage");
            }

            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().Where(c => c.IsActive).OrderBy(c => c.Name).ToList(), "CategoryId", "Name", model.CategoryId);
            return View(model);
        }

        // GET: /KnowledgeBase/Edit/5
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult Edit(int id)
        {
            var article = _unitOfWork.KnowledgeBaseArticles.GetById(id);
            if (article == null) return HttpNotFound();

            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().OrderBy(c => c.Name).ToList(), "CategoryId", "Name", article.CategoryId);
            return View(article);
        }

        // POST: /KnowledgeBase/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult Edit(KnowledgeBase model)
        {
            if (ModelState.IsValid)
            {
                var existing = _unitOfWork.KnowledgeBaseArticles.GetById(model.ArticleId);
                if (existing == null) return HttpNotFound();

                existing.Title = model.Title;
                existing.Content = model.Content;
                existing.CategoryId = model.CategoryId;
                existing.IsVisible = model.IsVisible;
                existing.UpdatedDate = DateTime.Now;

                _unitOfWork.SaveChanges();

                TempData["Success"] = "Đã cập nhật bài viết thành công!";
                return RedirectToAction("Manage");
            }

            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().OrderBy(c => c.Name).ToList(), "CategoryId", "Name", model.CategoryId);
            return View(model);
        }

        // POST: /KnowledgeBase/ToggleVisibility/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult ToggleVisibility(int id)
        {
            var article = _unitOfWork.KnowledgeBaseArticles.GetById(id);
            if (article != null)
            {
                article.IsVisible = !article.IsVisible;
                article.UpdatedDate = DateTime.Now;
                _unitOfWork.SaveChanges();
                TempData["Success"] = article.IsVisible ? "Đã hiển thị lại bài viết." : "Đã ẩn bài viết.";
            }
            return RedirectToAction("Manage");
        }

        // POST: /KnowledgeBase/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Manager", "Technician")]
        public ActionResult Delete(int id)
        {
            var article = _unitOfWork.KnowledgeBaseArticles.GetById(id);
            if (article != null)
            {
                _unitOfWork.KnowledgeBaseArticles.Delete(article);
                _unitOfWork.SaveChanges();
                TempData["Success"] = "Đã xóa bài viết khỏi cơ sở tri thức.";
            }

            return RedirectToAction("Manage");
        }

        // GET: /KnowledgeBase/Suggest (AJAX API for Ticket/Create)
        [AllowAnonymous]
        public JsonResult Suggest(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var q = query.Trim().ToLower();
            var matches = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Include(a => a.Category)
                .Where(a => a.IsVisible && (a.Title.ToLower().Contains(q) || a.Content.ToLower().Contains(q)))
                .OrderByDescending(a => a.ViewCount)
                .Take(4)
                .ToList()
                .Select(a => new
                {
                    articleId = a.ArticleId,
                    title = a.Title,
                    categoryName = a.Category != null ? a.Category.Name : "Chung",
                    viewCount = a.ViewCount
                });

            return Json(matches, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unitOfWork.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
