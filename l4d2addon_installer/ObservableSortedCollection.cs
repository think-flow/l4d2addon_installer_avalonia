using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace l4d2addon_installer.Collections.ObjectModel;

/// <summary>
/// 一种基于SortedList的Observable集合
/// </summary>
[Serializable]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public sealed class ObservableSortedCollection<T> :
    INotifyCollectionChanged, INotifyPropertyChanged, IList<T>, IList, IReadOnlyList<T>
    where T : notnull
{
    private readonly SortedList<T, T> _items;

    [NonSerialized]
    private int _blockReentrancyCount;

    private SimpleMonitor? _monitor; // Lazily allocated only when a subclass calls BlockReentrancy() or during serialization. Do not rename (binary serialization)

    public ObservableSortedCollection(Func<T?, T?, int> comparer)
        : this(new DefaultDelegateComparer(comparer))
    {
    }

    public ObservableSortedCollection(IComparer<T> comparer)
    {
        _items = new SortedList<T, T>(comparer);
    }

    public int Count => _items.Count;

    public T this[int index]
    {
        get => _items.Values[index];
        set => throw new NotSupportedException();
        // ThrowIfReadOnly();
        // CheckReentrancy();
        // var originalItem = this[index];
        // _items.Values[index] = value;
        // OnIndexerPropertyChanged();
        // OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, value, index);
    }

    public void Add(T item)
    {
        ThrowIfReadOnly();
        _items.Add(item, item);
        //二分查找法，查找key对应的index
        int index = _items.IndexOfKey(item);
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    public void Clear()
    {
        ThrowIfReadOnly();
        CheckReentrancy();
        _items.Clear();
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionReset();
    }

    public void CopyTo(T[] array, int arrayIndex) => _items.Values.CopyTo(array, arrayIndex);

    public bool Contains(T item) => _items.ContainsKey(item);

    public IEnumerator<T> GetEnumerator() => _items.Values.GetEnumerator();

    public int IndexOf(T item) => _items.IndexOfKey(item);

    public void Insert(int index, T item)
    {
        throw new NotSupportedException();
        // ThrowIfReadOnly();
        // _items.Values.Insert(index, item);
        // OnCountPropertyChanged();
        // OnIndexerPropertyChanged();
        // OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    public bool Remove(T item)
    {
        ThrowIfReadOnly();
        int index = _items.IndexOfKey(item);
        if (index < 0) return false;
        _items.RemoveAt(index);
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        return true;
    }

    public void RemoveAt(int index)
    {
        ThrowIfReadOnly();
        var removedItem = _items.GetKeyAtIndex(index);
        _items.RemoveAt(index);
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
    }

    private int IndexOfBasedLoop(ref T item)
    {
        int index = -1;
        for (int i = 0; i < _items.Keys.Count; i++)
        {
            var key = _items.Keys[i];

            if (0 == _items.Comparer.Compare(key, item))
            {
                index = i;
                var innerItem = _items.Keys[i];
                //如果传入的item不是内部_items中保存的，则将其替换为内部的
                if (!ReferenceEquals(item, innerItem))
                {
                    item = _items[key];
                }
            }
        }

        return index;
    }

    public int GetIndexFromKey(T item) => _items.Keys.IndexOf(item);

    /// <summary>
    /// 该方法循环内部key类型，并调用Comparer.Compare方法，判断是否相等
    /// 时间复杂度为O(n)
    /// 若要使用效率更高的请使用Remove方法（二分查找）
    /// </summary>
    public bool RemoveBasedEquals(T item)
    {
        ThrowIfReadOnly();
        int index = IndexOfBasedLoop(ref item);
        if (index < 0) return false;
        _items.RemoveAt(index);
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        return true;
    }

    #region core

    /// <summary>
    /// Occurs when the collection changes, either by adding or removing an item.
    /// </summary>
    [field: NonSerialized]
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    [field: NonSerialized]
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Helper to raise a PropertyChanged event for the Count property
    /// </summary>
    private void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

    /// <summary>
    /// Helper to raise a PropertyChanged event for the Indexer property
    /// </summary>
    private void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

    /// <summary>
    /// Helper to raise CollectionChanged event with action == Reset to any listeners
    /// </summary>
    private void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);

    /// <summary>
    /// Raise CollectionChanged event to any listeners.
    /// Properties/methods modifying this ObservableCollection will raise
    /// a collection changed event through this virtual method.
    /// </summary>
    /// <remarks>
    /// When overriding this method, either call its base implementation
    /// or call <see cref="BlockReentrancy" /> to guard against reentrant collection changes.
    /// </remarks>
    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        var handler = CollectionChanged;
        if (handler != null)
        {
            // Not calling BlockReentrancy() here to avoid the SimpleMonitor allocation.
            _blockReentrancyCount++;
            try
            {
                handler(this, e);
            }
            finally
            {
                _blockReentrancyCount--;
            }
        }
    }

    /// <summary>
    /// Helper to raise CollectionChanged event to any listeners
    /// </summary>
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
    }

    /// <summary>
    /// Helper to raise CollectionChanged event to any listeners
    /// </summary>
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? oldItem, object? newItem, int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
    }

    /// <summary>
    /// Disallow reentrant attempts to change this collection. E.g. an event handler
    /// of the CollectionChanged event is not allowed to make changes to this collection.
    /// </summary>
    /// <remarks>
    /// typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope:
    /// <code>
    ///         using (BlockReentrancy())
    ///         {
    ///             CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
    ///         }
    /// </code>
    /// </remarks>
    private IDisposable BlockReentrancy()
    {
        _blockReentrancyCount++;
        return EnsureMonitorInitialized();
    }

    /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
    /// <exception cref="InvalidOperationException">
    /// raised when changing the collection
    /// while another collection change is still being notified to other listeners
    /// </exception>
    private void CheckReentrancy()
    {
        if (_blockReentrancyCount > 0)
        {
            // we can allow changes if there's only one listener - the problem
            // only arises if reentrant changes make the original event args
            // invalid for later listeners.  This keeps existing code working
            // (e.g. Selector.SelectedItems).
            if (CollectionChanged?.GetInvocationList().Length > 1)
            {
                throw new InvalidOperationException("Cannot change ObservableCollection during a CollectionChanged event.");
            }
        }
    }

    private SimpleMonitor EnsureMonitorInitialized() => _monitor ??= new SimpleMonitor(this);

    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
        EnsureMonitorInitialized();
        _monitor!._busyCount = _blockReentrancyCount;
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (_monitor != null)
        {
            _blockReentrancyCount = _monitor._busyCount;
            _monitor._collection = this;
        }
    }

    private void ThrowIfReadOnly()
    {
        // bool isReadOnly = _items.Values.IsReadOnly;
        // if (isReadOnly)
        // {
        //     throw new NotSupportedException("Collection is read-only.");
        // }
    }

    private static bool IsCompatibleObject(object? value) =>
        // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
        // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
        value is T || (value == null && default(T) == null);

    // this class helps prevent reentrant calls
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed class SimpleMonitor : IDisposable
    {
        internal int _busyCount; // Only used during (de)serialization to maintain compatibility with desktop. Do not rename (binary serialization)

        [NonSerialized]
        internal ObservableSortedCollection<T> _collection;

        public SimpleMonitor(ObservableSortedCollection<T> collection)
        {
            Debug.Assert(collection != null);
            _collection = collection;
        }

        public void Dispose() => _collection._blockReentrancyCount--;
    }

    private class DefaultDelegateComparer(Func<T?, T?, int> comparer) : IComparer<T>
    {
        public int Compare(T? x, T? y) => comparer(x, y);
    }

    #endregion

    #region Interface

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _items.Values).GetEnumerator();

    object? IList.this[int index]
    {
        get => _items.Values[index];
        set
        {
            if (default(T) != null && value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var item = (T) value!;
            this[index] = item;
        }
    }

    int IList.Add(object? value)
    {
        ThrowIfReadOnly();

        var item = (T) value!;
        Add((T) value!);
        return _items.IndexOfKey(item);
    }

    bool IList.Contains(object? value) => IsCompatibleObject(value) && Contains((T) value!);

    int IList.IndexOf(object? value)
    {
        if (IsCompatibleObject(value))
        {
            return IndexOf((T) value!);
        }

        return -1;
    }

    void IList.Insert(int index, object? value)
    {
        ThrowIfReadOnly();
        if (IsCompatibleObject(value))
        {
            Insert(index, (T) value!);
        }
    }

    void IList.Remove(object? value)
    {
        ThrowIfReadOnly();
        if (IsCompatibleObject(value))
        {
            Remove((T) value!);
        }
    }

    bool IList.IsFixedSize => ((IList) _items.Values).IsFixedSize;

    bool IList.IsReadOnly => _items.Values.IsReadOnly;

    void ICollection.CopyTo(Array array, int index)
    {
        if (array is not T[] tArray)
        {
            throw new ArgumentException($"array is not {typeof(T).Name}[]", nameof(array));
        }

        CopyTo(tArray, index);
    }

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => ((ICollection) _items.Values).SyncRoot;

    bool ICollection<T>.IsReadOnly => _items.Values.IsReadOnly;

    #endregion
}

internal sealed class CollectionDebugView<T>
{
    private readonly ICollection<T> _collection;

    public CollectionDebugView(ICollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        _collection = collection;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var items = new T[_collection.Count];
            _collection.CopyTo(items, 0);
            return items;
        }
    }
}

internal static class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
    internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
    internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
}
