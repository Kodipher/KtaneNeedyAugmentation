using System.Collections.Generic;
using System.Linq;
using KModkit;


namespace SharedAssets.Utils {

	public static class KMExtensions {

		static readonly char[] vowels = new char[] { 'A', 'E', 'I', 'O', 'U' };

		/// <summary>Returns <see langword="true"/> if the bomb's serial number contains a vowel</summary>
		public static bool SerialContainsVowel(this KMBombInfo bombInfo, bool includeY = false) {
			IEnumerable<char> letters = bombInfo.GetSerialNumberLetters().Select(char.ToUpperInvariant);
			if (includeY && letters.Contains('Y')) return true;
			return letters.Any(x => vowels.Contains(x));
		}

		/// <summary>Returns digits displayed on the bomb's timer as digits</summary>
		public static IEnumerable<int> GetTimerDisplayDigits(this KMBombInfo bombInfo) {
			foreach (char displayChar in bombInfo.GetFormattedTime()) {
				if (displayChar >= '0' && displayChar <= '9') {
					yield return displayChar - '0';
				}
			}
		}

		/// <summary>Returns seconds digits displayed on the bomb's timer</summary>
		public static int GetTimerSecondsDisplay(this KMBombInfo bombInfo) {

			string formattedTime = bombInfo.GetFormattedTime();
			int dotPosition = formattedTime.IndexOf('.');
			int colonPosition = formattedTime.LastIndexOf(':');

			if (dotPosition == -1) {
				return int.Parse(formattedTime.Substring(colonPosition + 1, 2));
			}

			return int.Parse(formattedTime.Substring(dotPosition - 2, 2));
		}

		/// <summary>
		/// Returns centiseconds digits displayed on the bomb's timer,
		/// or <see langword="null"/> if they are not dispalyed.
		/// </summary>
		public static int? GetTimerCentisecondsDisplay(this KMBombInfo bombInfo) {

			string formattedTime = bombInfo.GetFormattedTime();
			int dotPosition = formattedTime.IndexOf('.');

			if (dotPosition == -1) return null;

			return int.Parse(formattedTime.Substring(dotPosition + 1, 2));
		}

		/// <summary>
		/// Equivalent of <see cref="KMAudio.PlaySoundAtTransformWithRef"/>
		/// but with the loop option disabled.
		/// </summary>
		public static KMAudio.KMAudioRef PlaySoundAtTransformWithRefNoLoop(
			this KMAudio audio, 
			string name,
			UnityEngine.Transform transform
		) {
			if (audio.HandlePlaySoundAtTransformWithRef != null) {
				return audio.HandlePlaySoundAtTransformWithRef(name, transform, false);
			}

			return null;
		}

	}

}