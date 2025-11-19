using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class BL
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "590",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Saint Barthelemy" },
				{ LanguageId.fr, "Saint-Barthélemy" }
			}
		);
	}
}
