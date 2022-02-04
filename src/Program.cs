using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Web;
using System.Net.Http;
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
            CreateHostBuilder().Build().Start();
            int listenPort = GetListenPort().Result;
            System.Console.WriteLine($"Listening on Port: {listenPort}");
            for (int loops = 0; loops < 30; loops++)
            {
                System.Threading.Thread.Sleep(1000);
            }


            return 0;
        }
        public static async Task<int> GetListenPort()
        {
            for (int loops = 0; loops < 100; loops++)
            {
                await Task.Delay(100);
                string address = Startup.Addresseses.Addresses.FirstOrDefault();

                if ((string.IsNullOrEmpty(address))
                || (address.Length < 18))
                {
                    continue;
                }
                if (int.TryParse(address.Substring(17), out int port))
                {
                    return port;
                }
            }
            throw new Exception("Failed to find an open port!");
        }

        ///
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.UseKestrel();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
