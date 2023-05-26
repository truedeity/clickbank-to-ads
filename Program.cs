using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        // Set your ClickBank API credentials
        string clickBankClientId = "YOUR_CLICKBANK_CLIENT_ID";
        string clickBankClientSecret = "YOUR_CLICKBANK_CLIENT_SECRET";

        // Set your Microsoft Ads API credentials
        string microsoftClientId = "YOUR_MICROSOFT_CLIENT_ID";
        string microsoftClientSecret = "YOUR_MICROSOFT_CLIENT_SECRET";
        string microsoftRefreshToken = "YOUR_MICROSOFT_REFRESH_TOKEN";

        // Get ClickBank access token
        string clickBankTokenUrl = "https://api.clickbank.com/oauth/token";
        var clickBankAuthData = new Dictionary<string, string>
        {
            { "client_id", clickBankClientId },
            { "client_secret", clickBankClientSecret },
            { "grant_type", "client_credentials" }
        };
        var clickBankTokenResponse = await SendPostRequest(clickBankTokenUrl, clickBankAuthData);
        var clickBankAccessToken = clickBankTokenResponse["access_token"].ToString();

        // Fetch ClickBank products
        string clickBankProductsUrl = "https://api.clickbank.com/rest/1.3/marketplace/search?type=xml";
        var clickBankHeaders = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {clickBankAccessToken}" }
        };
        var clickBankResponse = await SendGetRequest(clickBankProductsUrl, clickBankHeaders);

        // Parse XML response to retrieve product details
        var clickBankXml = XDocument.Parse(clickBankResponse);
        var products = clickBankXml.Descendants("Product");

        // Iterate through ClickBank products
        foreach (var product in products)
        {
            string productTitle = product.Element("Title").Value;
            string productDescription = product.Element("Description").Value;
            string productLink = product.Element("Site").Value;

            // Set Microsoft Ads details for creating the ad
            var adData = new Dictionary<string, string>
            {
                { "title", productTitle },
                { "text", productDescription },
                { "url", productLink },
                { "campaign_id", "YOUR_CAMPAIGN_ID" },
                { "ad_group_id", "YOUR_AD_GROUP_ID" }
            };

            // Get Microsoft Ads access token using the refresh token
            string microsoftAuthUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
            var microsoftAuthData = new Dictionary<string, string>
            {
                { "client_id", microsoftClientId },
                { "client_secret", microsoftClientSecret },
                { "refresh_token", microsoftRefreshToken },
                { "grant_type", "refresh_token" },
                { "scope", "https://ads.microsoft.com/.default" }
            };
            var microsoftAuthResponse = await SendPostRequest(microsoftAuthUrl, microsoftAuthData);
            var microsoftAccessToken = microsoftAuthResponse["access_token"].ToString();

            // Create the ad using Microsoft Ads API
            string microsoftAdEndpoint = $"https://api.ads.microsoft.com/v13.0/adgroups/YOUR_AD_GROUP_ID/ads";
            var microsoftHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", $"Bearer {microsoftAccessToken}" }
            };
            var microsoftResponse = await SendPostRequest(microsoftAdEndpoint, adData, microsoftHeaders);
            var microsoftResponseJson = JsonConvert.DeserializeObject<dynamic>(microsoftResponse);

            if (microsoftResponseJson != null && microsoftResponseJson["id"] != null)
            {
                string adId = microsoftResponseJson["id"].ToString();
                Console.WriteLine($"Ad created successfully with ID: {adId}");
            }
            else
            {
                string errorMessage = microsoftResponseJson["message"].ToString();
                Console.WriteLine($"Failed to create ad. Error: {errorMessage}");
            }
        }
    }

    static async Task<Dictionary<string, object>> SendPostRequest(string url, Dictionary<string, string> data)
    {
        using (HttpClient client = new HttpClient())
        {
            var content = new FormUrlEncodedContent(data);
            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
        }
    }

    static async Task<string> SendGetRequest(string url, Dictionary<string, string> headers)
    {
        using (HttpClient client = new HttpClient())
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await client.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
    }
    static async Task<string> SendPostRequest(string url, Dictionary<string, string> data, Dictionary<string, string> headers)
    {
        using (HttpClient client = new HttpClient())
        {
            var content = new FormUrlEncodedContent(data);
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
    }


}
