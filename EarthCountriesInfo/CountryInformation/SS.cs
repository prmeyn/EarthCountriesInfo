using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class SS
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "211",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "South Sudan" }
			}
		);
	}
}
