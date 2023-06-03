using IdsMLNet_Research.Data;
using Microsoft.ML;
using System;
using System.Collections.Generic;
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

            //Define DataViewSchema for data preparation pipeline and trained model
            DataViewSchema modelSchema;

            // Load trained model
            ITransformer trainedModel = mlContext.Model.Load(mlFile, out modelSchema);
            PredictionEngine<AuthEventTransform, AuthEventTransformPrediction> predictionEngine = mlContext.Model.CreatePredictionEngine<AuthEventTransform, AuthEventTransformPrediction>(trainedModel);


            var authEventsTest = LargeParserService.GetAuthEventsFromFile(testFileLocation);
            int correct = 0, incorrect = 0;

            foreach (var authEvent in authEventsTest)
            {
                var p = predictionEngine.Predict(authEvent);
                if (p.Prediction == authEvent.IsRedTeam)
                {
                    correct++;
                }
                else
                {
                    incorrect++;
                }
            }
            Console.WriteLine($"Correct: {correct}, Incorrect: {incorrect}");
        }
    }
}
