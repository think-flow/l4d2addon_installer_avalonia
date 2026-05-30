using System.Collections.Specialized;
using l4d2addon_installer.Collections.ObjectModel;

namespace l4d2addon_installer.Tests;

public class ObservableSortedCollectionTests
{
    private static ObservableSortedCollection<int> CreateCollection() =>
        new(Comparer<int>.Default);

    private static ObservableSortedCollection<int> CreateCollection(IEnumerable<int> items)
    {
        var collection = new ObservableSortedCollection<int>(Comparer<int>.Default);
        foreach (var item in items)
            collection.Add(item);
        return collection;
    }

    [Fact]
    public void Add_ShouldMaintainSortOrder()
    {
        var collection = CreateCollection();

        collection.Add(3);
        collection.Add(1);
        collection.Add(2);

        Assert.Equal(3, collection.Count);
        Assert.Equal(1, collection[0]);
        Assert.Equal(2, collection[1]);
        Assert.Equal(3, collection[2]);
    }

    [Fact]
    public void Add_ShouldRaiseCollectionChanged()
    {
        var collection = CreateCollection();
        NotifyCollectionChangedEventArgs? eventArgs = null;
        collection.CollectionChanged += (_, e) => eventArgs = e;

        collection.Add(42);

        Assert.NotNull(eventArgs);
        Assert.Equal(NotifyCollectionChangedAction.Add, eventArgs.Action);
        Assert.Equal(42, eventArgs.NewItems![0]);
        Assert.Equal(0, eventArgs.NewStartingIndex);
    }

    [Fact]
    public void Add_ShouldRaisePropertyChanged_Count()
    {
        var collection = CreateCollection();
        var propertyNames = new List<string>();
        collection.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName!);

        collection.Add(1);

