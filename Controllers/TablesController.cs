using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DAC.Models;

namespace DAC.Controllers
{
    public class TablesController : Controller
    {
        //cmd to retrieve rows
        private const string getRowsCmdText = @"SELECT * FROM ";
        //cmd to insert row
        private const string insertRowCmdText = @"INSERT INTO ";
        //cmd to update row
        private const string updateRowCmdText = @"UPDATE ";
        //cmd to delete row
        private const string deleteRowCmdText = @"DELETE FROM ";
        //connection string
        private readonly string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;
        //list containing string like data types
        private readonly List<string> stringLikeDataTypes = new List<string>() { "bit", "date", "datetimeoffset", "datetime", "datetime2", "smalldatetime", "time", "char", "varchar", "text", "nchar", "nvarchar", "ntext" };
        //list containing int like data types
        private readonly List<string> intLikeDataTypes = new List<string>() { "int", "bigint", "numeric", "smallint", "decimal", "smallmoney", "tinyint", "money" };
        //list containing float like data types
        private readonly List<string> floatLikeDataTypes = new List<string>() { "float", "real" };

        //get user permissions for all available tables, returns bool value inidicating whether data was successfully retrieved from database
        [NonAction]
        private bool GetUserPermissions(string username, out List<UserPermissions> permissions)
        {
            permissions = new List<UserPermissions>();
            List<string> tableNames = DbManager.GetTableNames();
            tableNames.Remove("Historia");

            UserPermissions u = null;
            foreach (string tableName in tableNames)
            {
                if (!GetUserPermissionsForTable(username, tableName, out u))   
                    return false;
                else
                    permissions.Add(u);
            }
            return true;
        }

        //get user permissions for specific table, returns bool value inidicating whether data was successfully retrieved from database
        [NonAction]
        private bool GetUserPermissionsForTable(string username, string tableName, out UserPermissions userPermissions)
        {
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(tableName);
            string cmdText = @"SELECT Uprawnienia" + tableName + " FROM Uzytkownicy WHERE Nazwa = '" + username + "';";

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(cmdText, connection);
                SqlDataReader reader;
                try
                {
                    connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    userPermissions = new UserPermissions(tableName, "0000");
                    return false;
                }

                if (reader.HasRows)
                {
                    reader.Read();
                    string p = Convert.ToString(reader.GetValue(0));
                    userPermissions = new UserPermissions(tableName, p);
                    reader.Close();
                    return true;
                }
                else
                {
                    userPermissions = new UserPermissions(tableName, "0000");
                    return false;
                }
            }
        }

        //list all available tables
        [Authorize]
        [HttpGet]
        public ActionResult Index()
        {
            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
              return RedirectToAction("Clear", "Login");

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handler errors
            }

            //get user permissions for all availavle tables
            List<UserPermissions> userPermissions;
            if(!GetUserPermissions(username, out userPermissions))
            {
                //TO DO: handler errors
            }
                
