using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TanuiApp.Services
{
    public interface IImageStorageService
    {
        Task<string> SaveProductImageAsync(IFormFile? imageFile);
    }

    public class ImageStorageService : IImageStorageService
    {
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
        private readonly IWebHostEnvironment _env;

        public ImageStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveProductImageAsync(IFormFile? imageFile)
        {
            // Default placeholder when nothing uploaded
            var defaultUrl = "/images/products/default.svg";
            if (imageFile == null || imageFile.Length == 0) return defaultUrl;

            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) return defaultUrl;

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/products/{fileName}";
        }
    }
}


