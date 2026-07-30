using EarthCountriesInfo;
using HumanLanguages;

namespace AreYouHuman
{
	/// <summary>
	/// Data-integrity checks over the whole country dataset. Each test asserts an invariant
	/// that held when the dataset was last audited, so that a regression in any one country
	/// file fails the build rather than shipping.
	/// </summary>
	[TestClass]
	public sealed class IntegrityChecks
	{
		private static readonly CountryIsoCode[] AllIsoCodes = Enum.GetValues<CountryIsoCode>();

		/// <summary>
		/// Country pairs that legitimately share a name in a given language. Empty today:
		/// the Saint Martin / Sint Maarten pairs that used to collide are now distinguished
		/// the way CLDR distinguishes them. Add an entry here (with a source) only when two
		/// territories genuinely share a name in that language.
		/// </summary>
		private static readonly HashSet<string> AllowedSharedNames = [];

		[TestMethod]
		public void EveryIsoCodeHasCountryProperties()
		{
			var missing = AllIsoCodes
				.Where(code => !Countries.CountryPropertiesDictionary.ContainsKey(code))
				.ToList();

			Assert.IsTrue(missing.Count == 0, $"No CountryProperties registered for: {string.Join(", ", missing)}");
		}

		[TestMethod]
		public void DictionaryContainsNoCodesOutsideTheEnum()
		{
			var known = AllIsoCodes.ToHashSet();
			var unexpected = Countries.CountryPropertiesDictionary.Keys.Where(k => !known.Contains(k)).ToList();

			Assert.IsTrue(unexpected.Count == 0, $"Unexpected keys: {string.Join(", ", unexpected)}");
			Assert.AreEqual(AllIsoCodes.Length, Countries.CountryPropertiesDictionary.Count);
		}

		[TestMethod]
		public void EveryCountrySupportsTheSameLanguages()
		{
			var reference = Countries.CountryPropertiesDictionary[AllIsoCodes[0]].CountryNames.Keys.ToHashSet();
			var problems = new List<string>();

			foreach (var (code, properties) in Countries.CountryPropertiesDictionary)
			{
				var languages = properties.CountryNames.Keys.ToHashSet();
				if (!languages.SetEquals(reference))
				{
					var missing = reference.Except(languages);
					var extra = languages.Except(reference);
					problems.Add($"{code}: missing [{string.Join(",", missing)}] extra [{string.Join(",", extra)}]");
				}
			}

			Assert.IsTrue(problems.Count == 0, string.Join("; ", problems));
		}

		[TestMethod]
		public void EveryCountryHasAnEnglishName()
		{
			var missing = Countries.CountryPropertiesDictionary
				.Where(entry => !entry.Value.CountryNames.TryGetValue(LanguageId.en, out var name)
								|| string.IsNullOrWhiteSpace(name))
				.Select(entry => entry.Key.ToString())
				.ToList();

			Assert.IsTrue(missing.Count == 0, $"No English name for: {string.Join(", ", missing)}");
		}

		[TestMethod]
		public void EveryPhoneCodeIsNonEmptyDigits()
		{
			var invalid = Countries.CountryPropertiesDictionary
				.Where(entry => entry.Value.CountryPhoneCode.Length == 0
								|| !entry.Value.CountryPhoneCode.All(char.IsAsciiDigit))
				.Select(entry => $"{entry.Key}='{entry.Value.CountryPhoneCode}'")
				.ToList();

			Assert.IsTrue(invalid.Count == 0, $"Invalid phone codes: {string.Join(", ", invalid)}");
		}

		[TestMethod]
		public void EveryPhoneFormatMaskMatchesItsLengthKey()
		{
			var problems = new List<string>();

			foreach (var (code, properties) in Countries.CountryPropertiesDictionary)
			{
				if (properties.ValidLengthsAndFormat is null)
				{
					continue;
				}

				foreach (var (length, mask) in properties.ValidLengthsAndFormat)
				{
					if (length <= 0)
					{
						problems.Add($"{code}: non-positive length {length}");
						continue;
					}

					var digits = mask.Count(character => character == '#');
					if (digits != length)
					{
						problems.Add($"{code}: length {length} but mask '{mask}' has {digits} '#'");
					}
				}
			}

			Assert.IsTrue(problems.Count == 0, string.Join("; ", problems));
		}

