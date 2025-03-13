using System.Security.Claims;

namespace PostApiService.Helper
{
    internal static class ClaimHelper
    {
        public static string SerializePermissions(params int[] permissions)
        {
            return permissions.Serialize();
        }

        public static List<int> DeserializePermissions(this Claim claim)
        {
            return claim.Value.Deserialize<List<int>>();
        }
    }
}
