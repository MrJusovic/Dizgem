using Dizgem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dizgem.Filters
{
    /// <summary>
    /// Bu filtre, giriş yapmış ancak 2FA'yı etkinleştirmemiş kullanıcıları,
    /// 2FA kurulum sayfasına yönlendirir.
    /// </summary>
    public class Ensure2faFilter : IAsyncActionFilter
    {
        private readonly UserManager<User> _userManager;

        public Ensure2faFilter(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controllerName = context.Controller.GetType().Name;
            var actionName = context.ActionDescriptor.RouteValues["action"];

            // Yönlendirme döngüsünü önlemek için belirli controller'ları ve action'ları hariç tut
            if (context.HttpContext.User.Identity?.IsAuthenticated != true ||
                controllerName == "AccountController" ||
                controllerName == "InstallController" ||
                (controllerName == "ManageController" && actionName == "EnableAuthenticator"))
            {
                await next();
                return;
            }

            var user = await _userManager.GetUserAsync(context.HttpContext.User);
            if (user != null && !user.TwoFactorEnabled)
            {
                // Kullanıcı giriş yapmış ama 2FA aktif değilse, kurulum sayfasına yönlendir.
                context.Result = new RedirectToActionResult("EnableAuthenticator", "Manage", new { area = "DizgemAdmin" });
                return;
            }

            await next();
        }
    }
}
