using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class MF
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "590",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Saint Martin" },
				{ LanguageId.fr, "Saint-Martin" }
			}
		);
	}
}
