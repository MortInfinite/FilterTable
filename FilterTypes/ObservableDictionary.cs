using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace FilterTypes
{
	/// <summary>
	/// Wraps a dictionary, to raise <see cref="DictionaryChangedEventArgs"/> when an item in the dictionary changes.
	/// </summary>
	/// <typeparam name="TKey">Type of key to store in the dictionary.</typeparam>
	/// <typeparam name="TValue">Type of value to store in the dictionary.</typeparam>
	public class ObservableDictionary<TKey, TValue>	:	IDictionary<TKey, TValue>, 
														IDictionary where TKey : notnull
    {
		#region Methods

		/// <summary>
		/// Notify subscribers that the dictionary changed.
		/// </summary>
		/// <param name="change">Change that occurred on the dictionary.</param>
		/// <param name="key">Key identifying the item which changed.</param>
		protected virtual void RaiseDictionaryChanged(DictionaryChange change, TKey key)
        {
            var eventHandler = DictionaryChanged;
            if (eventHandler != null)
                eventHandler(this, new DictionaryChangedEventArgs<TKey>(change, key));
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            RaiseDictionaryChanged(DictionaryChange.ItemInserted, key);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.Remove(key))
            {
                RaiseDictionaryChanged(DictionaryChange.ItemRemoved, key);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            TValue? currentValue;
            if(_dictionary.TryGetValue(item.Key, out currentValue) && Object.Equals(item.Value, currentValue) && _dictionary.Remove(item.Key))
            {
                RaiseDictionaryChanged(DictionaryChange.ItemRemoved, item.Key);
                return true;
            }
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                return _dictionary[key];
            }
            set
            {
                _dictionary[key] = value;
                RaiseDictionaryChanged(DictionaryChange.ItemChanged, key);
            }
        }

        public void Clear()
        {
			TKey[] keys = _dictionary.Keys.ToArray();
            _dictionary.Clear();
            foreach (var key in keys)
                RaiseDictionaryChanged(DictionaryChange.ItemRemoved, key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dictionary.Keys; }
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return _dictionary.Values; }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

		public bool IsFixedSize => ((IDictionary) _dictionary).IsFixedSize;

		ICollection IDictionary.Keys => ((IDictionary) _dictionary).Keys;

		ICollection IDictionary.Values => ((IDictionary) _dictionary).Values;

		public bool IsSynchronized => ((ICollection) _dictionary).IsSynchronized;

		public object SyncRoot => ((ICollection) _dictionary).SyncRoot;

		public object? this[object key] 
		{ 
			get
			{
				return ((IDictionary) _dictionary)[key];
			} 
			set
			{
				((IDictionary) _dictionary)[key]=value; 
                RaiseDictionaryChanged(DictionaryChange.ItemChanged, (TKey) key);
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable) _dictionary).GetEnumerator();
		}

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            int arraySize = array.Length;
            foreach (var pair in _dictionary)
            {
                if (arrayIndex >= arraySize) break;
                array[arrayIndex++] = pair;
            }
        }

		public void Add(object key, object? value)
		{
			((IDictionary) _dictionary).Add(key, value);
            RaiseDictionaryChanged(DictionaryChange.ItemInserted, (TKey) key);
		}

		public bool Contains(object key)
		{
			return ((IDictionary) _dictionary).Contains(key);
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return ((IDictionary) _dictionary).GetEnumerator();
		}

		public void Remove(object key)
		{
			bool contains = Contains(key);
			if(contains)
			{
				((IDictionary) _dictionary).Remove(key);
				RaiseDictionaryChanged(DictionaryChange.ItemRemoved, (TKey) key);
			}
		}

		public void CopyTo(Array array, int index)
		{
			((ICollection) _dictionary).CopyTo(array, index);
		}

		/// <summary>
		/// Return the value matching the specified <paramref name="key"/> or return <paramref name="defaultValue"/>
		/// if the <paramref name="key"/> isn't found.
		/// </summary>
		/// <param name="key">Key of the item to get.</param>
		/// <param name="defaultValue">Value to return if <paramref name="key"/> isn't found.</param>
		/// <returns></returns>
		public TValue? GetValueOrDefault(TKey key, TValue? defaultValue)
		{ 
			bool exists = TryGetValue(key, out TValue? value);
			if(!exists)
				return defaultValue;

			return value;
		}
			
		#endregion

		#region Events

		/// <summary>
		/// Raised when an item has changed in the dictionary.
		/// </summary>
        public event DictionaryChangedDelegate<TKey,TValue> DictionaryChanged;
		#endregion

		#region Fields
        private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
		#endregion
	}

	/// <summary>
	/// Types of changes that may occur in a collection.
	/// </summary>
	public enum DictionaryChange
	{
		/// <summary>
		/// An item was inserted into the dictionary.
		/// </summary>
		ItemInserted,

		/// <summary>
		/// An item was removed form the dictionary.
		/// </summary>
		ItemRemoved,

		/// <summary>
		/// An item was replaced in the dictionary.
		/// </summary>
		ItemChanged
	}

	/// <summary>
	/// Describes which change occurred in a dictionary.
	/// </summary>
    public class DictionaryChangedEventArgs<TKey>
    {
		/// <summary>
		/// Create a new <see cref="DictionaryChangedEventArgs"/>.
		/// </summary>
		/// <param name="change">Change that occurred on the dictionary.</param>
		/// <param name="key">Key identifying the item which changed.</param>
        public DictionaryChangedEventArgs(DictionaryChange change, TKey key)
        {
            ChangeAction = change;
            Key = key;
        }

		/// <summary>
		/// Change that occurred on the dictionary.
		/// </summary>
        public DictionaryChange ChangeAction 
		{ 
			get; 
		}

		/// <summary>
		/// Key identifying the item which changed.
		/// </summary>
        public TKey Key 
		{ 
			get; 
		}
    }

	/// <summary>
	/// Delegate used to indicate a change in the dictionary.
	/// </summary>
	/// <param name="source">Object which changed.</param>
	/// <param name="change">Change that occurred in the dictionary.</param>
	public delegate void DictionaryChangedDelegate<TKey,TValue>(IDictionary<TKey, TValue> source, DictionaryChangedEventArgs<TKey> change);
}
