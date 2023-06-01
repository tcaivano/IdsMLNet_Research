using Microsoft.ML;
using IdsMLNet_Research.Data;
using Microsoft.ML.Trainers.FastTree;
using static IdsMLNet_Research.Data.AuthEventTransform;

namespace IdsMLNet_Research.Services
{
    public static class TrainingService
    {
        public static void TrainNetwork(string truthFileLocation, string testFileLocation)
        {
            MLContext mlContext = new MLContext() { GpuDeviceId = 0, FallbackToCpu = false };
            IDataView trainingdata = mlContext.Data.LoadFromTextFile<AuthEventTransform>(truthFileLocation, hasHeader: false, separatorChar: ',');
            IDataView testDataView = mlContext.Data.LoadFromTextFile<AuthEventTransform>(testFileLocation, hasHeader: false, separatorChar: ';');


            var pipeline = mlContext.Transforms.Categorical.OneHotEncoding(new[] {
                new InputOutputColumnPair(nameof(AuthEventTransform.SourceUser), nameof(AuthEventTransform.SourceUser)),
                new InputOutputColumnPair(nameof(AuthEventTransform.DestinationUser), nameof(AuthEventTransform.DestinationUser)),
                new InputOutputColumnPair(nameof(AuthEventTransform.SourceComputer), nameof(AuthEventTransform.SourceComputer)),
                new InputOutputColumnPair(nameof(AuthEventTransform.DestinationComputer), nameof(AuthEventTransform.DestinationComputer)),
                new InputOutputColumnPair(nameof(AuthEventTransform.LogonType), nameof(AuthEventTransform.LogonType)),
                new InputOutputColumnPair(nameof(AuthEventTransform.AuthenticationOrientation), nameof(AuthEventTransform.AuthenticationOrientation)),
                new InputOutputColumnPair(nameof(AuthEventTransform.IsSuccessful), nameof(AuthEventTransform.IsSuccessful))
            });
            pipeline.Append(mlContext.Transforms.Concatenate("Features",
                nameof(AuthEventTransform.SourceUser),
                nameof(AuthEventTransform.DestinationUser),
                nameof(AuthEventTransform.SourceComputer),
                nameof(AuthEventTransform.DestinationComputer),
                nameof(AuthEventTransform.LogonType),
                nameof(AuthEventTransform.AuthenticationOrientation),
                nameof(AuthEventTransform.IsSuccessful)));
            pipeline.Append(mlContext.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options() { NumberOfLeaves = 4, MinimumExampleCountPerLeaf = 20, NumberOfTrees = 4, MaximumBinCountPerFeature = 254, FeatureFraction = 1, LearningRate = 0.1, LabelColumnName = @"IsRedTeam", FeatureColumnName = @"Features" }));
            pipeline.AppendCacheCheckpoint(mlContext);

            ITransformer trainedModel = pipeline.Fit(trainingdata);

            PredictionEngine<AuthEventTransform, AuthEventPrediction> predictionEngine = mlContext.Model.CreatePredictionEngine<AuthEventTransform, AuthEventPrediction>(trainedModel);


            var authEventsTest = LargeParserService.GetAuthEventsFromFile(testFileLocation);

            int correct = 0, incorrect = 0;

            foreach (var authEvent in authEventsTest)
            {
                var p = predictionEngine.Predict(authEvent);
                if (p.IsRedTeam == authEvent.IsRedTeam)
                {
                    correct++;
                }
                else
                {
                    incorrect++;
                }
            }
            Console.WriteLine($"Correct: {correct}, Incorrect: {incorrect}");

            //var predictions = trainedModel.Transform(testDataView);
            //var metrics = mlContext.BinaryClassification.Evaluate(predictions, "IsRedTeam");
            //Console.WriteLine($"F1: {metrics.F1Score}");
            DataViewSchema dataViewSchema = trainingdata.Schema;
            using (var fs = File.Create("test"))
            {
                mlContext.Model.Save(trainedModel, dataViewSchema, fs);
            }
        }
    }
}
