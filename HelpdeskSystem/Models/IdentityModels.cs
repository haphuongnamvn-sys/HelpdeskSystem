using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Security.Claims;
using System.Threading.Tasks;
using HelpdeskSystem.Models.Domain;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HelpdeskSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [StringLength(500)]
        public string AvatarUrl { get; set; }

        [Display(Name = "Phòng ban")]
        public int? DepartmentId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual Department Department { get; set; }

        public ApplicationUser()
        {
            IsActive = true;
            CreatedDate = DateTime.Now;
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            userIdentity.AddClaim(new Claim("FullName", FullName ?? ""));
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        // Domain DbSets
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Priority> Priorities { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<TicketHistory> TicketHistories { get; set; }
        public DbSet<TicketRating> TicketRatings { get; set; }
        public DbSet<SLAConfig> SLAConfigs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<KnowledgeBase> KnowledgeBaseArticles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ticket relationships
            modelBuilder.Entity<Ticket>()
                .HasRequired(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ticket>()
                .HasOptional(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ticket>()
                .HasRequired(t => t.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ticket>()
                .HasRequired(t => t.Priority)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.PriorityId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.TicketCode)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.Status);

            // TicketComment relationships
            modelBuilder.Entity<TicketComment>()
                .HasRequired(tc => tc.Ticket)
                .WithMany(t => t.TicketComments)
                .HasForeignKey(tc => tc.TicketId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TicketComment>()
                .HasRequired(tc => tc.User)
                .WithMany()
                .HasForeignKey(tc => tc.UserId)
                .WillCascadeOnDelete(false);

            // TicketAttachment relationships
            modelBuilder.Entity<TicketAttachment>()
                .HasRequired(ta => ta.Ticket)
                .WithMany(t => t.TicketAttachments)
                .HasForeignKey(ta => ta.TicketId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TicketAttachment>()
                .HasOptional(ta => ta.Comment)
                .WithMany(tc => tc.Attachments)
                .HasForeignKey(ta => ta.CommentId)
                .WillCascadeOnDelete(false);

            // TicketHistory relationships
            modelBuilder.Entity<TicketHistory>()
                .HasRequired(th => th.Ticket)
                .WithMany(t => t.TicketHistories)
                .HasForeignKey(th => th.TicketId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TicketHistory>()
                .HasRequired(th => th.ChangedBy)
                .WithMany()
                .HasForeignKey(th => th.ChangedById)
                .WillCascadeOnDelete(false);

            // TicketRating relationships
            modelBuilder.Entity<TicketRating>()
                .HasRequired(tr => tr.Ticket)
                .WithMany()
                .HasForeignKey(tr => tr.TicketId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TicketRating>()
                .HasRequired(tr => tr.User)
                .WithMany()
                .HasForeignKey(tr => tr.UserId)
                .WillCascadeOnDelete(false);

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasOptional(n => n.Ticket)
                .WithMany()
                .HasForeignKey(n => n.TicketId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.IsRead);

            // Category relationships
            modelBuilder.Entity<Category>()
                .HasOptional(c => c.Department)
                .WithMany(d => d.Categories)
                .HasForeignKey(c => c.DepartmentId)
                .WillCascadeOnDelete(false);

            // SLAConfig relationships
            modelBuilder.Entity<SLAConfig>()
                .HasRequired(s => s.Priority)
                .WithMany(p => p.SLAConfigs)
                .HasForeignKey(s => s.PriorityId)
                .WillCascadeOnDelete(false);

            // KnowledgeBase relationships
            modelBuilder.Entity<KnowledgeBase>()
                .HasRequired(k => k.Category)
                .WithMany()
                .HasForeignKey(k => k.CategoryId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<KnowledgeBase>()
                .HasRequired(k => k.CreatedBy)
                .WithMany()
                .HasForeignKey(k => k.CreatedById)
                .WillCascadeOnDelete(false);

            // ApplicationUser → Department
            modelBuilder.Entity<ApplicationUser>()
                .HasOptional(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .WillCascadeOnDelete(false);
        }
    }
}
