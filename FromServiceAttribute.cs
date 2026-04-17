using Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Marks a minimal API parameter as resolved from DI (IServiceProvider).
/// Required for generic interfaces like IValidator&lt;T&gt; in .NET 10,
/// where the RequestDelegateFactory cannot infer the parameter source automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromServiceAttribute : Attribute, IFromServiceMetadata;
