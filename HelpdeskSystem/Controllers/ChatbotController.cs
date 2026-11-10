using HelpdeskSystem.Filters;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Repositories;
using HelpdeskSystem.Services;
using Microsoft.AspNet.Identity;

namespace HelpdeskSystem.Controllers
{
    [AuthorizeRoles("EndUser", "Manager", "Technician")]
    public class ChatbotController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketService _ticketService;

        public ChatbotController()
        {
            var context = new ApplicationDbContext();
            _unitOfWork = new UnitOfWork(context);
            _ticketService = new TicketService(_unitOfWork);
        }

        // GET: /Chatbot
        public ActionResult Index(int? sessionId)
        {
            var userId = User.Identity.GetUserId();
            var sessions = _unitOfWork.ChatbotSessions.GetAll()
                .Include(s => s.Messages)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .Take(15)
                .ToList();

            ChatbotSession activeSession = null;
            if (sessionId.HasValue)
            {
                activeSession = sessions.FirstOrDefault(s => s.ChatbotSessionId == sessionId.Value);
            }

            if (activeSession == null)
            {
                activeSession = sessions.FirstOrDefault();
            }

            if (activeSession == null)
            {
                activeSession = new ChatbotSession
                {
                    UserId = userId,
                    StartedAt = DateTime.Now
                };
                _unitOfWork.ChatbotSessions.Add(activeSession);
                _unitOfWork.SaveChanges();

                var welcomeMsg = new ChatbotMessage
                {
                    ChatbotSessionId = activeSession.ChatbotSessionId,
                    SenderType = SenderType.Bot,
                    Content = "Xin chào! Tôi là Trợ lý ảo Hỗ trợ Kỹ thuật (IT Helpdesk Bot). Bạn có thể hỏi tôi về:\n" +
                              "• Cách tự khắc phục sự cố (máy in, mạng Wi-Fi, mật khẩu, phần mềm, email...)\n" +
                              "• Kiểm tra trạng thái phiếu hỗ trợ của bạn (gõ 'phiếu của tôi' hoặc mã phiếu)\n" +
                              "• Quy định thời hạn xử lý theo mức độ ưu tiên (SLA)\n" +
                              "• Số liệu thống kê phiếu (dành cho Quản lý / Nhân viên phụ trách)",
                    CreatedAt = DateTime.Now
                };
                _unitOfWork.ChatbotMessages.Add(welcomeMsg);
                _unitOfWork.SaveChanges();

                sessions.Insert(0, activeSession);
            }

            ViewBag.Sessions = sessions;
            ViewBag.ActiveSession = activeSession;
            var messages = _unitOfWork.ChatbotMessages
                .Find(m => m.ChatbotSessionId == activeSession.ChatbotSessionId)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            return View(messages);
        }

        // POST: /Chatbot/NewSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewSession()
        {
            var userId = User.Identity.GetUserId();
            var session = new ChatbotSession
            {
                UserId = userId,
                StartedAt = DateTime.Now
            };
            _unitOfWork.ChatbotSessions.Add(session);
            _unitOfWork.SaveChanges();

            var welcomeMsg = new ChatbotMessage
            {
                ChatbotSessionId = session.ChatbotSessionId,
                SenderType = SenderType.Bot,
                Content = "Đã bắt đầu phiên hội thoại mới! Tôi có thể giúp gì cho bạn hôm nay?",
                CreatedAt = DateTime.Now
            };
            _unitOfWork.ChatbotMessages.Add(welcomeMsg);
            _unitOfWork.SaveChanges();

            return RedirectToAction("Index", new { sessionId = session.ChatbotSessionId });
        }

