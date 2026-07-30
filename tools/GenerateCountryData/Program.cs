using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using EarthCountriesInfo;
using HumanLanguages;

namespace GenerateCountryData
{
	/// <summary>
	/// Regenerates EarthCountriesInfo/CountryInformation/*.cs so that the country names come from
	/// Unicode CLDR instead of from the machine translation the dataset originally shipped with.
	///
	/// Merge rule: use the CLDR base name for a (country, language) pair when CLDR has one;
	/// otherwise keep whatever the dataset already holds. CLDR does not cover 23 of the supported
	/// languages at all, and covers only a handful of territories for 28 more, so a strict
	/// CLDR-only replacement would delete about 4,200 values that we do have.
	///
	/// Phone data is not in CLDR: CountryPhoneCode and ValidLengthsAndFormat are read back out of
	/// the current dataset and re-emitted unchanged, so they stay hand-editable in the .cs files.
	/// </summary>
	internal static class Program
	{
		/// <summary>The only thing to change when refreshing to a newer CLDR release.</summary>
		private const string CldrVersion = "48.2.1";

		private const string CldrUrlFormat =
			"https://raw.githubusercontent.com/unicode-org/cldr-json/{0}/cldr-json/cldr-localenames-full/main/{1}/territories.json";

		private const int MaxParallelDownloads = 8;

		/// <summary>
		/// Values deliberately kept in preference to CLDR, with the reason. These are editorial
		/// choices by the maintainers rather than translation defects, so a regeneration must not
		/// silently revert them.
		/// </summary>
		private static readonly Dictionary<(CountryIsoCode Country, LanguageId Language), string> EditorialOverrides = new()
		{
			// CLDR says plainly "India". The dataset has always named Bharat alongside it, and the
			// CountryIsoCode enum comment does the same; that is intent, not a bad translation.
			[(CountryIsoCode.IN, LanguageId.en)] = "India (Bharat)",
		};

