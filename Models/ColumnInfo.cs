using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class ColumnInfo
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public int MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }

        public ColumnInfo(string columnName, string dataType, int maxLength, bool isNullable, bool isIdentity)
        {
            this.ColumnName = columnName;
            this.DataType = dataType;
            this.MaxLength = maxLength;
            this.IsNullable =isNullable;
            this.IsIdentity = isIdentity;
        }
    }
}