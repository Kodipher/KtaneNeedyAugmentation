using System.Collections.Generic;
using Newtonsoft.Json;


namespace TemplateMod {

	/// <summary>
	/// Contains user configuarion of this mod.
	/// Mutable but not global.
	/// </summary>
	public /*record*/ class TemplateSettings : SharedAssets.Utils.IVersioned {

		[JsonProperty(Required = Required.Always, Order = -1)]
		public int Version { get; set; } = 0;

	}

	public static class TemplateSettingsTweaksAnnotations {

		public static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[] {
			
			new Dictionary<string, object>() {
				{ "Filename", $"{nameof(TemplateSettings)}.json" },
				{ "Name", "Template" },
				{ "Listings", new List<Dictionary<string, object>> 
					{
						new Dictionary<string, object> { { "Key", nameof(TemplateSettings.Version) }, { "Type", "Hidden" } },
						
						/*
						new Dictionary<string, object> { { "Text", "Section text goes here if a note is needed" }, { "Type", "Section" } },
						new Dictionary<string, object> { { "Key", "json_property_name" }, { "Text", "Property Name" }, { "Description", "Description goes here." } },
						new Dictionary<string, object> { { "Key", "json_property_name" }, { "Text", "Property Name" } },
						*/
					}
				}
			}

		};

	}

}
