using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Data.SqlClient;
using System.Configuration;

namespace bsk.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        [HttpGet]
        public ActionResult Index()
        {
            //możlwia wiadomość o błędzie po nieprawidłowym logowaniu
            string errorMsg = TempData["errorMsg"] as string;

            //możliwe powiadomienie o poprawnym wylogowaniu
            string logoutMsg = TempData["logoutMsg"] as string;

            //jeżeli ktoś już jest zalogowany to nie ma dostępu do strony logowania - przekierowuję go od razu do Tables
            if (Session["username"] != null)
            {
                string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;
                using (SqlConnection sql = new SqlConnection(connectionStr))
                {
                    sql.Open();

                    SqlCommand com = new SqlCommand("select czyAdmin from Uzytkownicy where Nazwa = '" + Session["username"] as string + "'", sql);
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ViewBag.Admin = reader.GetBoolean(0) as bool?;
                        }
                    }
                }
                return RedirectToAction("Index", "Tables");
            }

            //jeżeli jest jakaś wiadomość o błędzie - dodaj do ViewBag
            if (errorMsg != null)
            {
                ViewBag.ErrorMsg = errorMsg;
            }

            //jeżeli jest powiadomienie o wylogowaniu - dodaj do ViewBag
            if (logoutMsg != null)
            {
                ViewBag.LogoutMsg = logoutMsg;
            }
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Index(FormCollection forms)
        {
            //pobranie nazwy użytkownika z formularza
            string username = forms["username"];

            //pobranie hasła z formularza
            string password = forms["password"];

            //połączenie z bazą danych i próba wyszukania użytkownika
            string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;
            string haslo = string.Empty;
            bool? czyAdmin = false;
            using (SqlConnection sql = new SqlConnection(connectionStr))
            {
                try
                {
                    sql.Open();

                    SqlCommand com = new SqlCommand("select Haslo, czyAdmin from Uzytkownicy where Nazwa = '" + username + "'", sql);
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            haslo = reader.GetString(0);
                            czyAdmin = reader.GetBoolean(1) as bool?;
                        }
                    }
                }
                catch (SqlException)
                {
                    TempData["errorMsg"] = "Wystąpił problem w połączeniu z bazą danych";
                    return RedirectToAction("Index", "Login");
                }
            }


            //jeżeli nie znaleziono użytkownika to redirect do ekranu logowania z odpowiednią informacją
            if (haslo == string.Empty)
            {
                TempData["errorMsg"] = "Uzytkownik o podanej nazwie nie istnieje";
                return RedirectToAction("Index", "Login");
            }
            //jeżeli znaleziono to trzeba jeszcze sprawdzić czy hasło jest poprawne
            else
            {
                //jeżeli hasło niepoprawne to redirect do ekranu logowania z odpowiednią informacją
                if (!haslo.Equals(password))
                {
                    TempData["errorMsg"] = "Podane hasło jest nieprawidłowe";
                    return RedirectToAction("Index", "Login");
                }
                //jeżeli hasło poprawne to ustawiamy zaszyfrowane ciasteczko, zmienne sesyjne i redirectujemy do My Tables
                else
                {
                    FormsAuthentication.SetAuthCookie(username, false);
                    Session.Timeout = 20;
                    Session.Add("username", username);

             //       ViewBag.Admin = czyAdmin;

                    return RedirectToAction("Index", "Tables");
                }
            }
        }

        //GET:  Logout
        [HttpGet]
        public ActionResult Logout()
        {
            //wyczyszczenie ciasteczek, sesji i redirect to ekranu logowania
            FormsAuthentication.SignOut();
            Session.RemoveAll();
            TempData["logoutMsg"] = "Wylogowano";
            return RedirectToAction("Index", "Login");
        }

        //GET: Clear
        [HttpGet]
        public ActionResult Clear()
        {
            //czyści all i przekierowuje do ekranu logowania
            FormsAuthentication.SignOut();
            Session.RemoveAll();
            return RedirectToAction("Index", "Login");
        }
    }
}