using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAC.Models
{
    public class CmdInfo
    {
        public string CmdText { get; set; }
        public bool Success { get; set; }
        public string Msg { get; set; }

        public CmdInfo() { }

        public CmdInfo(string cmdText, bool success, string msg)
        {
            this.CmdText = cmdText;
            this.Success = success;
            this.Msg = msg;
        }
    }
}