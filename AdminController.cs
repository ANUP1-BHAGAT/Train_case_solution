using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using WebApplication2.Views.Admin;

namespace WebApplication2.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult AdminDashboard()
        {
            if (Session["AdminID"] == null)
                return RedirectToAction("AdminLogin", "Login");

            return View();
        }
        //public ViewResult Index()
        //{
        //    return View();
        //}

        public ActionResult ViewTrainData()
        {
            try
            {
                Train_DbEntities2 rdc = new Train_DbEntities2();
                List<TrainTable> t = (from q in rdc.quotas
                                      join tc in rdc.train_class on q.class_id equals tc.id
                                      join td in rdc.train_details on tc.train_id equals td.id
                                      select new TrainTable()
                                      {
                                          price = q.price,
                                          seating_capacity = q.seating_capacity,
                                          quota_name = q.quota_name,
                                          class_name = tc.class_name,
                                          end_time = td.end_DateTime,
                                          start_time = td.start_DateTime,
                                          source = td.source,
                                          destination = td.destination,
                                          train_name = td.train_name,
                                          train_no = td.train_no
                                      }).ToList();
                return View(t);
            }
            catch (Exception ex)
            {

                return Content($"Something went worng : {ex.Message}");
            }
        }

        public ViewResult AddData()
        {
            var model = new TrainDetails();

            var classList = new[] { "First Ac", "Second Ac", "Sleeper Class" };
            var quotaList = new[] { "General Quota", "Ladies Quota", "Tatkal Quota" };

            model.class_list = new List<class_name>();

            foreach (var className in classList)
            {
                var classItem = new class_name
                {
                    Name = className,
                    quota_list = new List<quota_name>()
                };

                foreach (var quotaName in quotaList)
                {
                    classItem.quota_list.Add(new quota_name
                    {
                        Name = quotaName
                    });
                }

                model.class_list.Add(classItem);
            }

            return View(model);
        }
        public ViewResult DeleteData()
        {
            return View();
        }
        public ActionResult Submittrain_details(TrainDetails t)
        {
            try
            {
                using (Train_DbEntities2 rdc = new Train_DbEntities2())
                {
                    train_details td = new train_details() { end_DateTime = t.end_time, start_DateTime = t.start_time, train_name = t.train_name, train_no = t.train_no, destination = t.destination, source = t.source };
                    rdc.train_details.Add(td);
                    rdc.SaveChanges();
                    td = rdc.train_details.FirstOrDefault(i => i.train_no == t.train_no);
                    var classList = new[] { "First Ac", "Second Ac", "Sleeper Class" };
                    var quotaList = new[] { "General Quota", "Ladies Quota", "Tatkal Quota" };
                    for (int i = 0; i < t.class_list.Count; i++)
                    {
                        var className = classList[i];
                        if (className == null || className == "") return Content("xyxz");
                        train_class tc = new train_class() { class_name = className, train_id = td.id };
                        rdc.train_class.Add(tc);
                        rdc.SaveChanges();

                        tc = rdc.train_class.FirstOrDefault(j => j.class_name == className && j.train_id == td.id);

                        for (int k = 0; k < t.class_list[i].quota_list.Count; k++)
                        {
                            quota q = new quota()
                            {
                                quota_name = quotaList[k],
                                price = t.class_list[i].quota_list[k].price,
                                seating_capacity = t.class_list[i].quota_list[k].seating_capacity,
                                class_id = tc.id
                            };
                            rdc.quotas.Add(q);
                            rdc.SaveChanges();
                        }
                    }
                }
                return RedirectToAction("AdminDashboard");
            }
            catch (Exception ex)
            {
                return Content($"Something went worng : {ex.Message}");
            }
        }
        public ActionResult DeleteByTrainNo(TrainNum t)
        {
            try
            {
                using (var rdc = new Train_DbEntities2())
                {
                    var td = rdc.train_details.FirstOrDefault(i => i.train_no == t.Num);
                    if (td != null)
                    {
                        var r = rdc.reservations.FirstOrDefault(i => i.train_id == td.id);
                        if (r != null) return Content($"Unable to delete train details , reservation is done for {td.train_name}");
                        var tcList = rdc.train_class.Where(c => c.train_id == td.id).ToList();
                        if (tcList.Count > 0)
                        {
                            foreach (var tc in tcList)
                            {
                                var quotaList = rdc.quotas.Where(q => q.class_id == tc.id).ToList();
                                if (quotaList.Count > 0)
                                {
                                    foreach (var q in quotaList)
                                    {
                                        rdc.quotas.Remove(q);
                                    }
                                    rdc.SaveChanges();
                                }
                            }

                            foreach (var tc in tcList)
                            {
                                rdc.train_class.Remove(tc);
                            }

                            rdc.SaveChanges();
                        }

                        rdc.train_details.Remove(td);

                        rdc.SaveChanges();
                    }
                    else return Content("No Train Found to delete.");
                }

                return RedirectToAction("AdminDashboard");
            }
            catch (Exception ex)
            {
                return Content($"Something went worng : {ex.Message}");
            }
        }
        public ViewResult PendingReservations()
        {
            Train_DbEntities2 rdc = new Train_DbEntities2();
            List<PassengerReservation> pr = (from r in rdc.reservations
                                            join p in rdc.passengers on r.id equals p.reservation_id
                                            where r.current_status == "Pending"
                                            select new PassengerReservation()
                                            {
                                                id = r.id,
                                                name = p.name,
                                                age = p.age,
                                                gender = p.gender,
                                                address = p.address,
                                                pnr_no = r.pnr_no,
                                                current_status = r.current_status,
                                                created_date = r.created_date,
                                                amount_paid = r.amount_paid

                                            }).ToList();
            return View(pr);
        }

        public ViewResult ShowApprovedReservations()
        {
            Train_DbEntities2 rdc = new Train_DbEntities2();
            List<PassengerReservation> pr = (from r in rdc.reservations
                                            join p in rdc.passengers on r.id equals p.reservation_id
                                            where r.current_status == "Approved"
                                            select new PassengerReservation()
                                            {
                                                id = r.id,
                                                name = p.name,
                                                age = p.age,
                                                gender = p.gender,
                                                address = p.address,
                                                pnr_no = r.pnr_no,
                                                current_status = r.current_status,
                                                created_date = r.created_date,
                                                amount_paid = r.amount_paid

                                            }).ToList();
            return View(pr);
        }

        public ViewResult ApproveReservation()
        {
            return View();
        }

        public ActionResult ApproveByRId(PassengerReservation pr)
        {
            using (Train_DbEntities2 rdc = new Train_DbEntities2())
            {
                reservation r = rdc.reservations.FirstOrDefault(i => i.pnr_no == pr.pnr_no);
                if (r == null)
                {
                    r = rdc.reservations.FirstOrDefault(i => i.id == pr.id);
                    if (r != null)
                    {
                        r.pnr_no = pr.pnr_no;
                        r.current_status = "Approved";
                        rdc.SaveChanges();

                        int userId = Convert.ToInt32(Session["UserID"]);
                        var user = rdc.users.FirstOrDefault(u => u.id == userId);
                        var email = user.email; 
                        if (!string.IsNullOrEmpty(email))
                        {
                            string subject = "Reservation Approved";
                            string body = $"Dear<br/>" +
                                          $"Your reservation with PNR No: <strong>{r.pnr_no}</strong> has been approved.<br/>" +
                                          $"Thank you for booking with us!";

                            EmailService.SendEmail(email, subject, body);
                        }
                    }
                    else return Content("Reservation id not found. Please try again.");
                }
                else return Content("PNR Number must be unique. Please try again.");
            }
            return RedirectToAction("AdminDashboard");
        }
    }
}