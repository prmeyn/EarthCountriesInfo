using EarthCountriesInfo;

namespace AreYouHuman
{
	[TestClass]
	public sealed class IntegrityChecks
	{
		[TestMethod]
		public void AllCountryPropertiesExistInAllCountries()
		{
			// Assert
			Assert.IsFalse(Enum.GetValues(typeof(CountryIsoCode))
										.Cast<CountryIsoCode>()
										.Any(code => Countries.CountryPropertiesDictionary[code] == null));
		}
	}
}