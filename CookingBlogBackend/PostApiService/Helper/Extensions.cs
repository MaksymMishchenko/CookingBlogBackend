using Newtonsoft.Json;

namespace PostApiService.Helper
{
    internal static class Extensions
    {
        public static T To<T>(this object input)
        {
            if (input == null || input == DBNull.Value)
            {
                return default!;
            }
            return (T)Convert.ChangeType(input, typeof(T));
        }

        public static T Deserialize<T>(this string? input)
        {
            if (string.IsNullOrEmpty(input))
            {                
                return JsonConvert.DeserializeObject<T>(input ?? string.Empty) ?? Activator.CreateInstance<T>();
            }

            return JsonConvert.DeserializeObject<T>(input)!;
        }

        public static string Serialize(this object input)
        {
            if (input == null)
            {
                return "";
            }
            return JsonConvert.SerializeObject(input);
        }
    }
}