        Assert.Contains("Count", propertyNames);
        Assert.Contains("Item[]", propertyNames);
    }

    [Fact]
    public void Add_DuplicateItem_ShouldThrow()
    {
        var collection = CreateCollection();
        collection.Add(1);

        Assert.Throws<ArgumentException>(() => collection.Add(1));
    }

    [Fact]
    public void Remove_ExistingItem_ShouldReturnTrue()
    {
        var collection = CreateCollection([1, 2, 3]);

        bool result = collection.Remove(2);

        Assert.True(result);
        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0]);
        Assert.Equal(3, collection[1]);
    }

    [Fact]
    public void Remove_NonExistingItem_ShouldReturnFalse()
    {
        var collection = CreateCollection([1, 2, 3]);

        bool result = collection.Remove(99);

        Assert.False(result);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Remove_ShouldRaiseCollectionChanged()
    {
        var collection = CreateCollection([1, 2, 3]);
        NotifyCollectionChangedEventArgs? eventArgs = null;
        collection.CollectionChanged += (_, e) => eventArgs = e;

        collection.Remove(2);

        Assert.NotNull(eventArgs);
        Assert.Equal(NotifyCollectionChangedAction.Remove, eventArgs.Action);
        Assert.Equal(2, eventArgs.OldItems![0]);
    }

    [Fact]
    public void RemoveAt_ShouldRemoveCorrectItem()
    {
        var collection = CreateCollection([1, 2, 3]);

        collection.RemoveAt(1);

        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0]);
        Assert.Equal(3, collection[1]);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        var collection = CreateCollection([1, 2, 3]);
        bool resetRaised = false;
        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                resetRaised = true;
        };

        collection.Clear();

        Assert.Equal(0, collection.Count);
        Assert.True(resetRaised);
    }

    [Fact]
    public void Contains_ExistingItem_ShouldReturnTrue()
    {
        var collection = CreateCollection([1, 2, 3]);

        Assert.True(collection.Contains(2));
    }

    [Fact]
    public void Contains_NonExistingItem_ShouldReturnFalse()
    {
        var collection = CreateCollection([1, 2, 3]);

        Assert.False(collection.Contains(99));
    }

    [Fact]
    public void IndexOf_ShouldReturnCorrectIndex()
    {
        var collection = CreateCollection([10, 20, 30]);

        Assert.Equal(0, collection.IndexOf(10));
        Assert.Equal(1, collection.IndexOf(20));
        Assert.Equal(2, collection.IndexOf(30));
        Assert.Equal(-1, collection.IndexOf(99));
    }

    [Fact]
    public void Indexer_Set_ShouldThrowNotSupported()
    {
        var collection = CreateCollection([1, 2, 3]);

        Assert.Throws<NotSupportedException>(() => collection[0] = 99);
    }

    [Fact]
    public void Insert_ShouldThrowNotSupported()
    {
        var collection = CreateCollection();

        Assert.Throws<NotSupportedException>(() => collection.Insert(0, 1));
    }

    [Fact]
    public void RemoveBasedEquals_WithEqualButDifferentReference_ShouldWork()
    {
        var collection = new ObservableSortedCollection<StringWrapper>(new StringWrapperComparer());
        var item1 = new StringWrapper("hello");
        var item2 = new StringWrapper("hello"); // same value, different reference
        collection.Add(item1);

        bool result = collection.RemoveBasedEquals(item2);

        Assert.True(result);
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void RemoveBasedEquals_WithEqualButNotSameReference_ShouldRemove()
    {
        var collection = new ObservableSortedCollection<StringWrapper>(new StringWrapperComparer());
        var original = new StringWrapper("hello");
        collection.Add(original);

        var differentRef = new StringWrapper("hello");
        bool result = collection.RemoveBasedEquals(differentRef);

        Assert.True(result);
        Assert.Empty(collection);
    }

    [Fact]
    public void GetEnumerator_ShouldReturnItemsInSortedOrder()
    {
        var collection = CreateCollection([5, 3, 1, 4, 2]);

        var items = collection.ToList();

        Assert.Equal([1, 2, 3, 4, 5], items);
    }

    [Fact]
    public void CopyTo_ShouldCopyCorrectly()
    {
        var collection = CreateCollection([1, 2, 3]);
        var array = new int[3];

        collection.CopyTo(array, 0);

        Assert.Equal([1, 2, 3], array);
    }

    [Fact]
    public void IList_Add_ShouldReturnCorrectIndex()
    {
        var collection = CreateCollection([1, 3]);
        System.Collections.IList list = collection;

        int index = list.Add(2);

        Assert.Equal(1, index);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void IList_Contains_ShouldWork()
    {
        var collection = CreateCollection([1, 2, 3]);
        System.Collections.IList list = collection;

        Assert.True(list.Contains(2));
        Assert.False(list.Contains(99));
    }

    [Fact]
    public void IList_Remove_ShouldWork()
    {
        var collection = CreateCollection([1, 2, 3]);
        System.Collections.IList list = collection;

        list.Remove(2);

        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void CustomComparer_ShouldUseProvidedComparer()
    {
        // Reverse order comparer
        var collection = new ObservableSortedCollection<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));

        collection.Add(1);
        collection.Add(2);
        collection.Add(3);

        Assert.Equal(3, collection[0]);
        Assert.Equal(2, collection[1]);
        Assert.Equal(1, collection[2]);
    }

    [Fact]
    public void DelegateComparer_ShouldWork()
    {
        var collection = new ObservableSortedCollection<int>((a, b) => a.CompareTo(b));

        collection.Add(3);
        collection.Add(1);
        collection.Add(2);

        Assert.Equal(1, collection[0]);
        Assert.Equal(2, collection[1]);
        Assert.Equal(3, collection[2]);
    }

    [Fact]
    public void LargeCollection_ShouldMaintainSortOrder()
    {
        var collection = CreateCollection();
        var random = new Random(42);
        var expected = new List<int>();

        for (int i = 0; i < 1000; i++)
        {
            int value = random.Next(10000);
            if (!collection.Contains(value))
            {
                collection.Add(value);
                expected.Add(value);
            }
        }

        expected.Sort();

        Assert.Equal(expected.Count, collection.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], collection[i]);
        }
    }

    private class StringWrapper
    {
        public string Value { get; }
        public StringWrapper(string value) => Value = value;
    }

    private class StringWrapperComparer : IComparer<StringWrapper>
    {
        public int Compare(StringWrapper? x, StringWrapper? y) =>
            string.Compare(x?.Value, y?.Value, StringComparison.Ordinal);
    }
}
