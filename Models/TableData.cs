using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class TableData
    {
        public List<List<object>> Values { get; set; }
        public TableInfo TableInfo { get; set; }

        public TableData() { }

        public TableData(List<List<object>> values, TableInfo tableInfo)
        {
            this.Values = values;
            this.TableInfo = tableInfo;
        }
    }
}