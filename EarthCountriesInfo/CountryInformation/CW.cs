using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class CW
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "599",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Curacao" },
				{ LanguageId.nl, "Curaçao" }
			}
		);
	}
}
