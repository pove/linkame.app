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
        // Links api url.
        string url = "http://192.168.0.104/linkame/api/public/";

        // Local file to store links
        string linksPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "linkslist.json");

        // Links list
        List<Link> links = new List<Link>();

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

        void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Get the selected link
            Link link = links.Find(x => x.Id == this.ListAdapter.GetItemId(e.Position));

            // Check if it is a valid link
            if (link == null || string.IsNullOrWhiteSpace(link.Url))
            {
                Toast.MakeText(this, "It is not a valid link", ToastLength.Short).Show();
                return;
            }

            //set alert for executing the task
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(link.Name);
            alert.SetMessage(link.Url);

            alert.SetPositiveButton("Open link", (senderAlert, args) =>
            {
                // Open link on default navigator
                var uri = Android.Net.Uri.Parse(link.Url);
                var intent = new Intent(Intent.ActionView, uri);
                StartActivity(intent);
            });

            alert.SetNegativeButton("Delete", (senderAlert, args) =>
            {
                // Delete link
                DeleteLink(link.Id);
            });

            //run the alert in UI thread to display in the screen
            RunOnUiThread(() =>
            {
                alert.Show();
            });
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
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        Toast.MakeText(this, "Added " + intentName, ToastLength.Short).Show();

                        // Retrieve all links data
                        ProcessLinksAsync(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                Toast.MakeText(this, "Ups! Link is not added", ToastLength.Short).Show();
            }
        }

        // Send link received to the service and retrieve existing links
        private void DeleteLink(int linkId)
        {
            try
            {
                // Create a simple web request using the URL:
                WebRequest request = WebRequest.Create(url + "link/" + linkId);
                request.Method = "DELETE";

                // Send the request to the server and wait for the response:
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Toast.MakeText(this, "Link deleted", ToastLength.Short).Show();

                    // Retrieve all links data again
                    ProcessLinksAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                Toast.MakeText(this, "Ups! Link is not deleted", ToastLength.Short).Show();
            }
        }

        // Get and parse data from links service
        private async void ProcessLinksAsync(bool getFromService = false)
        {
            JsonValue json = null;
            bool reloadFromService = false;

            if (!getFromService && File.Exists(linksPath))
            {
                json = GetLinksFromFile();
                reloadFromService = true;
            }

            if (json == null)
            {
                // Get the links information from service 
                json = await GetLinksAsync(url + "links");
            }

            // parse the results, then update the screen:
            if (json != null)
                ParseAndDisplay(json, reloadFromService);
        }

        private JsonValue GetLinksFromFile()
        {
            try
            {
                // Get the links information from local file
                string fileText = File.ReadAllText(linksPath);
                return JsonValue.Parse(fileText);
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                return null;
            }
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

                        // Save the list on a local file
                        using (StreamWriter sw = File.CreateText(linksPath))
                        {
                            sw.Write(jsonDoc.ToString());
                            sw.Close();
                        }

                        // Return the JSON document:
                        return jsonDoc;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                Toast.MakeText(this, "Ups! Links cannot be loaded", ToastLength.Short).Show();
                return null;
            }
        }
        
        // Parse the links data, then write that info to the screen.
        private void ParseAndDisplay(JsonValue json, bool reloadFromService = false)
        {
            // Remove list click event and clear items list
            this.ListView.ItemClick -= listView_ItemClick;
            links.Clear();

            if (json.Count > 0)
            {
                // Serialize reveived json
                foreach (var item in json as JsonObject)
                {
                    links.Add(new Link(item.Value["id"], item.Value["name"], item.Value["url"]));
                }
            }
                        
            // Create the lisk adapter
            var adapter = new LinkAdapter(this, links);

            RunOnUiThread(() =>
            {
                // Link list with adapter
                this.ListAdapter = adapter;

                // Handle the list click
                //ListView listView = this.FindViewById<ListView>(Android.Resource.Id.List);
                this.ListView.ItemClick += listView_ItemClick;
            });        
              
            // If it has been loaded from file, update list calling the service
            if (reloadFromService)
            {
                ProcessLinksAsync(true);
            }
        }
    }
}

