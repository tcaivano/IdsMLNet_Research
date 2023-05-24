using CsvHelper.Configuration;
using CsvHelper;
using IdsMLNet_Research.Data;
using IdsMLNet_Research.Enum;
using System.Globalization;

namespace IdsMLNet_Research.Services
{
    public static class LargeParserService
    {
        /// <summary>
        /// Creates a base truth file from the original auth.txt file. Removes Incomplete records, determines Red Team data, and creates a cleaned output.
        /// </summary>
        /// <param name="authFileLocation">File location of auth.txt</param>
        /// <param name="redTeamFileLocation">File location of redteam.txt</param>
        /// <param name="newTruthFileLocation">Transformed file to be created</param>
        /// <param name="finalTestDataLocation">Validation data file to be created</param>
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

        /// <summary>
        /// Creates a new sampled dataset file from a specified ESampleStrategy.
        /// </summary>
        /// <param name="truthFileLocation">The base truth file to sample.</param>
        /// <param name="strategy">The sampling strategy to use</param>
        /// <param name="newFileLocation">The new file to be created</param>
        /// <param name="newFileLocation2"></param>
        /// <exception cref="NotImplementedException">Thrown for unimplemented ESampleStrategy cases.</exception>
        public static void CreateSampledDataSet(string truthFileLocation, ESampleStrategy strategy, string newFileLocation, string newFileLocation2)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            var cursorTop = GetInitialCursor(strategy);

            var totalCount = 0;
            var totalRedTeamsAdded = 0;
            var ignoreCount = 0;
            var addedCount = 0;

            var totalRedTeamsAdded2 = 0;
            var ignoreCount2 = 0;
            var addedCount2 = 0;

            Random rand = new Random();

            using (var readerTruth = new StreamReader(truthFileLocation))
            using (var csvTruth = new CsvReader(readerTruth, config))
            using (var fs = new StreamWriter(newFileLocation))
            using (var fs2 = new StreamWriter(newFileLocation2))
            {
                while (csvTruth.Read())
                {
                    var record = csvTruth.GetRecord<AuthEventTransform>();
                    if (record == null) continue;
                    totalCount++;
                    switch (strategy)
                    {
                        case ESampleStrategy.Day8And9:
                            Day8And9Sample(ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, fs, record);
                            PrintProgress(cursorTop - 1, strategy, totalCount, totalRedTeamsAdded, ignoreCount, addedCount);
                            break;
                        case ESampleStrategy.RedOnly:
                            RedOnlySample(ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, fs, record);
                            PrintProgress(cursorTop - 1, strategy, totalCount, totalRedTeamsAdded, ignoreCount, addedCount);
                            break;
                        case ESampleStrategy.RandomSample:
                            RandomSample(ref totalRedTeamsAdded, ref addedCount, ref totalRedTeamsAdded2, ref ignoreCount2, ref addedCount2, rand, fs, fs2, record);
                            PrintProgress(cursorTop - 2, strategy, totalCount, totalRedTeamsAdded, 0, addedCount);
                            PrintProgress(cursorTop - 1, strategy, totalCount, totalRedTeamsAdded2, ignoreCount2, addedCount2);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves authentication events from a CSV file and returns a list of AuthEventTransform objects.
        /// </summary>
        /// <param name="fileLocation">The location of the CSV file.</param>
        /// <returns>A list of AuthEventTransform objects containing the authentication events.</returns>
        public static List<AuthEventTransform> GetAuthEventsFromFile(string fileLocation)
        {
            List<AuthEventTransform> authEvents;
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            using (var fileReader = new StreamReader(fileLocation))
            using (var csvTruth = new CsvReader(fileReader, config))
            {
                authEvents = csvTruth.GetRecords<AuthEventTransform>().ToList();
            }
            return authEvents;
        }

        /// <summary>
        /// Gets the initial Console Cursor depending on the strategy. Delete when RandomSample is refactored.
        /// </summary>
        /// <param name="strategy"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static int GetInitialCursor(ESampleStrategy strategy)
        {
            switch (strategy)
            {
                case ESampleStrategy.Day8And9:
                    Console.WriteLine("Line 1");
                    break;
                case ESampleStrategy.RedOnly:
                    Console.WriteLine("Line 1");
                    break;
                case ESampleStrategy.RandomSample:
                    Console.WriteLine("Line 1");
                    Console.WriteLine("Line 2");
                    break;
                default:
                    throw new NotImplementedException();
            }
            return Console.CursorTop;
        }

        /// <summary>
        /// Prints the current sampling progress to the console.
        /// </summary>
        private static void PrintProgress(int cursorPosition, ESampleStrategy strategy, int totalCount, int totalRedTeamsAdded, int ignoreCount, int addedCount)
        {
            Console.SetCursorPosition(0, cursorPosition);
            Console.WriteLine("\r {0}: Total Lines Processed: {1}, Total Ignored: {2}, Total Added: {3}, Total Red Teams: {4}                            ", strategy, totalCount.ToString("N0"), ignoreCount.ToString("N0"), addedCount.ToString("N0"), totalRedTeamsAdded.ToString("N0"));
        }


        private static void Day8And9Sample(ref int totalRedTeamsAdded, ref int ignoreCount, ref int addedCount, StreamWriter fs, AuthEventTransform record)
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
        }

        private static void RedOnlySample(ref int totalRedTeamsAdded, ref int ignoreCount, ref int addedCount, StreamWriter fs, AuthEventTransform record)
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
        }

        private static void RandomSample(ref int totalRedTeamsAdded, ref int addedCount, ref int totalRedTeamsAdded2, ref int ignoreCount2, ref int addedCount2, Random rand, StreamWriter fs, StreamWriter fs2, AuthEventTransform record)
        {
            var randOverSample = rand.Next(1, 31);
            var randUnderSample = rand.Next(1, 400000);

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
