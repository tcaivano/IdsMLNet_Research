using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdsMLNet_Research.Data
{
    public class RedEvent
    {
        [Index(0)]
        public int TimeStamp { get; set; }
        [Index(1)]
        public string? SourceUser { get; set; }
        [Index(2)]
        public string? SourceComputer { get; set; }
        [Index(3)]
        public string? DestinationComputer { get; set; }
    }
}
