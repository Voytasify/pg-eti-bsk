using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class DbManager
    {
        //connection string
        private static readonly string connectionStr = ConfigurationManager.ConnectionStrings["MainDbConnectionString"].ConnectionString;
        //cmd to retrieve all table names
        private const string getTableNamesCmdText = @"SELECT T.[NAME] AS [table_name] FROM sys.[tables] AS T WHERE t.[is_ms_shipped] = 0";
        //cmd to retrieve columns and datatypes of given table
        private const string getColumnsInfoCmdText = @"SELECT AC.[Name] AS [column_name], TY.[Name] AS system_data_type, AC.[max_length], AC.[is_nullable], AC.[is_identity]
            FROM sys.[tables] AS T   
            INNER JOIN sys.[all_columns] AC ON T.[object_id] = AC.[object_id]  
            INNER JOIN sys.[types] TY ON AC.[system_type_id] = TY.[system_type_id] AND AC.[user_type_id] = TY.[user_type_id] 
            WHERE T.[Name] = @Name 
            ORDER BY AC.[column_id];";
        //cmd to check if user has 'take' permission
        private const string getUserTakePermissionCmdText = @"SELECT UprawnieniePrzejmij FROM Uzytkownicy WHERE Nazwa = '";
        //cmd to check if user is an administrator
        private const string getAdministratorStatusCmdText = @"SELECT czyAdmin FROM Uzytkownicy WHERE Nazwa = '";

        //checks if user is an administrator, returns bool value inidicating whether data was successfully retrieved from database
        public static bool IsAdmin(string username, out bool isAdmin)
        {
            string cmdText = getAdministratorStatusCmdText + username + "';";

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
                    isAdmin = false;
                    return false;
                }

                if (reader.HasRows)
                {
                    reader.Read();
                    isAdmin = Convert.ToBoolean(reader.GetValue(0));
                    reader.Close();
                    return true;
                }
                else
                {
                    isAdmin = false;
                    return false;
                }
            }
        }

        //get names of all available tables
        public static List<string> GetTableNames()
        {
            List<string> tableNames = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(getTableNamesCmdText, connection);
                SqlDataReader reader;

                try
                {
                    cmd.Connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    return tableNames;
                }

                //populate list of table names
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }

                reader.Close();
            }

            return tableNames;
        }

        //get columnInfos of given table
        public static List<ColumnInfo> GetColumnsInfo(string tableName)
        {
            List<ColumnInfo> columnsInfo = new List<ColumnInfo>();

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                SqlCommand cmd = new SqlCommand(getColumnsInfoCmdText, connection);
                cmd.Parameters.AddWithValue("@Name", tableName);
                SqlDataReader reader;

                try
                {
                    cmd.Connection.Open();
                    reader = cmd.ExecuteReader();
                }
                catch (SqlException)
                {
                    //TO DO: handle errors
                    return columnsInfo;
                }

                //populate list of table names
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        columnsInfo.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetInt16(2), Convert.ToBoolean(reader.GetValue(3)), Convert.ToBoolean(reader.GetValue(4))));
                    }
                }
                reader.Close();
            }

            return columnsInfo;
        }

        //check whether user has 'take' permission
        public static bool GetUserTakePermission(string username, out bool hasTakePermission)
        {
            string cmdText = getUserTakePermissionCmdText + username + "';";

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
                    hasTakePermission = false;
                    return false;
                }

                if (reader.HasRows)
                {
                    reader.Read();
                    hasTakePermission = Convert.ToString(reader.GetValue(0)) == "1";
                    reader.Close();
                    return true;
                }
                else
                {
                    hasTakePermission = false;
                    return false;
                }
            }
        }

        //get user permissions for all available tables, returns bool value inidicating whether data was successfully retrieved from database
        public static bool GetUserPermissions(string username, out List<UserPermissions> permissions)
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
        public static bool GetUserPermissionsForTable(string username, string tableName, out UserPermissions userPermissions)
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
    }
}