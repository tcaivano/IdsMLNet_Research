using CsvHelper.Configuration;
using CsvHelper;
using IdsMLNet_Research.Data;
using IdsMLNet_Research.Enum;
using System.Globalization;

namespace IdsMLNet_Research.Services
{
    public static class LargeParserService
    {
        public static void CreateBaseTruth(string authFileLocation, string redTeamFileLocation, string newTruthFileLocation, string finalTestDataLocation)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };


            var redTeams = new List<RedEvent>();
            using (var readerRed = new StreamReader(redTeamFileLocation))
            using (var csvRed = new CsvReader(readerRed, config))
            {
                redTeams = csvRed.GetRecords<RedEvent>().ToList();
            }
            var rand = new Random();
            using (var readerAuth = new StreamReader(authFileLocation))
            using (var csvAuth = new CsvReader(readerAuth, config))
            using (var fs = new StreamWriter(newTruthFileLocation))
            using (var fsFinal = new StreamWriter(finalTestDataLocation))
            {

                var totalCount = 0;
                var totalRedTeamsAdded = 0;
                var ignoreCount = 0;
                var addedCount = 0;
                var redTeamFinalRecordCount = 0;
                var notRedTeamFinalRecordCount = 0;

                while (csvAuth.Read())
                {

                    var record = csvAuth.GetRecord<AuthEvent>();
                    if (record != null && record.IsComplete())
                    {
                        if (RedTeamContainsRecord(redTeams, record))
                        {
                            record.IsRedTeam = true;
                        }

                        // Add to test data?
                        var isTestData = rand.Next(0, 2) == 0;
                        if (isTestData && record.IsRedTeam && redTeamFinalRecordCount < 100)
                        {
                            redTeamFinalRecordCount++;
                            AddRecord(fsFinal, record);
                        }
                        else if (isTestData && !record.IsRedTeam && notRedTeamFinalRecordCount < 1000)
                        {
                            notRedTeamFinalRecordCount++;
                            AddRecord(fsFinal, record);
                        }
                        else
                        {
                            AddRecord(fs, record);
                            if (record.IsRedTeam) totalRedTeamsAdded++;
                        }

                        addedCount++;
                    }
                    else
                    {
                        ignoreCount++;
                    }
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("\r Total Lines Processed: {0}, Total Ignored: {1}, Total Added: {2}, Total Red Teams: {3}                            ", totalCount++.ToString("N0"), ignoreCount.ToString("N0"), addedCount.ToString("N0"), totalRedTeamsAdded.ToString("N0"));
                    Console.Write("\r Total Test Data Not Red: {0}, Total Test Data Red: {1}                   ", notRedTeamFinalRecordCount.ToString("N0"), redTeamFinalRecordCount.ToString("N0"));
                }
            }
        }

        internal static void CreateSampledDataSet(string truthFileLocation, ESampleStrategy strategy, string newFileLocation, string? newFileLocation2, string? newFileLocation3, string? newFileLocation4)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            var totalCount = 0;
            var totalRedTeamsAdded = 0;
            var ignoreCount = 0;
            var addedCount = 0;

            var totalRedTeamsAdded2 = 0;
            var ignoreCount2 = 0;
            var addedCount2 = 0;

            var totalRedTeamsAdded3 = 0;
            var addedCount3 = 0;

            var totalRedTeamsAdded4 = 0;
            var ignoreCount4 = 0;
            var addedCount4 = 0;
            Random rand = new Random();

            using (var readerTruth = new StreamReader(truthFileLocation))
            using (var csvTruth = new CsvReader(readerTruth, config))
            using (var fs = new StreamWriter(newFileLocation))
            using (var fs2 = new StreamWriter(newFileLocation2))
            using (var fs3 = new StreamWriter(newFileLocation3))
            using (var fs4 = new StreamWriter(newFileLocation4))
            {
                while (csvTruth.Read())
                {
                    var record = csvTruth.GetRecord<AuthEventTransform>();
                    if (record == null) continue;
                    totalCount++;
                    switch (strategy)
                    {
                        case ESampleStrategy.All:
                            RedOnlySample(ref totalCount, ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, fs, record);
                            Day8And9Sample(ref totalCount, ref totalRedTeamsAdded2, ref ignoreCount2, ref addedCount2, fs2, record);
                            RandomSample(ref totalCount, ref totalRedTeamsAdded3, ref addedCount3, ref totalRedTeamsAdded4, ref ignoreCount4, ref addedCount4, rand, fs3, fs4, record);
                            break;
                        case ESampleStrategy.Day8And9:
                            Day8And9Sample(ref totalCount, ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, fs, record);
                            break;
                        case ESampleStrategy.RedOnly:
                            RedOnlySample(ref totalCount, ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, fs, record);
                            break;
                        case ESampleStrategy.RandomSample:
                            RandomSample(ref totalCount, ref totalRedTeamsAdded, ref addedCount, ref totalRedTeamsAdded2, ref ignoreCount2, ref addedCount2, rand, fs, fs2, record);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            


        }

        private static void Day8And9Sample(ref int totalCount, ref int totalRedTeamsAdded, ref int ignoreCount, ref int addedCount, StreamWriter fs, AuthEventTransform record)
        {
            if (record.TimeStamp > 800000 || record.TimeStamp < 700000)
            {
                ignoreCount++;
            }
            else
            {
                AddRecord(fs, record);
                addedCount++;
                if (record.IsRedTeam) totalRedTeamsAdded++;
            }

            Console.WriteLine("\r Day 8 and 9: Total Lines Processed: {0}, Total Ignored: {1}, Total Added: {2}, Total Red Teams: {3}                            ", totalCount.ToString("N0"), ignoreCount.ToString("N0"), addedCount.ToString("N0"), totalRedTeamsAdded.ToString("N0"));
        }

        private static void RedOnlySample(ref int totalCount, ref int totalRedTeamsAdded, ref int ignoreCount, ref int addedCount, StreamWriter fs, AuthEventTransform record)
        {
            if (!record.IsRedTeam)
            {
                ignoreCount++;
            }
            else
            {
                AddRecord(fs, record);
                addedCount++;
                totalRedTeamsAdded++;
            }

            Console.WriteLine("\r RedOnly: Total Lines Processed: {0}, Total Ignored: {1}, Total Added: {2}, Total Red Teams: {3}                            ", totalCount.ToString("N0"), ignoreCount.ToString("N0"), addedCount.ToString("N0"), totalRedTeamsAdded.ToString("N0"));
        }

        private static void RandomSample(ref int totalCount, ref int totalRedTeamsAdded, ref int addedCount, ref int totalRedTeamsAdded2, ref int ignoreCount2, ref int addedCount2, Random rand, StreamWriter fs, StreamWriter fs2, AuthEventTransform? record)
        {
            var randOverSample = rand.Next(1, 25);
            var randUnderSample = rand.Next(1, 500000);

            // Oversample Block
            if (record.IsRedTeam)
            {
                for (var i = 0; i <= randOverSample; i++)
                {
                    AddRecord(fs, record);
                    totalRedTeamsAdded++;
                }
            }
            else
            {
                AddRecord(fs, record);
            }
            addedCount++;

            // Undersample Block
            if (!record.IsRedTeam && randUnderSample == 1)
            {
                AddRecord(fs2, record);
                addedCount2++;
            }
            else if (record.IsRedTeam)
            {
                AddRecord(fs2, record);
                totalRedTeamsAdded2++;
            }
            else
            {
                ignoreCount2++;
            }

            Console.WriteLine("\r DownSample: Total Lines Processed: {0}, Total Ignored: {1}, Total Added: {2}, Total Red Teams: {3}                            ", totalCount.ToString("N0"), ignoreCount2.ToString("N0"), addedCount2.ToString("N0"), totalRedTeamsAdded2.ToString("N0"));
            Console.WriteLine("\r OverSample: Total Lines Processed: {0}, Total Ignored: 0, Total Added: {2}, Total Red Teams: {3}                            ", totalCount.ToString("N0"), addedCount.ToString("N0"), totalRedTeamsAdded.ToString("N0"));
        }

        private static bool RedTeamContainsRecord(List<RedEvent> redTeams, AuthEvent record)
        {
            return redTeams.Where(x => x.TimeStamp == record.TimeStamp && x.SourceUser == record.SourceUser && x.SourceComputer == record.SourceComputer && x.DestinationComputer == record.DestinationComputer).Any();
        }

        private static void AddRecord(StreamWriter sw, AuthEvent record)
        {
            var isLogon = record.AuthenticationOrientation == "LogOn";
            var success = record.IsSuccessful == "Success";

            sw.WriteLine(record.TimeStamp + ","
                + record.SourceUser + ","
                + record.DestinationUser + ","
                + record.SourceComputer + ","
                + record.DestinationComputer + ","
                + record.LogonType + ","
                + Convert.ToInt32(isLogon) + ","
                + Convert.ToInt32(success) + ","
                + Convert.ToInt32(record.IsRedTeam));
        }
        private static void AddRecord(StreamWriter sw, AuthEventTransform record)
        {
            sw.WriteLine(record.TimeStamp + ","
                + record.SourceUser + ","
                + record.DestinationUser + ","
                + record.SourceComputer + ","
                + record.DestinationComputer + ","
                + record.LogonType + ","
                + Convert.ToInt32(record.AuthenticationOrientation) + ","
                + Convert.ToInt32(record.IsSuccessful) + ","
                + Convert.ToInt32(record.IsRedTeam));
        }

        private class AuthEventMapper : ClassMap<AuthEvent>
        {
            public AuthEventMapper()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.IsRedTeam).Ignore();
            }
        }
    }
}
