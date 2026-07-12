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

	[RequireComponent(typeof(KMBombModule))]
	[RequireComponent(typeof(KMBombInfo))]
	[RequireComponent(typeof(KMAudio))]
	public class NeedyAugmentationModule : MonoBehaviour {

		#region /--- State ---/

		private enum State {
			Initiating,
			IntroAnimation,
			AwaitingHold,
			Held,
			StrikeAnimation,
			Solved
		}

		State state = State.Initiating;

		bool isButtonHeld = false;
		
		/// <summary>
		/// The digit displayed, when the button is held.
		/// Is null before the module picks a digit.
		/// The digit is still stored, when the module is solved.
		/// </summary>
		int? SelectedDigit { get; set; } = null;

		// Expose log id for LFA at module instance (required by Tweaks)
		public int LogFileAnalyzerId => this.logger.tagId ?? 0;
		
		#endregion
		
		#region /--- Components and parts ---/

		// KM
		internal KMBombModule kmModule;
		internal KMBombInfo kmBomb;
		internal KMAudio kmAudio;
		
		// Module parts
		internal Transform acknowledgeButton;
		internal KMSelectable acknowledgeButtonSelectable;

		const int DisplayHeightCharacters = 9;
		internal VerticalDisplay verticalDisplay; // created
		
		// Misc. Created
		internal System.Random rng;
		internal ModuleLogger logger;
		internal AnimationRunner animationRunner;

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
			
			// Button
			acknowledgeButton = transform.Find("objectScaler/button");
			
			acknowledgeButtonSelectable = acknowledgeButton.GetComponent<KMSelectable>();
			acknowledgeButtonSelectable.OnInteract += () => { OnButtonHold(); return false; };
			acknowledgeButtonSelectable.OnInteractEnded += OnButtonReleased;
			
			// Display
			var displayText = transform.Find("objectScaler/display/text").GetComponent<TextMesh>();

			var lightsTransform = transform.Find("objectScaler/display/lights");
			var letterLights = Enumerable
							.Range(0, lightsTransform.childCount)
							.Select(i => lightsTransform.GetChild(i).GetComponent<Light>())
							.ToArray();

			foreach (var letterLight in letterLights) {
				letterLight.range *= transform.lossyScale.x;
			}

			verticalDisplay = new VerticalDisplay(DisplayHeightCharacters, displayText, letterLights, logger);
		}

		#endregion

		#region /--- Events ---/

		void Start() {
			PrepareComponents();
			state = State.IntroAnimation; // hacky way to force a wait on hold
			animationRunner.Run(LightsOffEndlessRoutine());
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnActivate() {
			animationRunner.Clear(); // stops LightsOff routine
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

		// ReSharper disable Unity.PerformanceAnalysis
		void OnButtonHold() {

			if (isButtonHeld) return;
			isButtonHeld = true;
			
			// Cue
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, acknowledgeButton);
			acknowledgeButtonSelectable.AddInteractionPunch(0.5f);
			animationRunner.Run(CreateButtonPressMovement());
			
			// Logic
			switch (state) {
				
				case State.IntroAnimation:
				case State.StrikeAnimation:
					logger.LogString("Holding and waiting for animation to finish...");
					animationRunner.Run(WaitUntilAwaitingThenPickDigit());
					break;
				
				case State.AwaitingHold:
					logger.LogString("Holding...");
					state = State.Held;
					animationRunner.Run(PickDigitRoutine());
					break;
				
				case State.Initiating:
				case State.Held:
				case State.Solved:
				default:
					return;
			}
			
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		void OnButtonReleased() {
			
			if (!isButtonHeld) return;
			isButtonHeld = false;
			
			// Cue
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, acknowledgeButton);
			acknowledgeButtonSelectable.AddInteractionPunch(0.5f);
			animationRunner.Run(CreateButtonReleaseMovement());
			
			// Logic

			switch (state) {
				
				case State.IntroAnimation:
				case State.StrikeAnimation:
				case State.Held:
					string currentTime = kmBomb.GetFormattedTime();
					logger.LogString($"Released at {currentTime}.");

					if (!SelectedDigit.HasValue) {
						logger.LogString("Display has not yet settled on a digit. No time is valid.");
						kmModule.HandleStrike();

						if (state == State.Held) {
							state = State.StrikeAnimation;
							animationRunner.Run(IncorrectRoutine());
						}
						return;
					}
					
					int releaseDigit = (SelectedDigit.Value + GetNeedyModuleCount()) % 10;
					
					if (!currentTime.Contains(DigitToCharacter(releaseDigit))) {
						logger.LogString("Incorrect.");
						kmModule.HandleStrike();
						SelectedDigit = null;

						if (state != State.Held) {
							throw new System.InvalidOperationException("Assertion failed. Somehow the digit is picked before holding.");
						}
						
						state = State.StrikeAnimation;
						animationRunner.Run(IncorrectRoutine());
						return;
					}
					
					logger.LogString("Module Solved.");
					kmModule.HandlePass();
					kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
					state = State.Solved;
					animationRunner.Run(CorrectAnimationRoutine());
					return;
				
				case State.Initiating:
				case State.AwaitingHold:
				case State.Solved:
				default:
					return;
			}
		}
		
		public void TwitchHandleForcedSolve() {
			logger.LogString("Forced solve command received. Ending routines.");
			animationRunner.Clear();
			
			state = State.Solved;
			kmModule.HandlePass();
			logger.LogString("Module solved.");
		}

		#endregion
		
		#region /--- Run Button Animation ---/
		
		const float ButtonHeldOffsetY = -0.05f;
		static readonly TimeSpan buttonPressAnimationDuration = TimeSpan.FromSeconds(0.075);

		Shift1D CreateButtonPressMovement() {
			return new Shift1D(
				ButtonHeldOffsetY,
				buttonPressAnimationDuration,
				Easing.Linear,
				(yy) => {
					var position = acknowledgeButton.localPosition;
					position.y += yy;
					acknowledgeButton.localPosition = position;
				}
			);
		}

		Shift1D CreateButtonReleaseMovement() {
			return new Shift1D(
				-ButtonHeldOffsetY,
				buttonPressAnimationDuration,
				Easing.Linear,
				(yy) => {
					var position = acknowledgeButton.localPosition;
					position.y += yy;
					acknowledgeButton.localPosition = position;
				}
			);
		}
		
		#endregion
		
		#region /--- Display Texts ---/

		const string AugmentedText = "AUGMETNED";
		const string UnchangedText = "UNCHANGED";
		const string IncorrectText = "X*X*X*X*X";
		const string CorrectText = "CONFIMEDΩ";
		
		static readonly Pair<Color, Color> introColors = Pair.New(
			new Color(0.8f, 0.8f, 0.8f, 0.8f), 
			new Color(0.9f, 0.9f, 0.9f)
		);
		
		static readonly Pair<Color, Color> augmentedColors = Pair.New(
			new Color(0.8f, 0.8f, 0.0f, 0.8f), 
			new Color(0.9f, 0.9f, 0.4f)
		);
		
		static readonly Pair<Color, Color> unchangedColors = Pair.New(
			new Color(0.8f, 0.5f, 0.0f, 0.8f), 
			new Color(0.9f, 0.6f, 0.4f)
		);
		
		static readonly Pair<Color, Color> strikeColors = Pair.New(
			new Color(1.0f, 0.2f, 0.0f, 0.8f), 
			new Color(1.0f, 0.5f, 0.5f)
		);

		static readonly Pair<Color, Color> solvedColors = Pair.New(
			new Color(0.2f, 1.0f, 0.2f), 
			new Color(0.6f, 0.9f, 0.4f)
		);
		
		#endregion
		
		#region /--- Routines ---/

		static readonly TimeSpan frameDuration = TimeSpan.FromSeconds(0.075);
		
		static readonly char[] randomIntroCharacters = (
														@"!@$%^&*()[]<>{}/\|,.-=+?0123456789" +
														"abcdefghijklmnopqrstuvwxyz" +
														"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
														"£¥®µ¶¿ß÷čīšūžΏΐΓΔΛΞΠΣΤΦΨάαβδζηθλμνστω" +
														"ЖЗИЦЩШЪЫЬЮЯабийлтщ€" +
														"₸⃀√∟" +
														"アイウオカキサケシスセタツムマルレ円"
														).ToCharArray();
		
		IEnumerable<CoroutineYield> LightsOffEndlessRoutine() {
			
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

			// Set correct display
			// todo
			verticalDisplay.SetString(AugmentedText);
			verticalDisplay.Colors = augmentedColors;
			
			state = State.AwaitingHold;
		}


		IEnumerable<CoroutineYield> WaitUntilAwaitingThenPickDigit() {
			yield return CoroutineYield.SleepUntilTrue(() => state == State.AwaitingHold || !isButtonHeld);

			if (!isButtonHeld) yield break; // Released too early -- do not proceed to held state.
			
			state = State.Held;
			yield return PickDigitRoutine().ToAnimation();
		}

		IEnumerable<CoroutineYield> PickDigitRoutine() {

			// Animate
			int[] allPositions = Enumerable.Range(0, verticalDisplay.Size).ToArray();
			allPositions.Shuffle(rng); // shuffles in place

			for (int digitsDisplayed = 0; digitsDisplayed < allPositions.Length; digitsDisplayed += 2) {

				for (int i = 0; i <= digitsDisplayed; i++) {
					verticalDisplay.Characters[allPositions[i]] = DigitToCharacter(rng.Next(10));
				}
				
				yield return CoroutineYield.Sleep(frameDuration);
				if (!isButtonHeld) yield break; // if button is released too early
			}
			
			for (int spacesFromSides = 0; spacesFromSides < verticalDisplay.Size / 2; spacesFromSides++) {
				
				int i = 0;
				for (/*[nop]*/; i < spacesFromSides; i++) {
					verticalDisplay.Characters[i] = ' ';
				}
				for (/*[nop]*/; i < verticalDisplay.Size - spacesFromSides; i++) {
					verticalDisplay.Characters[i] = DigitToCharacter(rng.Next(10));
				}
				for (/*[nop]*/; i < verticalDisplay.Size; i++) {
					verticalDisplay.Characters[i] = ' ';
				}
				
				yield return CoroutineYield.Sleep(frameDuration);
				if (!isButtonHeld) yield break; // if button is released too early
			}

			verticalDisplay.ClearString();
			
			const int fakeRolls = 3;
			for (int fakeRollI = 0; fakeRollI <= fakeRolls; fakeRollI++) {
				verticalDisplay.Characters[verticalDisplay.Size / 2] = DigitToCharacter(rng.Next(10));
				yield return CoroutineYield.Sleep(frameDuration);
				if (!isButtonHeld) yield break; // if button is released too early
			}
			
			// Pick digit
			SelectedDigit = rng.Next(10);
			int needyCount = GetNeedyModuleCount();
			int releaseDigit = (SelectedDigit.Value + needyCount) % 10;
			logger.LogString($"Digit on the display is {SelectedDigit}");
			logger.LogString($"There are {needyCount} needy modules.");
			logger.LogString($"Release when a {releaseDigit} is in any position.");
			
			// Display
			verticalDisplay.SetString($" - ¯{DigitToCharacter(SelectedDigit.Value)}_ - ");
		}

		IEnumerable<CoroutineYield> IncorrectRoutine() {
			
			verticalDisplay.Colors = strikeColors;
			
			for (int i = 0; i < 9; i++) {
				if (i % 2 == 0) {
					verticalDisplay.ClearString();
				} else {
					verticalDisplay.SetString(IncorrectText);
				}
				yield return CoroutineYield.Sleep(frameDuration);
			}
			
			// Reset
			// todo
			verticalDisplay.SetString(AugmentedText);
			verticalDisplay.Colors = augmentedColors;
			
			state = State.AwaitingHold;
		}
		
		IEnumerable<CoroutineYield> CorrectAnimationRoutine() {
			
			verticalDisplay.Colors = solvedColors;
			
			for (int i = 0; i < 9; i++) {
				if (i % 2 == 0) {
					verticalDisplay.ClearString();
				} else {
					verticalDisplay.SetString(CorrectText);
				}
				yield return CoroutineYield.Sleep(frameDuration);
			}
			
			// Reset with solved colors
			// todo
			verticalDisplay.SetString(AugmentedText);
		}
		
		#endregion

		int GetNeedyModuleCount() {
			return kmBomb.GetModuleIDs().Count - kmBomb.GetSolvableModuleIDs().Count;
		}

		char DigitToCharacter(int digit) => (char)('0' + digit);

	}

}
