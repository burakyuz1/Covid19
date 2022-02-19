using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaApi.Models
{
    public class CountryCaseViewModel
    {
        public string CountryName { get; set; }
        public int Confirmed { get; set; }
        public int Death { get; set; }
        public int Population { get; set; }

    }
}
