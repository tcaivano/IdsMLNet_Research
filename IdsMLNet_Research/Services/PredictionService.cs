using CsvHelper;
using CsvHelper.Configuration;
using IdsMLNet_Research.Data;
using IdsMLNet_Research.Enum;
using Microsoft.ML;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdsMLNet_Research.Services
{
    public static class PredictionService
    {
        /// <summary>
        /// Predicts the labels for authentication events using a trained ML model.
        /// </summary>
        /// <param name="mlFile">The file location of the trained ML model.</param>
        /// <param name="testFileLocation">The file location of the test data.</param>
        public static void Predict(string mlFile, string testFileLocation)
        {
            MLContext mlContext = new MLContext();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            //Define DataViewSchema for data preparation pipeline and trained model
            DataViewSchema modelSchema;

            // Load trained model
            ITransformer trainedModel = mlContext.Model.Load(mlFile, out modelSchema);
            PredictionEngine<AuthEventTransform, AuthEventTransformPrediction> predictionEngine = mlContext.Model.CreatePredictionEngine<AuthEventTransform, AuthEventTransformPrediction>(trainedModel);

            int tp = 0, tn = 0, fp = 0, fn = 0;
            using (var readerTruth = new StreamReader(testFileLocation))
            using (var csvTruth = new CsvReader(readerTruth, config))
            {
                while (csvTruth.Read())
                {
                    var authEvent = csvTruth.GetRecord<AuthEventTransform>();
                    var p = predictionEngine.Predict(authEvent);
                    if (authEvent.IsRedTeam && p.Prediction == authEvent.IsRedTeam)
                    {
                        tp++;
                    }
                    else if (authEvent.IsRedTeam && p.Prediction != authEvent.IsRedTeam)
                    {
                        fn++;
                    }
                    else if (!authEvent.IsRedTeam && p.Prediction == authEvent.IsRedTeam)
                    {
                        tn++;
                    }
                    else
                    {
                        fp++;
                    }
                }
            }

            Console.WriteLine($"True Positive: {tp}, False Negative: {fn}, True Negative: {tn}, False Positive {fp}");
            float pr = (float)tp / (float)((float)tp + (float)fp);
            float r = (float)tp / ((float)tp + (float)fn);
            float f1 = (2 * pr * r) / (pr + r);
            Console.WriteLine($"F1 Score: {f1}");
        }
    }
}
