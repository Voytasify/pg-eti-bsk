using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class TableInfo
    {
        public List<ColumnInfo> ColumnsInfo { get; set; }
        public string TableName { get; set; }

        public TableInfo() { }

        public TableInfo(List<ColumnInfo> columnsInfo, string tableName)
        {
            this.ColumnsInfo = columnsInfo;
            this.TableName = tableName;
        }
    }
}