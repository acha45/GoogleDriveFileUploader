namespace VideoUploader;

public class VideoProcessingPipeline
{
    private readonly GoogleDriveService _driveService;
    private readonly string _workingDirectory;

    public VideoProcessingPipeline(GoogleDriveService driveService, string? workingDirectory = null)
    {
        _driveService = driveService;
        _workingDirectory = workingDirectory ?? Path.Combine(Path.GetTempPath(), "VideoUploader");
        Directory.CreateDirectory(_workingDirectory);
    }

    public async Task<(string FileId, string KeyPath)> UploadVideoAsync(string videoPath)
    {
        Console.WriteLine("\n=== Starting Video Upload Pipeline ===");
        Console.WriteLine($"Source video: {videoPath}\n");

        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException($"Video file not found: {videoPath}");
        }

        var videoFileName = Path.GetFileNameWithoutExtension(videoPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Step 1: Compress the video
        Console.WriteLine("[Step 1/4] Compressing video...");
        var compressedPath = Path.Combine(_workingDirectory, $"{videoFileName}_compressed_{timestamp}.zip");
        compressedPath = CompressionService.CompressFile(videoPath, compressedPath);

        // Step 2: Generate encryption key
        Console.WriteLine("\n[Step 2/4] Generating encryption key...");
        var encryptionKey = EncryptionService.GenerateKey();
        var keyPath = Path.Combine(_workingDirectory, $"{videoFileName}_key_{timestamp}.key");
        EncryptionService.SaveKey(encryptionKey, keyPath);

        // Step 3: Encrypt the compressed file
        Console.WriteLine("\n[Step 3/4] Encrypting compressed video...");
        var encryptedPath = Path.Combine(_workingDirectory, $"{videoFileName}_encrypted_{timestamp}.enc");
        EncryptionService.EncryptFile(compressedPath, encryptedPath, encryptionKey);
        Console.WriteLine($"Encrypted file created: {encryptedPath}");

        // Step 4: Upload to Google Drive
        Console.WriteLine("\n[Step 4/4] Uploading to Google Drive...");
        var uploadProgress = new Progress<Google.Apis.Upload.IUploadProgress>(progress =>
        {
            Console.WriteLine($"  Upload progress: {progress.BytesSent} bytes sent - Status: {progress.Status}");
        });

        var fileId = await _driveService.UploadFileAsync(
            encryptedPath, 
            $"{videoFileName}_encrypted_{timestamp}.enc",
            uploadProgress
        );

        // Cleanup temporary files
        Console.WriteLine("\nCleaning up temporary files...");
        File.Delete(compressedPath);
        File.Delete(encryptedPath);

        Console.WriteLine("\n=== Upload Pipeline Completed ===");
        Console.WriteLine($"File ID: {fileId}");
        Console.WriteLine($"Encryption Key saved at: {keyPath}");
        Console.WriteLine("\n??  IMPORTANT: Save the encryption key file securely! You'll need it to decrypt the video.\n");

        return (fileId, keyPath);
    }

    public async Task<string> DownloadAndDecryptVideoAsync(string fileId, string keyPath, string? outputDirectory = null)
    {
        Console.WriteLine("\n=== Starting Video Download & Decryption Pipeline ===");
        Console.WriteLine($"File ID: {fileId}");
        Console.WriteLine($"Key file: {keyPath}\n");

        outputDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DownloadedVideos");
        Directory.CreateDirectory(outputDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Step 1: Download encrypted file from Google Drive
        Console.WriteLine("[Step 1/3] Downloading encrypted file from Google Drive...");
        var downloadedPath = Path.Combine(_workingDirectory, $"downloaded_{timestamp}.enc");

        var downloadProgress = new Progress<long>(bytesDownloaded =>
        {
            Console.WriteLine($"  Downloaded: {FormatBytes(bytesDownloaded)}");
        });

        await _driveService.DownloadFileAsync(fileId, downloadedPath, downloadProgress);

        // Step 2: Load encryption key and decrypt
        Console.WriteLine("\n[Step 2/3] Decrypting file...");
        var encryptionKey = EncryptionService.LoadKey(keyPath);
        var decryptedPath = Path.Combine(_workingDirectory, $"decrypted_{timestamp}.zip");
        EncryptionService.DecryptFile(downloadedPath, decryptedPath, encryptionKey);
        Console.WriteLine($"Decrypted file created: {decryptedPath}");

        // Step 3: Decompress the video
        Console.WriteLine("\n[Step 3/3] Decompressing video...");
        var finalVideoPath = CompressionService.DecompressFile(decryptedPath, outputDirectory);

        // Cleanup temporary files
        Console.WriteLine("\nCleaning up temporary files...");
        File.Delete(downloadedPath);
        File.Delete(decryptedPath);

        Console.WriteLine("\n=== Download & Decryption Pipeline Completed ===");
        Console.WriteLine($"Video saved at: {finalVideoPath}\n");

        return finalVideoPath;
    }

    private static string FormatBytes(long bytes)
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
