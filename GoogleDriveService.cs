using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;

namespace VideoUploader;

public class GoogleDriveService
{
    private readonly DriveService _driveService;
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile };

    public GoogleDriveService(string credentialsPath)
    {
        UserCredential credential;

        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            var credPath = "token.json";
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;

            Console.WriteLine($"Credential file saved to: {credPath}");
        }

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Video Uploader POC"
        });

        Console.WriteLine("Google Drive service initialized successfully.");
    }

    public async Task<string> UploadFileAsync(string filePath, string? fileName = null, IProgress<IUploadProgress>? progress = null)
    {
        fileName ??= Path.GetFileName(filePath);

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            MimeType = "application/octet-stream"
        };

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        var request = _driveService.Files.Create(fileMetadata, stream, fileMetadata.MimeType);
        request.Fields = "id, name, size";

        if (progress != null)
        {
            request.ProgressChanged += progress.Report;
        }

        Console.WriteLine($"Uploading {fileName} to Google Drive...");
        var file = await request.UploadAsync();

        if (file.Status == UploadStatus.Failed)
        {
            throw new Exception($"Upload failed: {file.Exception.Message}");
        }

        Console.WriteLine($"Upload completed successfully!");
        Console.WriteLine($"  File ID: {request.ResponseBody.Id}");
        Console.WriteLine($"  File name: {request.ResponseBody.Name}");
        Console.WriteLine($"  File size: {FormatBytes(request.ResponseBody.Size ?? 0)}");

        return request.ResponseBody.Id;
    }

    public async Task DownloadFileAsync(string fileId, string destinationPath, IProgress<long>? progress = null)
    {
        var request = _driveService.Files.Get(fileId);

        // Get file metadata first
        var fileMetadata = await request.ExecuteAsync();
        Console.WriteLine($"Downloading {fileMetadata.Name} from Google Drive...");

        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);

        request.MediaDownloader.ProgressChanged += downloadProgress =>
        {
            if (downloadProgress.Status == Google.Apis.Download.DownloadStatus.Downloading)
            {
                progress?.Report(downloadProgress.BytesDownloaded);
            }
            else if (downloadProgress.Status == Google.Apis.Download.DownloadStatus.Completed)
            {
                Console.WriteLine("Download completed successfully!");
            }
            else if (downloadProgress.Status == Google.Apis.Download.DownloadStatus.Failed)
            {
                Console.WriteLine($"Download failed: {downloadProgress.Exception?.Message}");
            }
        };

        await request.DownloadAsync(fileStream);
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            await _driveService.Files.Delete(fileId).ExecuteAsync();
            Console.WriteLine($"File {fileId} deleted successfully from Google Drive.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete file: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFilesAsync(int maxResults = 10)
    {
        var request = _driveService.Files.List();
        request.PageSize = maxResults;
        request.Fields = "files(id, name, size, createdTime, mimeType)";

        var result = await request.ExecuteAsync();
        return result.Files.ToList();
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
