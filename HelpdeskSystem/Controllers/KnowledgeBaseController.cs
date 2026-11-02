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
    [Authorize]
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
            var query = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
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

            ViewBag.Search = search;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.Categories = _unitOfWork.Categories.GetAll().OrderBy(c => c.Name).ToList();
            ViewBag.PopularArticles = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .OrderByDescending(a => a.ViewCount)
                .Take(5)
                .ToList();

            return View(articles);
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

            // Related articles
            ViewBag.RelatedArticles = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Where(a => a.CategoryId == article.CategoryId && a.ArticleId != id)
                .OrderByDescending(a => a.ViewCount)
                .Take(4)
                .ToList();

            return View(article);
        }

        // GET: /KnowledgeBase/Manage
        [Authorize(Roles = "Admin,Technician")]
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
        [Authorize(Roles = "Admin,Technician")]
        public ActionResult Create()
        {
            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().OrderBy(c => c.Name).ToList(), "CategoryId", "Name");
            return View(new KnowledgeBase());
        }

        // POST: /KnowledgeBase/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "Admin,Technician")]
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

            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().OrderBy(c => c.Name).ToList(), "CategoryId", "Name", model.CategoryId);
            return View(model);
        }

        // GET: /KnowledgeBase/Edit/5
        [Authorize(Roles = "Admin,Technician")]
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
        [Authorize(Roles = "Admin,Technician")]
        public ActionResult Edit(KnowledgeBase model)
        {
            if (ModelState.IsValid)
            {
                var existing = _unitOfWork.KnowledgeBaseArticles.GetById(model.ArticleId);
                if (existing == null) return HttpNotFound();

                existing.Title = model.Title;
                existing.Content = model.Content;
                existing.CategoryId = model.CategoryId;
                existing.UpdatedDate = DateTime.Now;

                _unitOfWork.SaveChanges();

                TempData["Success"] = "Đã cập nhật bài viết thành công!";
                return RedirectToAction("Manage");
            }

            ViewBag.Categories = new SelectList(_unitOfWork.Categories.GetAll().OrderBy(c => c.Name).ToList(), "CategoryId", "Name", model.CategoryId);
            return View(model);
        }

        // POST: /KnowledgeBase/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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
                .Where(a => a.Title.ToLower().Contains(q) || a.Content.ToLower().Contains(q))
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
