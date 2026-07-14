using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Rephidock.GeneralUtilities.Collections;
using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Coroutines;
using Rephidock.GeneralUtilities.Randomness;

using SharedAssets.Utils;

using TimeSpan = System.TimeSpan;


namespace NeedyAugmentationMod {

	[RequireComponent(typeof(KMNeedyModule))]
	[RequireComponent(typeof(KMBombInfo))]
	[RequireComponent(typeof(KMAudio))]
	public class NeedyAugmentationModule : MonoBehaviour {

		#region /--- State ---/

		bool isNeedyRunning = false;
		
		// Expose log id for LFA at module instance (required by Tweaks)
		public int LogFileAnalyzerId => this.logger.tagId ?? 0;
		
		#endregion
		
		#region /--- Components and parts ---/

		// KM
		internal KMNeedyModule kmNeedyModule;
		internal KMBombInfo kmBomb;
		internal KMAudio kmAudio;
		
		// Module parts
		internal Transform refillButton;
		internal KMSelectable refillButtonSelectable;

		const int DisplayHeightCharacters = 9;
		internal VerticalDisplay verticalDisplay; // created
		
		// Misc. Created
		internal System.Random rng;
		internal ModuleLogger logger;
		internal AnimationRunner animationRunner;

		private void PrepareComponents() {

			// KM
			kmNeedyModule = GetComponent<KMNeedyModule>();
			kmBomb = GetComponent<KMBombInfo>();
			kmAudio = GetComponent<KMAudio>();

			kmNeedyModule.OnActivate += OnActivate;
			kmNeedyModule.OnNeedyActivation += OnNeedyActivation;
			kmNeedyModule.OnNeedyDeactivation += OnNeedyTerminated; // because that is how the proxy works
			kmNeedyModule.OnTimerExpired += OnNeedyTimerExpired;
			
			// Misc. common
			logger = new ModuleLogger(kmNeedyModule);
			rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
			animationRunner = new AnimationRunner();
			
			// Button
			refillButton = transform.Find("objectScaler/button");
			
			refillButtonSelectable = refillButton.GetComponent<KMSelectable>();
			refillButtonSelectable.OnInteract += () => { OnButtonPress(); return false; };
			
			// Display
			var displayText = transform.Find("objectScaler/display/text").GetComponent<TextMesh>();

			/*
			var lightsTransform = transform.Find("objectScaler/display/lights");
			var letterLights = Enumerable
							.Range(0, lightsTransform.childCount)
							.Select(i => lightsTransform.GetChild(i).GetComponent<Light>())
							.ToArray();

			foreach (var letterLight in letterLights) {
				letterLight.range *= transform.lossyScale.x;
			}
			*/
			
			verticalDisplay = new VerticalDisplay(DisplayHeightCharacters, displayText, logger);
		}

		#endregion

		#region /--- Events ---/

