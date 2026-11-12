namespace Prism.Common;

#nullable enable
/// <summary>
/// Provides a wrapper for managing callbacks for handling specific errors
/// </summary>
/// <remarks>
/// Orderlyze-Fork: Upstream nimmt jeden <see cref="MulticastDelegate"/> und ruft ihn ueber
/// <c>GetType().GetMethod("Invoke")</c> per Reflexion auf; unter Trimming und Native AOT ist das nicht sicher (IL2075).
/// Hier legt jede Ueberladung beim Eintragen fest, wie ihr Rueckruf aufgerufen wird. Unterstuetzt sind die Formen, die
/// Befehle und Dialoge anbieten: ohne Argument, mit der Ausnahme, mit Ausnahme und Parameter, jeweils synchron oder mit
/// <see cref="Task"/>. Die Formen „Parameter vor Ausnahme“ und „nur Parameter“ der Upstream-Fassung entfallen: Prism selbst
/// nutzt sie nicht, und als Lambda waeren beide Reihenfolgen mehrdeutig.
/// </remarks>
public readonly struct MulticastExceptionHandler
{
    private readonly Dictionary<Type, Func<Exception, object?, Task>> _handlers;

    /// <summary>
    /// Initializes a new MulticastExceptionHandler
    /// </summary>
    public MulticastExceptionHandler()
    {
        _handlers = [];
    }

    /// <summary>
    /// Registers a callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <param name="callback">The callback to invoke for the given <see cref="Exception"/> type.</param>
    public void Register<TException>(Action callback)
        where TException : Exception =>
        Add<TException>(callback, (_, _) =>
        {
            callback();
            return Task.CompletedTask;
        });

    /// <summary>
    /// Registers a callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <param name="callback">The callback to invoke with the <see cref="Exception"/>.</param>
    public void Register<TException>(Action<TException> callback)
        where TException : Exception =>
        Add<TException>(callback, (exception, _) =>
        {
            callback((TException)exception);
            return Task.CompletedTask;
        });

    /// <summary>
    /// Registers a callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <param name="callback">The callback to invoke with the <see cref="Exception"/> and the parameter passed to <see cref="HandleAsync"/>.</param>
    public void Register<TException>(Action<TException, object?> callback)
        where TException : Exception =>
        Register<TException, object?>(callback);

    /// <summary>
    /// Registers a callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <typeparam name="TParameter">The type of the parameter passed to <see cref="HandleAsync"/>.</typeparam>
    /// <param name="callback">The callback to invoke with the <see cref="Exception"/> and the parameter.</param>
    public void Register<TException, TParameter>(Action<TException, TParameter> callback)
        where TException : Exception =>
        Add<TException>(callback, (exception, parameter) =>
        {
            callback((TException)exception, Cast<TParameter>(parameter));
            return Task.CompletedTask;
        });

    /// <summary>
    /// Registers an async callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <param name="callback">The callback to invoke for the given <see cref="Exception"/> type.</param>
    public void Register<TException>(Func<Task> callback)
        where TException : Exception =>
        Add<TException>(callback, (_, _) => callback());

    /// <summary>
    /// Registers an async callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <param name="callback">The callback to invoke with the <see cref="Exception"/>.</param>
    public void Register<TException>(Func<TException, Task> callback)
        where TException : Exception =>
        Add<TException>(callback, (exception, _) => callback((TException)exception));

    /// <summary>
    /// Registers an async callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <param name="callback">The callback to invoke with the <see cref="Exception"/> and the parameter passed to <see cref="HandleAsync"/>.</param>
    public void Register<TException>(Func<TException, object?, Task> callback)
        where TException : Exception =>
        Register<TException, object?>(callback);

    /// <summary>
    /// Registers an async callback to handle the specified exception
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> type.</typeparam>
    /// <typeparam name="TParameter">The type of the parameter passed to <see cref="HandleAsync"/>.</typeparam>
    /// <param name="callback">The callback to invoke with the <see cref="Exception"/> and the parameter.</param>
    public void Register<TException, TParameter>(Func<TException, TParameter, Task> callback)
        where TException : Exception =>
        Add<TException>(callback, (exception, parameter) => callback((TException)exception, Cast<TParameter>(parameter)));

    /// <summary>
    /// Determines if there is a callback registered to handle the specified exception
    /// </summary>
    /// <param name="exception">An <see cref="Exception"/> to handle or rethrow</param>
    /// <returns><c>True</c> if a Callback has been registered for the given type of <see cref="Exception"/>.</returns>
    public bool CanHandle(Exception exception) =>
        GetHandler(exception.GetType()) is not null;

    /// <summary>
    /// Handles a specified
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="parameter"></param>
    public async void Handle(Exception exception, object? parameter = null) =>
        await HandleAsync(exception, parameter);

    /// <summary>
    /// Handles a specified <see cref="Exception"/> asynchronously with a given optional parameter
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> encountered.</param>
    /// <param name="parameter">An optional parameter which may be passed to a registered callback delegate.</param>
    /// <returns>An asynchronus Task.</returns>
    public async Task HandleAsync(Exception exception, object? parameter = null)
    {
        var handler = GetHandler(exception.GetType());

        if (handler is null)
            return;

        await handler(exception, parameter);
    }

    private void Add<TException>(Delegate callback, Func<Exception, object?, Task> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(callback);
        _handlers.Add(typeof(TException), handler);
    }

    // Wie zuvor MethodInfo.Invoke: null wird zum Standardwert des Parametertyps, ein anderer Typ ist ein Fehler.
    private static TParameter Cast<TParameter>(object? parameter) =>
        parameter is null ? default! : (TParameter)parameter;

    // Der Rueckruf fuer den Typ selbst, sonst fuer den naechsten Basistyp.
    private Func<Exception, object?, Task>? GetHandler(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            if (_handlers.TryGetValue(current, out var handler))
                return handler;
        }

        return null;
    }
}
