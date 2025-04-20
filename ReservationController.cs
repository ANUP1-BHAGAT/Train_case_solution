using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using WebApplication2.Views.Reservation;
using System.Data.Entity;
using WebApplication2.Views.ViewModels;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;


namespace WebApplication2.Controllers
{
    public class ReservationController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult UserDashboard()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("UserLogin", "Login");
            return View();
        }
        private readonly Train_DbEntities2 _context;

        public ReservationController()
        {
            _context = new Train_DbEntities2();
        }
        //[HttpGet]
        //public ActionResult Create()
        //{
        //    return View(new ReservationFormViewModel());
        //}
        public ActionResult SelectTrain(string source, string destination, DateTime? startDate)
        {
            var query = _context.train_details
            .Where(t => t.start_DateTime > DateTime.Now) 
            .AsQueryable();

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(t => t.source.Contains(source));
            }

            if (!string.IsNullOrEmpty(destination))
            {
                query = query.Where(t => t.destination.Contains(destination));
            }

            if (startDate.HasValue)
            {
                query = query.Where(t =>
                    DbFunctions.TruncateTime(t.start_DateTime) == DbFunctions.TruncateTime(startDate.Value));
            }

            var viewModel = new TrainSearchViewModel
            {
                Source = source,
                Destination = destination,
                StartDate = startDate,
                Trains = query.ToList()
            };

            return View(viewModel);
        }


        public ActionResult SelectClassQuota(int trainId)
        {
            var classes = _context.train_class
                .Include(c => c.quotas)
                .Where(c => c.train_id == trainId)
                .ToList();

            ViewBag.TrainId = trainId;
            return View(classes);
        }
        public ActionResult EnterDetails(int trainId, int classId, int quotaId)
        {
            var model = new ReservationFormViewModel
            {
                TrainId = trainId,
                ClassId = classId,
                QuotaId = quotaId,
                Passengers = new List<PassengerInputModel> { new PassengerInputModel() }
            };
            return View(model);
        }

        private decimal CalculateTotalAmount(Train_DbEntities context, int classId, int quotaId, int passengerCount)
        {
            var quota = context.quotas.FirstOrDefault(q => q.id == quotaId && q.class_id == classId);
            if (quota == null)
                return 0;

            return (decimal)(quota.price * passengerCount);
        }

        public ActionResult ConfirmReservation(ReservationFormViewModel model)
        {
            using (var context = new Train_DbEntities2())
            {
                int userId = Convert.ToInt32(Session["UserID"]);
                var user = context.users.FirstOrDefault(u => u.id == userId);

                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View("EnterDetails", model);
                }

                var classObj = context.train_class.FirstOrDefault(c => c.id == model.ClassId);
                var quotaObj = context.quotas.FirstOrDefault(q => q.id == model.QuotaId);

                if (quotaObj == null || model.Passengers.Count > quotaObj.seating_capacity)
                {
                    TempData["BookingError"] = $"Only {quotaObj?.seating_capacity} seat(s) available, but you tried to book {model.Passengers.Count}.";
                    return RedirectToAction("BookingSuccess", new { pnr = "NA" });
                }


                var bank = new bank_details
                {
                    user = user.username,
                    bank_name = model.BankName,
                    credit_card_no = model.CreditCardNo
                };
                context.bank_details.Add(bank);
                context.SaveChanges();

                var reservation = new reservation
                {
                    user_id = user.id,
                    train_id = model.TrainId,
                    bank_details_id = bank.id,
                    created_date = DateTime.Now,
                    current_status = "Pending",
                    amount_paid = CalculateTotalAmount(context, model.ClassId, model.QuotaId, model.Passengers.Count)
                };
                context.reservations.Add(reservation);
                context.SaveChanges();

                foreach (var p in model.Passengers)
                {
                    var SeatNo = "S" + quotaObj.seating_capacity.ToString();

                    var seat = new seat_details
                    {
                        class_name = classObj?.class_name,
                        quota_name = quotaObj.quota_name,
                        seat_no = SeatNo
                    };

                    context.seat_details.Add(seat);
                    context.SaveChanges();

                    var passenger = new passenger
                    {
                        reservation_id = reservation.id,
                        seat_id = seat.id,
                        name = p.Name,
                        age = p.Age,
                        gender = p.Gender,
                        address = p.Address
                    };

                    context.passengers.Add(passenger);
                    quotaObj.seating_capacity -= 1;
                    context.SaveChanges();
                }

                context.SaveChanges();
                return RedirectToAction("BookingSuccess");
            }
        }

        public ActionResult BookingSuccess(string pnr)
        {
            ViewBag.PNR = pnr;
            ViewBag.BookingError = TempData["BookingError"];
            return View();
        }


        public ActionResult CancelList()
        {
            var viewModelList = _context.reservations
                .Select(r => new ReservationWithTrainViewModel
                {
                    ReservationId = r.id,
                    PnrNo = r.pnr_no,
                    Status = r.current_status,
                    CreatedDate = r.created_date,
                    AmountPaid = r.amount_paid,
                    TrainName = r.train_details.train_name,
                    TrainNumber = r.train_details.train_no,
                    Source = r.train_details.source,
                    Destination = r.train_details.destination
                }).ToList();

            return View(viewModelList);
        }
        [HttpGet]
        public ActionResult Cancel(int id)
        {
            var reservation = _context.reservations.Find(id);
            if (reservation == null)
                return HttpNotFound();

            var model = new CancellationViewModel
            {
                ReservationId = reservation.id,
                PNRNo = reservation.pnr_no,
                ReservationDate = reservation.created_date,
                CurrentStatus = reservation.current_status,
                AmountPaid = reservation.amount_paid,
                CancellationStatus = "Cancelled",
                RefundAmount = reservation.amount_paid * 0.5m  
            };

            return View(model);
        }


        [HttpPost]
        public ActionResult Cancel(CancellationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var reservation = _context.reservations.Find(model.ReservationId);
                if (reservation == null)
                    return HttpNotFound();

                reservation.current_status = "Cancelled";

                var refundAmount = reservation.amount_paid * 0.5m;
                var status = "Cancelled";

                var cancellation = new cancellation_details
                {
                    reservation_id = reservation.id,
                    date = DateTime.Now,
                    status = status,
                    refund_amt = refundAmount
                };

                _context.cancellation_details.Add(cancellation);
                _context.SaveChanges();

                TempData["Message"] = "Reservation Cancelled Successfully";
                return RedirectToAction("UserDashboard");
            }

            return View(model);
        }

    }
}