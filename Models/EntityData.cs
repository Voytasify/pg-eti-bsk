using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class EntityData
    {
        public int Id { get; set; }
        public List<object> Values { get; set; }
        public TableInfo TableInfo { get; set; }

        public EntityData(int id, List<object> values, TableInfo tableInfo)
        {
            this.Id = id;
            this.Values = values;
            this.TableInfo = tableInfo;
        }
    }
}