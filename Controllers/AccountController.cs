using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Qaydak.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Invoice");
            }

            if (result.IsLockedOut)
            {
                ViewBag.Error = "الحساب مقفول مؤقتًا بسبب محاولات دخول فاشلة متكررة، حاول لاحقًا";
            }
            else
            {
                ViewBag.Error = "اسم المستخدم أو كلمة المرور غلط";
            }

            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

    }
}