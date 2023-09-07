using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;

namespace Labb2_ComputerVision
{
    internal class Program
    {
        private static ComputerVisionClient cvClient;
        static async Task Main(string[] args)
        {
            try
            {
                // Load configuration settings from appsettings.json
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot config = builder.Build();
                string cogEndpoint = config["CognitiveServiceEndpoint"];
                string cogKey = config["CognitiveServiceKey"];

                // Create credentials for the Cognitive Service API
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogKey);
                cvClient = new ComputerVisionClient(credentials)
                {
                    Endpoint = cogEndpoint
                };

                // Prompt the user for an image URL or a local file path
                Console.Write("Enter a URL or a local file that you want to analyze: ");
                string input = Console.ReadLine();

                if (!string.IsNullOrEmpty(input))
                {
                    if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                    {
                        // User provided a URL, so analyze the image from the URL
                        var imageAnalysis = await AnalyzeImageAsync(input, GetFeatures());
                        DisplayImageAnalysisResults(imageAnalysis);

                        await GenerateThumbnail(input);
                    }
                    else if (File.Exists(input))
                    {
                        // User provided a file path, so analyze the local image path
                        var imageAnalysis = await AnalyzeLocalImageAsync(input, GetFeatures());
                        DisplayImageAnalysisResults(imageAnalysis);

                        await GenerateThumbnail(input);
                    }
                    else
                    {
                        Console.WriteLine("You must provide a valid image URL or file path.");
                    }
                }
                else
                {
                    Console.WriteLine("You must provide a valid image URL or file path.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static List<VisualFeatureTypes?> GetFeatures()
        {
            return new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult
            };
        }
        // Analyze an image from a URL using Cognitive Services API
        static async Task<ImageAnalysis> AnalyzeImageAsync(string imagePath, List<VisualFeatureTypes?> features)
        {
            using (var httpClient = new HttpClient())
            {
                var imageStream = await httpClient.GetStreamAsync(imagePath);

                var imageAnalysis = await cvClient.AnalyzeImageInStreamAsync(imageStream, features);
               

                return imageAnalysis;
            }
        }

        // Analyze a local image file using Cognitive Services API
        static async Task<ImageAnalysis> AnalyzeLocalImageAsync(string imagePath, List<VisualFeatureTypes?> features)
        {
            using (var imageStream = File.OpenRead(imagePath))
            {
                var imageAnalysis = await cvClient.AnalyzeImageInStreamAsync(imageStream, features);

                return imageAnalysis;
            }
        }

        // Display the results of image analysis
        static async void DisplayImageAnalysisResults(ImageAnalysis imageAnalysis)
        {
            if (imageAnalysis != null)
            {
                Console.WriteLine("Image analysis results: ");
                Console.WriteLine($"Desription: {imageAnalysis.Description.Captions[0].Text}");
                Console.WriteLine("Tags: ");
                foreach (var tag in imageAnalysis.Tags)
                {
                    Console.WriteLine($"-{tag.Name}");
                }

                Console.WriteLine("Categories: ");
                foreach (var category in imageAnalysis.Categories)
                {
                    Console.WriteLine($"- {category.Name} (confidence: {category.Score.ToString("P")})");
                }

                Console.WriteLine("Brands: ");
                foreach (var brand in imageAnalysis.Brands)
                {
                    Console.WriteLine($"- {brand.Name} (confidence: {brand.Confidence.ToString("P")})");
                }

                Console.WriteLine("Objects in image: ");
                foreach (var detectedObject in imageAnalysis.Objects)
                {
                    Console.WriteLine($"- {detectedObject.ObjectProperty} (confidence: {detectedObject.Confidence.ToString("P")})");
                }

                Console.WriteLine("Ratings: ");
                Console.WriteLine($"- Adult: {imageAnalysis.Adult.IsAdultContent}");
                Console.WriteLine($"- Racy: {imageAnalysis.Adult.IsRacyContent}");
                Console.WriteLine($"- Gore: {imageAnalysis.Adult.IsGoryContent}");
            }
            else
            {
                Console.WriteLine("Image analysis failed or returned null results.");
            }
            
        }

        // Generate a thumbnail image from the provided source (URL or local file)
        static async Task GenerateThumbnail(string imageSource)
        {
            try
            {
                Console.WriteLine("Generating thumbnail...");
                Stream thumbnailStream = null;

                if (Uri.IsWellFormedUriString(imageSource, UriKind.Absolute))
                {
                    // For a URL, use directly
                    thumbnailStream = await cvClient.GenerateThumbnailAsync(100, 100, imageSource, true);
                }
                else if (File.Exists(imageSource))
                {
                    // For a local file, open and use a file stream
                    using (var imageStream = File.OpenRead(imageSource))
                    {
                        thumbnailStream = await cvClient.GenerateThumbnailInStreamAsync(100, 100, imageStream, true);
                    }
                }

                if (thumbnailStream != null)
                {
                    // Save the generated thumbnail to a file
                    string thumbnailFileName = @"D:\Visual Studios\.NET 22\AI\Labb2_ComputerVision\thumbnail.jpg";

                    using (Stream thumbnailFile = File.Create(thumbnailFileName))
                    {
                        thumbnailStream.CopyTo(thumbnailFile);
                    }
                    Console.WriteLine($"Thumbnail saved in {thumbnailFileName}");
                }
                else
                {
                    Console.WriteLine($"Failed to generate thumbnail.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
            }
        }

    }
}