using System.Linq;
using AuntieDot.Attributes;
using AuntieDot.Models;
using System.Threading.Tasks;
using AuntieDot.Data;
using AuntieDot.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopifySharp;
using ShopifySharp.Filters;

namespace AuntieDot.Controllers
{
    [AuthorizeWithActiveSubscription, CatchPermissionExceptions]
    public class OrdersController : Controller
    {
        public OrdersController(ILogger<HomeController> logger, DataContext userContext)
        {
            _logger = logger;
            _dataContext = userContext;
        }
        
        private readonly ILogger<HomeController> _logger;
        
        private readonly DataContext _dataContext;

        public async Task<IActionResult> Index([FromQuery] string pageInfo = null)
        {
            var user = await _dataContext.GetUserFromSessionAsync(HttpContext.User);
            var service = new OrderService(user.ShopifyShopDomain, user.ShopifyAccessToken);
            
            // Build a list filter to get the requested page of orders
            var limit = 50;
            var orderFields = "name,id,customer,line_items,created_at";
            var filter = new ListFilter<Order>(pageInfo, limit, orderFields);
            var orders = await service.ListAsync(filter);
            
            return View(new OrdersViewModel
            {
                Orders = orders.Items.Select(o => new OrderSummary(o)),
                NextPage = orders.GetNextPageFilter()?.PageInfo,
                PreviousPage = orders.GetPreviousPageFilter()?.PageInfo
            });
        }
    }
}
