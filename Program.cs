using Cocona;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace denPusher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CoconaApp.Run<Program>(args);
        }

        [Command("tar", Description = "Creates a tar archive from a source directory.")]
        public void TarCommand([Argument(Name = "destination", Description = "The output tar file name. This parameter is required.")] string destination,
            [Argument(Name = "source", Description = "The source directory to tar. If not specified, defaults to the current directory.")] string? source)
        {
            Console.WriteLine($"{HelperMethods.TarFolder(destination, source)} created");
        }

        [Command("pack", Description = "Packs files into a tar archive and then compresses it using the Zstandard method.")]
        public void PackCommand(
    [Argument(Name = "destination", Description = "The output tar file name. This parameter is required.")]
    string destination,

    [Argument(Name = "source", Description = "The source directory to pack. If not specified, defaults to the current directory.")]
    string? source = null,

    [Option("compression", ['c'], Description = "Optional compression level using Zstandard. Ranges from 1 (lowest compression) to 100 (highest compression). Default is 100.")]
    [Range(1, 100)]
    int level = 100)
        {
            Console.WriteLine($"{HelperMethods.PackFolder(destination, source, level)} created");
            Console.WriteLine("done!");
        }

        [Command("upload", Description = "Packs files into a tar archive, compresses them, and uploads to a web API. Requires only the URL of the API. Supports optional parameters for source directory, authentication (via tokens), compression level, and application binary version.")]
        public async Task UploadCommand(
           [Argument(Description = "The URL address of the web API where the tar file will be uploaded.")] string url,
   [Argument(Description = "The source directory to pack and compress. If not specified, defaults to the current directory.")] string? source,
   [Option("login", ['l'], Description = "The API endpoint for authentication, where username can be exchanged for a token. Specify only the endpoint part.")] string? login,
   [Option("username", ['u'], Description = "The username for API authentication.")] string? username,
   [Option("password", ['p'], Description = "The password for API authentication.")] string? password,
   [Range(1, 100)][Option("compression", ['c'], Description = "Optional compression level using Zstandard. Ranges from 1 (lowest compression) to 100 (highest compression). Default is 100.")] int level = 100,
   [Option("version", ['v'], Description = "The version of the application binaries, if required by the web API.")] string? version = null)
        {
            if (!string.IsNullOrEmpty(login) && (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)))
            {
                Console.WriteLine($"You need to provide username and password for login enabled web apis");
                Environment.Exit(1);
            }
            if (!url.StartsWith("http"))
            {
                url = "https://" + url;
            }
            var uri = new Uri(url);
            TokenResponse tkresponse = null;

            if (!string.IsNullOrEmpty(login))
            {
                var baseAddress = $"{uri.Scheme}://{uri.Authority}";
                tkresponse = await HelperMethods.GetToken(username, password, baseAddress + "/" + login);
                Console.WriteLine("Token obtained successfully");
                if (tkresponse == null)
                {
                    Console.WriteLine("Error obtaining token (are username and/or password correct?)");
                    Environment.Exit(1);
                }
            }
          

            var baseFileName = "appBinaries";
            var directoryPath = Directory.GetCurrentDirectory();
            var newFileName = HelperMethods.GenerateUniqueFileName(directoryPath, baseFileName);
            var fullPath = Path.Combine(directoryPath, newFileName);
            var archive = HelperMethods.PackFolder(fullPath, source, level);
            var allFiles = HelperMethods.AddFiles(Directory.GetCurrentDirectory());
            await HelperMethods.UploadFile(url, archive, allFiles, version, tkresponse);
        }

    }
}
