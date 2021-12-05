using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WheelsScraper;

namespace DataBaseInteractModule
{
    public class ExtWareInfo : WareInfo
    {
        public string ProductPartNumber { get; set; }
        public string GeneralImage { get; set; }
        public string ProductTitle { get; set; }
        public string ProductDescription { get; set; }
        public double WebPrice { get; set; }
        public double ItemWeight { get; set; }
        public string ApplicationSpecificImage { get; set; }
        public string PrimaryOptionTitle { get; set; }
        public string PrimaryOptionChoice { get; set; }
        public string ProductURL { get; set; }
    }
}
