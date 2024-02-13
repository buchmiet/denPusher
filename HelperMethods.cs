using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ZstdNet;

namespace denPusher
{
    internal static class HelperMethods
    {
        public static string TarFolder(string destination, string? source)
        {
            if (File.Exists(destination))
            {
                Console.WriteLine($"File {destination} already exists");
                Environment.Exit(1);
            }
            if (!destination.Contains(".tar", StringComparison.CurrentCultureIgnoreCase))
            {
                destination += ".tar";
            }
            try
            {
                using (var stream = File.Create(destination))
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file '{destination}': {ex.Message}");
                Environment.Exit(1);
            }

            var sourceDirectory = string.IsNullOrEmpty(source) ? Directory.GetCurrentDirectory() : source;
            var directoryInfo = new DirectoryInfo(sourceDirectory);

            if (!directoryInfo.Exists || directoryInfo.GetFiles("*", SearchOption.AllDirectories).Length == 0)
            {
                Console.WriteLine($"No files found in the source directory {sourceDirectory}.");
                Environment.Exit(1);
            }
            Console.Write($"Packing the content of {sourceDirectory} to {destination} ...");

            using (var tarStream = File.Create(destination))
            {
                try
                {
                    TarFile.CreateFromDirectory(sourceDirectory, tarStream, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Error while archiving file '{destination}': {ex.Message}");
                    Environment.Exit(1);
                }
            }
            Console.WriteLine("done.");
            return destination;
        }

        public static void CompressFile(string inputFile, string outputFile, byte compressionLevel)
        {
            const int bufferSize = 4096 * 32;
            CompressionOptions compressionOptions = new CompressionOptions(compressionLevel);
            using (var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (var bufferedInput = new BufferedStream(inputStream, bufferSize))
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (var compressionStream = new CompressionStream(outputStream, compressionOptions))
            {
                bufferedInput.CopyTo(compressionStream);
            }
        }

        public static string GenerateUniqueFileName(string directoryPath, string baseFileName)
        {
            var counter = 0;
            var newFileName = baseFileName;
            while (File.Exists(Path.Combine(directoryPath, newFileName)))
            {
                counter++;
                newFileName = $"{baseFileName}{counter}";
            }

            return newFileName;
        }

        public static string PackFolder(string destination, string? source, int level)
        {
            var inFile = TarFolder(destination, source);
            if (!inFile.Contains(".zstd", StringComparison.CurrentCultureIgnoreCase))
            {
                destination = inFile + ".zstd";
            }
            try
            {
                using (var stream = File.Create(destination))
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file '{destination}': {ex.Message}");
                Environment.Exit(1);
            }

            int originalMin = 1, originalMax = 100, targetMin = 1, targetMax = 22;
            int transformedLevel = (int)(((double)(level - originalMin) / (originalMax - originalMin)) * (targetMax - targetMin) + targetMin);
            transformedLevel = Math.Max(targetMin, Math.Min(transformedLevel, targetMax));
            Console.Write($"Compressing {source}...");
            CompressFile(inFile, destination, (byte)transformedLevel);
            File.Delete(inFile);
            return destination;
        }

        private static string FormatBytes(long bytes)
        {
            const long scale = 1024;
            string[] orders = new string[] { "GB", "MB", "KB", "B" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            var formattedBytes = new List<string>();
            foreach (string order in orders)
            {
                if (bytes >= max)
                {
                    long value = bytes / max;
                    if (value > 0)
                    {
                        formattedBytes.Add(string.Format("{0} {1}", value, order));
                    }

                    bytes %= max;
                }

                max /= scale;
            }

            return string.Join(" ", formattedBytes);
        }

        static readonly int totalBlocks = 20;
        static void DisplayProgress(long bytesRead, long totalBytes, long totalBytesToRead, Stopwatch stopwatch, int cursorTop)
        {
            double progress = (double)bytesRead / totalBytesToRead;
            int blocksFilled = (int)(progress * totalBlocks);
            double speed = 0;

            // Oblicz prędkość transferu (w megabitach na sekundę) tylko jeśli stopwatch ma wartość
            if (stopwatch.Elapsed.TotalSeconds > 0)
            {
                speed = (bytesRead * 8) / (stopwatch.Elapsed.TotalSeconds * 1024 * 1024); // Mbps
            }

            // Ustawienie kursora na zapisaną pozycję, aby zaktualizować postęp
            Console.SetCursorPosition(0, cursorTop);

            // Czyszczenie linii dla postępu i prędkości
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, cursorTop);

            // Aktualizacja postępu
            Console.Write("Progress [");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('■', blocksFilled));
            Console.ResetColor();
            Console.Write(new string(' ', totalBlocks - blocksFilled) + $"] {progress * 100:0.00}%");

            // Przejście do nowej linii dla prędkości
            Console.SetCursorPosition(0, cursorTop + 1);
            Console.Write(new string(' ', Console.WindowWidth)); // Czyszczenie linii
            Console.SetCursorPosition(0, cursorTop + 1);
            Console.Write($"{FormatBytes(bytesRead)} of {FormatBytes(totalBytesToRead)} at {speed:0.00} Mbps");

            // Zapewnienie, że kolejne komunikaty będą wyświetlane poniżej
            Console.SetCursorPosition(0, cursorTop + 2);
        }

        public static string AddFiles(string katalogBiezacy)
        {
            StringBuilder ret = new StringBuilder();
            var tymczasowepliki = Directory.GetFiles(katalogBiezacy, "*.*", SearchOption.AllDirectories);
            foreach (var tym in tymczasowepliki)
            {
                FileAttributes fileAttributes = File.GetAttributes(tym);
                if ((fileAttributes & FileAttributes.Directory) == 0)
                {
                    ret.Append(tym.AsSpan(katalogBiezacy.Length + 1, tym.Length - katalogBiezacy.Length - 1));
                    ret.AppendLine();
                }
            }
            return ret.ToString();
        }

        public static async Task UploadFile(string url, string filePath, string files, string? version = null, TokenResponse? tokenResponse = null)
        {
            long fileSize = new FileInfo(filePath).Length;
            Stopwatch stopwatch = new Stopwatch();
            int originalCursorTop = Console.CursorTop;
            stopwatch.Start();

            using var stream = new ProgressFileStream(filePath, FileMode.Open);
            stream.ProgressChanged += (bytesRead, totalBytes) =>
            {
                DisplayProgress((long)bytesRead, totalBytes, fileSize, stopwatch, originalCursorTop);
            };
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var filesContent = new StringContent(files, Encoding.UTF8, "text/plain");
            var content = new MultipartFormDataContent
            {
                { fileContent, "file", Path.GetFileName(filePath) },
                { filesContent, "files" }
            };
            if (!string.IsNullOrEmpty(version))
            {
                var versionContent = new StringContent(version, Encoding.UTF8, "text/plain");
                content.Add(versionContent, "version");
            }
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            if (tokenResponse != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(tokenResponse.tokenType, tokenResponse.accessToken);
            }
            using var client = new HttpClient();
            try
            {

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
               
                Console.WriteLine("File uploaded successfully.");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error. ");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }

        public static async Task<TokenResponse> GetToken(string username, string password, string url)
        {
            using var client = new HttpClient();
            TokenResponse tkresponse = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                var content = new StringContent($"{{\r\n \"email\":\"{username}\",\r\n \"password\":\"{password}\"\r\n}}", Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                tkresponse = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Environment.Exit(1);
            }
            return tkresponse;
        }
    }
}
