using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetResponseMediaType(this HttpRequestMessage request)
        {
            IEnumerable<MediaTypeWithQualityHeaderValue> mediaTypes = request.Headers.Accept
                .Where(p => p.MediaType.Contains("/xml") || p.MediaType.Contains("/json"))
                .OrderByDescending(p => p.Quality ?? 1);
            if (mediaTypes.Count() > 0)
            {
                MediaTypeWithQualityHeaderValue mediaType = mediaTypes.First();
                if (mediaType.MediaType.Contains("/xml")) return "application/xml";
            }
            return "application/json";
        }

        private static EncodingInfo[] EncodingInfos = Encoding.GetEncodings();

        public static Encoding GetResponseEncoding(this HttpRequestMessage request)
        {
            IEnumerable<StringWithQualityHeaderValue> charsets = request.Headers.AcceptCharset.OrderByDescending(p => p.Quality ?? 1);
            foreach (StringWithQualityHeaderValue charset in charsets)
            {
                EncodingInfo info = EncodingInfos.FirstOrDefault(p => p.Name == charset.Value);
                if (info != null) return info.GetEncoding();
            }
            return Encoding.UTF8;
        }


    }
}
