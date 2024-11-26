namespace HashingAPI.Models
{
    public class HashRequest
    {
        public int Bits { get; set; } // Длина хэша (2, 4, 8 бита)
        public IFormFile File { get; set; } // Загружаемый файл
    }
}
