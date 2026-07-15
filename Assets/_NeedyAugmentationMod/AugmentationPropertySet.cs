namespace NeedyAugmentationMod {

	public /*record*/ class AugmentationPropertySet {

		public /*required*/ string ModuleId { get; set; } = "";
			
		public float? InitialActivationTime { get; set; } = null;

		public float? CooldownMultiplier { get; set; } = null;
		public float? CooldownAddend { get; set; } = null;

		public int? ActivationLimit { get; set; } = null;
		public int? ActivationLimitAddendPerSolves { get; set; } = null;
		public int? ActivationLimitSolves { get; set; } = null;

		public float? StartThresholdSeconds { get; set; } = null;
		public int? StartThresholdModules { get; set; } = null;
			
		public float? StopThresholdSeconds { get; set; } = null;
		public int? StopThresholdModules { get; set; } = null;

		public float? TerminationThresholdSeconds { get; set; } = null;
		public int? TerminationThresholdModules { get; set; } = null;
			
	}

}