            return View(userPermissions);
        }

        //summary
        [Authorize]
        [HttpGet]
        public ActionResult Summary(string Name)
        {
            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(Name))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            UserPermissions u;
            if(!GetUserPermissionsForTable(username, Name, out u))
            {
                //TO DO: handle errors
                ViewBag.PermissionSelect = Permission.No;
                ViewBag.PermissionInsert = Permission.No;
                ViewBag.PermissionUpdate = Permission.No;
                ViewBag.PermissionDelete = Permission.No;
            }
            else
            {
                ViewBag.PermissionSelect = u.PermissionSelect;
                ViewBag.PermissionInsert = u.PermissionInsert;
                ViewBag.PermissionUpdate = u.PermissionUpdate;
                ViewBag.PermissionDelete = u.PermissionDelete;
            }

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handle errors
            }

            //possible messages if redirect happened
            ViewBag.FailureMsgText = TempData["FailureMsgtext"] as string;
            ViewBag.SuccessMsgText = TempData["SuccessMsgtext"] as string;

            //fetch data about columns 
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(Name);

            if (TempData["Select"] != null)
            {
                //each list == values by columns
                List<List<object>> values = new List<List<object>>();

                //add as many lists as many columns there is
                for (int i = 0; i < columnsInfo.Count; i++)
                    values.Add(new List<object>());

                //populate lists
                using (SqlConnection connection = new SqlConnection(connectionStr))
                {
                    SqlCommand cmd = new SqlCommand(getRowsCmdText + Name, connection);
                    SqlDataReader reader;

                    try
                    {
                        cmd.Connection.Open();
                        reader = cmd.ExecuteReader();
                    }
                    catch (SqlException)
                    {
                        //TO DO: handle errors
                        return View(new TableData(values, new TableInfo(columnsInfo, Name)));
                    }

                    //get rows
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < values.Count; i++)
                            {
                                values[i].Add(reader.GetValue(i));
                            }
                        }
                    }
                    reader.Close();
                }

                ViewBag.SelectPerformed = true;
                return View(new TableData(values, new TableInfo(columnsInfo, Name)));
            }
            else
            {
                ViewBag.SelectPerformed = false;
                return View(new TableData(null, new TableInfo(columnsInfo, Name)));
            }
        }

        //insert
        [Authorize]
        [HttpGet]
        public ActionResult Insert(string Name)
        {
            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(Name))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handler errors
            }

            //check if user has permission to insert to this table
            UserPermissions u;
            if (!GetUserPermissionsForTable(username, Name, out u))
            {
                //TO DO: handle errors
            }
            if (u.PermissionInsert == Permission.No)
                return RedirectToAction("Index", "Tables");

            //fetch data about columns 
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(Name);

            return View(new TableInfo(columnsInfo, Name));
        }

        //insert
        [HttpPost]
        public ActionResult Insert(FormCollection forms)
        {
            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(forms["tableName"]))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handler errors
            }

            //fetch data about columns 
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(forms["tableName"]);

            string cmdText = insertRowCmdText + forms["tableName"] + " ";
            bool onlyIdentityColumns = true;

            foreach (ColumnInfo columnInfo in columnsInfo)
            {
                if (!columnInfo.IsIdentity)
                {
                    onlyIdentityColumns = false;
                    break;
                }
            }

            if (onlyIdentityColumns)
                cmdText += "DEFAULT VALUES;";
            else
            {
                cmdText += "VALUES (";

                for (int i = 0; i < forms.Count - 1; i++)
                {
                    if (!columnsInfo[i].IsIdentity)
                    {
                        if (stringLikeDataTypes.Contains(columnsInfo[i].DataType))
                        {
                            cmdText += "'" + forms[i] + "'";
                        }
                        else
                        {
                            cmdText += forms[i];
                        }

                        if (i != forms.Count - 2 && !columnsInfo[i + 1].IsIdentity)
                            cmdText += ",";
                    }
                }

                cmdText += ");";
            }

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(cmdText, connection);

                try
                {
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    TempData["FailureMsgText"] = "Nie udało się wstawić rekordu.";
                    return RedirectToAction("Summary", new { Name = forms["tableName"] });
                }
            }

            TempData["SuccessMsgText"] = "Rekord został wstawiony.";
            return RedirectToAction("Summary", new { Name = forms["tableName"] });
        }

        //update
        [Authorize]
        [HttpGet]
        public ActionResult Edit(string Name, int Id)
        {
            string tableName = Name;

            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(tableName))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handler errors
            }

            //check if user has permission to insert to this table
            UserPermissions u;
            if (!GetUserPermissionsForTable(username, Name, out u))
            {
                //TO DO: handle errors
            }
            if (u.PermissionUpdate == Permission.No)
                return RedirectToAction("Index", "Tables");

            //fetch data about columns 
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(tableName);

            //each list == values by columns
            List<List<object>> values = new List<List<object>>();

            //add as many lists as many columns there is
            for (int i = 0; i < columnsInfo.Count; i++)
                values.Add(new List<object>());

            //populate lists
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(getRowsCmdText + tableName, connection);
                SqlDataReader reader;

                try
                {
                    cmd.Connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    TempData["FailureMsgText"] = "Nie udało się pobrać danych na temat rekordu.";
                    return RedirectToAction("Summary", new { Name = tableName });
                }

                //get rows
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < values.Count; i++)
                        {
                            values[i].Add(reader.GetValue(i));
                        }
                    }
                }

                reader.Close();
            }

            List<object> rowValues = new List<object>();

            for (int i = 0; i < values.Count; i++)
            {
                rowValues.Add(values[i][Id]);
            }

            return View(new EntityData(Id, rowValues, new TableInfo(columnsInfo, tableName)));
        }

        //update
        [HttpPost]
        public ActionResult Edit(FormCollection forms)
        {
            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(forms["tableName"]))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handler errors
            }

            int rowId = Convert.ToInt32(forms["rowId"]);

            //fetch data about columns 
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(forms["tableName"]);

            string cmdText = updateRowCmdText + forms["tableName"] + " SET ";

            for (int i = 0; i < columnsInfo.Count; i++)
            {
                if (!columnsInfo[i].IsIdentity)
                {
                    cmdText += columnsInfo[i].ColumnName + " = ";
                    if (stringLikeDataTypes.Contains(columnsInfo[i].DataType))
                    {
                        cmdText += "'" + forms[i] + "'";
                    }
                    else
                    {
                        cmdText += forms[i];
                    }

                    if (i != columnsInfo.Count - 1 && !columnsInfo[i + 1].IsIdentity)
                    {
                        cmdText += ", ";
                    }
                }
            }

            cmdText += " WHERE ";

            //each list == values by columns
            List<List<object>> values = new List<List<object>>();

            //add as many lists as many columns there is
            for (int i = 0; i < columnsInfo.Count; i++)
                values.Add(new List<object>());

            //populate lists
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(getRowsCmdText + forms["tableName"], connection);
                SqlDataReader reader;

                try
                {
                    cmd.Connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    TempData["FailureMsgText"] = "Edycja rekordu nie powiodła się.";
                    return RedirectToAction("Summary", new { Name = forms["tableName"] });
                }

                //get rows
                if (reader.HasRows)
                    while (reader.Read())
                        for (int i = 0; i < values.Count; i++)
                            values[i].Add(reader.GetValue(i));

                reader.Close();
            }

            for (int i = 0; i < values.Count; i++)
            {
                cmdText += columnsInfo[i].ColumnName + " = ";

                if (stringLikeDataTypes.Contains(columnsInfo[i].DataType))
                    cmdText += "'" + Convert.ToString(values[i][rowId]) + "'";
                else if (intLikeDataTypes.Contains(columnsInfo[i].DataType))
                    cmdText += Convert.ToInt32(values[i][rowId]);
                else if (floatLikeDataTypes.Contains(columnsInfo[i].DataType))
                    cmdText += Convert.ToDouble(values[i][rowId]);

                if (i != values.Count - 1)
                    cmdText += " AND ";
            }

            cmdText += ";";

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(cmdText, connection);

                try
                {
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    TempData["FailureMsgText"] = "Edycja rekordu nie powiodła się.";
                    return RedirectToAction("Summary", new { Name = forms["tableName"] });
                }
            }

            TempData["SuccessMsgText"] = "Rekord został zedytowany.";
            return RedirectToAction("Summary", new { Name = forms["tableName"] });
        }

        //delete
        [Authorize]
        [HttpGet]
        public ActionResult Delete(string Name, int Id)
        {
            //table name
            string tableName = Name;

            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(tableName))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            //check if user is an administrator
            bool isAdmin;
            if (DbManager.IsAdmin(username, out isAdmin))
                ViewBag.Admin = isAdmin;
            else
            {
                //TO DO: handler errors
            }

            //check if user has permission to insert to this table
            UserPermissions u;
            if (!GetUserPermissionsForTable(username, Name, out u))
            {
                //TO DO: handle errors
            }
            if (u.PermissionDelete == Permission.No)
                return RedirectToAction("Index", "Tables");

            //fetch data about columns 
            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo(Name);

            //each list == values by columns
            List<List<object>> values = new List<List<object>>();

            //add as many lists as many columns there is
            for (int i = 0; i < columnsInfo.Count; i++)
                values.Add(new List<object>());

            //populate lists
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(getRowsCmdText + Name, connection);
                SqlDataReader reader;

                try
                {
                    cmd.Connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    TempData["FailureMsgText"] = "Nie udało się usunać rekordu.";
                    return RedirectToAction("Summary", new { Name = tableName });
                }

                //get rows
                if (reader.HasRows)
                    while (reader.Read())
                        for (int i = 0; i < values.Count; i++)
                            values[i].Add(reader.GetValue(i));

                reader.Close();
            }

            string cmdText = deleteRowCmdText + tableName + " WHERE ";

            for (int i = 0; i < values.Count; i++)
            {
                cmdText += columnsInfo[i].ColumnName + " = ";

                if (stringLikeDataTypes.Contains(columnsInfo[i].DataType))
                    cmdText += "'" + Convert.ToString(values[i][Id]) + "'";
                else if (intLikeDataTypes.Contains(columnsInfo[i].DataType))
                    cmdText += Convert.ToInt32(values[i][Id]);
                else if (floatLikeDataTypes.Contains(columnsInfo[i].DataType))
                    cmdText += Convert.ToDouble(values[i][Id]);

                if (i != values.Count - 1)
                    cmdText += " AND ";
            }

            cmdText += " ;";

            //delete 
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(cmdText, connection);
                SqlDataReader reader;

                try
                {
                    cmd.Connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    TempData["FailureMsgText"] = "Nie udało się usunać rekordu.";
                    return RedirectToAction("Summary", new { Name = tableName });
                }
            }

            TempData["SuccessMsgText"] = "Rekord został usunięty.";
            return RedirectToAction("Summary", new { Name = tableName });
        }

        //select
        [Authorize]
        [HttpGet]
        public ActionResult Select(string Name)
        {
            //table name
            string tableName = Name;

            //if the table does not exist redirect to index
            if (!DbManager.GetTableNames().Contains(tableName))
                return RedirectToAction("Index");

            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            //check if user has permission to insert to this table
            UserPermissions u;
            if (!GetUserPermissionsForTable(username, Name, out u))
            {
                //TO DO: handle errors
            }
            if (u.PermissionSelect == Permission.No)
                return RedirectToAction("Index", "Tables");

            TempData["Select"] = true;
            return RedirectToAction("Summary", new { Name = tableName });
        }

    }
}