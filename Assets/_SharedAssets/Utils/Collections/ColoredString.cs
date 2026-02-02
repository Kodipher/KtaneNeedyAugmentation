using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rephidock.GeneralUtilities.Collections;

using Color = UnityEngine.Color;
using ColorUtility = UnityEngine.ColorUtility;


namespace SharedAssets.Utils.Collections {

	/// <summary>
	/// A string with a color assigned to each character.
	/// A color can be <see langword="null"/>, meaning that character 
	/// would be outside of any color tags when converted to a unity rich text string.
	/// Is mutable.
	/// </summary>
	public class ColoredString : IEnumerable<Pair<char, Color?>>, IEquatable<ColoredString>, ICloneable {

		#region //// Storage

		protected readonly char[] text;
		protected readonly Color?[] colors;

		public char[] Text => text;
		public Color?[] Colors => colors;

		public int Length => text.Length;

		/// <summary>
		/// Fills colored string with a single color.
		/// </summary>
		/// <param name="color">Color to fill the string with</param>
		/// <param name="ignoreAlpha">Set to true to ignore alpha of the given <paramref name="color"/></param>
		/// <param name="nullAlpha">If <paramref name="ignoreAlpha"/> is set, this will be used as alpha of the null source color.</param>
		public void FillColor(Color? color, bool ignoreAlpha = false, float nullAlpha = 1f) {

			if (ignoreAlpha && color.HasValue) {

				// Fill non-null color with ignoring alpha
				for (int i = 0; i < Length; i++) {

					Color cValue = color.Value;

					// Get old alpha
					float oldAlpha = colors[i].HasValue ? colors[i].Value.a : nullAlpha;
					
					// Replace color
					colors[i] = new Color(cValue.r, cValue.g, cValue.b, oldAlpha);
				}

				return;
			}

			// Regular fill
			for (int i = 0; i < Length; i++) {
				colors[i] = color;
			}
		}

		/// <summary>Replaces all null-colors with a color</summary>
		public void ReplaceNullColor(Color color) {
			for (int i = 0; i < Length; i++) {
				if (!colors[i].HasValue) colors[i] = color;
			}
		}

		#endregion

		#region //// Creation

		/// <summary>Creates an empty colored string.</summary>
		public ColoredString() {
			text = new char[0];
			colors = new Color?[0];
		}

		/// <summary>Creates an empty colored of specified length filled with nulls.</summary>
		public ColoredString(int length) {
			text = new char[length];
			colors = new Color?[length];
		}

		/// <summary>Creates a colored string from text.</summary>
		/// <param name="str">Initial text</param>
		public ColoredString(string str) {
			text = str.ToCharArray();
			colors = new Color?[str.Length];
		}

		/// <summary>Creates a colored string with text and a single color.</summary>
		/// <param name="str">Initial text</param>
		/// <param name="color">Default color to give to all characters</param>
		public ColoredString(string str, Color? color) : this(str) {
			for (int i = 0; i < Length; i++) {
				colors[i] = color;
			}
		}

		/// <summary>Creates a copy of a colored string</summary>
		public ColoredString Clone() {
			var clone = new ColoredString(Length);
			text.CopyTo(clone.text, 0);
			colors.CopyTo(clone.colors, 0);
			return clone;
		}

		object ICloneable.Clone() => Clone();

		/// <summary>
		/// Turns a rich string into a colored string.
		/// Only valid color tags are taken into account.
		/// Default color is null.
		/// Upon incorrect tag placement no colors are imbued.
		/// </summary>
		/// <param name="str">source rich text string</param>
		/// <returns>A new colored string</returns>
		public static ColoredString FromRichString(string str) {

			// Null guard
			if (null == str) return null;

			// Prepare
			ColoredString ret = new ColoredString(str.RemoveColorTags(), null);

			int strChars = 0;
			int retChars = 0;
			Stack<Color?> colorStack = new Stack<Color?>();

			// Get all color changes
			foreach (Match match in StringExtensions.RegexFindColorTags.Matches(str)) {

				// Fill until tag
				for (int i = match.Index - strChars; i > 0; i--) {
					ret.colors[retChars] = colorStack.Count > 0 ? colorStack.Peek() : null;
					retChars++;
				}
				strChars = match.Index + match.Length;


				// Process tag

				// Close tag (`</color>`)
				if (match.Groups["close"].Success) {
					if (colorStack.Count > 0) colorStack.Pop(); //pop color
					else return new ColoredString(str, null); //invalid tag placement => bail
				}

				// Open tag (`</color=X>`)
				else if (match.Groups["color"].Success) {
					//push color or null
					Color probe;
					if (ColorUtility.TryParseHtmlString(match.Groups["color"].Value, out probe)) {
						colorStack.Push(probe);
					} else {
						colorStack.Push(null);
					}
				}

				// Empty open tag (`<color>`)
				else {
					//push white
					colorStack.Push(Color.white);
				}
			}

			// After all tags
			if (colorStack.Count > 0) return new ColoredString(str, null); //invalid tag placement => bail

			// Copy remaining chars
			for (/* [nop] */; retChars < ret.Length; retChars++) {
				ret.colors[retChars] = null;
			}

			return ret;
		}

		#endregion

		#region //// Getters and interfaces

		public override string ToString() => new string(text);

		/// <summary>
		/// Converts a colored string into a string with color tags.
		/// </summary>
		/// <returns>String with color tags</returns>
		public static string ToRichString(char[] text, Color?[] colors) {

			// Consts
			const string openTag = "<color=#{0}>";
			const string closeTag = "</color>";

			// Building a string
			Color? curCol = null;
			StringBuilder stringBuilder = new StringBuilder();

			for (int i = 0; i < text.Length; i++) {

				// Add color
				if (colors[i] != curCol) {

					// Close old color
					if (curCol.HasValue) {
						stringBuilder.Append(closeTag);
					}

					// Open new color
					curCol = colors[i];
					if (curCol.HasValue) {
						string tagStr = string.Format(openTag, ColorUtility.ToHtmlStringRGBA(curCol.Value));
						stringBuilder.Append(tagStr);
					}
				}

				// Add character
				stringBuilder.Append(text[i]);
			}

			// Close final color
			if (curCol.HasValue) stringBuilder.Append(closeTag);

			// Return
			return stringBuilder.ToString();
		}

		/// <inheritdoc cref="ToRichString(char[], Color?[])"/>
		public string ToRichString() => ToRichString(text, colors);

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			// Null check
			if (obj == null) return false;

			// Compare if type matches
			if (obj is ColoredString) return Equals((ColoredString)obj);
			return false;
		}

		/// <inheritdoc/>
		public bool Equals(ColoredString other) {
			// Null check
			if (other == null) return false;
			// Compare
			return Enumerable.SequenceEqual(text, other.text) && Enumerable.SequenceEqual(colors, other.colors);
		}

		public override int GetHashCode() => base.GetHashCode();

		public IEnumerator<Pair<char, Color?>> GetEnumerator() {
			return text.Zip(colors).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

	}

}
