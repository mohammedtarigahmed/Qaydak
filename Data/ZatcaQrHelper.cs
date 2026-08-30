using System.Text;

namespace Qaydak.Data
{
    public static class ZatcaQrHelper
    {
        public static string GenerateBase64Tlv(
            string sellerName,
            string vatNumber,
            DateTime timestamp,
            decimal invoiceTotal,
            decimal vatAmount)
        {
            using var stream = new MemoryStream();

            WriteTlvField(stream, 1, sellerName);
            WriteTlvField(stream, 2, vatNumber);
            WriteTlvField(stream, 3, timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            WriteTlvField(stream, 4, invoiceTotal.ToString("F2"));
            WriteTlvField(stream, 5, vatAmount.ToString("F2"));

            return Convert.ToBase64String(stream.ToArray());
        }

        private static void WriteTlvField(MemoryStream stream, byte tag, string value)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            stream.WriteByte(tag);
            stream.WriteByte((byte)valueBytes.Length);
            stream.Write(valueBytes, 0, valueBytes.Length);
        }
    }
}