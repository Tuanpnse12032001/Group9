using Microsoft.AspNetCore.Mvc;

namespace Group9.Controllers
{
    [Route("auth-ui")]
    public class AuthPageController : Controller
    {
        [HttpGet("")]
        [HttpGet("login")]
        public IActionResult Login()
        {
            return View("~/Views/AuthPage/Login.cshtml");
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            return View("~/Views/AuthPage/Register.cshtml");
        }

        [HttpGet("profile")]
        public IActionResult Profile()
        {
            return View("~/Views/AuthPage/Profile.cshtml");
        }

        [HttpGet("files")]
        public IActionResult Files()
        {
            return View("~/Views/AuthPage/Files.cshtml");
        }

        [HttpGet("forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/AuthPage/ForgotPassword.cshtml");
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword()
        {
            return View("~/Views/AuthPage/ResetPassword.cshtml");
        }
    }
}