//#define MODULELOGGER_AUTOPRINT_HELLOWORLD


using System.Collections.Generic;
using UnityEngine;


namespace SharedAssets.Utils {

	/// <summary>
	/// A class that is designed to log stuff in a propper format for the Log File Analyzer.
	/// Also performs instance counting per given name (<see langword="static"/>ly).
	/// Should be created one time per module instance due to counting.
	/// </summary>
	public class ModuleLogger {

		#region /--- Constructor, Tag ---/

		/// <inheritdoc cref="ModuleLogger"/>
		/// <param name="moduleComponent">The module component to grab display name from.</param>
		public ModuleLogger(KMBombModule moduleComponent) : this(moduleComponent.ModuleDisplayName) {
		}

		/// <inheritdoc cref="ModuleLogger"/>
		/// <param name="moduleComponent">The module component to grab display name from.</param>
		public ModuleLogger(KMNeedyModule moduleComponent) : this(moduleComponent.ModuleDisplayName) {
		}

		/// <inheritdoc cref="ModuleLogger"/>
		/// <param name="displayName">Name that appears in the log and that is used to count instances</param>
		public ModuleLogger(string displayName) : this(displayName, CountNext(displayName)) {
		}

		/// <inheritdoc cref="ModuleLogger(string)"/>
		/// <param name="forcedInstanceIndex">
		/// <para>
		/// Forced index in the tag. <see langword="null"/> can be provided to 
		/// skip the number in the tag (e.g. for a service).
		/// </para>
		/// <para>
		/// Bypassed counting.
		/// </para>
		/// </param>
		public ModuleLogger(string displayName, int? forcedInstanceIndex) {

			// Create log tag
			tagId = forcedInstanceIndex;

			if (forcedInstanceIndex.HasValue) {
				tag = $"[{displayName} #{forcedInstanceIndex}]";
			} else {
				tag = $"[{displayName}]";
			}

			// Autoprint something
			#if MODULELOGGER_AUTOPRINT_HELLOWORLD
			LogString("Notice me, Logfile Analyzer!");
			LogLine();
			#endif
		}

		/// <summary>
		/// The numeric id used in the <see cref="tag"/> 
		/// or <see langword="null"/> for no numeric id.
		/// </summary>
		public readonly int? tagId;

		/// <summary>Log tag that is used to identify the module in the logfile.</summary>
		readonly string tag;

		#endregion

		#region /--- (static) Instance counting ---/

		private static readonly object counterLock = new object();

		private static readonly Dictionary<string, int> instanceCounts = new Dictionary<string, int>();

		/// <summary>Force-resets count of a specific name</summary>
		public static void CountReset(string name) {
			lock (counterLock) {
				instanceCounts.Remove(name);
			}
		}

		/// <summary>Starts a new instance counter at 1 or advances existing by one.</summary>
		/// <returns>Newly counted index (new total)</returns>
		public static int CountNext(string name) {
			lock (counterLock) {
				// Known name
				if (instanceCounts.ContainsKey(name)) {
					instanceCounts[name] = instanceCounts[name] + 1;
					return instanceCounts[name];
				}

				// New name
				instanceCounts[name] = 1;
				return instanceCounts[name];
			}
		}

		/// <summary>Returns current count of instances without advancing it</summary>
		/// <returns>Number of counted instances</returns>
		public static int CountCurrent(string name) {
			lock (counterLock) {
				// Known name
				if (instanceCounts.ContainsKey(name)) return instanceCounts[name];

				// New name
				return 0;
			}
		}

		#endregion

		#region /--- Logging ---/

		const char LineChar = '═';
		const int LineLength = 5;

		public void LogString(string str) {
			Debug.Log($"{tag} {str}");
		}

		public void LogStringFormat(string formatString, params object[] args) {
			Debug.LogFormat($"{tag} {formatString}", args);
		}

		public void LogStringError(string str) {
			Debug.LogError($"{tag} {str}");
		}

		public void LogException(System.Exception ex) {
			Debug.LogWarning($"{tag} An exception has occured. See in Filtered Log.");
			Debug.LogException(ex);
		}

		public void LogStrings(IEnumerable<string> strs) {
			foreach (string s in strs) LogString(s);
		}

		public void LogLine() {
			LogString(new string(LineChar, LineLength));
		}

		#endregion

	}

}
