using System.Web;
using System.Web.Mvc;

namespace Mvc5Web.Controllers
{
    public class HomeController : Controller
    {
        public string Index()
        {
            return HttpContext.Current.User.Identity.Name;
        }
    }
}
