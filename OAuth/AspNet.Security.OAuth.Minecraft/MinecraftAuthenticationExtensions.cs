using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AspNet.Security.OAuth.Minecraft
{
    /// <summary>
    /// Extension methods to add Patreon authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class MinecraftAuthenticationExtensions
    {
        /// <summary>
        /// Adds <see cref="MinecraftAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddMinecraft([NotNull] this AuthenticationBuilder builder)
        {
            return builder.AddMinecraft(MinecraftAuthenticationDefaults.AuthenticationScheme, options => { });
        }

        /// <summary>
        /// Adds <see cref="MinecraftAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="configuration">The delegate used to configure the OpenID 2.0 options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddMinecraft(
            [NotNull] this AuthenticationBuilder builder,
            [NotNull] Action<MinecraftAuthenticationOptions> configuration)
        {
            return builder.AddMinecraft(MinecraftAuthenticationDefaults.AuthenticationScheme, configuration);
        }

        /// <summary>
        /// Adds <see cref="MinecraftAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="scheme">The authentication scheme associated with this instance.</param>
        /// <param name="configuration">The delegate used to configure the Patreon options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddMinecraft(
            [NotNull] this AuthenticationBuilder builder,
            [NotNull] string scheme,
            [NotNull] Action<MinecraftAuthenticationOptions> configuration)
        {
            return builder.AddMinecraft(scheme, MinecraftAuthenticationDefaults.DisplayName, configuration);
        }

        /// <summary>
        /// Adds <see cref="MinecraftAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="scheme">The authentication scheme associated with this instance.</param>
        /// <param name="caption">The optional display name associated with this instance.</param>
        /// <param name="configuration">The delegate used to configure the Patreon options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddMinecraft(
            [NotNull] this AuthenticationBuilder builder,
            [NotNull] string scheme,
            [CanBeNull] string caption,
            [NotNull] Action<MinecraftAuthenticationOptions> configuration)
        {
            return builder.AddOAuth<MinecraftAuthenticationOptions, MinecraftAuthenticationHandler>(scheme, caption, configuration);
        }
    }
}
