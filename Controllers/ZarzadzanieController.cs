using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;

namespace DAC.Controllers
{
    public class ZarzadzanieController : Controller
    {
        private class Historia
        {
            public string Operacja { get; set; }
            public DateTime Data { get; set; }
            public int Inicjujacy { get; set; }
            public int Otrzymujacy { get; set; }
            public string Command
            {
                get { return "insert into Historia(Operacja, Data, Inicjujacy, Otrzymujacy) values ('" + Operacja + "', '" + Data + "', '" + Inicjujacy + "', '" + Otrzymujacy + "')"; }
            }
        }

        private class dropName
        {
            public string val { get; set; }

            public override string ToString()
            {
                return val;
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Index(FormCollection forms)
        {
            string username = Session["username"] as string;

            if (username == null)
            {
                return RedirectToAction("Clear", "Login");
            }

            int userId = 0;
            int targetId = 0;
            int targetPrzejmij = 0;
            string tname = TempData["target"] as string;
            List<string> lista = TempData["lista"] as List<string>;
            List<string> tableNames = new List<string>();

            List<string> except = new List<string>();
            string[] perms = new string[2];

            string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;

            using (SqlConnection sql = new SqlConnection(connectionStr))
            {
                try
                {
                    sql.Open();
                    SqlCommand com = new SqlCommand("select Id, czyAdmin from Uzytkownicy where Nazwa = '" + username + "'", sql);
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userId = reader.GetInt32(0);
                            ViewBag.Admin = reader.GetBoolean(1) as bool?;
                        }
                    }

                    com.CommandText = "select Id, UprawnieniaPrzejmij from Uzytkownicy where Nazwa = '" + tname + "'";
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            targetId = reader.GetInt32(0);
                            targetPrzejmij = int.Parse(reader.GetString(1));
                        }
                    }

                    com.CommandText = com.CommandText = "select INFORMATION_SCHEMA.TABLES.TABLE_NAME from INFORMATION_SCHEMA.TABLES";
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                            tableNames.Add(reader.GetString(0));
                    }
                    tableNames.Remove("Historia");

                    Historia history = new Historia();
                    history.Inicjujacy = userId;
                    history.Otrzymujacy = targetId;
                    history.Data = DateTime.Now;

                    if (targetPrzejmij == 1)
                    {
                        history.Operacja = "przejmij";

                        Dictionary<string, string> prawa = new Dictionary<string, string>();

                        string tabele = "select ";
                        string remove = "update Uzytkownicy set ";
                        for (int i = 0; i < tableNames.Count; i++)
                        {
                            tabele += "Uprawnienia" + tableNames[i];
                            remove += "Uprawnienia" + tableNames[i] + "='0000'";
                            if (i != tableNames.Count - 1)
                            {
                                tabele += ", ";
                                remove += ", ";
                            }
                        }
                        tabele += " from Uzytkownicy where Nazwa = '" + username + "'";
                        remove += " where Nazwa = '" + username + "'";

                        com.CommandText = tabele;
                        using (SqlDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < tableNames.Count; i++)
                                    prawa.Add(tableNames[i], reader.GetString(i));
                            }
                        }

                        com.CommandText = remove;
                        com.ExecuteNonQuery();

                        string add = "update Uzytkownicy set ";
                        for (int i = 0; i < tableNames.Count; i++)
                        {
                            add += "Uprawnienia" + tableNames[i] + "='" + prawa[tableNames[i]] + "', ";
                        }
                        add += "UprawnieniaPrzejmij = '0'";
                        add += " where Nazwa = '" + tname + "'";

                        com.CommandText = add;
                        com.ExecuteNonQuery();

                        List<Historia> hist = new List<Historia>();
                        com.CommandText = "select Operacja, Data, Inicjujacy, Otrzymujacy from Historia";
                        using (SqlDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                                hist.Add(new Historia { Operacja = reader.GetString(0), Data = reader.GetDateTime(1), Inicjujacy = reader.GetInt32(2), Otrzymujacy = reader.GetInt32(3) });
                        }

                        hist = hist.OrderByDescending(x => x.Data).ToList();

                        List<int> ids = new List<int>();
                        ToRemove(userId, hist, ref ids);
                        ids.Add(userId);
                        ids.RemoveAll(x => x == targetId);
                        bool? czyAdmin = false;
                        com.CommandText = "select czyAdmin from Uzytkownicy where Nazwa = '" + username + "'";
                        using (SqlDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                                czyAdmin = reader.GetBoolean(0) as bool?;
                        }

