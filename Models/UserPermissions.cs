using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class UserPermissions
    {
        public string TableName { get; set; }
        public Permission PermissionSelect
        {
            get
            {
                return Permissions[0];
            }
        }
        public Permission PermissionInsert
        {
            get
            {
                return Permissions[1];
            }
        }
        public Permission PermissionUpdate
        {
            get
            {
                return Permissions[3];
            }
        }
        public Permission PermissionDelete
        {
            get
            {
                return Permissions[2];
            }
        }

        private Permission[] Permissions;

        public UserPermissions() { }

        public UserPermissions(string tableName, string permissions)
        {
            this.TableName = tableName;
            this.Permissions = new Permission[4];
            setPermissions(permissions);
        }

        private void setPermissions(string permissions)
        {
            for (int i = 0; i < permissions.Length; i++ )
            {
                switch(permissions[i])
                {
                    case '0':
                        Permissions[i] = Permission.No;
                        break;
                    case '1':
                        Permissions[i] = Permission.Yes;
                        break;
                    case '2':
                        Permissions[i] = Permission.YesWithGrant;
                        break;
                    default:
                        Permissions[i] = Permission.No;
                        break;
                }
            }
        }
    }
}