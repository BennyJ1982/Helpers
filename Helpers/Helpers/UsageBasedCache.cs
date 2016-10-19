namespace Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// A cache that favors elements often looked for over ones never looked for.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class UsageBasedCache<TKey, TValue>
	{
		/// <summary>
		/// The internal data dictionary.
		/// </summary>
		private readonly Dictionary<TKey, CacheEntry<TValue>> data;

		/// <summary>
		/// The maximum cache size.
		/// </summary>
		private readonly int maxCacheSize;

		/// <summary>
		/// The shrink count (number if items to release when shrinking)
		/// </summary>
		private readonly int shrinkCount;

		/// <summary>
		/// The age.
		/// </summary>
		private long age;

		/// <summary>
		/// Initializes a new instance of the <see cref="UsageBasedCache{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="maxCacheSize">Maximum size of the cache.</param>
		public UsageBasedCache(int maxCacheSize)
			: this(maxCacheSize, 10)
		{
			this.data = new Dictionary<TKey, CacheEntry<TValue>>(maxCacheSize);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UsageBasedCache{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="maxCacheSize">Maximum size of the cache.</param>
		/// <param name="equalityComparer">The equality comparer.</param>
		public UsageBasedCache(int maxCacheSize, IEqualityComparer<TKey> equalityComparer)
			: this(maxCacheSize, 10)
		{
			this.data = new Dictionary<TKey, CacheEntry<TValue>>(maxCacheSize, equalityComparer);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UsageBasedCache{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="maxCacheSize">Maximum size of the cache.</param>
		/// <param name="shrinkPercentage">The shrink percentage.</param>
		private UsageBasedCache(int maxCacheSize, int shrinkPercentage)
		{
			this.maxCacheSize = maxCacheSize;
			this.shrinkCount = maxCacheSize - (maxCacheSize * shrinkPercentage);
		}

		/// <summary>
		/// Tries the get value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <returns><c>true</c> if getting the value was successful; otherwise, <c>false</c></returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			this.age++;
			CacheEntry<TValue> entry;
			if (this.data.TryGetValue(key, out entry))
			{
				entry.ReportHit(this.age);
				value = entry.Value;
				return true;
			}

			value = default(TValue);
			return false;
		}

		/// <summary>
		/// Adds the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="System.InvalidOperationException">Element already added.</exception>
		public void Add(TKey key, TValue value)
		{
			if (this.data.ContainsKey(key))
			{
				throw new InvalidOperationException("Element already added.");
			}

			this.ShrinkIfNecessary();
			this.data.Add(key, new CacheEntry<TValue>(value, this.age));
		}

		/// <summary>
		/// Shrinks if necessary.
		/// </summary>
		private void ShrinkIfNecessary()
		{
			if (this.data.Count < this.maxCacheSize)
			{
				return;
			}

			// get as many least used cache entries as we should delete
			var leastUsedKeys = this.data
				.OrderBy(kvp => kvp.Value.LastUsedAtAge)
				.Select(kvp => kvp.Key)
				.Take(this.shrinkCount).ToArray();

			foreach (var key in leastUsedKeys)
			{
				this.data.Remove(key);
			}
		}

		/// <summary>
		/// Straightforward cache entry implementation.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		private class CacheEntry<T>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="CacheEntry{T}"/> class.
			/// </summary>
			/// <param name="value">The value.</param>
			/// <param name="age">The age.</param>
			public CacheEntry(T value, long age)
			{
				this.Value = value;
				this.LastUsedAtAge = age;
			}

			/// <summary>
			/// Gets the value.
			/// </summary>
			public T Value { get; private set; }

			/// <summary>
			/// Gets the number of hits.
			/// </summary>
			public long NumberOfHits { get; private set; }

			/// <summary>
			/// Gets the last used at age.
			/// </summary>
			public long LastUsedAtAge { get; private set; }

			/// <summary>
			/// Reports a cache hit.
			/// </summary>
			/// <param name="age">The age.</param>
			public void ReportHit(long age)
			{
				this.NumberOfHits++;
				this.LastUsedAtAge = age;
			}
		}
	}
}
