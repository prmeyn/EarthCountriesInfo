using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class ME
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "382",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Montenegro" },
				{ LanguageId.sr, "Crna Gora" }
			}
		);
	}
}
