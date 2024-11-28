using DocumentFormat.OpenXml.Packaging;
using HashingAPI.Models;
using HashingAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HashingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HashController : ControllerBase
    {
        private readonly CustomHashService _hashService;

        public HashController()
        {
            _hashService = new CustomHashService();
        }

        [HttpPost("digest")]
        public IActionResult CalculateDigest([FromForm] HashRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is required.");

            using var memoryStream = new MemoryStream();
            request.File.CopyTo(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var hash = _hashService.ComputeHash(fileBytes, request.Bits);
            var a = Convert.ToBase64String(hash);
            return Ok(new { Hash = Convert.ToBase64String(hash) });
        }

        [HttpPost("collision")]
        public IActionResult GenerateCollision([FromForm] HashRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is required.");

            using var memoryStream = new MemoryStream();
            request.File.CopyTo(memoryStream);
            var originalBytes = memoryStream.ToArray();

            // Обчислюємо хеш оригінального файлу
            var originalHash = _hashService.ComputeHash(originalBytes, request.Bits);

            // Створюємо копію файлу та змінюємо її метаінформацію
            byte[] modifiedBytes;
            string fileType = request.File.ContentType.ToLower();

            if (fileType.Contains("word") || fileType.Contains("msword") || fileType.Contains("officedocument"))
            {
                modifiedBytes = ModifyWordFile(originalBytes, originalHash, request.Bits);
            }
            else if (fileType.Contains("image"))
            {
                modifiedBytes = ModifyImageBytes(originalBytes, originalHash, request.Bits);
            }
            else
            {
                modifiedBytes = ModifyTextFile(originalBytes, originalHash, request.Bits);
            }

            var newHash = _hashService.ComputeHash(modifiedBytes, request.Bits);
            return File(modifiedBytes, request.File.ContentType, "collision_" + request.File.FileName);
        }

        private byte[] ModifyWordFile(byte[] fileBytes, byte[] originalHash, int bits)
        {
            using var memoryStream = new MemoryStream(fileBytes);
            using var wordDocument = WordprocessingDocument.Open(memoryStream, true);

            var coreProps = wordDocument.PackageProperties;

            // Початкове редагування метаданих
            coreProps.Creator = "Modified Author";
            coreProps.Description = "Modified Content";
            coreProps.Keywords = "collision";
            coreProps.Modified = DateTime.Now;

            wordDocument.Save();
            var modifiedBytes = memoryStream.ToArray();

            // Циклічне редагування для досягнення колізії
            while (!_hashService.AreHashesEqual(originalHash, _hashService.ComputeHash(modifiedBytes, bits)))
            {
                coreProps.Keywords += " "; // Додаємо пробіл до ключових слів
                wordDocument.Save();
                modifiedBytes = memoryStream.ToArray();
            }

            return modifiedBytes;
        }

        private byte[] ModifyImageBytes(byte[] fileBytes, byte[] originalHash, int bits)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileBytes);

            // Змінюємо пікселі, щоб досягти колізії
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];

                    // Міняємо значення синього каналу (або іншого каналу) в межах ±1
                    if (pixel.B < 255)
                        pixel.B += 1;
                    else
                        pixel.B -= 1;

                    image[x, y] = pixel;

                    // Перевіряємо, чи досягли ми колізії
                    using var memoryStream = new MemoryStream();
                    image.SaveAsPng(memoryStream); // Зберігаємо тимчасовий файл
                    var modifiedBytes = memoryStream.ToArray();

                    if (_hashService.AreHashesEqual(originalHash, _hashService.ComputeHash(modifiedBytes, bits)))
                    {
                        return modifiedBytes; // Колізія досягнута
                    }
                }
            }

            throw new InvalidOperationException("Unable to generate collision for the image.");
        }

        private byte[] ModifyTextFile(byte[] fileBytes, byte[] originalHash, int bits)
        {
            var content = System.Text.Encoding.UTF8.GetString(fileBytes);

            // Початкове додавання коментарів
            content += "\n// Test";

            var modifiedBytes = System.Text.Encoding.UTF8.GetBytes(content);

            // Циклічне редагування для досягнення колізії
            while (!_hashService.AreHashesEqual(originalHash, _hashService.ComputeHash(modifiedBytes, bits)))
            {
                content += " ";
                modifiedBytes = System.Text.Encoding.UTF8.GetBytes(content);
            }

            return modifiedBytes;
        }
    }

}
