using System;

namespace smart_local
{
    /// Main Prog
    public static class Program
    {
        private const string _clientId = "fhir_demo_id";
        private const string _defaultFhirServerUrl = "https://launch.smarthealthit.org/v/r4/sim/eyJoIjoiMSIsImUiOiJlZmI1ZDRjZS1kZmZjLTQ3ZGYtYWE2ZC0wNWQzNzJmZGI0MDcifQ/fhir/";

        private static string _authCode = string.Empty;
        private static string _clientState = string.Empty;

        private static string _redirectUrl = string.Empty;

        private static string _tokenUrl = string.Empty;

        private static string _fhirServerUrl = string.Empty;
        static int Main(string fhirServerUrl)
        {
            if (string.IsNullOrEmpty(fhirServerUrl))
            {
                fhirServerUrl = _defaultFhirServerUrl;
            }

            System.Console.WriteLine($"  FHIR Server: {fhirServerUrl}");

            _fhirServerUrl = fhirServerUrl;
            Hl7.Fhir.Rest.FhirClient fhirClient = new Hl7.Fhir.Rest.FhirClient(fhirServerUrl);
            bool isDiscovered = FhirUtils.TryGetSmartUrls(fhirClient, out string authorizeUrl, out string tokenUrl);
            System.Console.WriteLine(isDiscovered);
            if (!isDiscovered)
            {
                System.Console.WriteLine($"Failed to discover SMART URLs");
                return -1;
            }
            System.Console.WriteLine($"Authorize URL: {authorizeUrl}");
            System.Console.WriteLine($"    Token URL: {tokenUrl}");
            _tokenUrl = tokenUrl;
            return 0;
        }
    }
}