		private static async Task<int> Main(string[] args)
		{
			try
			{
				var outputDirectory = ResolveOutputDirectory(args);
				Console.WriteLine($"CLDR version : {CldrVersion}");
				Console.WriteLine($"Output       : {outputDirectory}");

				var countries = Enum.GetValues<CountryIsoCode>();
				var allLanguages = Enum.GetValues<LanguageId>();
				var languages = ResolveLanguageOrder(allLanguages, Countries.CountryPropertiesDictionary[countries[0]]);

				var cldr = await DownloadCldrAsync(languages);
				Console.WriteLine($"CLDR locales : {cldr.Count} of {languages.Length} exist upstream");

				var statistics = new Statistics();
				foreach (var country in countries)
				{
					var existing = Countries.CountryPropertiesDictionary[country];
					var names = MergeNames(country, languages, existing, cldr, statistics);
					var source = RenderCountryFile(country, existing, languages, names);

					// UTF-8 without a BOM and CRLF endings, to match every file already in the tree.
					var path = Path.Combine(outputDirectory, $"{country}.cs");
					await File.WriteAllTextAsync(path, source, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				}

				statistics.Report(countries.Length * languages.Length);
				return 0;
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine($"FAILED: {exception.Message}");
				return 1;
			}
		}

		/// <summary>
		/// Emits languages in the order the dataset already uses rather than in strict
		/// <see cref="Enum.GetValues{T}"/> order. The two differ in exactly one respect — the
		/// dataset has always listed 'da' second instead of in its alphabetical slot — and
		/// preserving that matters for two reasons: CountryNames enumeration order is observable
		/// by consumers, and it keeps a regeneration diff limited to values that actually changed
		/// instead of burying them in a 56,000-line reorder.
		///
		/// Any language newly added to HumanLanguages is appended in enum order, and any language
		/// removed from it is dropped, so this still tracks the enum.
		/// </summary>
		private static LanguageId[] ResolveLanguageOrder(LanguageId[] all, CountryProperties sample)
		{
			var current = all.ToHashSet();
			var established = sample.CountryNames.Keys.Where(current.Contains).ToList();

			var seen = established.ToHashSet();
			var added = all.Where(language => !seen.Contains(language)).ToList();
			if (added.Count > 0)
			{
				Console.WriteLine($"  NOTE: appending {added.Count} language(s) new to HumanLanguages: {string.Join(", ", added)}");
			}

			var ordered = established.Concat(added).ToArray();
			if (ordered.Length != all.Length)
			{
				throw new InvalidOperationException(
					$"Resolved {ordered.Length} languages but the enum has {all.Length}; refusing to write a partial dataset.");
			}

			return ordered;
		}

		private static Dictionary<LanguageId, string> MergeNames(
			CountryIsoCode country,
			LanguageId[] languages,
			CountryProperties existing,
			Dictionary<LanguageId, Dictionary<string, string>> cldr,
			Statistics statistics)
		{
			var merged = new Dictionary<LanguageId, string>(languages.Length);

			foreach (var language in languages)
			{
				existing.CountryNames.TryGetValue(language, out var current);
				current ??= string.Empty;

				if (EditorialOverrides.TryGetValue((country, language), out var pinned))
				{
					merged[language] = pinned;
					statistics.Overridden++;
					continue;
				}

				string? fromCldr = null;
				if (cldr.TryGetValue(language, out var territories))
				{
					territories.TryGetValue(country.ToString(), out fromCldr);
				}

				if (!string.IsNullOrEmpty(fromCldr))
				{
					merged[language] = fromCldr;
					if (current.Length == 0)
					{
						statistics.Filled++;
					}
					else if (current != fromCldr)
					{
						statistics.Changed++;
					}
					else
					{
						statistics.AlreadyMatched++;
					}
				}
				else
				{
					// CLDR has nothing here: keep what we have rather than blank it.
					merged[language] = current;
					if (current.Length > 0)
					{
						statistics.KeptLegacy++;
					}
					else
					{
						statistics.StillEmpty++;
					}
				}
			}

			return merged;
		}

		private static async Task<Dictionary<LanguageId, Dictionary<string, string>>> DownloadCldrAsync(LanguageId[] languages)
		{
			var cacheDirectory = Path.Combine(Path.GetTempPath(), "cldr-json", CldrVersion);
			Directory.CreateDirectory(cacheDirectory);

			var result = new Dictionary<LanguageId, Dictionary<string, string>>();
			var gate = new SemaphoreSlim(MaxParallelDownloads);
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

			var downloads = languages.Select(async language =>
			{
				await gate.WaitAsync();
				try
				{
					var json = await ReadOrFetchAsync(client, cacheDirectory, language);
					return (language, territories: json is null ? null : ParseTerritories(json));
				}
				finally
				{
					gate.Release();
				}
			});

			foreach (var (language, territories) in await Task.WhenAll(downloads))
			{
				if (territories is { Count: > 0 })
				{
					result[language] = territories;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the raw territories JSON for a locale, or null when CLDR has no such locale.
		/// A 404 is an expected answer for 23 of the supported languages; any other failure is not,
		/// and is allowed to throw so a broken run cannot silently produce a half-filled dataset.
		/// </summary>
		private static async Task<string?> ReadOrFetchAsync(HttpClient client, string cacheDirectory, LanguageId language)
		{
			var locale = language.ToString();
			var cached = Path.Combine(cacheDirectory, $"{locale}.json");
			var missingMarker = Path.Combine(cacheDirectory, $"{locale}.absent");

			if (File.Exists(cached))
			{
				return await File.ReadAllTextAsync(cached);
			}

			if (File.Exists(missingMarker))
			{
				return null;
			}

			var url = string.Format(CultureInfo.InvariantCulture, CldrUrlFormat, CldrVersion, locale);
			using var response = await client.GetAsync(url);

			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				await File.WriteAllTextAsync(missingMarker, string.Empty);
				return null;
			}

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();
			await File.WriteAllTextAsync(cached, json);
			return json;
		}

		/// <summary>
		/// Pulls main.&lt;locale&gt;.localeDisplayNames.territories, keeping only plain two-letter
		/// territory codes. Numeric regions ("001") and alternate forms ("TR-alt-variant") are
		/// skipped: we want CLDR's base display name.
		/// </summary>
		private static Dictionary<string, string> ParseTerritories(string json)
		{
			var territories = new Dictionary<string, string>(StringComparer.Ordinal);

			using var document = JsonDocument.Parse(json);
			if (!document.RootElement.TryGetProperty("main", out var main))
			{
				return territories;
			}

			foreach (var locale in main.EnumerateObject())
			{
				if (!locale.Value.TryGetProperty("localeDisplayNames", out var displayNames)
					|| !displayNames.TryGetProperty("territories", out var node))
				{
					continue;
				}

				foreach (var entry in node.EnumerateObject())
				{
					if (entry.Name.Length != 2 || !entry.Name.All(char.IsAsciiLetterUpper))
					{
						continue;
					}

					var value = entry.Value.GetString();
					if (!string.IsNullOrEmpty(value))
					{
						territories[entry.Name] = value;
					}
				}
			}

			return territories;
		}

		private static string RenderCountryFile(
			CountryIsoCode country,
			CountryProperties existing,
			LanguageId[] languages,
			Dictionary<LanguageId, string> names)
		{
			var builder = new StringBuilder(80 * 1024);

			builder.Append("// Country names in this file are generated from Unicode CLDR ").Append(CldrVersion)
				   .Append(" by tools/GenerateCountryData.\r\n");
			builder.Append("// Where CLDR has no value for a language, the previously curated value is kept.\r\n");
			builder.Append("// CountryPhoneCode and ValidLengthsAndFormat are NOT CLDR data - edit those here by hand.\r\n");
			builder.Append("using HumanLanguages;\r\n");
			builder.Append("namespace EarthCountriesInfo.CountryInformation\r\n");
			builder.Append("{\r\n");
			builder.Append("\tpublic static class ").Append(country).Append("\r\n");
			builder.Append("\t{\r\n");
			builder.Append("\t\tpublic static CountryProperties CountryProperties { get; } = new(\r\n");
			builder.Append("\t\t\tCountryPhoneCode: \"").Append(existing.CountryPhoneCode).Append("\",\r\n");

			if (existing.ValidLengthsAndFormat is null)
			{
				builder.Append("\t\t\tValidLengthsAndFormat: null,\r\n");
			}
			else
			{
				builder.Append("\t\t\tValidLengthsAndFormat: new Dictionary<int, string>()\r\n");
				builder.Append("\t\t\t{\r\n");
				var entries = existing.ValidLengthsAndFormat.OrderBy(pair => pair.Key).ToList();
				for (var index = 0; index < entries.Count; index++)
				{
					builder.Append("\t\t\t\t{ ").Append(entries[index].Key.ToString(CultureInfo.InvariantCulture))
						   .Append(", \"").Append(entries[index].Value).Append("\" }")
						   .Append(index == entries.Count - 1 ? "\r\n" : ",\r\n");
				}
				builder.Append("\t\t\t},\r\n");
			}

			builder.Append("\t\t\tCountryNames: new Dictionary<LanguageId, string>()\r\n");
			builder.Append("\t\t\t{\r\n");
			foreach (var language in languages)
			{
				builder.Append("\t\t\t\t{ LanguageId.").Append(EscapeIdentifier(language.ToString()))
					   .Append(", \"").Append(names[language]).Append("\" },\r\n");
			}
			builder.Append("\t\t\t}\r\n");
			builder.Append("\t\t);\r\n");
			builder.Append("\t}\r\n");
			builder.Append("}\r\n");

			return builder.ToString();
		}

		/// <summary>Prefixes '@' when a language code collides with a C# keyword (as, is, ...).</summary>
		private static string EscapeIdentifier(string identifier) =>
			CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;

		private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
		{
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
			"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
			"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
			"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
			"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
			"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
			"short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
			"virtual", "void", "volatile", "while",
		};

		private static string ResolveOutputDirectory(string[] args)
		{
			if (args.Length > 0)
			{
				return Directory.Exists(args[0])
					? args[0]
					: throw new DirectoryNotFoundException($"No such directory: {args[0]}");
			}

			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EarthCountriesInfo.sln")))
			{
				directory = directory.Parent;
			}

			if (directory is null)
			{
				throw new InvalidOperationException(
					"Could not locate EarthCountriesInfo.sln above the executable; pass the CountryInformation path as an argument.");
			}

			var target = Path.Combine(directory.FullName, "EarthCountriesInfo", "CountryInformation");
			return Directory.Exists(target) ? target : throw new DirectoryNotFoundException(target);
		}

		private sealed class Statistics
		{
			public int Filled;
			public int Changed;
			public int AlreadyMatched;
			public int KeptLegacy;
			public int StillEmpty;
			public int Overridden;

			public void Report(int total)
			{
				Console.WriteLine();
				Console.WriteLine($"  filled from CLDR (was empty) : {Filled,6}");
				Console.WriteLine($"  changed by CLDR              : {Changed,6}");
				Console.WriteLine($"  already matched CLDR         : {AlreadyMatched,6}");
				Console.WriteLine($"  kept (CLDR has no value)     : {KeptLegacy,6}");
				Console.WriteLine($"  still empty                  : {StillEmpty,6}");
				Console.WriteLine($"  editorial overrides applied  : {Overridden,6}");
				var populated = total - StillEmpty;
				Console.WriteLine($"  populated                    : {populated,6} / {total} ({100.0 * populated / total:F1}%)");
			}
		}
	}
}
