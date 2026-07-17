using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

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
		
		/// <exception cref="FormatException">Incorrect format</exception>
		public static AugmentationConfig ParseConfigOrThrow([NotNull] string configNoTags) {

			string[] propertySets = configNoTags.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < propertySets.Length; i++) {
				propertySets[i] = propertySets[i].Trim();
			}
			
			
			if (propertySets.Length == 0 || (propertySets.Length == 1 && propertySets[0] == "")) {
				throw new FormatException("Configuration is empty.");
			}
			
			List<AugmentationPropertySet> sets = propertySets.Select(ParseModuleAugmentation).ToList();
			
			var config = new AugmentationConfig();
			config.ConfirmProperties(sets);
			return config;
		}
		
		/// <exception cref="FormatException">Incorrect format</exception>
		static AugmentationPropertySet ParseModuleAugmentation([NotNull] string str) {
			
			string[] idAndProperties = str.Split(':');
			
			// ID
			string idTrimmed = idAndProperties[0].Trim();
			
			if (idAndProperties.Length == 1) {
				
				if (idTrimmed == "") throw new FormatException("ID and property set are empty.");

				return new AugmentationPropertySet() { ModuleId = idTrimmed };
			}
			
			if (idAndProperties.Length > 2) {
				throw new FormatException("More than one colon within a property set or property sets are not separated.");
			}

			// Property list
			string trimmedPropertyList = idAndProperties[1].Trim();
			if (trimmedPropertyList == "") return new AugmentationPropertySet() { ModuleId = idTrimmed };
			
			Dictionary<string, string> properties = new Dictionary<string, string>(); 
																	// key is trimmed and in lowercase; value is trimmed
																	
			foreach (var propertyWithExpression in trimmedPropertyList.Split(',')) {
				
				string[] splitByEq = propertyWithExpression.Split('=');
				string propertyNamePreLower = splitByEq[0].Trim();
				string propertyName = splitByEq[0].Trim().ToLower();
				
				if (propertyName == "") throw new FormatException($"Empty/Unnamed property found for \"{idTrimmed}\".");
				if (properties.ContainsKey(propertyName)) throw new FormatException($"Property \"{propertyName}\" is duplicated for \"{idTrimmed}\".");
				if (splitByEq.Length == 1) throw new FormatException($"Property \"{propertyNamePreLower}\" for \"{idTrimmed}\" has no expression.");
				if (splitByEq.Length > 2) throw new FormatException($"More than one expression for property \"{propertyNamePreLower}\" for \"{idTrimmed}\".");
				
				properties.Add(propertyName, splitByEq[1].Trim());
			}

			// Properties
			var result = new AugmentationPropertySet() { ModuleId = idTrimmed };

			foreach (var pair in properties) {
				ParseAndAssignSingleExpression(result, pair.Key, pair.Value);
			}
			
			return result;
		}

		
		const string NumberPattern = @"[\+\-]?[.0-9]*"; // let int.Parse throw in case of decimal points
		
		static readonly Regex secondsPropertyRegex = new Regex($@"^({NumberPattern})\s*s?$", RegexOptions.Singleline);
		static readonly Regex secondsOrModulesPropertyRegex = new Regex($@"^({NumberPattern})\s*(s|m)$", RegexOptions.Singleline);
		static readonly Regex countdownPropertyRegex = new Regex($@"^(?:\*\s*({NumberPattern})\s*)?(?:(\+|-)\s*({NumberPattern}))?$", RegexOptions.Singleline);
		static readonly Regex activationsPropertyRegex = new Regex($@"^({NumberPattern})\s*(?:(\+|-)\s*({NumberPattern})\s*(?:\/\s*({NumberPattern})\s*)?)?$", RegexOptions.Singleline);
		
		/// <param name="currentSet">The set to assign the property to.</param>
		/// <param name="property">Property key to assign. Lowercase and trimmed.</param>
		/// <param name="expression">Expression of the property. Trimmed.</param>
		/// <exception cref="FormatException">Incorrect format or invalid property</exception>
		static void ParseAndAssignSingleExpression(AugmentationPropertySet currentSet, string property, string expression) {

			string moduleId = currentSet.ModuleId;
			
			switch (property) {

				case "acttime": {
					
					var match = secondsPropertyRegex.Match(expression);
					if (!match.Success) throw new FormatException($"Invalid expression for \"acttime\" for \"{moduleId}\"");
					
					float seconds = ConfigParseFloat(match.Groups[1].ToString(), "acttime", moduleId);
					currentSet.InitialActivationTime = TimeSpan.FromSeconds(seconds);
				} return;
					
				
				case "cd": {
					
					var match = countdownPropertyRegex.Match(expression);
					
					if (!match.Success || (!match.Groups[1].Success && !match.Groups[3].Success)) {
						throw new FormatException($"Invalid expression for \"cd\" for \"{moduleId}\"");
					}
					
					if (match.Groups[1].Success) {
						// Multiplier present
						currentSet.CooldownMultiplier = ConfigParseFloat(match.Groups[1].ToString(), "cd", moduleId);
					}

					if (match.Groups[3].Success) {
						
						// Addend of subtrahend present
						char sign = match.Groups[2].ToString()[0];
						float addend = ConfigParseFloat(match.Groups[3].ToString(), "cd", moduleId);
						
						if (sign == '-') addend = -addend;
						currentSet.CooldownAddend = addend;
					}
					
				} return;
					
				
				case "acts": {
					
					var match = activationsPropertyRegex.Match(expression);
					if (!match.Success) throw new FormatException($"Invalid expression for \"acts\" for \"{moduleId}\"");

					currentSet.ActivationLimit = ConfigParseInt(match.Groups[1].ToString(), "acts", moduleId);
	
					if (match.Groups[3].Success) {
						
						// pre present
						char sign = match.Groups[2].ToString()[0];
						int addend = ConfigParseInt(match.Groups[3].ToString(), "acts", moduleId);
						
						if (sign == '-') addend = -addend;
						currentSet.ActivationLimitAddendPerSolves = addend;
						
						// solves present
						if (match.Groups[4].Success) {
							int solves = ConfigParseInt(match.Groups[4].ToString(), "acts", moduleId);;
							currentSet.ActivationLimitSolves = solves;
						}
					}
					
				} return;
					
				
				case "start": {
					
					var match = secondsOrModulesPropertyRegex.Match(expression);
					if (!match.Success) throw new FormatException($"Invalid expression for \"start\" for \"{moduleId}\"");
					
					char suffix = match.Groups[2].ToString()[0];

					switch (suffix) {
						
						case 's': 
							float seconds = ConfigParseFloat(match.Groups[1].ToString(), "start", moduleId);
							currentSet.StartThresholdSeconds = TimeSpan.FromSeconds(seconds);
							break;
						
						case 'm':
							int modules = ConfigParseInt(match.Groups[1].ToString(), "start", moduleId);
							currentSet.StartThresholdModules = modules;
							break;
						
						default:
							throw new FormatException($"Invalid expression for \"start\" for \"{moduleId}\"");
					}
					
				} return;
					
				
				case "term": {
					
					var match = secondsOrModulesPropertyRegex.Match(expression);
					if (!match.Success) throw new FormatException($"Invalid expression for \"term\" for \"{moduleId}\"");
					
					char suffix = match.Groups[2].ToString()[0];

					switch (suffix) {
						
						case 's': 
							float seconds = ConfigParseFloat(match.Groups[1].ToString(), "term", moduleId);
							currentSet.TerminationThresholdSeconds = TimeSpan.FromSeconds(seconds);
							break;
						
						case 'm':
							int modules = ConfigParseInt(match.Groups[1].ToString(), "term", moduleId);
							currentSet.TerminationThresholdModules = modules;
							break;
						
						default:
							throw new FormatException($"Invalid expression for \"term\" for \"{moduleId}\"");
					}
					
				} return;
					
				
				case "stop": {
					
					var match = secondsOrModulesPropertyRegex.Match(expression);
					if (!match.Success) throw new FormatException($"Invalid expression for \"stop\" for \"{moduleId}\"");
					
					char suffix = match.Groups[2].ToString()[0];

					switch (suffix) {
						
						case 's': 
							float seconds = ConfigParseFloat(match.Groups[1].ToString(), "stop", moduleId);
							currentSet.StopThresholdSeconds = TimeSpan.FromSeconds(seconds);
							break;
						
						case 'm':
							int modules = ConfigParseInt(match.Groups[1].ToString(), "stop", moduleId);
							currentSet.StopThresholdModules = modules;
							break;
						
						default:
							throw new FormatException($"Invalid expression for \"stop\" for \"{moduleId}\"");
					}
					
				} return;
					
				
				default:
					throw new FormatException($"\"{property}\" is not a valid property for \"{moduleId}\".");
			}
			
			/*[unreachable]*/
			throw new InvalidOperationException();
		}

		/// <exception cref="FormatException">Incorrect format</exception>
		static float ConfigParseFloat(string s, string property, string moduleId) {
			try {
				return float.Parse(s, CultureInfo.InvariantCulture);
			} catch (FormatException ex) {
				throw new FormatException($"Invalid numeric format in \"{property}\" for \"{moduleId}\".", ex);
			}
		}
		
		
		/// <exception cref="FormatException">Incorrect format</exception>
		static int ConfigParseInt(string s, string property, string moduleId) {
			try {
				return int.Parse(s, CultureInfo.InvariantCulture);
			} catch (FormatException ex) {
				throw new FormatException($"Invalid numeric format in \"{property}\" for \"{moduleId}\".", ex);
			}
		} 
		
		#endregion

	}

}