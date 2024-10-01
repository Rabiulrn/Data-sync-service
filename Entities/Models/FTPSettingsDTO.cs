using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class FTPSettingsDTO
    {
        public string ftpServer { get; set; }
        public string ftpUsername { get; set; }
        public string ftpPassword { get; set; }
        public string directoryPath { get; set; }
    }
}
