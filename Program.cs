using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using TagLib;


namespace Finding_Album_Art
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the folder path containing your music files:");
            var folderPath = Console.ReadLine();

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Directory does not exist. Please check the path and try again.");
                return;
            }

            var audioFiles = Directory.GetFiles(folderPath, "*.wav", SearchOption.AllDirectories);
            foreach (var file in audioFiles)
            {
                var hasAlbumArt = CheckForAlbumArt(file);
                if (!hasAlbumArt)
                {
                    var songName = Path.GetFileNameWithoutExtension(file);
                    Console.WriteLine($"Fetching album art for: {songName}");
                    var imageUrl = await SearchForImageAsync(songName);
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        await DownloadAndSetAlbumArt(file, imageUrl);
                        Console.WriteLine($"Album art updated for: {songName}");
                    }
                    else
                    {
                        Console.WriteLine($"No album art found for: {songName}");
                    }
                }
            }
        }

        static bool CheckForAlbumArt(string filePath)
        {
            try
            {
                // Correctly use TagLib to check album art
                var file = TagLib.File.Create(filePath);
                bool hasAlbumArt = file.Tag.Pictures.Length > 0;
                file.Dispose(); // Properly dispose of the file to avoid locking it
                return hasAlbumArt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking album art for {filePath}: {ex.Message}");
                return false; // Assume no album art if there's an error
            }
        }

        static async Task<string> SearchForImageAsync(string songName)
        {
            using var httpClient = new HttpClient();
            var query = Uri.EscapeDataString(songName);
            var url = $"https://itunes.apple.com/search?term={query}&media=music&entity=song&limit=1";

            var response = await httpClient.GetStringAsync(url);
            return ExtractImageUrlFromResponse(response);
        }

        static string ExtractImageUrlFromResponse(string jsonResponse)
        {
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;
            var results = root.GetProperty("results");
            if (results.GetArrayLength() > 0)
            {
                var artworkUrl = results[0].GetProperty("artworkUrl100").GetString();
                return artworkUrl?.Replace("100x100", "500x500");
            }
            return null;
        }

        static async Task DownloadAndSetAlbumArt(string filePath, string imageUrl)
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

            // Define the path for the album art
            var albumDirectory = Path.GetDirectoryName(filePath);
            var albumArtPath = Path.Combine(albumDirectory, "cover.jpg");

            try
            {
                // Ensure the directory exists
                if (!Directory.Exists(albumDirectory))
                {
                    Directory.CreateDirectory(albumDirectory);
                }

                // Save the image as cover.jpg in the album directory
                await System.IO.File.WriteAllBytesAsync(albumArtPath, imageBytes);

                Console.WriteLine($"Album art saved to {albumArtPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting album art for {filePath}: {ex.Message}");
            }
        }
    }

    
}