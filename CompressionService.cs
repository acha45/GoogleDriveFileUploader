using System.IO.Compression;

namespace VideoUploader;

public class CompressionService
{
    public static string CompressFile(string inputFile, string? outputFile = null)
    {
        outputFile ??= inputFile + ".zip";

        using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
        using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        using var zipArchive = new ZipArchive(fsOutput, ZipArchiveMode.Create);

        var fileName = Path.GetFileName(inputFile);
        var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);

        using var entryStream = entry.Open();
        fsInput.CopyTo(entryStream);

        var originalSize = new FileInfo(inputFile).Length;
        var compressedSize = new FileInfo(outputFile).Length;
        var compressionRatio = (1 - (double)compressedSize / originalSize) * 100;

        Console.WriteLine($"Compression completed:");
        Console.WriteLine($"  Original size: {FormatBytes(originalSize)}");
        Console.WriteLine($"  Compressed size: {FormatBytes(compressedSize)}");
        Console.WriteLine($"  Compression ratio: {compressionRatio:F2}%");

        return outputFile;
    }

    public static string DecompressFile(string zipFile, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        using var zipArchive = ZipFile.OpenRead(zipFile);

        if (zipArchive.Entries.Count == 0)
        {
            throw new InvalidOperationException("ZIP file is empty");
        }

        var entry = zipArchive.Entries[0];
        var outputPath = Path.Combine(outputDirectory, entry.Name);

        entry.ExtractToFile(outputPath, overwrite: true);

        Console.WriteLine($"Decompressed to: {outputPath}");
        return outputPath;
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
