using System;
using UnityEngine;
using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Base;


namespace SharedAssets.Utils.AtomicAnimations {

	public class DualColorFade : Ease {

		readonly Color oldColor1;
		readonly Color oldColor2;
		readonly Color newColor1;
		readonly Color newColor2;
		readonly Action<Color, Color> setter;

		public DualColorFade(
			Color oldColor1,
			Color oldColor2,
			Color newColor1,
			Color newColor2,
			TimeSpan duration,
			EasingCurve easing,
			Action<Color, Color> setter
		) : base(duration, easing) {
			this.oldColor1 = oldColor1;
			this.oldColor2 = oldColor2;
			this.newColor1 = newColor1;
			this.newColor2 = newColor2;
			this.setter = setter;
		}

		protected override void EaseUpdateImpl(float valueProgressNew) {
			Color lerpedColor1 = Color.Lerp(oldColor1, newColor1, valueProgressNew);
			Color lerpedColor2 = Color.Lerp(oldColor2, newColor2, valueProgressNew);
			setter(lerpedColor1, lerpedColor2);
		}

	}

}
