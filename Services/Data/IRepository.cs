using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services.Data;

/// <summary>
/// Repository pattern for scaffolded apps (performance plan P1-3): the
/// sample starting point when a second table/feature arrives, so schema
/// code grows through <see cref="DatabaseService"/> migrations instead of
/// ad-hoc SQL in view models. A suggestion, not a forced dependency —
/// nothing in the template implements this except
/// <see cref="InMemoryRepository{T}"/> (tests, previews, prototyping).
/// </summary>
public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task UpsertAsync(T item);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// In-memory <see cref="IRepository{T}"/> (tests, design-time data).
/// Keyed by <paramref name="keySelector"/>; thread-safe; never throws
/// out of reads (missing keys return null/false).
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly Func<T, string> _keySelector;
    private readonly Dictionary<string, T> _items = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public InMemoryRepository(Func<T, string> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _keySelector = keySelector;
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _items.Count;
            }
        }
    }

    public Task<IReadOnlyList<T>> GetAllAsync()
    {
        lock (_gate)
        {
            IReadOnlyList<T> snapshot = new List<T>(_items.Values);
            return Task.FromResult(snapshot);
        }
    }

    public Task<T?> GetByIdAsync(string id)
    {
        lock (_gate)
        {
            _items.TryGetValue(id ?? string.Empty, out T? item);
            return Task.FromResult(item);
        }
    }

    public Task UpsertAsync(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            _items[_keySelector(item)] = item;
        }
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id)
    {
        lock (_gate)
        {
            return Task.FromResult(_items.Remove(id ?? string.Empty));
        }
    }
}
