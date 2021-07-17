using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace AspNet.Security.OAuth.Minecraft
{
    /// <summary>
    /// Defines a set of options used by <see cref="MinecraftAuthenticationHandler"/>.
    /// </summary>
    public class MinecraftAuthenticationOptions : OAuthOptions
    {
        public MinecraftAuthenticationOptions()
        {
            ClaimsIssuer = MinecraftAuthenticationDefaults.Issuer;
            CallbackPath = MinecraftAuthenticationDefaults.CallbackPath;

            AuthorizationEndpoint = MinecraftAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = MinecraftAuthenticationDefaults.TokenEndpoint;
            UserInformationEndpoint = MinecraftAuthenticationDefaults.UserInformationEndpoint;

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            ClaimActions.MapJsonSubKey(ClaimTypes.Email, "attributes", "email");
            ClaimActions.MapJsonSubKey(ClaimTypes.GivenName, "attributes", "first_name");
            ClaimActions.MapJsonSubKey(ClaimTypes.Name, "attributes", "full_name");
            ClaimActions.MapJsonSubKey(ClaimTypes.Surname, "attributes", "last_name");
            ClaimActions.MapJsonSubKey(ClaimTypes.Webpage, "attributes", "url");
            //ClaimActions.MapJsonSubKey(Claims.Avatar, "attributes", "thumb_url");
        }

        /// <summary>
        /// Gets the list of fields to retrieve from the user information endpoint.
        /// </summary>
        public ISet<string> Fields { get; } = new HashSet<string>
        {
            "first_name",
            "full_name",
            "last_name",
            "thumb_url",
            "url",
        };

        /// <summary>
        /// Gets the list of related data to include from the user information endpoint.
        /// </summary>
        public ISet<string> Includes { get; } = new HashSet<string>();
    }
}
