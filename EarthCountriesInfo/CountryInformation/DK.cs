using HumanLanguages;

namespace EarthCountriesInfo.CountryInformation
{
	public sealed class DK
	{
		public static CountryProperties CountryProperties => new(
			CountryPhoneCode: "45",
			ValidLengthsAndFormat : new Dictionary<int, string>()
			{
				{ 8, "## ## ## ##" }
			},
			CountryNames: new Dictionary<LanguageId, string>()
			{
				{ LanguageId.en , "Denmark" },
				{ LanguageId.da, "Danmark" }
			}
		);
	}
}
