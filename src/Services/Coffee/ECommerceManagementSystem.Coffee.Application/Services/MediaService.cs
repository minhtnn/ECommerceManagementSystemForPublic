using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Google.Protobuf;
using System.Text.RegularExpressions;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using Microsoft.Extensions.Options;

namespace ECommerceManagementSystem.Coffee.Application.Services;

public class MediaService : IMediaService
{
    private readonly FirebaseStorageSetting _firebaseStorageSetting;
    private readonly StorageClient _storageClient;
    private readonly UrlSigner _urlSigner;
    private readonly ILogger _logger;

    public MediaService(IOptions<FirebaseStorageSetting> firebaseStorageSettings, ILogger logger)
    {
        _logger = logger;
        _firebaseStorageSetting = firebaseStorageSettings.Value;
        var json = $@"{{
                        ""type"": ""{_firebaseStorageSetting.Type}"",
                        ""project_id"": ""{_firebaseStorageSetting.ProjectId}"",
                        ""private_key_id"": ""{_firebaseStorageSetting.PrivateKeyId}"",
                        ""private_key"": ""{_firebaseStorageSetting.PrivateKey}"",
                        ""client_email"": ""{_firebaseStorageSetting.ClientEmail}"",
                        ""client_id"": ""{_firebaseStorageSetting.ClientId}"",
                        ""auth_uri"": ""{_firebaseStorageSetting.AuthUri}"",
                        ""token_uri"": ""{_firebaseStorageSetting.TokenUri}"",
                        ""auth_provider_x509_cert_url"": ""{_firebaseStorageSetting.AuthProviderX509CertUrl}"",
                        ""client_x509_cert_url"": ""{_firebaseStorageSetting.ClientX509CertUrl}"",
                        ""universe_domain"": ""{_firebaseStorageSetting.UniverseDomain}""
                    }}";
        var credential = GoogleCredential.FromJson(json);
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = _firebaseStorageSetting.ProjectId
            });
        }

        _storageClient = StorageClient.Create(credential);
        _urlSigner = UrlSigner.FromCredential(credential);
    }

    public async Task<string> UploadImageAsync(ByteString byteString)
    {
        var fileName = $"{Guid.NewGuid()}.jpg";
        try
        {
            using var stream = new MemoryStream(byteString.ToByteArray());
            await _storageClient.UploadObjectAsync(
                bucket: _firebaseStorageSetting.BucketName,
                objectName: fileName,
                contentType: "image/jpeg",
                source: stream
            );

            // Trả về fileName để lưu vào database
            return fileName;
        }
        catch (Google.GoogleApiException ex)
        {
            throw new InvalidOperationException("Failed to upload image", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to upload image", ex);
        }
    }

    public async Task<string> UploadExcelAsync(ByteString byteString)
    {
        var fileName = $"{Guid.NewGuid()}.xlsx";
        try
        {
            using var stream = new MemoryStream(byteString.ToByteArray());
            await _storageClient.UploadObjectAsync(
                bucket: _firebaseStorageSetting.BucketName,
                objectName: fileName,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                source: stream
            );

            // Trả về fileName để lưu vào database
            return fileName;
        }
        catch (Google.GoogleApiException ex)
        {
            throw new InvalidOperationException("Failed to upload excel", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to upload excel", ex);
        }
    }

    public async Task<string> GetImageUrlAsync(string fileName, TimeSpan? expiration)
    {
        var exp = expiration ?? TimeSpan.FromHours(1);
        return await GenerateSignedUrlAsync(fileName, exp);
    }

    public async Task<byte[]> DownloadFileAsync(string fileName)
    {
        try
        {
            using var stream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(
                bucket: _firebaseStorageSetting.BucketName,
                objectName: fileName,
                destination: stream
            );

            return stream.ToArray();
        }
        catch (Google.GoogleApiException ex)
        {
            throw new InvalidOperationException("Failed to download file", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to download file", ex);
        }
    }

    public async Task<string> GetImagePermanentUrlAsync(string path)
    {
        try
        {
            var storageObject = await _storageClient.GetObjectAsync(
                bucket: _firebaseStorageSetting.BucketName,
                objectName: path
            );

            // Kiểm tra token có sẵn chưa
            var token = storageObject.Metadata != null &&
                        storageObject.Metadata.TryGetValue("firebaseStorageDownloadTokens", out var t) &&
                        !string.IsNullOrWhiteSpace(t)
                ? t
                : null;

            // Nếu chưa có token → tạo mới và patch vào metadata
            if (token == null)
            {
                token = Guid.NewGuid().ToString();

                storageObject.Metadata ??= new Dictionary<string, string>();
                storageObject.Metadata["firebaseStorageDownloadTokens"] = token;

                await _storageClient.UpdateObjectAsync(storageObject);

                _logger.Information("[MediaService] Patched download token for {Path}", path);
            }

            var encodedPath = string.Join("%2F",
                path.Split('/').Select(Uri.EscapeDataString));

            return
                $"https://firebasestorage.googleapis.com/v0/b/{_firebaseStorageSetting.BucketName}/o/{encodedPath}?alt=media&token={token}";
        }
        catch (Google.GoogleApiException ex)
        {
            _logger.Error("[MediaService] Failed to get permanent URL for {Path}: {Error}", path, ex.Message);
            throw new InvalidOperationException($"Failed to get permanent URL for {path}", ex);
        }
    }

    private async Task<string> GenerateSignedUrlAsync(string fileName, TimeSpan expiration)
    {
        var signedUrl = _urlSigner.Sign(
            _firebaseStorageSetting.BucketName,
            fileName,
            expiration,
            HttpMethod.Get
        );

        return signedUrl;
    }

    /// <summary>
    /// Upload ảnh từ IFormFile lên Firebase Storage
    /// </summary>
    public async Task<MediaUploadResult> UploadImageFromFormAsync(
        IFormFile file,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return MediaUploadResult.Failure("File rỗng hoặc không hợp lệ");
            }

            // Validate extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return MediaUploadResult.Failure(
                    $"Định dạng file không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}"
                );
            }

            // Validate size (5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return MediaUploadResult.Failure("Kích thước file vượt quá 5MB");
            }

            // Tạo tên file unique
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Tạo full path với folder
            // VD: "products/abc-123.jpg"
            var fullPath = string.IsNullOrWhiteSpace(folderPath.ToLowerInvariant())
                ? uniqueFileName
                : $"{folderPath.ToLowerInvariant().TrimEnd('/')}/{uniqueFileName}";

            // Xác định content type
            var contentType = GetContentType(extension);

            // Upload lên Firebase Storage
            using var stream = file.OpenReadStream();
            await _storageClient.UploadObjectAsync(
                bucket: _firebaseStorageSetting.BucketName,
                objectName: fullPath,
                contentType: contentType,
                source: stream,
                cancellationToken: cancellationToken
            );

            return MediaUploadResult.Success(fullPath);
        }
        catch (Google.GoogleApiException ex)
        {
            return MediaUploadResult.Failure($"Lỗi Firebase API: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return MediaUploadResult.Failure($"Lỗi upload ảnh: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Xóa file trên Firebase Storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            await _storageClient.DeleteObjectAsync(
                bucket: _firebaseStorageSetting.BucketName,
                objectName: fileName,
                cancellationToken: cancellationToken
            );

            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // File không tồn tại, coi như đã xóa
            return true;
        }
        catch (Google.GoogleApiException ex)
        {
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<string> ResolveContentImageUrlsAsync(
        string htmlContent,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(htmlContent)) return htmlContent;

        var pattern = @"src=""(posts/inline/[^""]+)""";
        var matches = Regex.Matches(htmlContent, pattern);
        if (matches.Count == 0) return htmlContent;

        var exp = expiration ?? TimeSpan.FromDays(7);
        var result = htmlContent;
        var cache = new Dictionary<string, string>();

        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value;
            if (cache.ContainsKey(path)) continue;
            try
            {
                cache[path] = await GenerateSignedUrlAsync(path, exp);
            }
            catch (Exception ex)
            {
                _logger.Error("[MediaService] Failed to sign {Path}: {Error}",
                    path, ex.Message);
            }
        }

        foreach (var (path, url) in cache)
            result = result.Replace($"src=\"{path}\"", $"src=\"{url}\"");

        return result;
    }

    public Task<string> NormalizeContentImageUrlsAsync(string? htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return Task.FromResult(htmlContent ?? string.Empty);

        var pattern =
            $@"src=""https://storage\.googleapis\.com/{Regex.Escape(_firebaseStorageSetting.BucketName)}/([^?""]+)[^""]*""";

        var result = Regex.Replace(htmlContent, pattern, match =>
            $"src=\"{match.Groups[1].Value}\""
        );

        return Task.FromResult(result);
    }

    /// <summary>
    /// Lấy content type dựa vào extension
    /// </summary>
    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}