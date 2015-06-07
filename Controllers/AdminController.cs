using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DAC.Models;
using System.Configuration;
using System.Data.SqlClient;

namespace DAC.Controllers
{
    public class AdminController : Controller
    {
        private const string successMsg = "Polecenie wykonane!";
        private const string failureMsg = "Nie udało się wykonać polecenia!";

        [HttpGet]
        public ActionResult Index()
        {
            CmdInfo c = TempData["cmdInfo"] as CmdInfo;
            return View(c);
        }

        [HttpPost]
        [ActionName("ExecuteCommand")]
        public ActionResult ProcessSqlCmd(FormCollection forms)
        {
            CmdInfo c = new CmdInfo(forms["cmdText"], true, successMsg);         
            string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand com = new SqlCommand(c.CmdText, connection);

                try
                {
                    com.Connection.Open();
                    com.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    c.Success = false;
                    c.Msg = failureMsg;
                    TempData["cmdInfo"] = c;
                    return RedirectToAction("Index");
                }
            }

            TempData["cmdInfo"] = c;
            return RedirectToAction("Index");
        }
    }
}