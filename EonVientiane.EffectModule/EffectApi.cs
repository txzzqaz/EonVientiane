namespace EonVientiane.EffectModule;

public static class EffectApi
{
    private const string EffectStoreStateKey = "battle.effectStore";

    public static void Initialize(IDictionary<string, object> state)
    {
        _ = EnsureStore(state);
    }

    public static object? Read(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId, string key)
    {
        return EnsureStore(state).Read(scope, ownerId, sourceItemId, key);
    }

    public static void Write(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId, string key, object? value)
    {
        EnsureStore(state).Write(scope, ownerId, sourceItemId, key, value);
    }

    public static void Delete(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId, string key)
    {
        EnsureStore(state).Delete(scope, ownerId, sourceItemId, key);
    }

    public static string[] ListKeys(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId)
    {
        return EnsureStore(state).ListKeys(scope, ownerId, sourceItemId);
    }

    public static void Clear(IDictionary<string, object> state)
    {
        EnsureStore(state).Clear();
    }

    private static EffectStore EnsureStore(IDictionary<string, object> state)
    {
        if (!state.TryGetValue(EffectStoreStateKey, out var storeObj) || storeObj is not EffectStore store)
        {
            store = new EffectStore();
            state[EffectStoreStateKey] = store;
        }

        return store;
    }

    private sealed class EffectStore
    {
        private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal);

        public object? Read(string scope, string ownerId, string sourceItemId, string key)
        {
            values.TryGetValue(BuildKey(scope, ownerId, sourceItemId, key), out var value);
            return value;
        }

        public void Write(string scope, string ownerId, string sourceItemId, string key, object? value)
        {
            values[BuildKey(scope, ownerId, sourceItemId, key)] = value;
        }

        public void Delete(string scope, string ownerId, string sourceItemId, string key)
        {
            values.Remove(BuildKey(scope, ownerId, sourceItemId, key));
        }

        public string[] ListKeys(string scope, string ownerId, string sourceItemId)
        {
            var prefix = BuildPrefix(scope, ownerId, sourceItemId);
            return values.Keys
                .Where(x => x.StartsWith(prefix, StringComparison.Ordinal))
                .Select(x => x[prefix.Length..])
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
        }

        public void Clear()
        {
            values.Clear();
        }

        private static string BuildPrefix(string scope, string ownerId, string sourceItemId)
        {
            return $"{Normalize(scope)}|{Normalize(ownerId)}|{Normalize(sourceItemId)}|";
        }

        private static string BuildKey(string scope, string ownerId, string sourceItemId, string key)
        {
            return BuildPrefix(scope, ownerId, sourceItemId) + Normalize(key);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "_" : value.Trim();
        }
    }
}
