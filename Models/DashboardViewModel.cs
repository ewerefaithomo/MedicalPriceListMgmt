using MedicalPriceListApp.Models;
using MedicalPriceListMgmt.Models;
using System.Collections.Generic;

namespace MedicalPriceListMgmt.Models
{
    public class DashboardViewModel
    {
        public int TotalServices { get; set; }

        public int TotalDepartments { get; set; }

        public int TotalRevenueFunds { get; set; }

        public decimal AverageServicePrice { get; set; }

        public List<PriceList> RecentServices { get; set; } = new();
    }
}