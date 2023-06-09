using CsvHelper.Configuration;
using CsvHelper;
using IdsMLNet_Research.Data;
using IdsMLNet_Research.Enum;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        /// <param name="newFiles">The files to be used</param>
        /// <exception cref="NotImplementedException">Thrown for unimplemented ESampleStrategy cases.</exception>
        public static void CreateSampledDataSet(string truthFileLocation, ESampleStrategy strategy, List<string> newFiles)
        {
            switch(strategy)
            {
                case ESampleStrategy.SMOTE:
                    GenerateSMOTEPoints(truthFileLocation, newFiles);
                    break;
                case ESampleStrategy.ADASYN:
                    GenerateADASYNPoints(truthFileLocation, newFiles);
                    break;
                case ESampleStrategy.CNN:
                    GenerateCNNPoints(truthFileLocation, newFiles);
                    break;
                case ESampleStrategy.ENN:
                    GenerateENNPoints(truthFileLocation, newFiles);
                    break;
                default:
                    GenerateLinearSample(truthFileLocation, strategy, newFiles.First());
                    break;
            }
        }

        private static void GenerateCNNPoints(string truthFileLocation, List<string> newFiles)
        {
            const int k = 35;
            if (k % 2 != 1) throw new ArgumentException("k should always be odd");

            Random rand = new Random();
            var newFileLocation = newFiles[0];

            var events = GetAuthEventsFromFile(truthFileLocation);
            var startingRed = events.Where(x => x.IsRedTeam).OrderBy(x => Guid.NewGuid()).Take(k/2);
            var startingNotRed = events.Where(x => !x.IsRedTeam).OrderBy(x => Guid.NewGuid()).Take(k/2);
            List<AuthEventTransform> cnnSet = new List<AuthEventTransform>
            {
                events.OrderBy(x => Guid.NewGuid()).First()
            };
            cnnSet.AddRange(startingRed);
            cnnSet.AddRange(startingNotRed);

            var changesMade = true;
            int iterations = 1;
            int totalAdded = 0;
            int totalRemoved = 0;
            while (changesMade)
            {
                Console.WriteLine($"CNN Iteration {iterations} starting, running total {totalAdded} and {totalRemoved} removed...\t\t\t");
                changesMade = false;
                var iterationInteral = 1;
                var iterationInteralAdded = 0;
                var iterationInteralRemoved = 0;
                foreach (var e in events)
                {
                    Console.Write($"\rInteral iteration {iterationInteral}, {iterationInteralAdded} added and {iterationInteralRemoved} removed.......\t\t\t");
                    var nearestNeighbors = AuthEventTransform.GetNearestNeighbors(e, cnnSet.ToArray(), k).ToList();
                    bool classification = AuthEventTransform.KnnClassify(nearestNeighbors);

                    if (classification != e.IsRedTeam)
                    {
                        var nearestNeighborsOriginal = AuthEventTransform.GetNearestNeighbors(e, events.ToArray(), events.Length - 1).ToList();
                        nearestNeighborsOriginal = nearestNeighborsOriginal.Where(x => x.Item1.IsRedTeam == e.IsRedTeam).Where(x => !cnnSet.Contains(x.Item1)).ToList();
                        var itemToAdd = nearestNeighborsOriginal.FirstOrDefault();

                        if (itemToAdd != null)
                        {
                            cnnSet.Add(itemToAdd.Item1);
                            changesMade = true;
                            totalAdded++;
                            iterationInteralAdded++;
                        }
                    }
                    else
                    {
                        totalRemoved++;
                        iterationInteralRemoved++;
                    }
                    iterationInteral++;
                }
                iterations++;
            }
            Console.WriteLine();
            Console.WriteLine($"CNN Iteration {iterations} finished, running total {totalAdded} and {totalRemoved} removed...\t\t\t");

            var totalCount = 0;
            using (StreamWriter sw = File.AppendText(newFileLocation))
            {
                foreach (var cnn in cnnSet)
                {
                    AddRecord(sw, cnn);
                    PrintProgress(ESampleStrategy.CNN, totalCount++, 0, 0, 0);
                }
            }
        }

        private static void GenerateENNPoints(string truthFileLocation, List<string> args)
        {
            int k = int.Parse(args[1]);
            if (k % 2 != 1) throw new ArgumentException("k should always be odd");

            Random rand = new Random();
            var newFileLocation = args[0];

            var events = GetAuthEventsFromFile(truthFileLocation);
            var startingRed = events.Where(x => x.IsRedTeam).OrderBy(x => Guid.NewGuid()).Take(k / 2);
            var startingNotRed = events.Where(x => !x.IsRedTeam).OrderBy(x => Guid.NewGuid()).Take(k / 2);
            List<AuthEventTransform> ennSet = new List<AuthEventTransform>();

            var iterationInteral = 1;
            var iterationInteralAdded = 0;
            var iterationInteralRemoved = 0;

            foreach (var e in events)
            {
                Console.Write($"\rInteral iteration {iterationInteral}, {iterationInteralAdded} added and {iterationInteralRemoved} removed.......\t\t\t");

                if (!e.IsRedTeam)
                {
                    var nearestNeighbors = AuthEventTransform.GetNearestNeighbors(e, events.ToArray(), k).ToList();
                    bool classification = AuthEventTransform.KnnClassify(nearestNeighbors);

                    if (classification != e.IsRedTeam)
                    {
                        iterationInteralRemoved++;
                        continue;
                    }
                }

                iterationInteral++;
                ennSet.Add(e);
            }

            Console.WriteLine();

            var totalCount = 0;
            using (StreamWriter sw = File.AppendText(newFileLocation))
            {
                foreach (var cnn in ennSet)
                {
                    AddRecord(sw, cnn);
                    PrintProgress(ESampleStrategy.CNN, totalCount++, 0, 0, 0);
                }
            }
        }

        /// <summary>
        /// Generates ADASYN points based on the given truth file and new file locations.
        /// </summary>
        /// <param name="truthFileLocation">The location of the truth file.</param>
        /// <param name="newFiles">A list of new file locations.</param>
        private static void GenerateADASYNPoints(string truthFileLocation, List<string> newFiles)
        {
            const float ml = 9124406f;
            const float ms = 186f;
            const float beta = 0.1f;
            const float k = 10;
            const float G = (ml - ms) * beta;

            var newFileLocation = newFiles[0];
            var redTeamFileLocation = newFiles[1];
            var eventsFileLocation = newFiles[2];

            var redTeams = GetAuthEventsFromFile(redTeamFileLocation);
            var allEvents = GetAuthEventsFromFile(eventsFileLocation);
            List<float> rs = new List<float>();
            float riSum = 0;

            foreach (var redTeamEvent in redTeams)
            {
                float ri = (float)AuthEventTransform.GetNearestNeighbors(redTeamEvent, allEvents, (int)k).Where(x => x.Item2 != 0 && !x.Item1.IsRedTeam).Count() / k;
                rs.Add(ri);
                riSum += ri;
            }

            if (riSum == 0) throw new ArithmeticException();

            var riHats = new List<float>();
            foreach (var ri in rs)
            {
                riHats.Add(ri / riSum);
            }

            var index = 0;
            List<AuthEventTransform> adasynPoints = new List<AuthEventTransform>();
            Random rand = new Random();
            foreach (var redTeamEvent in redTeams)
            {
                var Gi = G * riHats[index];
                // Other redteam data in a neighborhood is really rare, so we will upscale Gi times.
                if (rs[index] >= 0.9)
                {
                    for (int i = 0; i < Math.Round(Gi); i++)
                    {
                        adasynPoints.Add(redTeamEvent);
                    }
                }
                else
                {
                    var nearestNeighbors = AuthEventTransform.GetNearestNeighbors(redTeamEvent, allEvents, (int)k).Where(x => x.Item2 != 0 && x.Item1.IsRedTeam);
                    for (int i = 0; i < Math.Round(Gi); i++)
                    {
                        var randomNeighbor = nearestNeighbors.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                        if (randomNeighbor != null) adasynPoints.Add(GetRandomProperties(redTeamEvent, randomNeighbor.Item1, rand));
                        else adasynPoints.Add(redTeamEvent);
                    }
                }
                index++;
            }

            var totalCount = 0;
            File.Copy(truthFileLocation, newFileLocation);
            using (StreamWriter sw = File.AppendText(newFileLocation))
            {
                sw.WriteLine('\n');
                foreach (var adasyn in adasynPoints)
                {
                    AddRecord(sw, adasyn);
                    PrintProgress(ESampleStrategy.ADASYN, totalCount++, 0, 0, 0);
                }
            }
        }

        /// <summary>
        /// Generates a linear sample from a truth file based on the specified sampling strategy and saves it to a new file location.
        /// </summary>
        /// <param name="truthFileLocation">The location of the truth file.</param>
        /// <param name="strategy">The sampling strategy to use.</param>
        /// <param name="newFileLocation">The new file location to save the generated sample.</param>
        private static void GenerateLinearSample(string truthFileLocation, ESampleStrategy strategy, string newFileLocation)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            Random rand = new Random();
            var totalCount = 0;
            var totalRedTeamsAdded = 0;
            var ignoreCount = 0;
            var addedCount = 0;

            using (var readerTruth = new StreamReader(truthFileLocation))
            using (var csvTruth = new CsvReader(readerTruth, config))
            using (var fs = new StreamWriter(newFileLocation))
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
                            PrintProgress(strategy, totalCount, totalRedTeamsAdded, ignoreCount, addedCount);
                            break;
                        case ESampleStrategy.RedOnly:
                            RedOnlySample(ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, fs, record);
                            PrintProgress(strategy, totalCount, totalRedTeamsAdded, ignoreCount, addedCount);
                            break;
                        case ESampleStrategy.RandomUpSample:
                            RandomSampleUp(ref totalRedTeamsAdded, ref addedCount, rand, fs, record);
                            PrintProgress(strategy, totalCount, totalRedTeamsAdded, 0, addedCount);
                            break;
                        case ESampleStrategy.RandomDownSample:
                            RandomSampleDown(ref totalRedTeamsAdded, ref ignoreCount, ref addedCount, rand, fs, record);
                            PrintProgress(strategy, totalCount, totalRedTeamsAdded, 0, addedCount);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        /// <summary>
        /// Generates SMOTE points based on the truth file and a list of new file locations. Saves the SMOTE points to the new files.
        /// </summary>
        /// <param name="truthFileLocation">The location of the truth file.</param>
        /// <param name="newFiles">A list of new file locations to save the generated SMOTE points.</param>
        private static void GenerateSMOTEPoints(string truthFileLocation, List<string> newFiles)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            Random rand = new Random();
            var newFileLocation = newFiles[0];
            var redTeamFileLocation = newFiles[1];

            File.Copy(truthFileLocation, newFileLocation);

            var redTeams = GetAuthEventsFromFile(redTeamFileLocation);
            List<AuthEventTransform> smotedPoints = new List<AuthEventTransform>();

            foreach (var redTeamEvent in redTeams)
            {
                var neighbors = AuthEventTransform.GetNearestNeighbors(redTeamEvent, redTeams, 11).Where(x => x.Item2 != 0).ToArray();
                var randomIndex = rand.Next(0, neighbors.Length);
                var randomNearestNeighbor = neighbors[randomIndex].Item1;

                var smoteAuthEvent = GetRandomProperties(redTeamEvent, randomNearestNeighbor, rand);
                smotedPoints.Add(smoteAuthEvent);
            }

            var totalCount = 0;
            var totalRedTeamsAdded = 0;

            using (StreamWriter sw = File.AppendText(newFileLocation))
            {
                sw.WriteLine('\n');
                foreach (var smoteNew in smotedPoints)
                {
                    AddRecord(sw, smoteNew);
                    PrintProgress(ESampleStrategy.SMOTE, totalCount++, totalRedTeamsAdded++, 0, totalCount);
                }
            }
        }

        /// <summary>
        /// Generates random properties for creating a new AuthEventTransform based on a red team event and its random nearest neighbor.
        /// </summary>
        /// <param name="redTeamEvent">The red team event.</param>
        /// <param name="randomNearestNeighbor">The random nearest neighbor of the red team event.</param>
        /// <param name="rand">The random number generator.</param>
        /// <returns>A new AuthEventTransform with random properties.</returns>
        private static AuthEventTransform GetRandomProperties(AuthEventTransform redTeamEvent, AuthEventTransform randomNearestNeighbor, Random rand)
        {
            var newAuthEvent = new AuthEventTransform();

            newAuthEvent.SourceComputer = GetRandomProperty(redTeamEvent.SourceComputer, randomNearestNeighbor.SourceComputer, rand);
            newAuthEvent.DestinationComputer = GetRandomProperty(redTeamEvent.DestinationComputer, randomNearestNeighbor.DestinationComputer, rand);
            newAuthEvent.LogonType = GetRandomProperty(redTeamEvent.LogonType, randomNearestNeighbor.LogonType, rand);
            newAuthEvent.AuthenticationOrientation = GetRandomProperty(redTeamEvent.AuthenticationOrientation, randomNearestNeighbor.AuthenticationOrientation, rand);
            newAuthEvent.IsSuccessful = GetRandomProperty(redTeamEvent.IsSuccessful, randomNearestNeighbor.IsSuccessful, rand);
            newAuthEvent.IsRedTeam = GetRandomProperty(redTeamEvent.IsRedTeam, randomNearestNeighbor.IsRedTeam, rand);

            var a1s = redTeamEvent.SourceUser.Split('@');
            var a2s = randomNearestNeighbor.SourceUser.Split('@');
            newAuthEvent.SourceUser = GetRandomProperty(a1s[0], a2s[0], rand) + "@" + GetRandomProperty(a1s[1], a2s[1], rand);

            var b1s = redTeamEvent.DestinationUser.Split('@');
            var b2s = randomNearestNeighbor.DestinationUser.Split('@');
            newAuthEvent.DestinationUser = GetRandomProperty(b1s[0], b2s[0], rand) + "@" + GetRandomProperty(b1s[1], b2s[1], rand);

            return newAuthEvent;
        }

        /// <summary>
        /// Gets a random property value between two given values based on a random number generator.
        /// </summary>
        /// <typeparam name="T">The type of the property values.</typeparam>
        /// <param name="a">The first property value.</param>
        /// <param name="b">The second property value.</param>
        /// <param name="rand">The random number generator.</param>
        /// <returns>A random property value between the two given values.</returns>
        private static T GetRandomProperty<T>(T a, T b, Random rand)
        {
            if (rand.Next(2) == 0)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        /// <summary>
        /// Retrieves authentication events from a CSV file and returns a list of AuthEventTransform objects.
        /// </summary>
        /// <param name="fileLocation">The location of the CSV file.</param>
        /// <returns>A list of AuthEventTransform objects containing the authentication events.</returns>
        public static AuthEventTransform[] GetAuthEventsFromFile(string fileLocation)
        {
            AuthEventTransform[] authEvents;
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            using (var fileReader = new StreamReader(fileLocation))
            using (var csvTruth = new CsvReader(fileReader, config))
            {
                authEvents = csvTruth.GetRecords<AuthEventTransform>().ToArray();
            }
            return authEvents;
        }

        /// <summary>
        /// Prints the current sampling progress to the console.
        /// </summary>
        private static void PrintProgress(ESampleStrategy strategy, int totalCount, int totalRedTeamsAdded, int ignoreCount, int addedCount)
        {
            Console.Write("\r {0}: Total Lines Processed: {1}, Total Ignored: {2}, Total Added: {3}, Total Red Teams: {4}                            ", strategy, totalCount.ToString("N0"), ignoreCount.ToString("N0"), addedCount.ToString("N0"), totalRedTeamsAdded.ToString("N0"));
        }

        /// <summary>
        /// Performs the Day8And9 sampling strategy by checking the timestamp of the record. Adds the record to the file if it falls within the specified time range.
        /// </summary>
        /// <param name="totalRedTeamsAdded">The total count of red team events added.</param>
        /// <param name="ignoreCount">The count of ignored records.</param>
        /// <param name="addedCount">The count of added records.</param>
        /// <param name="fs">The StreamWriter to write the record.</param>
        /// <param name="record">The AuthEventTransform record to process.</param>
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

        /// <summary>
        /// Performs the RedOnly sampling strategy by checking if the record is a red team event. Adds the record to the file if it is a red team event.
        /// </summary>
        /// <param name="totalRedTeamsAdded">The total count of red team events added.</param>
        /// <param name="ignoreCount">The count of ignored records.</param>
        /// <param name="addedCount">The count of added records.</param>
        /// <param name="fs">The StreamWriter to write the record.</param>
        /// <param name="record">The AuthEventTransform record to process.</param>
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

        /// <summary>
        /// Performs the RandomSampleDown sampling strategy by randomly undersampling non-red team events based on a given probability. Adds the record to the file according to the undersampling rules.
        /// </summary>
        /// <param name="totalRedTeamsAdded">The total count of red team events added.</param>
        /// <param name="ignoreCount">The count of ignored records.</param>
        /// <param name="addedCount">The count of added records.</param>
        /// <param name="rand">The random number generator.</param>
        /// <param name="fs">The StreamWriter to write the record.</param>
        /// <param name="record">The AuthEventTransform record to process.</param>
        private static void RandomSampleDown(ref int totalRedTeamsAdded, ref int ignoreCount, ref int addedCount, Random rand, StreamWriter fs, AuthEventTransform record)
        {
            var randUnderSample = rand.Next(1, 45000);

            // Undersample Block
            if (!record.IsRedTeam && randUnderSample == 1)
            {
                AddRecord(fs, record);
                addedCount++;
            }
            else if (record.IsRedTeam)
            {
                AddRecord(fs, record);
                totalRedTeamsAdded++;
            }
            else
            {
                ignoreCount++;
            }
        }

        /// <summary>
        /// Performs the RandomSampleUp sampling strategy by randomly oversampling red team events based on a given probability. Adds the record to the file according to the oversampling rules.
        /// </summary>
        /// <param name="totalRedTeamsAdded">The total count of red team events added.</param>
        /// <param name="addedCount">The count of added records.</param>
        /// <param name="rand">The random number generator.</param>
        /// <param name="fs">The StreamWriter to write the record.</param>
        /// <param name="record">The AuthEventTransform record to process.</param>
        private static void RandomSampleUp(ref int totalRedTeamsAdded, ref int addedCount, Random rand, StreamWriter fs, AuthEventTransform record)
        {
            var randOverSample = rand.Next(1, 31);

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
        }

        /// <summary>
        /// Checks if the list of red team events contains a specific AuthEvent record based on matching properties.
        /// </summary>
        /// <param name="redTeams">The list of red team events.</param>
        /// <param name="record">The AuthEvent record to search for.</param>
        /// <returns>True if the record is found in the red team events list; otherwise, false.</returns>
        private static bool RedTeamContainsRecord(List<RedEvent> redTeams, AuthEvent record)
        {
            return redTeams.Where(x => x.TimeStamp == record.TimeStamp && x.SourceUser == record.SourceUser && x.SourceComputer == record.SourceComputer && x.DestinationComputer == record.DestinationComputer).Any();
        }

        /// <summary>
        /// Writes an AuthEvent record to a StreamWriter in a specific format.
        /// </summary>
        /// <param name="sw">The StreamWriter to write the record.</param>
        /// <param name="record">The AuthEvent record to write.</param>
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

        /// <summary>
        /// Writes an AuthEventTransform record to a StreamWriter in a specific format.
        /// </summary>
        /// <param name="sw">The StreamWriter to write the record.</param>
        /// <param name="record">The AuthEventTransform record to write.</param>
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
