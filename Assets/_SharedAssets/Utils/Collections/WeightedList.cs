using System;
using System.Collections;
using System.Collections.Generic;
using Rephidock.GeneralUtilities.Collections;

using Random = System.Random;


namespace SharedAssets.Utils.Collections {

	public struct WeightedItem<T> : IEquatable<WeightedItem<T>> {

		/// <summary>The value of the item (the item itself)</summary>
		public T Value { get; set; }

		/// <summary>The weight of the item. Positive or 0.</summary>
		public int Weight { get; set; }

		public WeightedItem(T value, int weight) {
			Value = value;
			Weight = Math.Max(weight, 0);
		}

		public static implicit operator Pair<T, int>(WeightedItem<T> item) {
			return new Pair<T, int>(item.Value, item.Weight);
		}

		public static explicit operator WeightedItem<T>(Pair<T, int> pair) {
			return new WeightedItem<T>(pair.First, pair.Second);
		}

		public override bool Equals(object obj) {
			if (obj == null) return false;
			if (obj is WeightedItem<T>) return Equals((WeightedItem<T>)obj);
			if (obj is Pair<T, int>) return ((Pair<T, int>)obj).Equals((Pair<T, int>)obj);
			return false;
		}

		public bool Equals(WeightedItem<T> other) {
			return Value.Equals(other.Value) && Weight.Equals(other.Weight);
		}

		public override int GetHashCode() => base.GetHashCode();

		public override string ToString() => $"WeightedItem[{Value}, {Weight}]";

	}

	/// <summary>A list of weighted items.</summary>
	public class WeightedList<T> : IEnumerable<WeightedItem<T>>, IList<WeightedItem<T>> {

		#region //// Storage

		readonly List<WeightedItem<T>> items;
		public int WeightsTotal { get; private set; }
		public int Count => items.Count;

		#endregion

		#region //// Creation

		public WeightedList() {
			items = new List<WeightedItem<T>>();
			WeightsTotal = 0;
		}

		public WeightedList(int capacity) {
			items = new List<WeightedItem<T>>(capacity);
			WeightsTotal = 0;
		}

		public WeightedList(IEnumerable<WeightedItem<T>> collection) {
			items = new List<WeightedItem<T>>(collection);
			RecalcWeightTotal();
		}

		private void RecalcWeightTotal() {
			WeightsTotal = 0;
			foreach (var pair in items) {
				WeightsTotal += pair.Weight;
			}
		}

		#endregion

		#region //// List stuff

		public WeightedItem<T> this[int index] {
			get { return items[index]; }
			set {
				WeightsTotal -= items[index].Weight;
				items[index] = value;
				WeightsTotal += items[index].Weight;
			}
		}

		public void Add(WeightedItem<T> item) {
			items.Add(item);
			WeightsTotal += item.Weight;
		}

		public void Add(T value, int weight) {
			Add(new WeightedItem<T>(value, weight));
		}

		public void AddRange(IEnumerable<WeightedItem<T>> items) {
			foreach (var item in items) Add(item);
		}

		public void Clear() {
			items.Clear();
			WeightsTotal = 0;
		}

		public int IndexOf(WeightedItem<T> item) {
			return items.IndexOf(item);
		}

		public bool Remove(WeightedItem<T> item) {
			bool removed = items.Remove(item);
			if (removed) WeightsTotal -= item.Weight;
			return removed;
		}

		public void RemoveAt(int index) {
			WeightsTotal -= this[index].Weight;
			items.RemoveAt(index);
		}

		public bool IsReadOnly => false;

		public bool Contains(WeightedItem<T> item) {
			return items.Contains(item);
		}

		public void Insert(int index, WeightedItem<T> item) {
			items.Insert(index, item);
			WeightsTotal += item.Weight;
		}

		public void CopyTo(WeightedItem<T>[] array, int arrayIndex) {
			items.CopyTo(array, arrayIndex);
		}

		public IEnumerator<WeightedItem<T>> GetEnumerator() => items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

		#region //// Random picker

		public T Pick(Random rng) {

			// Guards
			if (WeightsTotal <= 0 || Count < 1) {
				throw new ArgumentException("Cannot pick items from a weighted storage equivalent to empty");
			}

			// Pick
			int chosenWeight = rng.Next(0, WeightsTotal);
			int curWeight = 0;
			foreach (WeightedItem<T> item in this) {
				curWeight += item.Weight;
				if (curWeight > chosenWeight) return item.Value;
			}

			// [unreachable]
			throw new InvalidOperationException("Something went wrong. This should not be ever thrown.");
		}

		#endregion

	}

}
