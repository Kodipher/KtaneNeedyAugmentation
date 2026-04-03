using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Coroutines;

using SharedAssets.Utils;

using TimeSpan = System.TimeSpan;


namespace TemplateMod {

	[RequireComponent(typeof(KMBombModule))]
	[RequireComponent(typeof(KMBombInfo))]
	[RequireComponent(typeof(KMAudio))]
	public class TemplateModule : MonoBehaviour {

		#region /--- State ---/

		private enum State {
			Initiating,
			Solved
		}

		State state = State.Initiating;

		// Expose log id for LFA at module instance (required by Tweaks)
		public int LogFileAnalyzerId => this.logger.tagId ?? 0;

		#endregion

		#region /--- Components and parts ---/

		// KM
		internal KMBombModule kmModule;
		internal KMBombInfo kmBomb;
		internal KMAudio kmAudio;

		// Created 
		internal System.Random rng;
		internal ModuleLogger logger;
		internal AnimationRunner animationRunner;
		internal TemplateSettings settings;

		private void PrepareComponents() {

			// KM
			kmModule = GetComponent<KMBombModule>();
			kmBomb = GetComponent<KMBombInfo>();
			kmAudio = GetComponent<KMAudio>();

			kmModule.OnActivate += OnActivate;

			// Misc. common
			logger = new ModuleLogger(kmModule);
			rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
			animationRunner = new AnimationRunner();
			settings = SettingsReader<TemplateSettings>.ReadSettings();

		}

		#endregion

		#region /--- Events ---/

		void Start() {

			// Init
			PrepareComponents();
		}

		void OnActivate() {

		}

		void Update() {
			try {
				animationRunner?.Update(TimeSpan.FromSeconds(Time.deltaTime));
			} catch (System.Exception ex) {
				// ReSharper disable Unity.PerformanceCriticalCodeInvocation
				logger.LogString("Exception from AnimationRunner.");
				logger.LogException(ex);
				// ReSharper restore Unity.PerformanceCriticalCodeInvocation
				animationRunner?.Clear();
				throw;
			}
		}

		void OnDestroy() {
			animationRunner?.Dispose();
		}
		
		public void TwitchHandleForcedSolve() {
			logger.LogString("Forced solve command received. Ending routines.");
			animationRunner.Clear();
			
			state = State.Solved;
			kmModule.HandlePass();
			logger.LogString("Module solved.");
		}

		#endregion

		#region /--- Routines ---/

		IEnumerable<CoroutineYield> TemplateRoutine() {
			yield break;
		}

		#endregion

	}

}
