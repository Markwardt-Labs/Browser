namespace Markwardt.Browser.Mvvm;

/// <summary>
/// Base for types that notify the UI when a property changes.
/// </summary>
internal abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for a property.
    /// </summary>
    /// <param name="name">The property that changed.</param>
    protected void Raise(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Assigns a backing field and raises <see cref="PropertyChanged"/> when the value actually changed.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="field">The backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="name">The property name; filled in by the compiler.</param>
    protected void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            Raise(name);
        }
    }
}
