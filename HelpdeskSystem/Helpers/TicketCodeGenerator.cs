using System;
using System.Linq;
using HelpdeskSystem.Models;

namespace HelpdeskSystem.Helpers
{
    public static class TicketCodeGenerator
    {
        public static string GenerateCode(ApplicationDbContext context)
        {
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string prefix = $"TK-{dateStr}-";

            var todayTickets = context.Tickets
                .Where(t => t.TicketCode.StartsWith(prefix))
                .ToList();

            int count = todayTickets.Count;
            int nextNumber = count + 1;

            return $"{prefix}{nextNumber:D3}";
        }
    }
}
