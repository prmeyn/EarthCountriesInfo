using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class PS
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "970",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Palestine" },
				{ LanguageId.ar, "فلسطين" }
			}
		);
	}
}