        // POST: /Chatbot/Ask (AJAX or Form)
        [HttpPost]
        public JsonResult Ask(int? sessionId, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { success = false, error = "Vui lòng nhập câu hỏi." });
            }

            var userId = User.Identity.GetUserId();
            ChatbotSession session = null;

            if (sessionId.HasValue && sessionId.Value > 0)
            {
                session = _unitOfWork.ChatbotSessions
                    .Find(s => s.ChatbotSessionId == sessionId.Value && s.UserId == userId)
                    .FirstOrDefault();
            }

            if (session == null)
            {
                session = _unitOfWork.ChatbotSessions
                    .Find(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefault();
            }

            if (session == null)
            {
                session = new ChatbotSession { UserId = userId, StartedAt = DateTime.Now };
                _unitOfWork.ChatbotSessions.Add(session);
                _unitOfWork.SaveChanges();
            }

            // 1. Save user message
            var userMsg = new ChatbotMessage
            {
                ChatbotSessionId = session.ChatbotSessionId,
                SenderType = SenderType.User,
                Content = question.Trim(),
                CreatedAt = DateTime.Now
            };
            _unitOfWork.ChatbotMessages.Add(userMsg);
            _unitOfWork.SaveChanges();

            // 2. Generate answer based on system data and user permissions
            string answer = GenerateBotAnswer(question.Trim(), userId);

            // 3. Save bot message
            var botMsg = new ChatbotMessage
            {
                ChatbotSessionId = session.ChatbotSessionId,
                SenderType = SenderType.Bot,
                Content = answer,
                CreatedAt = DateTime.Now
            };
            _unitOfWork.ChatbotMessages.Add(botMsg);
            _unitOfWork.SaveChanges();

            return Json(new
            {
                success = true,
                sessionId = session.ChatbotSessionId,
                answer = answer,
                time = botMsg.CreatedAt.ToString("HH:mm")
            });
        }

        private string GenerateBotAnswer(string question, string userId)
        {
            string q = question.ToLower();
            bool isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");
            bool isTechnician = User.IsInRole("Technician");

            var allowedTickets = _unitOfWork.Context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                if (isTechnician)
                    allowedTickets = allowedTickets.Where(t => t.AssignedToId == userId || t.CreatedById == userId);
                else
                    allowedTickets = allowedTickets.Where(t => t.CreatedById == userId);
            }

            // 1. Check if asking about a specific ticket code (e.g., T-2026...)
            var codeMatch = Regex.Match(question, @"T-\d+", RegexOptions.IgnoreCase);
            if (codeMatch.Success)
            {
                string code = codeMatch.Value.ToUpper();
                var ticket = allowedTickets.FirstOrDefault(t => t.TicketCode.ToUpper() == code);
                if (ticket == null)
                {
                    return $"Không tìm thấy phiếu mã **{code}** trong phạm vi quyền truy cập của bạn.";
                }

                var sb = new StringBuilder();
                sb.AppendLine($"📌 **Thông tin phiếu {ticket.TicketCode}:**");
                sb.AppendLine($"• **Tiêu đề:** {ticket.Title}");
                sb.AppendLine($"• **Danh mục:** {ticket.Category?.Name ?? "N/A"} | **Mức ưu tiên:** {ticket.Priority?.Name ?? "N/A"}");
                sb.AppendLine($"• **Trạng thái:** {_ticketService.GetStatusDisplayName(ticket.Status)}");
                sb.AppendLine($"• **Nhân viên phụ trách:** {ticket.AssignedTo?.FullName ?? "Chưa phân công"}");
                if (ticket.SLADueDate.HasValue)
                    sb.AppendLine($"• **Hạn xử lý:** {ticket.SLADueDate.Value:dd/MM/yyyy HH:mm} ({(ticket.IsSLAViolated ? "Quá hạn" : "Trong hạn")})");
                if (!string.IsNullOrEmpty(ticket.Solution))
                    sb.AppendLine($"• **Giải pháp xử lý:** {ticket.Solution}");
                sb.AppendLine($"👉 [Xem chi tiết phiếu](/Ticket/Details/{ticket.TicketId})");
                return sb.ToString();
            }

            // 2. Check if asking about "phiếu của tôi", "trạng thái phiếu", "phiếu được giao", "danh sách phiếu"
            if (q.Contains("phiếu của tôi") || q.Contains("trạng thái phiếu") || q.Contains("phiếu gần đây") || q.Contains("phiếu được giao") || q.Contains("tiến độ phiếu"))
            {
                var recent = allowedTickets.OrderByDescending(t => t.CreatedDate).Take(5).ToList();
                if (!recent.Any())
                {
                    return "Hiện tại bạn chưa có phiếu yêu cầu nào trong hệ thống. Bạn có thể bấm vào mục **Gửi yêu cầu mới** để tạo phiếu hỗ trợ.";
                }

                var sb = new StringBuilder();
                sb.AppendLine($"📋 **5 phiếu gần nhất trong phạm vi của bạn:**");
                foreach (var t in recent)
                {
                    sb.AppendLine($"• **{t.TicketCode}** - {t.Title} — Trạng thái: **{_ticketService.GetStatusDisplayName(t.Status)}** (Phụ trách: {t.AssignedTo?.FullName ?? "Chưa phân công"}) [Xem](/Ticket/Details/{t.TicketId})");
                }
                return sb.ToString();
            }

            // 3. Check if asking about statistics / overdue / unassigned (especially for Manager / Admin / Technician)
            if (q.Contains("thống kê") || q.Contains("báo cáo") || q.Contains("quá hạn") || q.Contains("chưa phân công") || q.Contains("tổng số phiếu"))
            {
                var list = allowedTickets.ToList();
                int total = list.Count;
                int unassigned = list.Count(t => t.AssignedToId == null && t.Status == TicketStatus.New);
                int inProgress = list.Count(t => t.Status == TicketStatus.Assigned || t.Status == TicketStatus.InProgress);
                int resolved = list.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
                int overdue = list.Count(t => t.IsSLAViolated && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled);

                var sb = new StringBuilder();
                sb.AppendLine("📊 **Tổng hợp số liệu phiếu trong phạm vi quyền của bạn:**");
                sb.AppendLine($"• **Tổng số phiếu:** {total} phiếu");
                sb.AppendLine($"• **Chưa phân công (Mới):** {unassigned} phiếu");
                sb.AppendLine($"• **Đang xử lý / Đã giao:** {inProgress} phiếu");
                sb.AppendLine($"• **Đã giải quyết / Đã đóng:** {resolved} phiếu");
                sb.AppendLine($"• **Quá hạn xử lý (SLA):** {overdue} phiếu");
                return sb.ToString();
            }

            // 4. Check if asking about SLA / Thời hạn xử lý / Mức ưu tiên
            if (q.Contains("sla") || q.Contains("thời hạn") || q.Contains("ưu tiên") || q.Contains("bao lâu"))
            {
                var configs = _unitOfWork.SLAConfigs.GetAll().Include(s => s.Priority).OrderBy(s => s.Priority.Level).ToList();
                var sb = new StringBuilder();
                sb.AppendLine("⏱️ **Quy định thời hạn phản hồi và xử lý theo mức độ ưu tiên:**");
                foreach (var c in configs)
                {
                    sb.AppendLine($"• **Mức {c.Priority?.Name}:** Phản hồi trong **{c.ResponseTimeHours} giờ**, Xử lý xong trong **{c.ResolutionTimeHours} giờ** (Cảnh báo ở mức {c.WarningThresholdPercent}%).");
                }
                return sb.ToString();
            }

            // 5. Search Knowledge Base articles for troubleshooting keywords
            var keywords = q.Split(new[] { ' ', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !new[] { "làm", "sao", "như", "thế", "nào", "cho", "tôi", "hỏi", "cách", "với", "của", "trong", "khi", "bị", "lỗi", "không", "được" }.Contains(w))
                .ToList();

            var allArticles = _unitOfWork.KnowledgeBaseArticles.GetAll()
                .Include(a => a.Category)
                .Where(a => a.IsVisible)
                .ToList();

            var matchedArticles = allArticles
                .Select(a => new
                {
                    Article = a,
                    Score = (a.Title.ToLower().Contains(q) ? 10 : 0) +
                            keywords.Count(k => a.Title.ToLower().Contains(k)) * 3 +
                            keywords.Count(k => a.Content.ToLower().Contains(k))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .ToList();

            if (matchedArticles.Any())
            {
                var top = matchedArticles.First().Article;
                string plainContent = Regex.Replace(top.Content ?? "", "<.*?>", " ");
                plainContent = System.Net.WebUtility.HtmlDecode(plainContent).Trim();
                if (plainContent.Length > 280) plainContent = plainContent.Substring(0, 280) + "...";

                var sb = new StringBuilder();
                sb.AppendLine($"💡 Tôi tìm thấy hướng dẫn phù hợp trong **Cơ sở tri thức**:");
                sb.AppendLine($"\n**{top.Title}** *(Danh mục: {top.Category?.Name})*");
                sb.AppendLine($"{plainContent}");
                sb.AppendLine($"\n👉 [Xem chi tiết bài viết hướng dẫn](/KnowledgeBase/Details/{top.ArticleId})");

                if (matchedArticles.Count > 1)
                {
                    sb.AppendLine("\n**Bài viết liên quan khác:**");
                    foreach (var item in matchedArticles.Skip(1))
                    {
                        sb.AppendLine($"• [{item.Article.Title}](/KnowledgeBase/Details/{item.Article.ArticleId})");
                    }
                }
                sb.AppendLine("\nNếu làm theo hướng dẫn vẫn chưa khắc phục được, bạn hãy bấm **Gửi yêu cầu mới** để nhân viên kỹ thuật hỗ trợ trực tiếp nhé!");
                return sb.ToString();
            }

            // 6. Fallback helpful response
            return "Tôi đã ghi nhận câu hỏi của bạn. Hiện tại bạn có thể:\n" +
                   "1. Hỏi theo từ khóa sự cố cụ thể (ví dụ: *máy in*, *mạng internet*, *quên mật khẩu*, *teams*, *zoom*).\n" +
                   "2. Gõ **\"phiếu của tôi\"** hoặc nhập mã phiếu (ví dụ `T-...`) để tra cứu tiến độ xử lý.\n" +
                   "3. Gõ **\"thời hạn xử lý\"** để xem quy định SLA.\n" +
                   "4. Hoặc truy cập mục [Cơ sở tri thức](/KnowledgeBase) / [Tạo phiếu yêu cầu mới](/Ticket/Create) để được nhân viên phụ trách hỗ trợ!";
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
