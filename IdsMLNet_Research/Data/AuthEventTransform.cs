using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdsMLNet_Research.Data
{
    public class AuthEventTransform
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
        public string? LogonType { get; set; }
        [Index(6)]
        public bool AuthenticationOrientation { get; set; }
        [Index(7)]
        public bool IsSuccessful { get; set; }
        [Index(8)]
        public bool IsRedTeam { get; set; }

        /// <summary>
        /// Calculates the Euclidean distance between two AuthEventTransform objects based on various attribute comparisons.
        /// </summary>
        /// <param name="a1">The first AuthEventTransform object.</param>
        /// <param name="a2">The second AuthEventTransform object.</param>
        /// <returns>The Euclidean distance between the two objects.</returns>
        public static double GetEulcideanDistance(AuthEventTransform a1, AuthEventTransform a2)
        {
            int x1, x2, x3, x4, x5, x6, x7, x8;

            // Source User Comparison
            x1 = GetTupleStringDistance(a1.SourceUser, a2.SourceUser);

            // Destination User Comparison
            x2 = GetTupleStringDistance(a1.DestinationUser, a2.DestinationUser);

            // Source Computer Comparison
            x3 = Convert.ToInt16(!a1.SourceComputer.Equals(a2.SourceComputer, StringComparison.Ordinal));

            // Destination Computer Comparison
            x4 = Convert.ToInt16(!a1.DestinationComputer.Equals(a2.DestinationComputer, StringComparison.Ordinal));

            // Logon Type Comparison
            x5 = Convert.ToInt16(!a1.LogonType.Equals(a2.LogonType, StringComparison.Ordinal));

            // Authentication Orientation Comparison
            x6 = Convert.ToInt16(a1.AuthenticationOrientation != a2.AuthenticationOrientation);

            // Is Successful Comparison
            x7 = Convert.ToInt16(a1.IsSuccessful != a2.IsSuccessful);

            // Is Successful Comparison
            x8 = Convert.ToInt16(a1.IsRedTeam != a2.IsRedTeam);

            return Math.Sqrt(Math.Pow(x1, 2) + Math.Pow(x2, 2) + Math.Pow(x3, 2) + Math.Pow(x4, 2) + Math.Pow(x5, 2) + Math.Pow(x6, 2) + Math.Pow(x7, 2) + Math.Pow(x8, 2));
        }

        private static int GetTupleStringDistance(string a1, string a2)
        {
            var a1s = a1.Split('@');
            var a2s = a2.Split('@');

            var set1Match = a1s[0] == a2s[0];
            var set2Match = a1s[1] == a2s[1];
            if (set1Match && set2Match)
            {
                // Both sets match
                return 0;
            }
            else if (set1Match || set2Match)
            {
                // Only one set matches
                return 1;
            }
            else
            {
                // Neither set matches
                return 2;
            }
        }
    }
}
