using HumanLanguages;

namespace EarthCountriesInfo
{
	public sealed record CountryProperties(
		Dictionary<LanguageId, string> CountryNames,
		string CountryPhoneCode,
		Dictionary<int, string>? ValidLengthsAndFormat
	);
}
