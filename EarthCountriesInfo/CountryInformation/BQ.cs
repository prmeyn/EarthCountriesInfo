using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class BQ
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "599",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Bonaire, Sint Eustatius and Saba" },
				{ LanguageId.nl, "Bonaire, Sint Eustatius en Saba" }
			}
		);
	}
}
