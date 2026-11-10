using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class ChatbotSession
    {
        [Key]
        public int ChatbotSessionId { get; set; }

        public string UserId { get; set; }

        public DateTime StartedAt { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<ChatbotMessage> Messages { get; set; }

        public ChatbotSession()
        {
            StartedAt = DateTime.Now;
            Messages = new HashSet<ChatbotMessage>();
        }
    }
}
