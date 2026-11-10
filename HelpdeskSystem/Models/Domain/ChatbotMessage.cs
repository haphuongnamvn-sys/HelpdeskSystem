using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class ChatbotMessage
    {
        [Key]
        public int ChatbotMessageId { get; set; }

        public int ChatbotSessionId { get; set; }

        public SenderType SenderType { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ChatbotSession Session { get; set; }

        public ChatbotMessage()
        {
            CreatedAt = DateTime.Now;
        }
    }
}
