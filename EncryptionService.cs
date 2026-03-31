using System.Security.Cryptography;

namespace VideoUploader;

public class EncryptionService
{
    private const int KeySize = 256;
    private const int IvSize = 16;

    public static byte[] GenerateKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.GenerateKey();
        return aes.Key;
    }

    public static void EncryptFile(string inputFile, string outputFile, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

        // Write IV at the beginning of the file
        fsOutput.Write(aes.IV, 0, aes.IV.Length);

        using var encryptor = aes.CreateEncryptor();
        using var cryptoStream = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write);
        using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read);

        fsInput.CopyTo(cryptoStream);
    }

    public static void DecryptFile(string inputFile, string outputFile, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read);

        // Read IV from the beginning of the file
        var iv = new byte[IvSize];
        fsInput.Read(iv, 0, IvSize);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read);
        using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

        cryptoStream.CopyTo(fsOutput);
    }

    public static void SaveKey(byte[] key, string keyFilePath)
    {
        File.WriteAllBytes(keyFilePath, key);
        Console.WriteLine($"Encryption key saved to: {keyFilePath}");
        Console.WriteLine("IMPORTANT: Keep this key safe! You'll need it to decrypt your files.");
    }

    public static byte[] LoadKey(string keyFilePath)
    {
        if (!File.Exists(keyFilePath))
        {
            throw new FileNotFoundException($"Key file not found: {keyFilePath}");
        }
        return File.ReadAllBytes(keyFilePath);
    }
}
