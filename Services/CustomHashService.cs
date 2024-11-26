using System.Text;

namespace HashingAPI.Services
{

    public class CustomHashService
    {
        public byte[] ComputeHash(byte[] data, int bits)
        {
            if (bits != 2 && bits != 4 && bits != 8)
            {
                throw new ArgumentException("Bits must be 2, 4, or 8.", nameof(bits));
            }

            byte result = 0;

            // XOR each byte with a shift to ensure bit-level non-linearity
            foreach (var b in data)
            {
                result ^= (byte)((b << 3) | (b >> 5));
            }

            // Reduce to desired bit length
            int mask = (1 << bits) - 1;
            result = (byte)(result & mask);

            return new[] { result };
        }


        public bool AreHashesEqual(byte[] hash1, byte[] hash2)
        {
            return hash1.SequenceEqual(hash2);
        }
    }

}
