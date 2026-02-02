using System.Text;
using System.Text.RegularExpressions;


namespace SharedAssets.Utils {

	public static class StringExtensions {

		static readonly Regex regexContainsEnglishLetters = new Regex(@"[a-zA-Z]");

		/// <summary>Check if a sting contains letters (a through z)</summary>
		public static bool ContainsModernEnglishLetters(this string str) {
			return regexContainsEnglishLetters.IsMatch(str);
		}

		internal static readonly Regex RegexFindColorTags = new Regex(@"<(?<close>\/)?color(?(close)|=(?<color>[^>]*))?>", RegexOptions.IgnoreCase);

		/// <summary>Removes color tags from the string</summary>
		/// <returns>A string without color tags</returns>
		public static string RemoveColorTags(this string str) {

			StringBuilder stringBuilder = new StringBuilder(str.Length);
			int copyStartI = 0;

			// For all individual color tags
			foreach (Match match in RegexFindColorTags.Matches(str)) {
				// Copy until tag
				stringBuilder.Append(str, copyStartI, match.Index - copyStartI);
				// Skip the tag itself
				copyStartI = match.Index + match.Length;
			}

			// Copy remaining chars
			stringBuilder.Append(str, copyStartI, str.Length - copyStartI);

			// Return
			return stringBuilder.ToString();
		}


		/// <summary>No-Break space character</summary>
		public const char Nbsp = '\u00A0';

		/// <summary>
		/// Returns given string with underscores replaced by nbsp (no-break space)
		/// </summary>
		public static string UnderscoreToNoBreakSpace(this string str) {
			return str.Replace('_', Nbsp);
		}

		/// <summary>Returns the number of english vowels in the string.</summary>
		public static int CountEnglishVowels(this string str, bool includeY = false) {

			int count = 0;

			for (int i = 0; i < str.Length; i++) {
				char chr = char.ToUpperInvariant(str[i]);

				if (chr < 'A' || chr > 'Z') continue;
				if (chr == 'A' || chr == 'E' || chr == 'I' || chr == 'O' || chr == 'U') count++;
				if (includeY && chr == 'Y') count++; 
			}

			return count;
		}

	}

}