                        if (czyAdmin == true)
                        {
                            com.CommandText = "update Uzytkownicy set czyAdmin = 'True' where Nazwa = '" + tname + "'";
                            com.ExecuteNonQuery();

                            com.CommandText = "update Uzytkownicy set czyAdmin = 'False' where Nazwa = '" + username + "'";
                            com.ExecuteNonQuery();
                        }
                        else
                        {
                            string cleaner = "update Uzytkownicy set ";
                            for (int i = 0; i < tableNames.Count; i++)
                            {
                                cleaner += "Uprawnienia" + tableNames[i] + "='0000'";
                                if (i != tableNames.Count - 1)
                                    cleaner += ", ";
                            }
                            cleaner += " where ";
                            for (int i = 0; i < ids.Count; i++)
                            {
                                cleaner += "Id = " + ids[i];
                                if (i != ids.Count - 1)
                                    cleaner += " OR ";
                            }

                            com.CommandText = cleaner;
                            com.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        history.Operacja = "przekaz";

                        Dictionary<string, string> prawa = new Dictionary<string, string>();
                        string tabele = "select ";
                        string[] splitter;

                        for (int i = 0; i < lista.Count; i++)
                        {
                            splitter = lista[i].Split(' ');
                            tabele += "Uprawnienia" + splitter[0];
                            if (i != lista.Count - 1)
                                tabele += ", ";
                        }

                        tabele += " from Uzytkownicy where Nazwa = '" + tname + "'";

                        com.CommandText = tabele;
                        using (SqlDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (!prawa.ContainsKey(lista[i].Split(' ')[0]))
                                        prawa.Add(lista[i].Split(' ')[0], reader.GetString(i));
                                }
                            }
                        }

                        string cmd = "update Uzytkownicy set ";
                        lista.Sort();

                        while (lista.Count != 0)
                        {
                            var asd = lista.FindAll(x => x.Split(' ')[0] == lista[0].Split(' ')[0]);            //choose a table name at random and get all passed permissions

                            asd.Sort();                                                                         //sort it by permissions granted
                            splitter = asd[0].Split(' ');                                                         //get the table name

                            var grant = from string upr in forms                                                //get permissions with grant                  
                                        where upr.Contains(splitter[0])
                                        select upr;

                            cmd += "Uprawnienia" + splitter[0] + "='";                                                              //add table name to command
                            for (int i = 0; i < 4; i++)
                            {
                                if (asd.Contains(splitter[0] + " " + i))                                             //if granting permission of index i
                                {
                                    if (prawa[splitter[0]][i] == '2' || (prawa[splitter[0]][i] == '1' && !grant.Contains(splitter[0] + " " + i)))
                                    {
                                        except.Add(asd[i]);                                                      //overwrite protection
                                        cmd += prawa[splitter[0]][i];
                                    }
                                    else
                                    {
                                        if (grant.Contains(splitter[0] + " " + i))
                                            cmd += "2";                                                         //add permission with grant
                                        else
                                            cmd += "1";                                                         //without grant
                                    }
                                }
                                else
                                {
                                    cmd += prawa[splitter[0]][i];                             //keep old permissions if index i not in the list
                                }
                            }
                            cmd += "'";
                            lista.RemoveAll(x => x.Split(' ')[0] == lista[0].Split(' ')[0]);
                            if (lista.Count != 0)
                                cmd += ", ";
                        }

                        cmd += " where Nazwa = '" + tname + "'";

                        com.CommandText = cmd;
                        com.ExecuteNonQuery();
                    }

                    com.CommandText = history.Command;
                    com.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    ViewBag.ErrMsg = "Wystąpił problem z połączeniem z bazą danych";
                    return RedirectToAction("Pass", "Zarzadzanie");
                }
            }

            TempData["msg"] = "Przekazano uprawnienia!";
            except.Sort();
            TempData["except"] = except;

            return RedirectToAction("Pass", "Zarzadzanie");
        }

        [Authorize]
        public ActionResult Pass()
        {
            string errMsg = TempData["errMsg"] as string;
            string msg = TempData["msg"] as string;
            TempData.Keep("except");
            if (TempData["except"] != null)
            {
                List<string> except = new List<string>(TempData["except"] as List<string>);
                if (except.Count != 0)
                {
                    ViewBag.Except = except;
                }
                else
                {
                    ViewBag.Except = null;
                }
            }

            if (errMsg != null)
            {
                ViewBag.ErrMsg = errMsg;
            }
            else
            {
                ViewBag.ErrMsg = string.Empty;
            }

            if (msg != null)
            {
                ViewBag.Msg = msg;
            }
            else
            {
                ViewBag.Msg = string.Empty;
            }

            string username = Session["username"] as string;

            if (username == null)
            {
                return RedirectToAction("Clear", "Login");
            }

            List<dropName> lista = new List<dropName>();
            string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;

            List<string> prawa = new List<string>();
            List<string> tnames = new List<string>();
            List<string> przejmujacy = new List<string>();

            using (SqlConnection sql = new SqlConnection(connectionStr))
            {
                try
                {
                    sql.Open();
                    SqlCommand com = new SqlCommand("SELECT Nazwa, UprawnieniaPrzejmij FROM Uzytkownicy WHERE Nazwa != '" + username + "'", sql);
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new dropName { val = reader.GetString(0) });
                            przejmujacy.Add(reader.GetString(0) + " " + reader.GetString(1));
                        }

                    }

                    com.CommandText = "select INFORMATION_SCHEMA.TABLES.TABLE_NAME from INFORMATION_SCHEMA.TABLES";
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                            tnames.Add(reader.GetString(0));
                    }

                    tnames.Remove("Historia");
                    tnames.Sort();

                    string tabele = "select ";
                    for (int i = 0; i < tnames.Count; i++)
                    {
                        tabele += "Uprawnienia" + tnames[i] + ", ";
                    }
                    tabele += "czyAdmin from Uzytkownicy where Nazwa = '" + username + "'";

                    com.CommandText = tabele;
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < tnames.Count; i++)
                                prawa.Add(reader.GetString(i));
                            ViewBag.Admin = reader.GetBoolean(tnames.Count) as bool?;
                        }
                    }
                }
                catch (SqlException)
                {
                    return RedirectToAction("Clear", "Login");
                }
            }

            ViewData["tnames"] = tnames;
            ViewBag.Permissions = prawa;
            ViewData["przejmujacy"] = przejmujacy;
            ViewData["list"] = new SelectList(lista);
            ViewBag.Lista = lista;
            return View();
        }

        [NonAction]
        private bool LoopCheck(int idUser, int idTarget, List<Historia> history)
        {
            Historia h = history.Find(x => (x.Inicjujacy == idUser || x.Inicjujacy == idTarget) && x.Operacja == "przejmij");
            if (h != null)
            {
                history.RemoveAll(x => x.Data <= h.Data);
            }

            List<Historia> hist = history.FindAll(x => x.Otrzymujacy == idUser && x.Operacja == "przekaz");
            foreach (Historia hi in hist)
            {
                if (hi.Inicjujacy == idTarget)
                    return true;
                else if (LoopCheck(hi.Inicjujacy, idTarget, history))
                    return true;
            }
            return false;
        }

        [NonAction]
        private void ToRemove(int idUser, List<Historia> history, ref List<int> users)
        {
            Historia h = history.Find(x => x.Inicjujacy == idUser && x.Operacja == "przejmij");
            if (h != null)
            {
                history.RemoveAll(x => x.Data <= h.Data);
            }
            List<Historia> hist = history.FindAll(x => x.Inicjujacy == idUser && x.Operacja == "przekaz");
            int count = hist.Count();
            for (int i = 0; i < count; i++)
            {
                if (hist[i].Inicjujacy == idUser && hist[i].Operacja == "przekaz")
                {
                    users.Add(hist[i].Otrzymujacy);
                    ToRemove(hist[i].Otrzymujacy, history, ref users);
                }
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Grant(FormCollection forms, string uname)
        {
            string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;
            int userId = 0;
            int targetId = 0;
            List<Historia> history = new List<Historia>();

            string username = Session["username"] as string;

            if (username == null)
            {
                return RedirectToAction("Clear", "Login");
            }

            using (SqlConnection sql = new SqlConnection(connectionStr))
            {
                try
                {
                    sql.Open();
                    SqlCommand com = new SqlCommand("select Id, czyAdmin from Uzytkownicy where Nazwa = '" + username + "'", sql);
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userId = reader.GetInt32(0);
                            ViewBag.Admin = reader.GetBoolean(1) as bool?;
                        }
                    }

                    com.CommandText = "select Id from Uzytkownicy where Nazwa = '" + uname + "'";
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                            targetId = reader.GetInt32(0);
                    }

                    com.CommandText = "select Operacja, Data, Inicjujacy, Otrzymujacy from Historia";
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                            history.Add(new Historia { Operacja = reader.GetString(0), Data = reader.GetDateTime(1), Inicjujacy = reader.GetInt32(2), Otrzymujacy = reader.GetInt32(3) });
                    }
                }
                catch (SqlException)
                {
                    ViewBag.ErrMsg = "Nie przekazano uprawnień - wystąpił problem z połączeniem z bazą";
                    return RedirectToAction("Pass", "Zarzadzanie");
                }
            }

            history = history.OrderByDescending(x => x.Data).ToList();
            if (forms.Count == 0 || (forms.Count == 1 && (forms.AllKeys.First(x => x == "uname") != null)))
            {
                TempData["errMsg"] = "Należy wybrać przynajmniej jedno uprawnienie do przekazania!";
                return RedirectToAction("Pass", "Zarzadzanie");
            }
            else if (LoopCheck(userId, targetId, history))
            {
                TempData["errMsg"] = "Nie można przekazać uprawnień temu użytkownikowi (pętla nadawań)!";
                return RedirectToAction("Pass", "Zarzadzanie");
            }
            else
            {
                ViewBag.Uname = uname;
                List<string> lista = new List<string>();
                foreach (string str in forms.AllKeys)
                {
                    if (str != "uname")
                        lista.Add(str);
                }
                lista.Sort();
                ViewData["lista"] = lista;
                return View();
            }
        }
    }
}