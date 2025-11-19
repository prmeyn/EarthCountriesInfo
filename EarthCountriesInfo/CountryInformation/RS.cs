using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class RS
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "381",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Serbia" },
				{ LanguageId.sr, "Srbija" }
			}
		);
	}
}
