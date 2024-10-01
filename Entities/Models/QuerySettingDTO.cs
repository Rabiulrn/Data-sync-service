using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class QuerySettingDTO
    {
        private string _JobName;
        private string _DMPFileName;
        private string _LogFileName;
        public string JobName
        {
            get { return _JobName + "_job"+ Regex.Replace(DateTime.Now.ToString(), @"[^0-9a-fA-F]", ""); }
            set { _JobName = value; }
        }
        public string DMPFileName
        {
            get { return _DMPFileName + "_BackUp" + Regex.Replace(DateTime.Now.ToString(), @"[^0-9a-fA-F]", ""); }
            set { _DMPFileName = value; }
        }
        public string LogFileName
        {
            get { return _LogFileName + "_log" + Regex.Replace(DateTime.Now.ToString(), @"[^0-9a-fA-F]", ""); }
            set { _LogFileName = value; }
        }
        public string DirectoryName{ get; set; }
        public string DBUserName{ get; set; }
    }
}
