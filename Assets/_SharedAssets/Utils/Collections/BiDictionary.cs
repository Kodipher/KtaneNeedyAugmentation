using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rephidock.GeneralUtilities.Collections;


namespace SharedAssets.Utils.Collections {

	// Based on: https://stackoverflow.com/questions/10966331/two-way-bidirectional-dictionary-in-c

	/// <summary>
	/// Bidirectional dictionary -- a collection of bijective pairs.
	/// (each value is also a key and each key is also a value).
	/// </summary>
	public class BiDictionary<T1, T2> : IEnumerable<Pair<T1, T2>> {

		readonly private Dictionary<T1, T2> _forward;
		readonly private Dictionary<T2, T1> _inverse;

		public ReadOnlyIndexer<T1, T2> Forward { get; }
		public ReadOnlyIndexer<T2, T1> Inverse { get; }

		public int Count => _forward.Count;

		public BiDictionary() {
			_forward = new Dictionary<T1, T2>();
			_inverse = new Dictionary<T2, T1>();
			Forward = new ReadOnlyIndexer<T1, T2>(_forward);
			Inverse = new ReadOnlyIndexer<T2, T1>(_inverse);
		}

		public void Add(T1 t1, T2 t2) {

			if (_forward.ContainsKey(t1) || _inverse.ContainsKey(t2)) {
				throw new System.ArgumentException($"Cannot add a pair of [{t1}, {t2}] as one or both values already exist.");
			}

			_forward.Add(t1, t2);
			_inverse.Add(t2, t1);
		}

		public void AddMany(IEnumerable<KeyValuePair<T1, T2>> pairs) {
			foreach (var pair in pairs) Add(pair.Key, pair.Value); ;
		}

		public void AddManyInverse(IEnumerable<KeyValuePair<T2, T1>> inversedPairs) {
			foreach (var inversePair in inversedPairs) Add(inversePair.Value, inversePair.Key);
		}

		public IEnumerator<Pair<T1, T2>> GetEnumerator() {
			return Forward.Cast<Pair<T1, T2>>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	}


	public class ReadOnlyIndexer<UKey, UValue> : IEnumerable<KeyValuePair<UKey, UValue>> {

		readonly private IDictionary<UKey, UValue> _dictionary;

		public ReadOnlyIndexer(IDictionary<UKey, UValue> dictionary) {
			_dictionary = dictionary;
		}

		public UValue this[UKey key] => _dictionary[key];

		public ICollection<UKey> Keys => _dictionary.Keys;

		public ICollection<UValue> Values => _dictionary.Values;

		public int Count => _dictionary.Count;

		public bool TryGetValue(UKey key, out UValue value) {
			return _dictionary.TryGetValue(key, out value);
		}

		public IEnumerator<KeyValuePair<UKey, UValue>> GetEnumerator() {
			return _dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	}

}
