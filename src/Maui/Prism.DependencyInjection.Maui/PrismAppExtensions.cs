using Microsoft.Extensions.DependencyInjection;
using Prism;

namespace Prism.DependencyInjection.Maui;

/// <summary>
/// Configures the application to use Prism with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class PrismAppExtensions
{
    /// <summary>
    /// Adds Prism to the application on a <see cref="MicrosoftContainerExtension"/> with default options.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configurePrism">The callback registering services, views, modules, and startup navigation.</param>
    /// <returns>The same application builder for further configuration.</returns>
    /// <remarks>
    /// The first call selects the application container. Later calls reuse the same Prism builder and container.
    /// Configuration callbacks execute in call order.
    /// </remarks>
    public static MauiAppBuilder UsePrism(this MauiAppBuilder builder, Action<PrismAppBuilder> configurePrism) =>
        builder.UsePrism(new ServiceProviderOptions(), configurePrism);

    /// <summary>
    /// Adds Prism to the application on a <see cref="MicrosoftContainerExtension"/> built with <paramref name="options"/>.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="options">The options for the service provider, for example
    /// <see cref="ServiceProviderOptions.ValidateOnBuild"/> during development.</param>
    /// <param name="configurePrism">The callback registering services, views, modules, and startup navigation.</param>
    /// <returns>The same application builder for further configuration.</returns>
    /// <remarks>
    /// If Prism is already configured for this builder, the selected container is reused and
    /// <paramref name="options"/> do not apply.
    /// </remarks>
    public static MauiAppBuilder UsePrism(this MauiAppBuilder builder, ServiceProviderOptions options, Action<PrismAppBuilder> configurePrism)
    {
        ArgumentNullException.ThrowIfNull(options);
        return builder.UsePrism(() => new MicrosoftContainerExtension(options), configurePrism);
    }
}
