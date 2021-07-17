using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AspNet.Security.OAuth.BuyMeACoffee
{
    /// <summary>
    /// Extension methods to add Patreon authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class BuyMeACoffeeAuthenticationExtensions
    {
        /// <summary>
        /// Adds <see cref="BuyMeACoffeeAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBuyMeACoffee([NotNull] this AuthenticationBuilder builder)
        {
            return builder.AddBuyMeACoffee(BuyMeACoffeeAuthenticationDefaults.AuthenticationScheme, options => { });
        }

        /// <summary>
        /// Adds <see cref="BuyMeACoffeeAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="configuration">The delegate used to configure the OpenID 2.0 options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBuyMeACoffee(
            [NotNull] this AuthenticationBuilder builder,
            [NotNull] Action<BuyMeACoffeeAuthenticationOptions> configuration)
        {
            return builder.AddBuyMeACoffee(BuyMeACoffeeAuthenticationDefaults.AuthenticationScheme, configuration);
        }

        /// <summary>
        /// Adds <see cref="BuyMeACoffeeAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="scheme">The authentication scheme associated with this instance.</param>
        /// <param name="configuration">The delegate used to configure the Patreon options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBuyMeACoffee(
            [NotNull] this AuthenticationBuilder builder,
            [NotNull] string scheme,
            [NotNull] Action<BuyMeACoffeeAuthenticationOptions> configuration)
        {
            return builder.AddBuyMeACoffee(scheme, BuyMeACoffeeAuthenticationDefaults.DisplayName, configuration);
        }

        /// <summary>
        /// Adds <see cref="BuyMeACoffeeAuthenticationHandler"/> to the specified
        /// <see cref="AuthenticationBuilder"/>, which enables Patreon authentication capabilities.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="scheme">The authentication scheme associated with this instance.</param>
        /// <param name="caption">The optional display name associated with this instance.</param>
        /// <param name="configuration">The delegate used to configure the Patreon options.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddBuyMeACoffee(
            [NotNull] this AuthenticationBuilder builder,
            [NotNull] string scheme,
            [CanBeNull] string caption,
            [NotNull] Action<BuyMeACoffeeAuthenticationOptions> configuration)
        {
            return builder.AddOAuth<BuyMeACoffeeAuthenticationOptions, BuyMeACoffeeAuthenticationHandler>(scheme, caption, configuration);
        }
    }
}
