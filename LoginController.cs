using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class LoginController : Controller
    {
        Train_DbEntities2 db = new Train_DbEntities2();
        [HttpGet]
        public ActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UserLogin(user u)
        {
            if (ModelState.IsValid)
            {
                var data = db.users.FirstOrDefault(x => x.username == u.username && x.pass == u.pass);
                if (data != null)
                {
                    Session["UserID"] = data.id;
                    Session["Username"] = data.username;
                    return RedirectToAction("UserDashboard", "Reservation");
                }
                else
                {
                    ViewBag.Error = "Invalid username or password.";
                }
            }
            return View(u);
        }


        [HttpGet]
        public ActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AdminLogin(admin a)
        {
            var data = db.admins.FirstOrDefault(x => x.username == a.username && x.pass == a.pass);
            if (data != null)
            {
                Session["AdminID"] = data.id;
                Session["AdminName"] = data.username;
                return RedirectToAction("AdminDashboard", "Admin");
            }
            else
            {
                ViewBag.Error = "Invalid Admin Credentials";
                return View();
            }
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("UserLogin");
        }
    }
}
