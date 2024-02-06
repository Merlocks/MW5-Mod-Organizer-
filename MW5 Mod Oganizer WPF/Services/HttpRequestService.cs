using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Services
{
    public class HttpRequestService
    {
        static readonly HttpClient client = new HttpClient();

        public async Task<string> Main()
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                using HttpResponseMessage response = await client.GetAsync(@"https://merlocks.github.io/mod-manager-api/version.json");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                Console.WriteLine(responseBody);
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return string.Empty;
            }
        }
    }
}
