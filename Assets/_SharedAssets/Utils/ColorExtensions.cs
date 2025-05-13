using Color = UnityEngine.Color;


namespace SharedAssets.Utils {

	public static class ColorExtensions {

		/// <summary>Returns a transparent version of the color</summary>
		public static Color Transparent(this Color col) {
			return new Color(col.r, col.g, col.b, 0f);
		}

		/// <summary>
		/// A variant of <see cref="Color.Lerp(Color, Color, float)"/>
		/// that keeps <paramref name="source"/>'s alpha.
		/// </summary>
		public static Color LerpColorKeepSourceAlpha(Color source, Color dest, float t) {
			Color newColor = Color.Lerp(source, dest, t);
			newColor.a = source.a;
			return newColor;
		}

	}

}
