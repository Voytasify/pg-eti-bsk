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
        //success message text
        private const string successMsgText = "Polecenie wykonane.";
        //failure message text
        private const string failureMsgText = "Nie udało się wykonać polecenia.";
        //connection string
        private readonly string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;
        //cmd to add column to users table
        private const string addColumnCmdText = @"ALTER TABLE Uzytkownicy ADD Uprawnienia";
        //cmd to remove column form users table
        private const string removeColumnCmdText = @"ALTER TABLE Uzytkownicy DROP COLUMN Uprawnienia";
        //cmd to drop constraints on specific column
        private readonly string[] dropConstraintsCmdText = { @"DECLARE @sql NVARCHAR(MAX)
            WHILE 1=1
            BEGIN
            SELECT TOP 1 @sql = N'alter table Uzytkownicy drop constraint ['+dc.NAME+N']'
            from sys.default_constraints dc
            JOIN sys.columns c
            ON c.default_object_id = dc.object_id
            WHERE 
            dc.parent_object_id = OBJECT_ID('Uzytkownicy')
            AND c.name = N'", @"' IF @@ROWCOUNT = 0 BREAK EXEC (@sql) END" };
        //cmd to set permissions to '2222' to the creator of the new table
        private const string updateTablePermissionsCmdText = @"UPDATE Uzytkownicy SET Uprawnienia";

        [NonAction]
        private bool addColumnToUsers(string tableName, string executorName)
        {
            bool result = true;

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand comAlter = new SqlCommand(addColumnCmdText + tableName + " VARCHAR(4) NOT NULL DEFAULT '0000';", connection);
                SqlCommand comUpdate = new SqlCommand(updateTablePermissionsCmdText + tableName + " = '2222' WHERE Nazwa = '" + executorName + "';", connection);
                try
                {
                    comAlter.Connection.Open();
                    comAlter.ExecuteNonQuery();
                    comUpdate.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    result = false;
                }
                finally
                {
                    comAlter.Connection.Close();
                }
            }

            return result;
        }

        [NonAction]
        private bool removeColumnFromUsers(string tableName)
        {
            bool result = true;

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmdDropConstraint = new SqlCommand(dropConstraintsCmdText[0] + "Uprawnienia" + tableName + dropConstraintsCmdText[1], connection);
                SqlCommand cmdDropColumn = new SqlCommand(removeColumnCmdText + tableName + ";", connection);

                try
                {
                    cmdDropConstraint.Connection.Open();
                    cmdDropConstraint.ExecuteNonQuery();
                    cmdDropColumn.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    result = false;
                }
                finally
                {
                    cmdDropConstraint.Connection.Close();
                }
            }

            return result;
        }

        [NonAction]
        private bool fixDbIntegrity(string executorName)
        {
            bool result = true;
            List<string> tableNames = DbManager.GetTableNames();
            tableNames.Remove("Historia");

            List<ColumnInfo> columnsInfo = DbManager.GetColumnsInfo("Uzytkownicy");

            //add columns for newly created tables
            bool columnExist;
            foreach (string t in tableNames)
            {
                columnExist = false;
                foreach (ColumnInfo c in columnsInfo)
                {
                    if (c.ColumnName.Contains("Uprawnienia"))
                    {
                        if (c.ColumnName.Substring(11) == t)
                        {
                            columnExist = true;
                            break;
                        }
                    }
                }

                if (!columnExist)
                {
                    if (!addColumnToUsers(t, executorName))
                    {
                        result = false;
                    }
                }
            }

            //remove columns for deleted tables
            foreach (ColumnInfo c in columnsInfo)
            {
                if (c.ColumnName.Contains("Uprawnienia"))
                {
                    if (!tableNames.Contains(c.ColumnName.Substring(11)))
                    {
                        if (!removeColumnFromUsers(c.ColumnName.Substring(11)))
                        {
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

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

            //if not admin - redirect to Tables
            if (!isAdmin)
                return RedirectToAction("Index", "Tables");

            CmdInfo c = TempData["cmdInfo"] as CmdInfo;
            return View(c);
        }

        [HttpPost]
        [ActionName("ExecuteCommand")]
        public ActionResult ProcessSqlCmd(FormCollection forms)
        {
            //try to fetch username from session data
            string username = Session["username"] as string;
            if (username == null)
                return RedirectToAction("Clear", "Login");

            CmdInfo c = new CmdInfo(forms["cmdText"], true, successMsgText);

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand com = new SqlCommand(c.CmdText, connection);

                try
                {
                    com.Connection.Open();
                    com.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    c.Success = false;
                    c.Msg = failureMsgText + "*" + ex.Message;
                }
                finally
                {
                    TempData["cmdInfo"] = c;
                    fixDbIntegrity(username);
                }

                return RedirectToAction("Index");
            }

        }
    }
}