		[TestMethod]
		public void NoCountryNameHasLeadingOrTrailingWhitespace()
		{
			var problems = (from entry in Countries.CountryPropertiesDictionary
							from name in entry.Value.CountryNames
							where name.Value != name.Value.Trim()
							select $"{entry.Key}/{name.Key}='{name.Value}'").ToList();

			Assert.IsTrue(problems.Count == 0, $"Untrimmed names: {string.Join(", ", problems)}");
		}

		/// <summary>
		/// Two different countries sharing an identical name within the same language is
		/// almost always a copy-paste error in the source data — it is how the wrong values
		/// for Turkey (Portuguese), Niger (Indonesian) and South Sudan (Polish) were found.
		/// </summary>
		[TestMethod]
		public void NoTwoCountriesShareANameInTheSameLanguage()
		{
			var seen = new Dictionary<(LanguageId Language, string Name), CountryIsoCode>();
			var collisions = new List<string>();

			foreach (var (code, properties) in Countries.CountryPropertiesDictionary)
			{
				foreach (var (language, name) in properties.CountryNames)
				{
					if (string.IsNullOrEmpty(name))
					{
						continue;
					}

					var key = (language, name.ToLowerInvariant());
					if (seen.TryGetValue(key, out var other))
					{
						var pair = string.Join("/", new[] { other.ToString(), code.ToString() }.Order());
						if (!AllowedSharedNames.Contains($"{language}:{pair}"))
						{
							collisions.Add($"{language} '{name}' shared by {pair}");
						}
					}
					else
					{
						seen[key] = code;
					}
				}
			}

			Assert.IsTrue(collisions.Count == 0, string.Join("; ", collisions));
		}

		/// <summary>
		/// The names are generated from CLDR by tools/GenerateCountryData. A half-completed
		/// download or a botched run would silently produce blanks rather than crash, so hold the
		/// dataset to a coverage floor. Actual coverage at the CLDR 48.2.1 migration was 83.6%;
		/// the floor is set below that so ordinary CLDR churn does not trip it, but any large
		/// regression fails the build.
		/// </summary>
		[TestMethod]
		public void PopulatedNameCoverageDoesNotRegress()
		{
			const double MinimumCoverage = 0.80;

			var total = 0;
			var populated = 0;
			foreach (var properties in Countries.CountryPropertiesDictionary.Values)
			{
				foreach (var name in properties.CountryNames.Values)
				{
					total++;
					if (!string.IsNullOrEmpty(name))
					{
						populated++;
					}
				}
			}

			var coverage = (double)populated / total;
			Assert.IsTrue(
				coverage >= MinimumCoverage,
				$"Name coverage fell to {coverage:P1} ({populated}/{total}); floor is {MinimumCoverage:P0}. "
				+ "Did a regeneration run against an incomplete CLDR download?");
		}

		/// <summary>
		/// Values that deliberately diverge from CLDR live in the generator's EditorialOverrides
		/// table. This pins the one that exists so a future regeneration cannot quietly revert it.
		/// </summary>
		[TestMethod]
		public void EditorialOverridesSurviveRegeneration()
		{
			Assert.AreEqual(
				"India (Bharat)",
				Countries.CountryPropertiesDictionary[CountryIsoCode.IN].CountryNames[LanguageId.en],
				"The IN/en editorial override was lost; check EditorialOverrides in tools/GenerateCountryData.");
		}

		/// <summary>
		/// Guards the fix for the data classes having previously exposed
		/// <c>CountryProperties => new(...)</c>, which rebuilt the record and both of its
		/// dictionaries on every single read.
		/// </summary>
		[TestMethod]
		public void CountryPropertiesAreCachedNotRebuiltPerAccess()
		{
			Assert.IsTrue(ReferenceEquals(
				EarthCountriesInfo.CountryInformation.DK.CountryProperties,
				EarthCountriesInfo.CountryInformation.DK.CountryProperties));
		}
	}
}
