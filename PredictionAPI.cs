using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CleanPlateBot
{

    class predictionApi
    {
        // Set your Custom Vision service details here...
        static string Endpoint = "https://southcentralus.api.cognitive.microsoft.com";
        static string PredictionKey = "c1cc5979557b4a118cb78602ef56bd75";

        // Set your Custom Vision project details here...
        static string ProjetId = "80f7b38c-dcea-413a-bce6-0f6d5cdb599d";
        static string PublishedName = "Classification";
        

        private static CustomVisionPredictionClient AuthenticatePrediction(string endpoint, string predictionKey)
        {
        // Create a prediction endpoint, passing in the obtained prediction key
         CustomVisionPredictionClient predictionApi = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(predictionKey))
           {
              Endpoint = endpoint
           };
           return predictionApi;
        }
        public static async Task<string> GetImagePredictionsAsync(string filePath)
        {
            CustomVisionPredictionClient predictionApi = AuthenticatePrediction(Endpoint, PredictionKey);
            
            // Get predictions from the image
            //using (var imageStream = new FileStream(imageFile, FileMode.Open))
            using (var imageStream = new MemoryStream(File.ReadAllBytes(filePath)))
            {
                var results = await predictionApi.ClassifyImageAsync
                    (new Guid(ProjetId), PublishedName, imageStream);
                var topPrediction = results.Predictions.OrderByDescending(q => q.Probability).FirstOrDefault();
                string result = topPrediction.TagName;
                return result;
            };
            
        }
    }  
}

