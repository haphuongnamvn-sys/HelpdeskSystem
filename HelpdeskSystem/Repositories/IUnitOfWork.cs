using System;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;

namespace HelpdeskSystem.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Ticket> Tickets { get; }
        IRepository<Category> Categories { get; }
        IRepository<Priority> Priorities { get; }
        IRepository<Department> Departments { get; }
        IRepository<TicketComment> TicketComments { get; }
        IRepository<TicketAttachment> TicketAttachments { get; }
        IRepository<TicketHistory> TicketHistories { get; }
        IRepository<TicketRating> TicketRatings { get; }
        IRepository<SLAConfig> SLAConfigs { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<KnowledgeBase> KnowledgeBaseArticles { get; }
        IRepository<ArticleRating> ArticleRatings { get; }
        IRepository<ChatbotSession> ChatbotSessions { get; }
        IRepository<ChatbotMessage> ChatbotMessages { get; }
        ApplicationDbContext Context { get; }
        int SaveChanges();
    }
}