		void Start() {
			PrepareComponents();
			animationRunner.Run(FixTimerPositionRoutine());
			animationRunner.Run(RandomCharacterFlashInfiniteRoutine());
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnActivate() {
			animationRunner.Clear(); // stops RandomCharacterFlashRoutine routine
			animationRunner.Run(IntroAnimationRoutine());
		}

		void Update() {
			var deltaTimeSpan = TimeSpan.FromSeconds(Time.deltaTime);
			animationRunner?.Update(deltaTimeSpan);
			verticalDisplay?.Update(deltaTimeSpan);
		}

		void OnDestroy() {
			animationRunner?.Dispose();
		}

		void OnNeedyActivation() {
			isNeedyRunning = true;
			animationRunner.Run(DrainingBarInfiniteRoutine());
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnNeedyTerminated() {
			OnNeedyDeactivation();
			animationRunner.Clear();
			animationRunner.Run(WriteOutTextRoutine(""));
		}
		
		void OnNeedyDeactivation() {
			isNeedyRunning = false;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnNeedyTimerExpired() {
			kmNeedyModule.HandleStrike();
			OnNeedyDeactivation(); // need to deactivate manually because... that is how the proxy works
			animationRunner.Run(StrikeRoutine());
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnButtonPress() {
			
			// Cue
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, refillButton);
			refillButtonSelectable.AddInteractionPunch(0.5f);
			animationRunner.Run(ButtonPressMovementRoutine());

			// Logic
			if (!isNeedyRunning) return;

			float solveAtTime = kmNeedyModule.GetNeedyTimeRemaining();
			
			kmNeedyModule.HandlePass();
			OnNeedyDeactivation(); // need to deactivate manually because... that is how the proxy works
			animationRunner.Run(PassRoutine(solveAtTime));
		}

		#endregion
		
		#region /--- Button Routine ---/
		
		const float ButtonHeldOffsetY = -0.05f;
		static readonly TimeSpan buttonPressAnimationDuration = TimeSpan.FromSeconds(0.075);

		IEnumerable<CoroutineYield> ButtonPressMovementRoutine() {

			System.Action<float> yPosSetter = yy => {
				var position = refillButton.localPosition;
				position.y += yy;
				refillButton.localPosition = position;
			};

			yield return new Shift1D(
				ButtonHeldOffsetY,
				buttonPressAnimationDuration,
				Easing.Linear,
				yPosSetter
			);

			yield return CoroutineYield.WaitPrevious;

			yield return new Shift1D(
				-ButtonHeldOffsetY,
				buttonPressAnimationDuration,
				Easing.Linear,
				yPosSetter
			);
		}
		
		#endregion
		
		#region /--- Display Texts ---/

		const string AugmentedText = "AUGMETNED";
		const string UnchangedText = "UNCHANGED";
		const string NoConfigText = "NO CONFIG";
		const string StrikeText = "X*X*X*X*X";
		
		static readonly Pair<Color, Color> introColors = Pair.New(
			new Color(0.8f, 0.8f, 0.8f, 0.8f), 
			new Color(0.9f, 0.9f, 0.9f)
		);
		
		static readonly Pair<Color, Color> noConfigColors = Pair.New(
			new Color(0.8f, 0.8f, 0.0f, 0.8f), 
			new Color(0.9f, 0.9f, 0.4f)
		);
		
		static readonly Pair<Color, Color> unchangedColors = Pair.New(
			new Color(0.8f, 0.5f, 0.0f, 0.8f), 
			new Color(0.9f, 0.6f, 0.4f)
		);
		
		static readonly Pair<Color, Color> errorColors = Pair.New(
			new Color(1.0f, 0.2f, 0.0f, 0.8f), 
			new Color(1.0f, 0.5f, 0.5f)
		);

		static readonly Pair<Color, Color> augmentedColors = Pair.New(
			new Color(0.2f, 1.0f, 0.2f), 
			new Color(0.4f, 0.9f, 0.4f)
		);
		
		static readonly char[] randomIntroCharacters = (
														@"!@$%^&*()[]<>{}/\|,.-=+?0123456789" +
														"abcdefghijklmnopqrstuvwxyz" +
														"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
														"€£¥®µ¶¿ß÷₸⃀√∟" +
														"čīšūžΏΐΓΔΛΞΠΣΤΦΨάαβδζηθλμνστω" +
														"ЖЗИЦЩШЪЫЬЮЯабийлтщ" +
														"アイウオカキサケシスセタツムマルレ円"
														).ToCharArray();

		static readonly char[] fillProgressCharacters = " _шШ#".ToCharArray();
		
		#endregion
		
		#region /--- Display Routines ---/

		static readonly TimeSpan frameDuration = TimeSpan.FromSeconds(0.075);
		
		/// <remarks>Enumerates infinitely.</remarks>
		IEnumerable<CoroutineYield> RandomCharacterFlashInfiniteRoutine() {

			verticalDisplay.Colors = introColors;
			
			const int repetitionsPerPair = 4;
			
			while (true) {

				char first = randomIntroCharacters.PickRandom(rng);
				char second = randomIntroCharacters.PickRandom(rng);
				
				for (int repetitionsI = 0; repetitionsI < repetitionsPerPair; repetitionsI++) {
					
					for (int i = 0; i < verticalDisplay.Size; i++) {
						bool isFirst = (i + repetitionsI) % 2 == 0;
						verticalDisplay.Characters[i] = isFirst ? first : second;
					}
					
					yield return CoroutineYield.Sleep(frameDuration);
				}
			}

			// [unreachable]
			throw new System.InvalidOperationException();
			// ReSharper disable once IteratorNeverReturns
		}
		
		IEnumerable<CoroutineYield> IntroAnimationRoutine() {
			
			// Animate
			verticalDisplay.Colors = introColors;
			
			for (int frameI = 0; frameI < verticalDisplay.Size; frameI++) {
				
				verticalDisplay.ClearString();
				verticalDisplay.Characters[frameI] = 'o';
				verticalDisplay.Characters[verticalDisplay.Size - frameI - 1] = 'o';
				
				yield return CoroutineYield.Sleep(frameDuration);
			}

			verticalDisplay.ClearString();
			
			// Set correct display
			foreach (var @yield in WriteOutCurrentStateRoutine()) yield return @yield;
		}
		
		/// <remarks>Enumerates infinitely, until <see cref="isNeedyRunning"/> is false.</remarks>
		IEnumerable<CoroutineYield> DrainingBarInfiniteRoutine() {
			while (isNeedyRunning) {

				float maxTime = kmNeedyModule.CountdownTime;
				float currentTime = kmNeedyModule.GetNeedyTimeRemaining();

				float fillProgressInCharacters = currentTime * verticalDisplay.Size / maxTime;

				DrawBar(fillProgressInCharacters);
				
				yield return CoroutineYield.Suspend; // run every frame
			}
		}

		IEnumerable<CoroutineYield> StrikeRoutine() {
			
			verticalDisplay.Colors = errorColors;
			verticalDisplay.ClearString();

			for (int i = 0; i < verticalDisplay.Size; i++) {
					
				int position = verticalDisplay.Size - 1 - i;

				verticalDisplay.Characters[position] = StrikeText[position];
				if (i % 2 == 0) yield return CoroutineYield.Sleep(frameDuration);
			}
			verticalDisplay.SetString(StrikeText);
			
			for (int i = 0; i < 7; i++) {
				if (i % 2 == 0) {
					verticalDisplay.ClearString();
				} else {
					verticalDisplay.SetString(StrikeText);
				}
				yield return CoroutineYield.Sleep(frameDuration);
			}
			
			// Reset
			foreach (var @yield in WriteOutCurrentStateRoutine()) yield return @yield;
		}


		static readonly TimeSpan refillDuration = TimeSpan.FromSeconds(2);
		static readonly EasingCurve refillEasing = Easing.CubicOut;
		
		IEnumerable<CoroutineYield> PassRoutine(float solvedAtTime) {

			float maxTime = kmNeedyModule.CountdownTime;
			
			yield return new Move1D(
								solvedAtTime * verticalDisplay.Size / maxTime,
								verticalDisplay.Size,
								refillDuration,
								refillEasing,
								DrawBar
							);
			yield return CoroutineYield.WaitPrevious;
			yield return CoroutineYield.Sleep(frameDuration);
			
			// Reset
			foreach (var @yield in WriteOutCurrentStateRoutine()) yield return @yield;
		}
		
		public IEnumerable<CoroutineYield> WriteOutCurrentStateRoutine(bool setColor = true) {
			
			// todo
			string displayText = NoConfigText;
			var displayColor = noConfigColors;
			
			// Animate
			if (setColor) {
				verticalDisplay.Colors = displayColor;
			}
			
			foreach (var @yield in WriteOutTextRoutine(displayText)) yield return @yield;
		}
		
		public IEnumerable<CoroutineYield> WriteOutTextRoutine(string displayText) {
			
			for (int i = 0; i < verticalDisplay.Size; i++) {
				
				verticalDisplay.Characters[i] = '#';

				if (i > 0) {
					verticalDisplay.Characters[i-1] = i - 1 < displayText.Length ? displayText[i - 1] : ' ';
				}
				
				yield return CoroutineYield.Sleep(frameDuration);
			}
			
			verticalDisplay.SetString(displayText);
		}
		
		/// <param name="fillAmountCharacters">Fill amount. 1.0 corresponds to 1 full character.</param>
		void DrawBar(float fillAmountCharacters) {
			
			// Extend the "last drop"
			int maxChrIndex = fillProgressCharacters.Length - 1;
			float fillPerChrIndex = 1f / maxChrIndex;
			
			if (fillAmountCharacters >= fillPerChrIndex * 0.25f && fillAmountCharacters <= fillPerChrIndex) {
				fillAmountCharacters = fillPerChrIndex;
			}
			
			// Extend the topped out
			if (fillAmountCharacters + fillPerChrIndex * 0.25f >= verticalDisplay.Size) {
				fillAmountCharacters = verticalDisplay.Size;
			}
			
			// Draw
			for (int i = 0; i < verticalDisplay.Size; i++) {
					
				int position = verticalDisplay.Size - 1 - i;
				float chrProgress = Mathf.Clamp(fillAmountCharacters - i, 0, 1);
				int chrIndex = Mathf.FloorToInt(chrProgress * maxChrIndex);
				
				verticalDisplay.Characters[position] = fillProgressCharacters[chrIndex];
			}
			
		}
		
		#endregion

		IEnumerable<CoroutineYield> FixTimerPositionRoutine() {
			
			// Original source
			// https://github.com/VFlyer/FlyersOtherModules/blob/master/Assets/NeedyPuzzleLeague/CollapseCore.cs

			yield return CoroutineYield.Suspend; // wait a frame
			
			var needyTimer = transform.Find("NeedyTimer(Clone)");
			if (needyTimer == null) yield break;
			
			needyTimer.transform.Rotate(Vector3.up * -90);
		}
		
	}

}
