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

		#region //// State

		private enum State {
		}

		State state;

		#endregion

		#region //// Components and parts

		// Created 
		internal KMBombModule kmModule;
		internal KMBombInfo kmBomb;
		internal KMAudio kmAudio;

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

		#region //// Events

		void Start() {

			// Init
			PrepareComponents();
		}

		void OnActivate() {

		}

		void Update() {
			animationRunner?.Update(TimeSpan.FromSeconds(Time.deltaTime));
		}

		void OnDestroy() {
			animationRunner?.Dispose();
		}

		#endregion

		#region //// Routines

		IEnumerable<CoroutineYield> TemplateRoutine() {
			yield break;
		}

		#endregion

	}

}
