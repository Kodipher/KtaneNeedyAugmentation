using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public class AugmentationConfig {

		public const string WildcardId = "*";

		public ReadOnlyCollection<AugmentationPropertySet> PropertySets { get; private set; } =
										new ReadOnlyCollection<AugmentationPropertySet>(new AugmentationPropertySet[0]);
		
	}

}