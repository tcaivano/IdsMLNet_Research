using Microsoft.ML;
using IdsMLNet_Research.Data;
using Microsoft.ML.Trainers.FastTree;
using static IdsMLNet_Research.Data.AuthEventTransform;
using SkiaSharp;

namespace IdsMLNet_Research.Services
{
    public static class TrainingService
    {
        public static void TrainNetwork(string truthFileLocation, string testFileLocation)
        {
            MLContext mlContext = new MLContext() { GpuDeviceId = 0, FallbackToCpu = false };
            IDataView trainingdata = mlContext.Data.LoadFromTextFile<AuthEventTransform>(truthFileLocation, hasHeader: false, separatorChar: ',');
            IDataView testDataView = mlContext.Data.LoadFromTextFile<AuthEventTransform>(testFileLocation, hasHeader: false, separatorChar: ',');

            var pipeline = mlContext.Transforms.Text.FeaturizeText("SourceUserEncoded", "SourceUser")
                .Append(mlContext.Transforms.Text.FeaturizeText("DestinationUserEncoded", "DestinationUser"))
                .Append(mlContext.Transforms.Text.FeaturizeText("SourceComputerEncoded", "SourceComputer"))
                .Append(mlContext.Transforms.Text.FeaturizeText("DestinationComputerEncoded", "DestinationComputer"))
                .Append(mlContext.Transforms.Text.FeaturizeText("LogonTypeEncoded", "LogonType"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("AuthenticationOrientationEncoded", "AuthenticationOrientation"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("IsSuccessfulEncoded", "IsSuccessful"))
                .Append(mlContext.Transforms.Concatenate("Features", "SourceUserEncoded", "DestinationUserEncoded", "SourceComputerEncoded", "LogonTypeEncoded", "AuthenticationOrientationEncoded", "IsSuccessfulEncoded"))
                .Append(mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: @"IsRedTeam", featureColumnName: @"Features"));


            var now = DateTime.Now;
            Console.WriteLine($"************************************************************");
            Console.WriteLine($"*      Fit Start                                           *");
            Console.WriteLine($"*      Start Time: {now}");
            Console.WriteLine($"************************************************************");
            
            var trainedModel = pipeline.Fit(trainingdata);
            var end = DateTime.Now;
            Console.WriteLine($"************************************************************");
            Console.WriteLine($"*      Fit End                                             *");
            Console.WriteLine($"*      End Time: {end}");
            Console.WriteLine($"*      Time Elapsed: {(end - now).TotalSeconds} seconds");
            Console.WriteLine($"************************************************************");
            var predictions = trainedModel.Transform(testDataView);

            var metrics = mlContext.BinaryClassification.EvaluateNonCalibrated(predictions, "IsRedTeam");


            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine($"************************************************************");
            Console.WriteLine($"*       Metrics for {trainedModel.ToString()} binary classification model      ");
            Console.WriteLine($"*-----------------------------------------------------------");
            Console.WriteLine($"*       Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"*       Area Under Roc Curve:      {metrics.AreaUnderRocCurve:P2}");
            Console.WriteLine($"*       Area Under PrecisionRecall Curve:  {metrics.AreaUnderPrecisionRecallCurve:P2}");
            Console.WriteLine($"*       F1Score:  {metrics.F1Score:P2}");
            Console.WriteLine($"*       PositivePrecision:  {metrics.PositivePrecision:#.##}");
            Console.WriteLine($"*       PositiveRecall:  {metrics.PositiveRecall:#.##}");
            Console.WriteLine($"*       NegativePrecision:  {metrics.NegativePrecision:#.##}");
            Console.WriteLine($"*       NegativeRecall:  {metrics.NegativeRecall:P2}");
            Console.WriteLine($"************************************************************");
            Console.WriteLine("");
            Console.WriteLine("");


            DataViewSchema dataViewSchema = trainingdata.Schema;
            using (var fs = File.Create(truthFileLocation + "\\Outputs\\" + now.ToFileTime()))
            {
                mlContext.Model.Save(trainedModel, dataViewSchema, fs);
            }
        }

        private static void Predict(MLContext mlContext, ITransformer trainedModel, string testFileLocation)
        {
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
