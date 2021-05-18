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

    }
}
