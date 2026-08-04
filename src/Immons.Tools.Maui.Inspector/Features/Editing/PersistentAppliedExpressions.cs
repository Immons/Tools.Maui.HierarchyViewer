using System.Security.Cryptography;
using System.Text;
using Immons.Tools.Maui.Inspector.Features.Editing.Storage;
using Immons.Tools.Maui.Inspector.Shared.Storage;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Durable variant of <see cref="IAppliedExpressions"/>: expressions survive app restarts in the
/// configured store, keyed by the element's XAML source location (fallback: type + x:Name/AutomationId).
/// Elements with no stable identity (created in C#, unnamed) stay in the per-session store so
/// unrelated instances never share an entry.
/// </summary>
internal sealed class PersistentAppliedExpressions : IAppliedExpressions
{
    const string KeyPrefix = "hv_expr_";

    static IExpressionStore Store => InspectorStorage.Current.Expressions;

    readonly AppliedExpressions _session = new();
    readonly Dictionary<string, string?> _cache = [];
    readonly object _gate = new();

    public void Record(object target, string property, string? expression)
    {
        if (IdentityOf(target) is not { } identity)
        {
            _session.Record(target, property, expression);
            return;
        }

        var key = Key(identity, property);
        lock (_gate)
        {
            _cache[key] = expression;
        }

        Store.Save(key, expression);
    }

    public string? Find(object target, string property)
    {
        if (IdentityOf(target) is not { } identity)
            return _session.Find(target, property);

        var key = Key(identity, property);
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached;
        }

        var stored = Store.Find(key);

        lock (_gate)
        {
            _cache[key] = stored;
        }
        return stored;
    }

    static string? IdentityOf(object target)
    {
        if (XamlSource.Describe(target) is { } source)
            return source;
        if (target is Element element
            && (!string.IsNullOrEmpty(element.StyleId) || !string.IsNullOrEmpty(element.AutomationId)))
            return $"{target.GetType().FullName}|@{element.StyleId}|#{element.AutomationId}";
        return null;
    }

    static string Key(string identity, string property) =>
        KeyPrefix + Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes($"{identity}|{property}")))[..20];
}
