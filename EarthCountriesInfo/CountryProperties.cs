using HumanLanguages;

namespace EarthCountriesInfo
{
	/// <summary>
	/// Information about a single country: its name in each supported language, its
	/// international dialling code, and — where known — the valid national phone number
	/// lengths with a display mask for each.
	/// </summary>
	/// <param name="CountryNames">
	/// Country name per language. Every supported <see cref="LanguageId"/> is present as a
	/// key; languages with no translation available map to an empty string rather than being
	/// absent, so callers should treat empty as "unknown" and fall back to another language.
	/// </param>
	/// <param name="CountryPhoneCode">International dialling code, digits only, without a leading '+'.</param>
	/// <param name="ValidLengthsAndFormat">
	/// Valid national number lengths mapped to a display mask, where each '#' is one digit.
	/// The number of '#' in a mask always equals its length key. <c>null</c> when unknown.
	/// </param>
	/// <remarks>
	/// Equality caveat: although this is a <c>record</c>, its dictionary members do not
	/// implement structural equality, so the compiler-generated value equality compares them
	/// by reference. Two instances holding identical content therefore compare as unequal —
	/// compare the individual members instead of whole instances.
	/// </remarks>
	public sealed record CountryProperties(
		IReadOnlyDictionary<LanguageId, string> CountryNames,
		string CountryPhoneCode,
		IReadOnlyDictionary<int, string>? ValidLengthsAndFormat
	);
}
