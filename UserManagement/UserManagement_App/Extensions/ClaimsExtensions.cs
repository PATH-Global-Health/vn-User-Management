using System.Linq;
using System.Security.Claims;

namespace UserManagement_App.Extensions
{
    public static class ClaimsExtensions
    {
        public static string GetId(this ClaimsPrincipal user)
        {
            var idClaim = user.Claims.FirstOrDefault(i => i.Type.Equals("Id"));
            if (idClaim != null)
            {
                return idClaim.Value;
            }
            return "";
        }

        public static string GetCredential(this ClaimsPrincipal user)
        {
            var credentialClaim = user.Claims.FirstOrDefault(i => i.Type.Equals("Credential"));
            if (credentialClaim != null)
            {
                return credentialClaim.Value;
            }
            return "";
        }

    }
}
