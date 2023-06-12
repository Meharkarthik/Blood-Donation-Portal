using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BloodDonation.classes;
using MongoDB.Driver;

namespace BloodDonation.Controllers
{
    public class DonationController : Controller
    {
        [HttpGet]
        public ActionResult LogMeIn()
        {
            if(Session["loggedinuser"]==null)
            {
                return View();
            }
            else
            {
                Session.Clear();
                return RedirectToAction("homepage");
            }
            
        }

        [HttpPost]
        public ActionResult LogMeIn(string userid,string password)
        {
            // check in database
            IMongoCollection<user> userscollection = connecttouserscollection();
            var builder = Builders<user>.Filter;
            var filter = builder.Eq(x => x.userid, userid) & builder.Eq(x => x.password, password);
            long count = userscollection.Find(filter).CountDocuments();
            if(count==0)
            {
                //creds not present in database
                ViewBag.error = "You have entered wrong credentials!!";
                return View();
            }
            else
            {
                //successfully authenticated
                user currentuser= userscollection.Find(x => x.userid == userid).FirstOrDefault();
                Session["loggedinuser"] = userid;
                Session["usertype"] = currentuser.usertype;
                return RedirectToAction("homepage");
            }


        }

     public ActionResult homepage()
        {
            return View();
        }

        public static IMongoCollection<user> connecttouserscollection()
        {
            string constring = "mongodb://localhost:27017";
        MongoClient client = new MongoClient(constring);
        IMongoDatabase database = client.GetDatabase("blooddonation");
        IMongoCollection<user> userscollection = database.GetCollection<user>("users");
            return userscollection;
    }

        public static IMongoCollection<eligibilityrequirement> connectoeligibilityrequirementscollection()
        {
            string constring = "mongodb://localhost:27017";
            MongoClient client = new MongoClient(constring);
            IMongoDatabase database = client.GetDatabase("blooddonation");
            IMongoCollection<eligibilityrequirement> elig = database.GetCollection<eligibilityrequirement>("eligibilityrequirements");
            return elig;
        }
        public static IMongoCollection<venue> connecttovenuescollection()
        {
            string constring = "mongodb://localhost:27017";
            MongoClient client = new MongoClient(constring);
            IMongoDatabase database = client.GetDatabase("blooddonation");
            IMongoCollection<venue> listofvenues = database.GetCollection<venue>("venues");
            return listofvenues;
        }

        public static IMongoCollection<slot> connecttoslotscollection()
        {
            string constring = "mongodb://localhost:27017";
            MongoClient client = new MongoClient(constring);
            IMongoDatabase database = client.GetDatabase("blooddonation");
            IMongoCollection<slot> slotscollection = database.GetCollection<slot>("slots");
            return slotscollection;
        }

        public ActionResult vieweligibility()
        {
            IMongoCollection<eligibilityrequirement> list = connectoeligibilityrequirementscollection();
            List<eligibilityrequirement> listofall = list.Find(x => true).ToList();            
            return View(listofall);
        }

        [HttpGet]
        public ActionResult bookslot()
        {
            IMongoCollection<venue> collection = connecttovenuescollection();
            List<venue> allvenues = collection.Find(x => true).ToList();
            List<string> venuenames = new List<string>();
            foreach(venue v in allvenues)
            {
                venuenames.Add(v.name);
            }
            return View(venuenames);
        }

        [HttpPost]
        public ActionResult bookslot(string bookingdate,string venuename)
        {
            slot obj = new slot();
            IMongoCollection<slot> slotscollection = connecttoslotscollection();
            if (slotscollection.Find(x => x.bookingdate== DateTime.Parse(bookingdate)).CountDocuments() > 0)
            {
                ViewBag.errormesg = "This time slot is already booked by some other donor. Please chose a different time!!";
                IMongoCollection<venue> collection = connecttovenuescollection();
                List<venue> allvenues = collection.Find(x => true).ToList();
                List<string> venuenames = new List<string>();
                foreach (venue v in allvenues)
                {
                    venuenames.Add(v.name);
                }
                return View(venuenames);
            }
            else
            {

                long count = slotscollection.Find(x => true).Count();
                obj.slotid = (int)(count + 1);
                obj.userid = Session["loggedinuser"].ToString();
                obj.bookingdate = DateTime.Parse(bookingdate);
                obj.venuename = venuename;
                obj.status = "booked";
                slotscollection.InsertOne(obj);
                return RedirectToAction("slotbooked", new { id = obj.slotid });
            }
        }

