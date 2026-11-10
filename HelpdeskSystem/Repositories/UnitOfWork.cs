using System;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;

namespace HelpdeskSystem.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IRepository<Ticket> _tickets;
        private IRepository<Category> _categories;
        private IRepository<Priority> _priorities;
        private IRepository<Department> _departments;
        private IRepository<TicketComment> _ticketComments;
        private IRepository<TicketAttachment> _ticketAttachments;
        private IRepository<TicketHistory> _ticketHistories;
        private IRepository<TicketRating> _ticketRatings;
        private IRepository<SLAConfig> _slaConfigs;
        private IRepository<Notification> _notifications;
        private IRepository<KnowledgeBase> _knowledgeBaseArticles;
        private IRepository<ArticleRating> _articleRatings;
        private IRepository<ChatbotSession> _chatbotSessions;
        private IRepository<ChatbotMessage> _chatbotMessages;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ApplicationDbContext Context => _context;

        public IRepository<Ticket> Tickets => _tickets ?? (_tickets = new Repository<Ticket>(_context));
        public IRepository<Category> Categories => _categories ?? (_categories = new Repository<Category>(_context));
        public IRepository<Priority> Priorities => _priorities ?? (_priorities = new Repository<Priority>(_context));
        public IRepository<Department> Departments => _departments ?? (_departments = new Repository<Department>(_context));
        public IRepository<TicketComment> TicketComments => _ticketComments ?? (_ticketComments = new Repository<TicketComment>(_context));
        public IRepository<TicketAttachment> TicketAttachments => _ticketAttachments ?? (_ticketAttachments = new Repository<TicketAttachment>(_context));
        public IRepository<TicketHistory> TicketHistories => _ticketHistories ?? (_ticketHistories = new Repository<TicketHistory>(_context));
        public IRepository<TicketRating> TicketRatings => _ticketRatings ?? (_ticketRatings = new Repository<TicketRating>(_context));
        public IRepository<SLAConfig> SLAConfigs => _slaConfigs ?? (_slaConfigs = new Repository<SLAConfig>(_context));
        public IRepository<Notification> Notifications => _notifications ?? (_notifications = new Repository<Notification>(_context));
        public IRepository<KnowledgeBase> KnowledgeBaseArticles => _knowledgeBaseArticles ?? (_knowledgeBaseArticles = new Repository<KnowledgeBase>(_context));
        public IRepository<ArticleRating> ArticleRatings => _articleRatings ?? (_articleRatings = new Repository<ArticleRating>(_context));
        public IRepository<ChatbotSession> ChatbotSessions => _chatbotSessions ?? (_chatbotSessions = new Repository<ChatbotSession>(_context));
        public IRepository<ChatbotMessage> ChatbotMessages => _chatbotMessages ?? (_chatbotMessages = new Repository<ChatbotMessage>(_context));

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
