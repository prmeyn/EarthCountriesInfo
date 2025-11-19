using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class CI
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "225",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Cote d'Ivoire" },
				{ LanguageId.fr, "Côte d'Ivoire" }
			}
		);
	}
}
