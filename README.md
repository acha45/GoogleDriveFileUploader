# Video Uploader POC

A Proof of Concept application demonstrating secure video upload and download to Google Drive with encryption and compression.

## Features

? **AES-256 Encryption** - Videos are encrypted before upload and travel over the network in encrypted form  
? **ZIP Compression** - Videos are compressed to reduce size before encryption  
? **Secure Storage** - Videos stored on Google Drive remain encrypted  
? **Automatic Decryption** - Downloaded videos are automatically decrypted and decompressed  
? **Key Management** - Encryption keys are generated and saved locally for security  

## Security Flow

```
Upload: Video ? Compress (ZIP) ? Encrypt (AES-256) ? Upload to Google Drive
Download: Download from Google Drive ? Decrypt (AES-256) ? Decompress ? Original Video
```

## Prerequisites

1. **.NET 8 SDK** - Already configured in your project
2. **Google Cloud Project** with Drive API enabled
3. **Service Account credentials** from Google Cloud Console

## Setup Instructions

### 1. Create Google Drive API Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the **Google Drive API**:
   - Navigate to "APIs & Services" ? "Library"
   - Search for "Google Drive API"
   - Click "Enable"
4. Create credentials:
   - Go to "APIs & Services" ? "Credentials"
   - Click "Create Credentials" ? "Service Account"
   - Fill in the service account details
   - Click "Create and Continue"
   - Skip the optional steps
5. Download the JSON key:
   - Click on the created service account
   - Go to "Keys" tab
   - Click "Add Key" ? "Create new key"
   - Choose "JSON" format
   - Download the file
6. Rename the downloaded file to `credentials.json`
7. Place `credentials.json` in the project root directory (same folder as VideoUploader.csproj)

### 2. Restore NuGet Packages

```bash
dotnet restore
```

### 3. Build the Project

```bash
dotnet build
```

### 4. Run the Application

```bash
dotnet run
```

## Usage

### Upload a Video

1. Select option `1` from the menu
2. Enter the full path to your video file (e.g., `C:\Videos\sample.mp4`)
3. The application will:
   - Compress the video (ZIP)
   - Encrypt the compressed file (AES-256)
   - Upload to Google Drive
   - Save the encryption key locally
4. **Save the File ID and Key Path** - you'll need them to download

### Download a Video

1. Select option `2` from the menu
2. Enter the Google Drive File ID (from upload step)
3. Enter the path to the encryption key file (.key)
4. Optionally specify an output directory
5. The application will:
   - Download the encrypted file from Google Drive
   - Decrypt the file using your key
   - Decompress the video
   - Save to the output directory

### List Files

Select option `3` to view all files in your Google Drive (created by this app)

### Delete Files

Select option `4` to remove files from Google Drive

## Project Structure

```
VideoUploader/
??? Program.cs                      # Main entry point with interactive menu
??? EncryptionService.cs            # AES-256 encryption/decryption
??? CompressionService.cs           # ZIP compression/decompression
??? GoogleDriveService.cs           # Google Drive API integration
??? VideoProcessingPipeline.cs      # Complete upload/download workflow
??? credentials.json                # Google API credentials (add this)
??? VideoUploader.csproj            # Project configuration
```

## Security Notes

?? **Important Security Considerations:**

- **Encryption keys are stored locally** - Keep them safe! Without the key, encrypted videos cannot be decrypted
- Videos are encrypted with **AES-256** which is industry-standard encryption
- Files travel over the network in **encrypted form only**
- Videos stored on Google Drive remain **encrypted at rest**
- Each video gets a **unique encryption key**
- Consider implementing a secure key management system for production use

## Technical Details

### Encryption
- Algorithm: AES-256 (Advanced Encryption Standard)
- Key Size: 256 bits
- IV: Randomly generated for each file (stored with encrypted file)
- Mode: CBC (Cipher Block Chaining)

### Compression
- Format: ZIP
- Compression Level: Optimal
- Reduces file size significantly depending on video codec

### Google Drive Integration
- Uses Google Drive API v3
- Service Account authentication
- Chunked upload/download for large files
- Progress reporting

## Example Output

```
=== Starting Video Upload Pipeline ===
Source video: C:\Videos\sample.mp4

[Step 1/4] Compressing video...
Compression completed:
  Original size: 50.25 MB
  Compressed size: 48.13 MB
  Compression ratio: 4.22%

[Step 2/4] Generating encryption key...
Encryption key saved to: C:\Temp\VideoUploader\sample_key_20240115_143022.key

[Step 3/4] Encrypting compressed video...
Encrypted file created: C:\Temp\VideoUploader\sample_encrypted_20240115_143022.enc

[Step 4/4] Uploading to Google Drive...
Upload completed successfully!
  File ID: 1abc123def456ghi789jkl
  File name: sample_encrypted_20240115_143022.enc
  File size: 48.14 MB

=== Upload Pipeline Completed ===
```

## Troubleshooting

**"credentials.json not found"**
- Ensure you've downloaded the Service Account JSON key from Google Cloud Console
- Place it in the project root directory
- Rename it to exactly `credentials.json`

**"Access denied" errors**
- Verify the Google Drive API is enabled in your project
- Check that the service account has proper permissions

**Large file uploads failing**
- Google Drive API has file size limits for free accounts
- Consider implementing resumable uploads for very large files

## Future Enhancements

- [ ] Add support for multiple video files (batch processing)
- [ ] Implement secure key storage (e.g., Azure Key Vault)
- [ ] Add progress bars for compression/encryption
- [ ] Support for different encryption algorithms
- [ ] Resumable uploads for large files
- [ ] Video metadata preservation
- [ ] File integrity verification (checksums)

## License

This is a Proof of Concept for educational purposes.
