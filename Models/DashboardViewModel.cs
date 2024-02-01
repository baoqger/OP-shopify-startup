using System.Collections.Generic;

namespace AuntieDot.Models
{
    public class DashboardViewModel
    {
        public IEnumerable<OrderSummary> Orders { get; set; }
        
        public string NextPage { get; set; }
        
        public string PreviousPage { get; set; }
    }
}
