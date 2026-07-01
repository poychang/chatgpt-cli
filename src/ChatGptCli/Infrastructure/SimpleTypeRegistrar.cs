using Spectre.Console.Cli;

namespace ChatGptCli.Infrastructure;

/// <summary>
/// Minimal <see cref="ITypeRegistrar"/> backed by a simple service map, so
/// commands can receive <c>ICodexAdapter</c> without pulling in a full DI container.
/// </summary>
public sealed class SimpleTypeRegistrar : ITypeRegistrar
{
    private readonly Dictionary<Type, Type> _registrations = new();
    private readonly Dictionary<Type, object> _instances = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    public void Register(Type service, Type implementation)
        => _registrations[service] = implementation;

    public void RegisterInstance(Type service, object implementation)
        => _instances[service] = implementation;

    public void RegisterLazy(Type service, Func<object> factory)
        => _factories[service] = factory;

    public ITypeResolver Build() => new SimpleTypeResolver(_registrations, _instances, _factories);

    private sealed class SimpleTypeResolver(
        Dictionary<Type, Type> registrations,
        Dictionary<Type, object> instances,
        Dictionary<Type, Func<object>> factories) : ITypeResolver
    {
        public object? Resolve(Type? type)
        {
            if (type is null)
            {
                return null;
            }

            if (instances.TryGetValue(type, out var instance))
            {
                return instance;
            }

            if (factories.TryGetValue(type, out var factory))
            {
                return factory();
            }

            var target = registrations.TryGetValue(type, out var impl) ? impl : type;
            if (target.IsAbstract || target.IsInterface)
            {
                // Spectre resolves IEnumerable<T> services (e.g. help providers);
                // return an empty sequence rather than null for unregistered ones.
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return Array.CreateInstance(type.GetGenericArguments()[0], 0);
                }

                return null;
            }

            return CreateInstance(target);
        }

        private object CreateInstance(Type type)
        {
            // Pick the greediest constructor and resolve its parameters recursively.
            var ctor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor is null)
            {
                return Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"Could not create {type}.");
            }

            var args = ctor.GetParameters()
                .Select(p => Resolve(p.ParameterType))
                .ToArray();

            return ctor.Invoke(args);
        }
    }
}
