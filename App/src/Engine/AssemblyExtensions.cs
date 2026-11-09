namespace Markwardt.Browser.Engine;

/// <summary>
/// Reads what is embedded in an assembly.
/// </summary>
internal static class AssemblyExtensions
{
    extension(Assembly assembly)
    {
        /// <summary>
        /// Reads an embedded text resource, such as a script.
        /// </summary>
        /// <param name="resource">The full resource name.</param>
        /// <returns>The text of the resource.</returns>
        /// <exception cref="InvalidOperationException">The assembly has no such resource.</exception>
        public string ReadText(string resource)
        {
            using Stream stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"The resource {resource} is not embedded in the assembly.");
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
    }
}
