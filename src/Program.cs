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
using System.Net.Http.Headers;

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

            Task.Run(() => CreateHostBuilder().Build().Run());

            int listenPort = GetListenPort().Result;
            System.Console.WriteLine($"Listening on Port: {listenPort}");
            _redirectUrl = $"http://127.0.0.1:{listenPort}";

            string url =
                $"{authorizeUrl}" +
                $"?response_type=code" +
                $"&client_id={_clientId}" +
                $"&redirect_uri={HttpUtility.UrlEncode(_redirectUrl)}" +
                $"&scope={HttpUtility.UrlEncode("openid fhirUser profile launch/patient patient/*.read")}" +
                $"&state=local_state" +
                $"&aud={fhirServerUrl}";

            LaunchUrl(url);

            for (int loops = 0; loops < 30; loops++)
            {
                System.Threading.Thread.Sleep(1000);
            }

            return 0;
        }


        ///
        public static async void SetAuthCode(string code, string state)
        {
            _authCode = code;
            _clientState = state;

            System.Console.WriteLine($"Code Received: {code}");

            Dictionary<string, string> requestValues = new Dictionary<string, string>(){
                {"grant_type", "authorization_code" },
                {"code", code},
                {"redirect_uri", _redirectUrl},
                {"client_id", _clientId},
            };

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_tokenUrl),
                Content = new FormUrlEncodedContent(requestValues),
            };

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine("Failed to exchange token code.");
                throw new Exception($"Unauthorized: {response.StatusCode}");
            }
            string json = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine("---------Authorization Response---------");
            System.Console.WriteLine(json);
            System.Console.WriteLine("---------Authorization Response---------");

            SmartResponse smartResponse = JsonSerializer.Deserialize<SmartResponse>(json);
        }

        ///
        // public static void DoSomethingWithToken(SmartResponse smartResponse)
        // {
        //     if (smartResponse == null)
        //     {
        //         throw new ArgumentNullException(nameof(smartResponse));
        //     }
        //     if (string.IsNullOrEmpty(smartResponse.AccessToken))
        //     {
        //         throw new ArgumentNullException("SMART Access Token is required!");
        //     }

        //     Hl7.Fhir.Rest.FhirClient fhirClient = new Hl7.Fhir.Rest.FhirClient(_fhirServerUrl);
        //     using (var handler = new fhirClient.
        //     ())
        //     {
        //         using (Hl7.Fhir.Rest.FhirClient client = new Hl7.Fhir.Rest.FhirClient(new Uri(_fhirServerUrl), messageHandler: handler))
        //         {
        //             handler.OnBeforeRequest += (sender, e) =>
        //             {
        //                 e.RawRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "Your Oauth token");
        //             };
        //         }
        //     }


        // }

        ///
        public static bool LaunchUrl(string url)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true,

                };
                Process.Start(startInfo);
                return true;
            }
            catch (Exception)
            {
                // ignore
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    return true;
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string[] allowedProgramsToRun = { "xdg-open", "gnome-open", "kfmclient" };

                foreach (string helper in allowedProgramsToRun)
                {
                    try
                    {
                        Process.Start(helper, url);
                        return true;
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    Process.Start("open", url);
                    return true;
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            System.Console.WriteLine($"Failed to launch URL");
            return false;
        }

        ///
        public static async Task<int> GetListenPort()
        {
            for (int loops = 0; loops < 100; loops++)
            {
                await Task.Delay(100);
                if (Startup.Addresseses == null)
                {
                    continue;
                }

                string address = Startup.Addresseses.Addresses.FirstOrDefault();

                if ((string.IsNullOrEmpty(address))
                || (address.Length < 18))
                {
                    continue;
                }
                if (int.TryParse(address.Substring(17), out int port) && (port != 0))
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
