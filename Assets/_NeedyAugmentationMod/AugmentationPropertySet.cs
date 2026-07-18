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
			
			List<FormattableString> properties = new List<FormattableString>(capacity: 5);

			if (InitialActivationTime.HasValue) {
				properties.Add($"acttime={InitialActivationTime.Value.TotalSeconds}");
			}
			
			if (CooldownMultiplier.HasValue && !CooldownAddend.HasValue) {
				properties.Add($"cd=*{CooldownMultiplier.Value}");
			} else if (!CooldownMultiplier.HasValue && CooldownAddend.HasValue) {
				char sign = CooldownAddend.Value >= 0 ? '+' : '-';
				float magnitude = Math.Abs(CooldownAddend.Value);
				properties.Add($"cd={sign}{magnitude}");
			} else if (CooldownMultiplier.HasValue && CooldownAddend.HasValue) {
				char sign = CooldownAddend.Value >= 0 ? '+' : '-';
				float magnitude = Math.Abs(CooldownAddend.Value);
				properties.Add($"cd=*{CooldownMultiplier.Value}{sign}{magnitude}");
			}

			if (ActivationLimit.HasValue) {

				if (ActivationLimitAddendPerSolves.HasValue) {
					
					char sign = ActivationLimitAddendPerSolves.Value >= 0 ? '+' : '-';
					int magnitude = Math.Abs(ActivationLimitAddendPerSolves.Value);
					
					if (ActivationLimitSolves.HasValue) {
						properties.Add($"acts=*{ActivationLimit.Value}{sign}{magnitude}/{ActivationLimitSolves.Value}");
					} else {
						properties.Add($"acts=*{ActivationLimit.Value}{sign}{magnitude}");
					}

				} else {
					properties.Add($"acts=*{ActivationLimit.Value}");
				}
			}

			if (StartThresholdSeconds.HasValue) {
				properties.Add($"start={StartThresholdSeconds.Value.TotalSeconds}s");
			}
			
			if (StartThresholdModules.HasValue) {
				properties.Add($"start={StartThresholdModules.Value}m");
			}

			if (StopThresholdSeconds.HasValue) {
				properties.Add($"stop={StopThresholdSeconds.Value.TotalSeconds}s");
			}
			
			if (StopThresholdModules.HasValue) {
				properties.Add($"stop={StopThresholdModules.Value}m");
			}
			
			if (TerminationThresholdSeconds.HasValue) {
				properties.Add($"term={TerminationThresholdSeconds.Value.TotalSeconds}s");
			}
			
			if (TerminationThresholdModules.HasValue) {
				properties.Add($"term={TerminationThresholdModules.Value}m");
			}

			return properties
					.Select(formattableString => formattableString.ToString(CultureInfo.InvariantCulture))
					.JoinString(",");
		}
		
		public override string ToString() {
			return $"{ModuleId}:{ToStringSkipId()}";
		}
			
	}

}