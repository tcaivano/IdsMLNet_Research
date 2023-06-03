using Microsoft.ML;
using IdsMLNet_Research.Data;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Data;
using Microsoft.ML.Calibrators;
using IdsMLNet_Research.Enum;

namespace IdsMLNet_Research.Services
{
    public static class TrainingService
    {
        /// <summary>
        /// Trains a binary classification model using the provided truth file and test file, with the given trainer
        /// </summary>
        /// <param name="truthFileLocation">The file location of the truth data used for training.</param>
        /// <param name="testFileLocation">The file location of the test data used for evaluation.</param>
        /// <param name="backupFileLocation">The backup file location to save the trained model.</param>
        /// <param name="modelName">The name of the model.</param>"
        /// <param name="trainer">The trainer to use</param>
        /// <param name="saveModel">Indicates if the file should be saved</param>
        public static void TrainNetwork(string truthFileLocation, string testFileLocation, string backupFileLocation, string modelName, ETrainer trainer, bool saveModel = false)
        {
            // Prepare
            MLContext mlContext = new MLContext() { GpuDeviceId = 0, FallbackToCpu = false };
            IDataView trainingdata = mlContext.Data.LoadFromTextFile<AuthEventTransform>(truthFileLocation, hasHeader: false, separatorChar: ',');
            IDataView testDataView = mlContext.Data.LoadFromTextFile<AuthEventTransform>(testFileLocation, hasHeader: false, separatorChar: ',');

            var options = new FastTreeBinaryTrainer.Options
            {
                DiskTranspose = false,
                LabelColumnName = @"IsRedTeam",
                FeatureColumnName = @"Features"
            };

            // Create Pipeline
            IEstimator<ITransformer> pipeline = mlContext.Transforms.Text.FeaturizeText("SourceUserEncoded", "SourceUser")
                .Append(mlContext.Transforms.Text.FeaturizeText("DestinationUserEncoded", "DestinationUser"))
                .Append(mlContext.Transforms.Text.FeaturizeText("SourceComputerEncoded", "SourceComputer"))
                .Append(mlContext.Transforms.Text.FeaturizeText("DestinationComputerEncoded", "DestinationComputer"))
                .Append(mlContext.Transforms.Text.FeaturizeText("LogonTypeEncoded", "LogonType"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("AuthenticationOrientationEncoded", "AuthenticationOrientation"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("IsSuccessfulEncoded", "IsSuccessful"))
                .Append(mlContext.Transforms.Concatenate("Features", "SourceUserEncoded", "DestinationUserEncoded", "SourceComputerEncoded", "LogonTypeEncoded", "AuthenticationOrientationEncoded", "IsSuccessfulEncoded"));

            switch (trainer)
            {
                case ETrainer.FastTree:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.FastTree(options));
                    break;
                case ETrainer.SdcaLogisticRegression:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.AveragedPerceptron:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.AveragedPerceptron(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.FastForest:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.FieldAwareFactorizationMachine:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.Gam:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.Gam(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.LbfgsLogisticRegression:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.LdSvm:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.LdSvm(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.LinearSvm:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.Prior:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.Prior(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.SdcaNonCalibrated:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.SdcaNonCalibrated(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.SgdCalibrated:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.SgdCalibrated(labelColumnName: "IsRedTeam"));
                    break;
                case ETrainer.SgdNonCalibrated:
                    pipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.SgdNonCalibrated(labelColumnName: "IsRedTeam"));
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Fit
            DateTime now = GetNowAndDisplay();
            ITransformer trainedModel = pipeline.Fit(trainingdata);
            DisplayTrainingEnd(now);

            // Evaluate
            var predictions = trainedModel.Transform(testDataView);
            var metrics = mlContext.BinaryClassification.EvaluateNonCalibrated(predictions, "IsRedTeam");
            DisplayTrainingMetrics(trainedModel, metrics);

            // Save
            if (saveModel) SaveModel(backupFileLocation, modelName, mlContext, trainingdata, now, trainedModel);
        }

        /// <summary>
        /// Displays the training metrics of the trained model.
        /// </summary>
        /// <param name="trainedModel">The trained model.</param>
        /// <param name="metrics">The binary classification metrics.</param>
        private static void DisplayTrainingMetrics(object trainedModel, BinaryClassificationMetrics metrics)
        {
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
        }

        /// <summary>
        /// Displays the end of the training process.
        /// </summary>
        /// <param name="now">The start time of the training.</param>
        private static void DisplayTrainingEnd(DateTime now)
        {
            var end = DateTime.Now;
            Console.WriteLine($"************************************************************");
            Console.WriteLine($"*      Fit End                                             *");
            Console.WriteLine($"*      End Time: {end}");
            Console.WriteLine($"*      Time Elapsed: {(end - now).TotalSeconds} seconds");
            Console.WriteLine($"************************************************************");
        }

        /// <summary>
        /// Gets the current time and displays the start of the training.
        /// </summary>
        /// <returns>The current time.</returns>
        private static DateTime GetNowAndDisplay()
        {
            var now = DateTime.Now;
            Console.WriteLine($"************************************************************");
            Console.WriteLine($"*      Fit Start                                           *");
            Console.WriteLine($"*      Start Time: {now}");
            Console.WriteLine($"************************************************************");
            return now;
        }

        /// <summary>
        /// Saves the trained model to a file.
        /// </summary>
        /// <param name="backupFileLocation">The backup file location to save the trained model.</param>
        /// <param name="modelName">The name of the model.</param>
        /// <param name="mlContext">The MLContext.</param>
        /// <param name="trainingdata">The training data.</param>
        /// <param name="now">The start time of the training.</param>
        /// <param name="trainedModel">The trained model.</param>
        private static void SaveModel(string backupFileLocation, string modelName, MLContext mlContext, IDataView trainingdata, DateTime now, ITransformer trainedModel)
        {
            DataViewSchema dataViewSchema = trainingdata.Schema;
            using (var fs = File.Create(backupFileLocation + "model" + modelName + now.ToFileTimeUtc()))
            {
                mlContext.Model.Save(trainedModel, dataViewSchema, fs);
            }
        }
    }
}
