using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public class AugmentationConfig {

		public const string WildcardId = "*";

		public ReadOnlyCollection<AugmentationPropertySet> PropertySets { get; private set; } =
										new ReadOnlyCollection<AugmentationPropertySet>(new AugmentationPropertySet[0]);
		
		readonly Dictionary<string, List<AugmentationPropertySet>> propertiesById = new Dictionary<string, List<AugmentationPropertySet>>();
		readonly Dictionary<string, int> nextIndexToAssignById = new Dictionary<string, int>();
		
		void ConfirmProperties(IList<AugmentationPropertySet> propertySets) {
			
			PropertySets = new ReadOnlyCollection<AugmentationPropertySet>(propertySets);

			foreach (var propertySet in propertySets) {
				
				// Dupe id
				List<AugmentationPropertySet> dupe;
				if (propertiesById.TryGetValue(propertySet.ModuleId, out dupe)) {
					dupe.Add(propertySet);
					continue;
				}
				
				// New id
				propertiesById.Add(propertySet.ModuleId, new List<AugmentationPropertySet>());
				nextIndexToAssignById.Add(propertySet.ModuleId, 0);
				
			}
		}
		
		[CanBeNull] public AugmentationPropertySet AssignPropertySet(string moduleId) {
			
			if (moduleId == "whiteModule") return null; // Black and White are linked; Black has authority.
			
			List<AugmentationPropertySet> dupes;
			if (propertiesById.TryGetValue(moduleId, out dupes)) {
				int index = nextIndexToAssignById[moduleId];
				nextIndexToAssignById[moduleId] = (index + 1) % dupes.Count;
				return dupes[index];
			}
			
			if (moduleId != WildcardId) {
				return AssignPropertySet(WildcardId);
			}
			
			return null;
		}

		#region /--- Parsing ---/

		static readonly Regex singleLineConfigRegex = new Regex(@"\[NeedyAugmentation\](.*)$", RegexOptions.Multiline);
		static readonly Regex multilineConfigRegex = new Regex(@"\[NeedyAugmentation\](.*)\[\/NeedyAugmentation\]", RegexOptions.Singleline);
		
		/// <summary>
		/// Given a description, returns the config, without the tags,
		/// or null if it does not exist.
		/// </summary>
		[CanBeNull] public static string ExtractConfigFromDescription(string description) {

			Match multilineMatch = multilineConfigRegex.Match(description);
			if (multilineMatch.Success) return multilineMatch.Groups[1].ToString().Trim();

			Match singleLineMatch = singleLineConfigRegex.Match(description);
			if (singleLineMatch.Success) return singleLineMatch.Groups[1].ToString().Trim();

			return null;
		}
		
	}

}