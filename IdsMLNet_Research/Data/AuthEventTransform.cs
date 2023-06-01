using CsvHelper.Configuration;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdsMLNet_Research.Data
{
    public class AuthEventTransform
    {
        [Index(0)]
        [LoadColumn(0)]
        public int TimeStamp { get; set; }
        [Index(1)]
        [LoadColumn(1)]
        public string? SourceUser { get; set; }
        [Index(2)]
        [LoadColumn(2)]
        public string? DestinationUser { get; set; }
        [Index(3)]
        [LoadColumn(3)]
        public string? SourceComputer { get; set; }
        [Index(4)]
        [LoadColumn(4)]
        public string? DestinationComputer { get; set; }
        [Index(5)]
        [LoadColumn(5)]
        public string? LogonType { get; set; }
        [Index(6)]
        [LoadColumn(6)]
        public bool AuthenticationOrientation { get; set; }
        [Index(7)]
        [LoadColumn(7)]
        public bool IsSuccessful { get; set; }
        [Index(8)]
        [LoadColumn(8)]
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

            // Is Red Team Comparison
            x8 = Convert.ToInt16(a1.IsRedTeam != a2.IsRedTeam);

            return Math.Sqrt(Math.Pow(x1, 2) + Math.Pow(x2, 2) + Math.Pow(x3, 2) + Math.Pow(x4, 2) + Math.Pow(x5, 2) + Math.Pow(x6, 2) + Math.Pow(x7, 2) + Math.Pow(x8, 2));
        }


        public static List<Tuple<AuthEventTransform, double>> GetNearestNeighbors(AuthEventTransform a, AuthEventTransform[] dataSet, int k)
        {
            List<Tuple<AuthEventTransform, double>> result = new List<Tuple<AuthEventTransform, double>>();

            for (int i = 0; i < dataSet.Length; i++)
            {
                var distance = GetEulcideanDistance(a, dataSet[i]);
                var tup = new Tuple<AuthEventTransform, double>(dataSet[i], distance);
                result.Add(tup);
            }

            return result.OrderBy(x => x.Item2).Take(k).ToList();
        }

        public static List<Tuple<AuthEventTransform, double>> GetNearestNeighbors(AuthEventTransform a, string dataSetLocation, int k)
        {
            List<Tuple<AuthEventTransform, double>> result = new List<Tuple<AuthEventTransform, double>>();
            for (int i = 0; i < k; i++)
            {
                result.Add(new Tuple<AuthEventTransform, double>(null, int.MaxValue));
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };
            var total = 0;
            using (var readerTruth = new StreamReader(dataSetLocation))
            using (var csvTruth = new CsvReader(readerTruth, config))
            {
                while (csvTruth.Read())
                {
                    total++;
                    var record = csvTruth.GetRecord<AuthEventTransform>();
                    if (record == null) continue;
                    var distance = GetEulcideanDistance(a, record);
                    Console.Write("\rtotal: {0}      ", total.ToString("N0"));
                    if (distance >= result.Last().Item2) continue;
                    var tup = new Tuple<AuthEventTransform, double>(record, distance);
                    result.Add(tup);
                    result = result.OrderBy(x => x.Item2).Take(k).ToList();
                }
            }

            return result.OrderBy(x => x.Item2).Take(k).ToList();
        }

        private static int GetTupleStringDistance(string a1, string a2)
        {
            int result;
            var a1s = a1.Split('@');
            var a2s = a2.Split('@');

            var set1Match = a1s[0] == a2s[0];
            var set2Match = a1s[1] == a2s[1];
            if (set1Match && set2Match)
            {
                // Both sets match
                result = 0;
            }
            else if (set1Match || set2Match)
            {
                // Only one set matches
                result = 1;
            }
            else
            {
                // Neither set matches
                result = 2;
            }
            return result;
        }
    }
}
