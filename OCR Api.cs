using System;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    class OCR_api
    {
        // Add your Computer Vision subscription key and endpoint
        static string subscriptionKey = "8e9a84fb94654317ae472ff48c200ca7";
        static string endpoint = "https://ocrapi123.cognitiveservices.azure.com/";

        //private const string READ_TEXT_URL_IMAGE = "https://raw.githubusercontent.com/Azure-Samples/cognitive-services-sample-data-files/master/ComputerVision/Images/printed_text.jpg";


        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static async Task<List<string>> ReadFileUrl(string urlFile)
        {
          ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
            // Read text from URL
            List<string> ocr = new List<string>();
            var textHeaders = await client.ReadAsync(urlFile);
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            

            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Console.WriteLine($"Extracting text from URL file {Path.GetFileName(urlFile)}...");
            Console.WriteLine();
            do
            {   Thread.Sleep(1000);
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            // Display the found text.
            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    //Console.WriteLine(line.Text);
                    ocr.Add(line.Text);
                }
            }
            //Console.WriteLine();
            return ocr;
        }
        public static async Task<List<string>> GetOcr(string imageFile)
        {    
            ComputerVisionClient cvClient = Authenticate(endpoint, subscriptionKey);
            // Use Read API to read text in image
            using (var imageData = File.OpenRead(imageFile))
            {    
                 var readOp = await cvClient.ReadInStreamAsync(imageData,"zh-Hans");


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
            while ((results.Status == OperationStatusCodes.Running ||
            results.Status == OperationStatusCodes.NotStarted));
            List<string> ocr_list = new List<string>();

            // If the operation was successfuly, process the text line by line
            if (results.Status == OperationStatusCodes.Succeeded)
            {
                var textUrlFileResults = results.AnalyzeResult.ReadResults;
                foreach (ReadResult page in textUrlFileResults)
                {
                    foreach (Line line in page.Lines)
                    {
                       
                        ocr_list.Add(line.Text);
                    }
                }
            }    
            return ocr_list;    
            }   

        }

    }
}