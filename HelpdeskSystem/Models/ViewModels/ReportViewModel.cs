using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HelpdeskSystem.Models.ViewModels
{
    public class ReportViewModel
    {
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [Display(Name = "Mức ưu tiên")]
        public int? PriorityId { get; set; }

        // Summary
        public int TotalTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int OverdueTickets { get; set; }
        public double SLAComplianceRate { get; set; }
        public double AvgResolutionTimeHours { get; set; }

        // Chart data
        public Dictionary<string, int> TicketsByCategory { get; set; }
        public Dictionary<string, int> TicketsByPriority { get; set; }
        public Dictionary<string, int> TicketsByStatus { get; set; }
        public Dictionary<string, double> ProcessingTimeByCategory { get; set; }

        // Staff performance
        public List<StaffPerformanceItem> StaffPerformance { get; set; }

        // Dropdowns
        public SelectList Categories { get; set; }
        public SelectList Priorities { get; set; }

        public ReportViewModel()
        {
            TicketsByCategory = new Dictionary<string, int>();
            TicketsByPriority = new Dictionary<string, int>();
            TicketsByStatus = new Dictionary<string, int>();
            ProcessingTimeByCategory = new Dictionary<string, double>();
            StaffPerformance = new List<StaffPerformanceItem>();
        }
    }

    public class StaffPerformanceItem
    {
        public string TechnicianName { get; set; }
        public int TotalAssigned { get; set; }
        public int TotalResolved { get; set; }
        public double AvgResolutionHours { get; set; }
        public double AvgRating { get; set; }
        public double SLAComplianceRate { get; set; }
    }
}
