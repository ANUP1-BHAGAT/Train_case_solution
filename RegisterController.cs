using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using WebApplication2.Views.Register;

namespace WebApplication2.Controllers
{
    public class RegisterController : Controller
    {
        private Train_DbEntities2 db = new Train_DbEntities2(); // Your EF DB context


        [HttpGet]
        public ActionResult User()
        {
            return View();
        }

        [HttpPost]
        public ActionResult User(UserRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool isUsernameTaken = db.users.Any(u => u.username == model.Username);
                if (isUsernameTaken)
                {
                    ModelState.AddModelError("Username", "Username is already taken.");
                }
                bool isEmailTaken = db.users.Any(u => u.email == model.Email);
                if (isEmailTaken)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                }
                if (!isUsernameTaken && !isEmailTaken)
                {
                    user newUser = new user
                    {
                        username = model.Username,
                        first_name = model.FirstName,
                        last_name = model.LastName,
                        pass = model.Pass,
                        email = model.Email,
                        mobile_no = model.MobileNo
                    };

                    db.users.Add(newUser);
                    db.SaveChanges();

                    return RedirectToAction("UserLogin", "Login");
                }
            }

            return View(model);
        }


        [HttpGet]
        public ActionResult Admin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Admin(AdminRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool isUsernameTaken = db.admins.Any(a => a.username == model.Username);
                if (isUsernameTaken)
                {
                    ModelState.AddModelError("Username", "Username is already taken.");
                }
                bool isEmailTaken = db.admins.Any(a => a.email == model.Email);
                if (isEmailTaken)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                }

                if (!isUsernameTaken && !isEmailTaken)
                {
                    admin newAdmin = new admin
                    {
                        username = model.Username,
                        pass = model.Pass,
                        email = model.Email
                    };

                    db.admins.Add(newAdmin);
                    db.SaveChanges();

                    return RedirectToAction("AdminLogin", "Login");
                }
            }

            return View(model); 
        }
        //public ActionResult Success()
        //{
        //    return View();
        //}
    }
}
