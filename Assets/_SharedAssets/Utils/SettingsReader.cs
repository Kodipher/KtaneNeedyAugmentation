

namespace SharedAssets.Utils {

	public interface IVersioned {
		int Version { get; set; }
	}

	/// <typeparam name="TSettings">The settings type and also the filename.</typeparam>
	public static class SettingsReader<TSettings> where TSettings : IVersioned, new() {

		/// <summary>
		/// Reads <typeparamref name="TSettings"/> from its settings file.
		/// Performs checks on the validity and version.
		/// Always returns a new object.
		/// </summary>
		/// <remarks>
		/// Internally uses <see cref="ModConfig{T}.Read"/>.
		/// </remarks>
		public static TSettings ReadSettings() {

			// Read using ModConfig
			ModConfig<TSettings> modconfig = new ModConfig<TSettings>(typeof(TSettings).Name);
			TSettings read = modconfig.Read();

			// If it is invalid -- start anew
			if (!modconfig.SuccessfulRead) {
				modconfig.SuccessfulRead = true;    // force write to work
				TSettings newObject = new TSettings();
				modconfig.Write(newObject);
				return newObject;
			}

			// If settings are outdated -- resave with new version
			int currentVersion = new TSettings().Version;
			if (read.Version < currentVersion) {
				read.Version = currentVersion;
				modconfig.Write(read);
			}

			return read;
		}

	}

}
