using HumanLanguages;
namespace EarthCountriesInfo.CountryInformation
{
	public sealed class AX
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "358",
			ValidLengthsAndFormat : null,
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en, "Aland Islands" },
				{ LanguageId.sv, "Åland" }
			}
		);
	}
}
