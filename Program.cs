namespace VideoUploader;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Video Uploader POC ===");
        Console.WriteLine("Secure Video Upload/Download with Encryption & Compression\n");

        try
        {
            // Configuration
            const string credentialsPath = "credentials.json"; // Google Drive API credentials

            // Check if credentials exist
            if (!File.Exists(credentialsPath))
            {
                Console.WriteLine("❌ Error: credentials.json not found!");
                Console.WriteLine("\nTo use this POC, you need Google Drive API credentials:");
                Console.WriteLine("1. Go to https://console.cloud.google.com/");
                Console.WriteLine("2. Create a new project or select existing one");
                Console.WriteLine("3. Enable Google Drive API");
                Console.WriteLine("4. Create credentials (Service Account)");
                Console.WriteLine("5. Download the JSON key file and save it as 'credentials.json' in the project directory");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // Initialize services
            var driveService = new GoogleDriveService(credentialsPath);
            var pipeline = new VideoProcessingPipeline(driveService);

            // Interactive menu
            while (true)
            {
                Console.WriteLine("\n=== Main Menu ===");
                Console.WriteLine("1. Upload Video (Compress + Encrypt + Upload)");
                Console.WriteLine("2. Download Video (Download + Decrypt + Decompress)");
                Console.WriteLine("3. List Files on Google Drive");
                Console.WriteLine("4. Delete File from Google Drive");
                Console.WriteLine("5. Exit");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await UploadVideoAsync(pipeline);
                        break;
                    case "2":
                        await DownloadVideoAsync(pipeline);
                        break;
                    case "3":
                        await ListFilesAsync(driveService);
                        break;
                    case "4":
                        await DeleteFileAsync(driveService);
                        break;
                    case "5":
                        Console.WriteLine("\nExiting... Goodbye!");
                        return;
                    default:
                        Console.WriteLine("\n❌ Invalid option. Please try again.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"\nStack trace: {ex.StackTrace}");
        }
    }

    static async Task UploadVideoAsync(VideoProcessingPipeline pipeline)
    {
        Console.Write("\nEnter the path to your video file: ");
        var videoPath = Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrEmpty(videoPath))
        {
            Console.WriteLine("❌ No file path provided.");
            return;
        }

        if (!File.Exists(videoPath))
        {
            Console.WriteLine($"❌ File not found: {videoPath}");
            return;
        }

        var (fileId, keyPath) = await pipeline.UploadVideoAsync(videoPath);

        Console.WriteLine("\n✅ Upload successful!");
        Console.WriteLine($"   Google Drive File ID: {fileId}");
        Console.WriteLine($"   Encryption Key Location: {keyPath}");
        Console.WriteLine("\n   Save these details to download the video later.");
    }

    static async Task DownloadVideoAsync(VideoProcessingPipeline pipeline)
    {
        Console.Write("\nEnter the Google Drive File ID: ");
        
        var fileId = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(fileId))
        {
            Console.WriteLine("❌ No file ID provided.");
            return;
        }

        Console.Write("Enter the path to the encryption key file (.key): ");
        var keyPath = Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrEmpty(keyPath))
        {
            Console.WriteLine("❌ No key path provided.");
            return;
        }

        if (!File.Exists(keyPath))
        {
            Console.WriteLine($"❌ Key file not found: {keyPath}");
            return;
        }

        Console.Write("Enter the output directory (press Enter for default): ");
        var outputDir = Console.ReadLine()?.Trim().Trim('"');

        var videoPath = await pipeline.DownloadAndDecryptVideoAsync(
            fileId, 
            keyPath, 
            string.IsNullOrEmpty(outputDir) ? null : outputDir
        );

        Console.WriteLine($"\n✅ Download and decryption successful!");
        Console.WriteLine($"   Video saved at: {videoPath}");
    }

    static async Task ListFilesAsync(GoogleDriveService driveService)
    {
        Console.WriteLine("\n=== Files on Google Drive ===");
        var files = await driveService.ListFilesAsync(20);

        if (files.Count == 0)
        {
            Console.WriteLine("No files found.");
            return;
        }

        Console.WriteLine($"\nFound {files.Count} file(s):\n");
        foreach (var file in files)
        {
            Console.WriteLine($"Name: {file.Name}");
            Console.WriteLine($"  ID: {file.Id}");
            Console.WriteLine($"  Size: {FormatBytes(file.Size ?? 0)}");
            Console.WriteLine($"  Created: {file.CreatedTime?.ToLocalTime()}");
            Console.WriteLine();
        }
    }

    static async Task DeleteFileAsync(GoogleDriveService driveService)
    {
        Console.Write("\nEnter the Google Drive File ID to delete: ");
        var fileId = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(fileId))
        {
            Console.WriteLine("❌ No file ID provided.");
            return;
        }

        Console.Write($"Are you sure you want to delete file {fileId}? (yes/no): ");
        var confirmation = Console.ReadLine()?.Trim().ToLower();

        if (confirmation == "yes" || confirmation == "y")
        {
            var success = await driveService.DeleteFileAsync(fileId);
            if (success)
            {
                Console.WriteLine("✅ File deleted successfully!");
            }
        }
        else
        {
            Console.WriteLine("❌ Deletion cancelled.");
        }
    }

    static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F2} {suffixes[suffixIndex]}";
    }
}

