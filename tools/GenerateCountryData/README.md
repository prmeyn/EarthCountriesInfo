# GenerateCountryData

Regenerates the country **names** in `EarthCountriesInfo/CountryInformation/*.cs` from Unicode CLDR.

## Running it

```bash
dotnet build -c Release                                   # the generator reads the current dataset
dotnet run --project tools/GenerateCountryData -c Release
dotnet build -c Release && dotnet test -c Release         # rebuild with the new data, then verify
```

The first run downloads 240 locale files from GitHub and caches them under
`%TEMP%/cldr-json/<version>/`; later runs are offline. It prints a summary of how many values were
filled, changed, kept or left empty — compare that against the previous run when reviewing a diff.

## Refreshing to a newer CLDR release

Change one constant, `CldrVersion`, in `Program.cs`, delete the temp cache, and re-run. Then update
the version stated in `README.md` and `THIRD-PARTY-NOTICES.md` to match.

## How it decides each value

Per (country, language), in order:

1. an entry in `EditorialOverrides` wins — deliberate divergences from CLDR live there, with reasons;
2. otherwise the CLDR **base** display name, if CLDR has one (`-alt-short` and `-alt-variant` forms
   are ignored on purpose, so `GB` is "United Kingdom" rather than "UK");
3. otherwise whatever the dataset already held. CLDR has no locale for 23 of the 240 supported
   languages and covers only a few territories for 28 more, so a strict CLDR-only replacement would
   delete about 4,200 values we do have.

## Things that are deliberate, please keep them

- **Phone data is not touched.** `CountryPhoneCode` and `ValidLengthsAndFormat` are read back out of
  the referenced `EarthCountriesInfo` assembly and re-emitted unchanged, so they remain
  hand-maintainable in the `.cs` files. This is also why the generator references the library it
  regenerates: build, generate, rebuild.
- **Language order is preserved, not alphabetised.** The dataset lists `da` second rather than in
  its alphabetical slot. `CountryNames` enumeration order is observable by consumers, and preserving
  it keeps a regeneration diff limited to values that actually changed instead of burying them in a
  56,000-line reorder. Languages new to `HumanLanguages` are appended.
- **Output is UTF-8 without a BOM, CRLF, tab-indented**, matching every file already in the tree.
- **A 404 from CLDR means "no such locale"** and is expected; any other HTTP failure throws, so a
  partial download cannot silently produce a half-blank dataset.

## Guardrails

`AreYouHuman/IntegrityChecks.cs` will fail the build if a regeneration goes wrong — it asserts the
enum/dictionary bijection, a coverage floor, no same-language collisions between countries, no
untrimmed values, phone-mask consistency, and that the `IN` editorial override survived.
