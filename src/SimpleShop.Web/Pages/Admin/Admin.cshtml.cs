using Microsoft.AspNetCore.Mvc.RazorPages;


namespace SimpleShop.Web.Pages.Admin
{
    //[Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {

        public void OnGet()
        {
            Console.WriteLine("admin model !!!");
        }
    }
}
