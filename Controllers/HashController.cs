using HashingAPI.Models;
using HashingAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            var originalHash = _hashService.ComputeHash(originalBytes, request.Bits);

            var modifiedBytes = new byte[originalBytes.Length];
            originalBytes.CopyTo(modifiedBytes, 0);

            // Простая модификация: изменяем последний байт
            modifiedBytes[^1] ^= 0xFF;

            var newHash = _hashService.ComputeHash(modifiedBytes, request.Bits);

            while (!_hashService.AreHashesEqual(originalHash, newHash))
            {
                modifiedBytes[^1] = (byte)((modifiedBytes[^1] + 1) % 256);
                newHash = _hashService.ComputeHash(modifiedBytes, request.Bits);
            }

            return Ok(new { ModifiedBytes = modifiedBytes, NewHash = Convert.ToBase64String(newHash) });
        }
    }

}
