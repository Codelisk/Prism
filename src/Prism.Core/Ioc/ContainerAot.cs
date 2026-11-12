using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#nullable enable
namespace Prism.Ioc
{
    /// <summary>
    /// Keeps implementation types that must remain activatable when reflection-based lookup is not
    /// available, for example in Native AOT. <see cref="Prism.Mvvm.ViewModelLocationProvider"/> asks
    /// here before it falls back to reflection.
    /// </summary>
    /// <remarks>
    /// Orderlyze-Fork: Upstream nimmt ContainerAot aus Prism.Container 10, das nicht auf nuget.org liegt.
    /// Diese Fassung bietet die Schnittstelle, die Prism.Core braucht. Einen Container-Generator, der
    /// Registrierungen automatisch einträgt, gibt es nicht: Typen werden mit <see cref="Preserve{T}"/>
    /// oder <see cref="Preserve{TService, TImplementation}"/> eingetragen.
    /// </remarks>
    public static class ContainerAot
    {
        private const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors;

        // Nur Typen, die über die annotierten Preserve-Methoden kamen, landen hier; ihre Konstruktoren bleiben erhalten.
        // Gespeichert als Preserved: ein Type als Wert eines Wörterbuchs verlöre die Annotation (IL2067 beim Herausgeben).
        private static readonly ConcurrentDictionary<Type, Preserved> s_byServiceType = new();
        private static readonly ConcurrentDictionary<(string FullName, Assembly Assembly), Preserved> s_byName = new();

        /// <summary>
        /// Preserves <typeparamref name="T"/> so it can be found by its type and by its full name.
        /// </summary>
        /// <typeparam name="T">The type to keep activatable.</typeparam>
        public static void Preserve<[DynamicallyAccessedMembers(Constructors)] T>() =>
            Add(typeof(T), typeof(T));

        /// <summary>
        /// Preserves <typeparamref name="TImplementation"/> as the implementation of <typeparamref name="TService"/>.
        /// The implementation can also be found by its own type and full name.
        /// </summary>
        /// <typeparam name="TService">The requested type.</typeparam>
        /// <typeparam name="TImplementation">The type that is created for <typeparamref name="TService"/>.</typeparam>
        public static void Preserve<TService, [DynamicallyAccessedMembers(Constructors)] TImplementation>()
            where TImplementation : TService
        {
            Add(typeof(TImplementation), typeof(TImplementation));
            s_byServiceType[typeof(TService)] = new Preserved(typeof(TImplementation));
        }

        /// <summary>
        /// Gets the preserved implementation for <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The requested type.</param>
        /// <param name="implementationType">The preserved implementation, if any.</param>
        /// <returns><c>true</c> if a type was preserved for <paramref name="serviceType"/>.</returns>
        public static bool TryGetImplementationType(
            Type serviceType,
            [NotNullWhen(true)][DynamicallyAccessedMembers(Constructors)] out Type? implementationType)
        {
            if (serviceType is not null && s_byServiceType.TryGetValue(serviceType, out var found))
            {
                implementationType = found.Type;
                return true;
            }

            implementationType = null;
            return false;
        }

        /// <summary>
        /// Gets the preserved type with the given full name from <paramref name="assembly"/>.
        /// </summary>
        /// <param name="fullName">The full name of the type, for example a ViewModel found by convention.</param>
        /// <param name="assembly">The assembly that must contain the type.</param>
        /// <param name="implementationType">The preserved type, if any.</param>
        /// <returns><c>true</c> if such a type was preserved.</returns>
        public static bool TryGetImplementationType(
            string fullName,
            Assembly assembly,
            [NotNullWhen(true)][DynamicallyAccessedMembers(Constructors)] out Type? implementationType)
        {
            if (fullName is not null && assembly is not null && s_byName.TryGetValue((fullName, assembly), out var found))
            {
                implementationType = found.Type;
                return true;
            }

            implementationType = null;
            return false;
        }

        private static void Add(Type serviceType, [DynamicallyAccessedMembers(Constructors)] Type implementationType)
        {
            var preserved = new Preserved(implementationType);
            s_byServiceType[serviceType] = preserved;
            if (implementationType.FullName is { } fullName)
                s_byName[(fullName, implementationType.Assembly)] = preserved;
        }

        private readonly struct Preserved
        {
            public Preserved([DynamicallyAccessedMembers(Constructors)] Type type) => Type = type;

            [DynamicallyAccessedMembers(Constructors)]
            public readonly Type Type;
        }
    }
}