        public ActionResult slotbooked(int id)
        {
            ViewBag.notice = "Slot Booked successfully. Booking ID is " + id;
            return View();
        }

        [HttpGet]
        public ActionResult deleteslot()
        {
            IMongoCollection<slot> allslots = connecttoslotscollection();
            List<slot> allmyslots = allslots.Find(x => x.userid == Session["loggedinuser"].ToString()).ToList();
            return View(allmyslots);
        }

        public ActionResult Delete(int id)
        {
            IMongoCollection<slot> allslots = connecttoslotscollection();
            allslots.DeleteOne(x => x.slotid == id);
            return RedirectToAction("deleteslot");
        }

        [HttpGet]
        public ActionResult searchdonors()
        {
            return View();
        }

        [HttpPost]
        public ActionResult searchdonors(string bloodgroup)
        {
            IMongoCollection<user> allusers = connecttouserscollection();
            var builder = Builders<user>.Filter;
            var filter = builder.Eq(x => x.usertype, "donor") & builder.Eq(x => x.bloodgroup,bloodgroup);
            long count = allusers.Find(filter).CountDocuments();
            if(count==0)
            {
                return View("nodonorsfound");
            }
            else
            {
                List<user> userslist = allusers.Find(filter).ToList();
                return View("donorslist",userslist);
            }

        }

        public ActionResult viewstats()
        {
            IMongoCollection<slot> allslots = connecttoslotscollection();
            List<slot> slots = allslots.Find(x => true).ToList();
            foreach(slot s in slots)
            {
                if(s.bookingdate<DateTime.Now)
                {
                    s.status = "Donated";
                }
            }
            return View(slots);
        }

        [HttpGet]
        public ActionResult addvenue()
        {
            return View();
        }

        [HttpPost]
        public ActionResult addvenue(string venue)
        {
            IMongoCollection<venue> allvenues = connecttovenuescollection();
            long counter=allvenues.Find(x => true).CountDocuments();
            venue obj = new venue();
            obj.venueid = (int)(counter + 1);
            obj.name = venue;
            allvenues.InsertOne(obj);
            return View("homepage");
        }

        [HttpGet]
        public ActionResult createdoctorlogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult createdoctorlogin(user obj)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            else
            {
                obj.usertype = "doctor";
                IMongoCollection<user> userscollection = connecttouserscollection();
                userscollection.InsertOne(obj);
                return View("homepage");
            }
        }

        [HttpGet]
        public ActionResult addneweligibility()
        {
            return View();
        }

        [HttpPost]
        public ActionResult addneweligibility(string eligiblity)
        {
            IMongoCollection<eligibilityrequirement> alleligibilites = connectoeligibilityrequirementscollection();
            long counter = alleligibilites.Find(x => true).CountDocuments();
            eligibilityrequirement obj = new eligibilityrequirement();
            obj.requirementid = (int)(counter + 1);
            obj.description = eligiblity;
            alleligibilites.InsertOne(obj);
            return View("homepage");
        }

        
        [HttpGet]
        public ActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Signup(user obj)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            else if(obj.password!=obj.confirmpassword)
                {
                ViewBag.error = "Password and confirm password should be same";
                return View();
            }
            else
            {
                obj.usertype = "donor";
                IMongoCollection<user> userscollection = connecttouserscollection();
                userscollection.InsertOne(obj);
                return View("homepage");
            }
        }

    }
}