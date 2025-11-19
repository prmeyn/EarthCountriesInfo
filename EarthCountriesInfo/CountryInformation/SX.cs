using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class SX
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "1",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Sint Maarten" },
				{ LanguageId.nl, "Sint Maarten" }
			}
		);
	}
}
