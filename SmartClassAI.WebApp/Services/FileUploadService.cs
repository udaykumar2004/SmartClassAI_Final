// ============================================================
// FileUploadService.cs — NEW SERVICE
//
// Centralizes all file upload logic:
//  - Path traversal protection (was a bug in AssignmentSubmissionController.Download())
//  - Extension validation
//  - Size validation
//  - Old file cleanup
//  - Unique GUID naming
// ============================================================

using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IWebHostEnvironment env,
            ILogger<FileUploadService> logger)
        {
            _env = env;
            _logger = logger;
        }

        // =============================================
        // UPLOAD A FILE TO A SUBFOLDER OF wwwroot
        // Returns the relative URL path ("/uploads/xyz.pdf")
        // Returns null if validation fails (check errorMessage)
        // =============================================
        public async Task<(string? RelativeUrl, string? ErrorMessage)> UploadAsync(
            IFormFile file,
            string folderName,
            string[] allowedExtensions,
            long maxSizeBytes = 10 * 1024 * 1024)
        {
            if (file == null || file.Length == 0)
                return (null, "No file selected.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return (null, $"File type '{extension}' is not allowed.");

            if (file.Length > maxSizeBytes)
                return (null, $"File size exceeds {maxSizeBytes / (1024 * 1024)} MB limit.");

            // Sanitize folder name — no path traversal
            folderName = SanitizeFolderName(folderName);

            string uploadDir = Path.Combine(_env.WebRootPath, folderName);

            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString() + extension;
            string fullPath = Path.Combine(uploadDir, fileName);

            // Verify the resolved path is inside wwwroot (path traversal protection)
            string wwwRoot = Path.GetFullPath(_env.WebRootPath);
            string resolved = Path.GetFullPath(fullPath);

            if (!resolved.StartsWith(wwwRoot, StringComparison.OrdinalIgnoreCase))
                return (null, "Invalid upload path.");

            try
            {
                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed: {FileName}", fileName);
                return (null, "Upload failed. Please try again.");
            }

            return ($"/{folderName}/{fileName}", null);
        }

        // =============================================
        // DELETE A FILE (safe — validates path is inside wwwroot)
        // =============================================
        public void DeleteIfExists(string? relativeUrl) => DeleteFile(relativeUrl);

        public void DeleteFile(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;

            string wwwRoot = Path.GetFullPath(_env.WebRootPath);
            string fullPath = Path.GetFullPath(
                Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/')));

            // Path traversal protection
            if (!fullPath.StartsWith(wwwRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempted path traversal on delete: {Path}", relativeUrl);
                return;
            }

            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file: {Path}", fullPath);
                }
            }
        }

        // =============================================
        // SAFE FILE DOWNLOAD (validates path)
        // FIX: AssignmentSubmissionController.Download() had no path traversal protection
        // =============================================
        public (string FullPath, bool IsValid) GetSafeDownloadPath(string relativeUrl)
        {
            string wwwRoot = Path.GetFullPath(_env.WebRootPath);
            string fullPath = Path.GetFullPath(
                Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/')));

            bool isValid = fullPath.StartsWith(wwwRoot, StringComparison.OrdinalIgnoreCase)
                        && File.Exists(fullPath);

            return (fullPath, isValid);
        }

        public (Stream? Stream, string ContentType, string FileName) GetDownloadStream(string relativeUrl)
        {
            var (fullPath, isValid) = GetSafeDownloadPath(relativeUrl);
            if (!isValid)
                return (null, "application/octet-stream", string.Empty);

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = Path.GetFileName(fullPath);
            var contentType = GetContentType(Path.GetExtension(fileName));
            return (stream, contentType, fileName);
        }

        private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };

        private static string SanitizeFolderName(string folder)
        {
            // Only allow alphanumeric and hyphens
            return new string(folder
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .ToArray());
        }
    }
}