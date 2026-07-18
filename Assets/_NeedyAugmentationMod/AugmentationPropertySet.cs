using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using Rephidock.GeneralUtilities.Collections;

using TimeSpan = System.TimeSpan;


namespace NeedyAugmentationMod {

	public /*record*/ class AugmentationPropertySet {

		public /*required*/ string ModuleId { get; set; } = "";
			
		public TimeSpan? InitialActivationTime { get; set; } = null;

		public float? CooldownMultiplier { get; set; } = null;
		public float? CooldownAddend { get; set; } = null;

		public int? ActivationLimit { get; set; } = null;
		public int? ActivationLimitAddendPerSolves { get; set; } = null;
		public int? ActivationLimitSolves { get; set; } = null;

		public TimeSpan? StartThresholdSeconds { get; set; } = null;
		public int? StartThresholdModules { get; set; } = null;
			
		public TimeSpan? StopThresholdSeconds { get; set; } = null;
		public int? StopThresholdModules { get; set; } = null;

		public TimeSpan? TerminationThresholdSeconds { get; set; } = null;
		public int? TerminationThresholdModules { get; set; } = null;


		public string ToStringSkipId() {
			
			// Unfortunately, FormattableString is not a thing in netframework35
			
			List<Pair<string, object[]>> properties = new List<Pair<string, object[]>>(capacity: 5);

			if (InitialActivationTime.HasValue) {
				var formatPair = Pair.New("acttime={0}", new object[] { InitialActivationTime.Value.TotalSeconds });
				properties.Add(formatPair);
			}
			
			if (CooldownMultiplier.HasValue && !CooldownAddend.HasValue) {
				var formatPair = Pair.New("cd=*{0}", new object[] { CooldownMultiplier.Value });
				properties.Add(formatPair);
				
			} else if (!CooldownMultiplier.HasValue && CooldownAddend.HasValue) {
				char sign = CooldownAddend.Value >= 0 ? '+' : '-';
				float magnitude = Math.Abs(CooldownAddend.Value);
				
				var formatPair = Pair.New("cd={0}{1}", new object[] { sign, magnitude });
				properties.Add(formatPair);
				
			} else if (CooldownMultiplier.HasValue && CooldownAddend.HasValue) {
				char sign = CooldownAddend.Value >= 0 ? '+' : '-';
				float magnitude = Math.Abs(CooldownAddend.Value);
				
				var formatPair = Pair.New("cd=*{0}{1}{2}", new object[] { CooldownMultiplier.Value, sign, magnitude });
				properties.Add(formatPair);
			}

			if (ActivationLimit.HasValue) {

				if (ActivationLimitAddendPerSolves.HasValue) {
					
					char sign = ActivationLimitAddendPerSolves.Value >= 0 ? '+' : '-';
					int magnitude = Math.Abs(ActivationLimitAddendPerSolves.Value);
					
					if (ActivationLimitSolves.HasValue) {
						var formatPair = Pair.New(
											"acts=*{0}{1}{2}/{3}", 
											new object[] { ActivationLimit.Value, sign, magnitude, ActivationLimitSolves.Value }
										);
						properties.Add(formatPair);
					} else {
						var formatPair = Pair.New("acts=*{0}{1}{2}", new object[] { ActivationLimit.Value, sign, magnitude });
						properties.Add(formatPair);
					}

				} else {
					var formatPair = Pair.New("acts=*{0}", new object[] { ActivationLimit.Value });
					properties.Add(formatPair);
				}
			}

			if (StartThresholdSeconds.HasValue) {
				var formatPair = Pair.New("start={0}s", new object[] { StartThresholdSeconds.Value.TotalSeconds });
				properties.Add(formatPair);
			}
			
			if (StartThresholdModules.HasValue) {
				var formatPair = Pair.New("start={0}m", new object[] { StartThresholdModules.Value });
				properties.Add(formatPair);
			}

			if (StopThresholdSeconds.HasValue) {
				var formatPair = Pair.New("stop={0}s", new object[] { StopThresholdSeconds.Value.TotalSeconds });
				properties.Add(formatPair);
			}
			
			if (StopThresholdModules.HasValue) {
				var formatPair = Pair.New("stop={0}m", new object[] { StopThresholdModules.Value });
				properties.Add(formatPair);
			}
			
			if (TerminationThresholdSeconds.HasValue) {
				var formatPair = Pair.New("term={0}s", new object[] { TerminationThresholdSeconds.Value.TotalSeconds });
				properties.Add(formatPair);
			}
			
			if (TerminationThresholdModules.HasValue) {
				var formatPair = Pair.New("term={0}m", new object[] { TerminationThresholdModules.Value });
				properties.Add(formatPair);
			}

			return properties
					.Select(pair => string.Format(CultureInfo.InvariantCulture, pair.First, pair.Second))
					.JoinString(",");
		}
		
		public override string ToString() {
			return $"{ModuleId}:{ToStringSkipId()}";
		}
			
	}

}