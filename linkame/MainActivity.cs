using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using System.Json;
using System.Net;
using System.IO;
using linkame.Adapters;
using linkame.Models;
using System.Collections.Generic;

namespace linkame
{
    [Activity(Label = "Linkame", MainLauncher = true, Icon = "@drawable/icon")]
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain")]
    public class MainActivity : ListActivity
    {
        // Get links api url.
        string url = "http://192.168.0.X/linkame/api/public/";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Check if we are receiving something from intent filter
            if (String.IsNullOrEmpty(Intent.GetStringExtra(Intent.ExtraText)))
            {
                // If not, retrieve links data from service
                ProcessLinksAsync();
            }
            else
            {
                // If so, send link data to the service
                SendLink();
            }            
        }

        // Send link received to the service and retrieve existing links
        private void SendLink()
        {
            try
            {
                string intentName = Intent.GetStringExtra(Intent.ExtraSubject) ?? "name not available";
                string intentUrl = Intent.GetStringExtra(Intent.ExtraText) ?? "url not available";

                // Create an HTTP web request using the URL:
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url + "link"));
                request.ContentType = "application/json";
                request.Method = "POST";

                // Create json object value to post on the service:
                JsonObject value = new JsonObject(new KeyValuePair<string, JsonValue>("name", intentName), new KeyValuePair<string, JsonValue>("url", intentUrl));

                // Write data to the request:
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(value.ToString());
                }

                // Send the request to the server and wait for the response:
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Toast.MakeText(this, "Added " + intentName, ToastLength.Short).Show();

                        // Retrieve all links data
                        ProcessLinksAsync();
                    }
                }
            }
            catch
            {
                Toast.MakeText(this, "Ups! Link is not added.", ToastLength.Short).Show();
            }
        }
        // Get and parse data from links service
        private async void ProcessLinksAsync()
        {
            // Get the links information asynchronously, 
            JsonValue json = await GetLinksAsync(url + "links");

            // parse the results, then update the screen:
            if (json != null)
                ParseAndDisplay(json);
        }

        // Gets links data from the passed URL.
        private async Task<JsonValue> GetLinksAsync(string url)
        {
            try
            {
                // Create an HTTP web request using the URL:
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
                request.ContentType = "application/json";
                request.Method = "GET";

                // Send the request to the server and wait for the response:
                using (WebResponse response = await request.GetResponseAsync())
                {
                    // Get a stream representation of the HTTP web response:
                    using (Stream stream = response.GetResponseStream())
                    {
                        // Use this stream to build a JSON document object:
                        JsonValue jsonDoc = await Task.Run(() => JsonObject.Load(stream));
                        Console.Out.WriteLine("Response: {0}", jsonDoc.ToString());

                        // Return the JSON document:
                        return jsonDoc;
                    }
                }
            }
            catch
            {
                Toast.MakeText(this, "Ups! Links cannot be loaded.", ToastLength.Short).Show();
                return null;
            }
        }
        
        // Parse the links data, then write that info to the screen.
        private void ParseAndDisplay(JsonValue json)
        {
            List<Link> links = new List<Link>();

            // Serialize reveived json
            foreach (var item in json as JsonObject)
            {
                links.Add(new Link(item.Value["id"], item.Value["name"], item.Value["url"]));
            }

            // Create the lisk adapter
            var adapter = new LinkAdapter(this, links);

            RunOnUiThread(() =>
            {
                // Link list with adapter
                this.ListAdapter = adapter;
            });            
        }
    }
}

