using UnityEngine;
using System;
using System.Collections.Generic;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	/// <summary>
	/// <para>
	/// Handles activation and deactivation of the needy it is attached to.
	/// </para>
	/// <para>
	/// A lot of logic is left to the original method,
	/// with harmony patches querying the activator.
	/// </para>
	/// <para>
	/// <see cref="CurrentState"/> overrules the needy's state, unless
	/// it is <see cref="ActivatorState.NormalBehavior"/>.
	/// (It is a bit of a mess, setting Needy's internal state has unintended effects)
	/// </para>
	/// </summary>
	public class AugmentedActivatorComponent : MonoBehaviour {

		#region /--- Global Instance Cache ---/

		static readonly object cacheDictLock = new object();

		// Keys: object ids, the components are attached to
		static readonly Dictionary<int, WeakReference> instanceCache = new Dictionary<int, WeakReference>();

		public static void ClearDeadCacheReferences() {
			lock (cacheDictLock) {

				List<int> keysToRemove = new List<int>(); // cannot mutate during iteration
				
				foreach (var pair in instanceCache) {
					
					var maybeComponent = pair.Value.Target as AugmentedActivatorComponent;
					if (maybeComponent == null) {
						keysToRemove.Add(pair.Key);
					}
				}

				foreach (var key in keysToRemove) {
					instanceCache.Remove(key);
				}
				
			}
		}
		
		[ContractAnnotation("=> true, component: notnull; => false, component: null")]
		public static bool TryGetCached(int targetObjectId, out AugmentedActivatorComponent component) {
			lock (cacheDictLock) {
				
				WeakReference weakRef;
				if (instanceCache.TryGetValue(targetObjectId, out weakRef)) {
					
					var maybeComponent = weakRef.Target as AugmentedActivatorComponent;
					if (maybeComponent == null) {
						// Garbage collected
						instanceCache.Remove(targetObjectId);
						component = null;
						return false;
					}

					// Exists
					component = maybeComponent;
					return true;
				}
				
				// does not exist
				component = null;
				return false;
			}
		}

		public static void AddExistingToCache(int targetObjectId, AugmentedActivatorComponent component) {
			lock (cacheDictLock) {
				instanceCache[targetObjectId] = new WeakReference(component);
			}
		}
		
		public static AugmentedActivatorComponent CreateForAndCache(
			IntegrationHelper.NeedyComponentProxy needyProxy, 
			AugmentationPropertySet augmentation
		) {
			lock (cacheDictLock) {

				var gameObject = needyProxy.NeedyComponent.gameObject;
				
				var component = gameObject.AddComponent<AugmentedActivatorComponent>();
				component.Settings = augmentation;
				component.NeedyComponent = needyProxy;
				
				int id = gameObject.GetInstanceID();
				instanceCache[id] = new WeakReference(component);
				
				return component;
			}
		}
		
		#endregion

		#region /--- Settings and Math ---/
		
		public AugmentationPropertySet Settings { get; private set; }
		
		int? CalculateActivationLimitForSolves(int solves) {

			int? settingBase = Settings.ActivationLimit;
			int? settingPer = Settings.ActivationLimitAddendPerSolves;
			int? settingSolves = Settings.ActivationLimitSolves;
			
			if (!settingBase.HasValue) return null; // inf
			if (!settingPer.HasValue) return settingBase; // base
			if (!settingSolves.HasValue) return settingBase.Value + solves * settingPer.Value; // base + perSolve

			if (settingSolves.Value == 0) {
				if (settingPer.Value == 0) return settingBase; // base+0 (assumes 0/0 is 0)
				if (settingPer.Value < 0) return 0; // base-inf (assumes -x/0 is -inf)
				return null; // base+inf (assumes x/0 is inf)
			}
			
			return settingBase.Value + (solves / settingSolves.Value) * settingPer.Value; // base + per/solves
		}

		bool IsActivationLimitRisingWithSolves() {
			
			int? settingBase = Settings.ActivationLimit;
			int? settingPer = Settings.ActivationLimitAddendPerSolves;
			int? settingSolves = Settings.ActivationLimitSolves;

			if (!settingBase.HasValue) return false;
			if (!settingPer.HasValue) return false;
			if (!settingSolves.HasValue) return settingPer > 0;

			if (settingPer.Value == 0) return false; 
			if (settingSolves.Value == 0) return false; 
			
			return Math.Sign(settingPer.Value) == Math.Sign(settingSolves.Value);
		}

		/// <returns>seconds</returns>
		float ModifyCooldown(float cooldownSeconds) {
			float result = cooldownSeconds;
			if (Settings.CooldownMultiplier.HasValue) result *= Settings.CooldownMultiplier.Value;
			if (Settings.CooldownAddend.HasValue) result += Settings.CooldownAddend.Value;
			if (result <= 0) return 0;
			return result;
		}
		
		#endregion
		
		#region /--- State ---/

		public enum ActivatorState {
			Initiating,
			WaitingForStartModules,
			WaitingForStartTime,
			WaitingForActivationInterruptable,
			NormalBehavior,
			WaitingForSolve,
			Stopped,
			Terminated
		}

		public ActivatorState CurrentState { get; private set; } = ActivatorState.Initiating;
		
		TimeSpan startDelayLeft = TimeSpan.Zero;
		TimeSpan initialActivationDelayLeft = TimeSpan.Zero;

		public int TimesActivated { get; private set; } = 0;
		
		/// <remarks>Is updates to be in line with <see cref="modulesSolved"/>.</remarks>
		public int? ActivationLimit { get; private set; } = null;

		public bool ActivationLimitRisingWithSolves { get; private set; } = false;
		
		/// <summary>Should not be set directly. Use <see cref="UpdateModulesSolvedAndActivationLimit"/>.</summary>
		int modulesSolved = -1;

		int modulesSolvableTotal = -1;
		
		void UpdateModulesSolvedAndActivationLimit() {
			int newSolvedModules = NeedyComponent.BombCountSolvedComponents();
			if (newSolvedModules == modulesSolved) return;
			
			modulesSolved = newSolvedModules;
			ActivationLimit = CalculateActivationLimitForSolves(modulesSolved);
		}
		
		public bool IsInStoppedState() {
			return CurrentState == ActivatorState.Stopped || CurrentState == ActivatorState.Terminated;
		}
		
		#endregion
		
		public IntegrationHelper.NeedyComponentProxy NeedyComponent { get; private set; }
		public IntegrationHelper.TimerComponentProxy TimerComponent { get; private set; }

		public void Start() {
			var needyGameObject = NeedyComponent.NeedyComponent.gameObject;
			TimerComponent = IntegrationHelper.TimerComponentProxy.CreateFromComponentOnTheSameBomb(needyGameObject);

			ActivationLimitRisingWithSolves = IsActivationLimitRisingWithSolves();
			modulesSolvableTotal = NeedyComponent.BombCountSolvableComponents();
		}

	}
	
}