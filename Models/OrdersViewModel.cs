using System.Collections.Generic;

namespace AuntieDot.Models
{
    public class OrdersViewModel
    {
        public IEnumerable<OrderSummary> Orders { get; set; }
        
        public string NextPage { get; set; }
        
        public string PreviousPage { get; set; }
    }
}
