using Microsoft.AspNetCore.Mvc;

using SimpleShop.Application.Queries.Carts;
using SimpleShop.Domain.Entities;

namespace SimpleShop.Web.ViewComponents
{
    public class BannerViewComponent : ViewComponent
    {


        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View("~/Pages/Shared/Components/Banner/Default.cshtml");
        }
    }
}