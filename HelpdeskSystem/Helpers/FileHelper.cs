using System;
using System.IO;
using System.Web;

namespace HelpdeskSystem.Helpers
{
    public static class FileHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xlsx", ".xls", ".zip", ".rar", ".txt" };
        private static readonly long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public static (bool isValid, string errorMessage) ValidateFile(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return (false, "File is empty or null.");
            }

            if (file.ContentLength > MaxFileSize)
            {
                return (false, $"File size exceeds the maximum allowed size of {FormatFileSize(MaxFileSize)}.");
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (Array.IndexOf(AllowedExtensions, extension) < 0)
            {
                return (false, "File extension is not allowed.");
            }

            return (true, string.Empty);
        }

        public static (string fileName, string filePath, long fileSize) SaveFile(HttpPostedFileBase file, string subFolder)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new ArgumentException("File is empty or null.");
            }

            string originalExtension = Path.GetExtension(file.FileName);
            string newFileName = Guid.NewGuid().ToString("N") + originalExtension;

            string uploadDir = HttpContext.Current.Server.MapPath($"~/Uploads/{subFolder}/");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string fullPath = Path.Combine(uploadDir, newFileName);
            file.SaveAs(fullPath);

            string relativePath = $"/Uploads/{subFolder}/{newFileName}";
            return (newFileName, relativePath, file.ContentLength);
        }

        public static void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            string serverPath = HttpContext.Current.Server.MapPath(filePath);
            if (File.Exists(serverPath))
            {
                File.Delete(serverPath);
            }
        }

        public static string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".zip": return "application/zip";
                case ".rar": return "application/x-rar-compressed";
                case ".txt": return "text/plain";
                default: return "application/octet-stream";
            }
        }

        public static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
}
