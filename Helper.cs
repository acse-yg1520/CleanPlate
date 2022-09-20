using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;

namespace CleanPlateBot
{
    public static class Helper
    {
        public static async Task<bool> ImagePromptValidatorAsync(PromptValidatorContext<IList<Attachment>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                var attachments = promptContext.Recognized.Value;
                var validImages = new List<Attachment>();

                foreach (var attachment in attachments)
                {
                    if (attachment.ContentType.Contains("image/") || attachment.ContentType == FileDownloadInfo.ContentType)
                    {
                        validImages.Add(attachment);
                    }
                }

                promptContext.Recognized.Value = validImages;

                // If none of the attachments are valid images, the retry prompt should be sent.
                return validImages.Any();
            }
            else
            {
                await promptContext.Context.SendActivityAsync("No attachments received. Proceeding without a profile picture...");

                // We can return true from a validator function even if Recognized.Succeeded is false.
                return false;
            }
        }

        public static async Task<string> DownloadImage(HttpClient client, string credentialToken, Attachment attachment, string channelId, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;

            if (channelId == "msteams")
            {
                if (attachment.ContentType == "image/*")
                { // send from Mobile Teams
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentialToken);
                    response = await client.GetAsync(attachment.ContentUrl);
                }
                else
                { //send from Desktop Teams, ContentType == "application/vnd.microsoft.teams.file.download.info"
                    var fileDownload = JObject.FromObject(attachment.Content).ToObject<FileDownloadInfo>();
                    response = await client.GetAsync(fileDownload.DownloadUrl);
                }
            }
            else
            {
                //from other channels, worked with Bot Emulator
                response = await client.GetAsync(attachment.ContentUrl);
            }

            var filePath = Path.Combine("Files", $"{Guid.NewGuid().ToString()}.png");
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                //add memory stream here
                await response.Content.CopyToAsync(fileStream);
            }

            return filePath;
        }

        public static async Task<List<string>> GetOcr(string imageFile, string endpoint, string subscriptionKey)
        {
            //initialize ComputerVisionClient
            var credential = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(subscriptionKey);
            var cvClient = new ComputerVisionClient(credential);
            cvClient.Endpoint = endpoint;

            // Use Read API to read text in image
            using (var imageData = File.OpenRead(imageFile))
            {
                var readOp = await cvClient.ReadInStreamAsync(imageData, "zh-Hans");

                // Get the async operation ID so we can check for the results
                string operationLocation = readOp.OperationLocation;
                string operationId = operationLocation.Substring(operationLocation.Length - 36);

                // Wait for the asynchronous operation to complete
                ReadOperationResult results;
                do
                {
                    Thread.Sleep(1000);
                    results = await cvClient.GetReadResultAsync(Guid.Parse(operationId));
                }
                while (
                    results.Status == OperationStatusCodes.Running ||
                    results.Status == OperationStatusCodes.NotStarted);

                var ocr_list = new List<string>();

                // If the operation was successfuly, process the text line by line
                if (results.Status == OperationStatusCodes.Succeeded)
                {
                    var textUrlFileResults = results.AnalyzeResult.ReadResults;
                    foreach (var page in textUrlFileResults)
                    {
                        foreach (var line in page.Lines)
                        {
                            ocr_list.Add(line.Text);
                        }
                    }
                }
                return ocr_list;
            }
        }

        public static async Task<string> GetImagePredictionsAsync(string filePath, string predictionKey, string endpoint, string projetId, string publishedName)
        {
            //initialize CustomVisionPredictionClient
            var credential = new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(predictionKey);
            var predictionApi = new CustomVisionPredictionClient(credential);
            predictionApi.Endpoint = endpoint;

            // Get predictions from the image
            //using (var imageStream = new FileStream(imageFile, FileMode.Open))
            using (var imageStream = new MemoryStream(File.ReadAllBytes(filePath)))
            {
                var results = await predictionApi.ClassifyImageAsync(new Guid(projetId), publishedName, imageStream);
                var topPrediction = results.Predictions
                                    .OrderByDescending(q => q.Probability)
                                    .FirstOrDefault();

                return topPrediction.TagName;
            };

        }
    }
}