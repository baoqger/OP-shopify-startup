using System;
using System.Threading.Tasks;

namespace AuntieDot.Helpers
{
    public interface IOauthHelper
    {
        Task<Uri> CreateOauthUrl(string shop);
    }
}
