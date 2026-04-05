using System;

using Color = UnityEngine.Color;
using ColorUtility = UnityEngine.ColorUtility;


namespace SharedAssets.Utils.Collections {

	/// <summary>
	/// <para>
	/// Represents a string with an (optional) <see cref="Color"/>.
	/// Unlike <see cref="ColoredString"/>, only stores 1 color for all characters.
	/// </para>
	/// <para>
	/// Immutable.
	/// </para>
	/// </summary>
	public /* record */ class MonoColoredString {

		#region /--- Storage and Creation ---/
		
		public string String { get; /* init; */ }
		
		public Color? Color { get; /* init; */ }

		public MonoColoredString() {
			String = "";
			Color = null;
		}
		
		public MonoColoredString(string str, Color? col = null) {

			if (str == null) throw new ArgumentNullException(nameof(str));
			
			String = str;
			Color = col;
		}

		public MonoColoredString WithColor(Color? col) => new MonoColoredString(this.String, col);
		
		public MonoColoredString WithString(string str) => new MonoColoredString(str, this.Color);
		
		#endregion

		#region /--- Conversions ---/
		
		public override string ToString() => this.String;
		
		/// <summary>
		/// Wraps the <see cref="String"/> in the color tag
		/// with <see cref="Color"/> as the color, is not null.
		/// </summary>
		public string ToRichString() {
			
			if (!this.Color.HasValue) return this.String;

			string colorString = ColorUtility.ToHtmlStringRGBA(this.Color.Value);
			return $"<color=#{colorString}>{this.String}</color>";
		}
		
		public ColoredString ToColoredString() => new ColoredString(this.String, this.Color);
		
		#endregion
		
	}

}
