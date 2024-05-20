using EarthCountriesInfo.CountryInformation;

namespace EarthCountriesInfo
{
	public static class Countries
	{
		public static Dictionary<CountryIsoCode, CountryProperties> CountryPropertiesDictionary = new()
		{
			{
				CountryIsoCode.DK,
				DK.CountryProperties
			}
		};
	}
}
