﻿//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace WebGoatCore.Controllers
//{
//    public class EmailController : Controller
//    {
//        private UserManager<IdentityUser> userManager;
//        public EmailController(UserManager<IdentityUser> usrMgr)
//        {
//            userManager = usrMgr;
//        }

//        public async Task<IActionResult> ConfirmEmail(string token, string email)
//        {
//            var user = await userManager.FindByEmailAsync(email);
//            if (user == null)
//                return View("Error");

//            var result = await userManager.ConfirmEmailAsync(user, token);
//            return View(result.Succeeded ? "ConfirmEmail" : "Error");
//        }
//    }
//}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebGoatCore.Controllers
{
    public class EmailController : Controller
    {
        private UserManager<IdentityUser> _userManager;
        public EmailController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return View("Error");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }
    }
}