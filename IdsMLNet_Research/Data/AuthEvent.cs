using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdsMLNet_Research.Data
{
    public class AuthEvent
    {
        [Index(0)]
        public int TimeStamp { get; set; }
        [Index(1)]
        public string? SourceUser { get; set; }
        [Index(2)]
        public string? DestinationUser { get; set; }
        [Index(3)]
        public string? SourceComputer { get; set; }
        [Index(4)]
        public string? DestinationComputer { get; set; }
        [Index(5)]
        public string? AuthenticationType { get; set; }
        [Index(6)]
        public string? LogonType { get; set; }
        [Index(7)]
        public string? AuthenticationOrientation { get; set; }
        [Index(8)]
        public string? IsSuccessful { get; set; }
        [Ignore]
        public bool IsRedTeam { get; set; }

        /// <summary>
        /// Indicates if the record has any missing fields.
        /// </summary>
        /// <returns>True if any fields are equal to ?</returns>
        public bool IsComplete()
        {
            return !(new[] { SourceUser, DestinationUser, SourceComputer, DestinationComputer, AuthenticationType, LogonType, AuthenticationOrientation }).Any(x => x != null && x.Equals("?"));
        }
    }
}
