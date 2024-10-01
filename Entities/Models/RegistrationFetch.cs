using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFetcher.Models
{
    public class RegistrationFetch
    {
        public string name { get; set; }
        public string compnay_name { get; set; }
        public string mobile_no { get; set; }
        public string licence_no { get; set; }
        public string amount_toll_rate { get; set; }
        public string vehicle_class { get; set; }
        public string tag { get; set; }
        public DateTime date_time { get; set; }
        public string ac { get; set; }
        public string bank_name { get; set; }
        public string code_no_rfid_user { get; set; }
        public string registration_number { get; set; }
        public int vehicle_reg_id { get; set; }
    }

}
