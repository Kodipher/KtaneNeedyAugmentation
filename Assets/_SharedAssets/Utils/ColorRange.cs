using Rephidock.GeneralUtilities.Maths;
using Color = UnityEngine.Color;


namespace SharedAssets.Utils {

	/// <summary>
	/// Holds a range (0..1) of hue, saturation and brightness values.
	/// Can be used with <see cref="System.Random"/> to pick a random color.
	/// </summary>
	public class ColorRange {

		public float MinHue { get; set; }
		public float MaxHue { get; set; }
		public float MinSaturation { get; set; }
		public float MaxSaturation { get; set; }
		public float MinBrightness { get; set; }
		public float MaxBrightness { get; set; }

		public ColorRange(
			float minHue, 
			float maxHue, 
			float minSaturation, 
			float maxSaturation, 
			float minBrightness, 
			float maxBrightness
		) {
			MinHue = minHue;
			MaxHue = maxHue;
			MinSaturation = minSaturation;
			MaxSaturation = maxSaturation;
			MinBrightness = minBrightness;
			MaxBrightness = maxBrightness;
		}

		public Color PickRandom(System.Random rng) {
			return Color.HSVToRGB(
						(float)MoreMath.Lerp(MinHue, MaxHue, rng.NextDouble()),
						(float)MoreMath.Lerp(MinSaturation, MaxSaturation, rng.NextDouble()),
						(float)MoreMath.Lerp(MinBrightness, MaxBrightness, rng.NextDouble())
					);
		}

	}

}
