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
using Android.Preferences;

namespace linkame
{
    [Activity(Label = "Linkame", MainLauncher = true, Icon = "@drawable/icon",
        FinishOnTaskLaunch = true, // to prevent recreate the last link again when reopen
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)] // to prevent recreate the last link again when orientation changed
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain")]
    public class MainActivity : ListActivity
    {
        // Local file to store links
        string linksPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "linkslist{0}.json");

        // Links list
        List<Link> links = new List<Link>();

        // Main preferences
        string device = string.Empty;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Get main preferences
            ISharedPreferences getprefs = PreferenceManager.GetDefaultSharedPreferences(this);
            device = getprefs.GetString("device", string.Empty);

            // Check if we are receiving something from intent filter
            if (String.IsNullOrEmpty(Intent.GetStringExtra(Intent.ExtraText)))
            {
                // If not, retrieve links data from local file or service
                ProcessLinksAsync();
            }
            else
            {
                // If we have more than one device, open device selector
                if (getprefs.GetInt("devicesnum", 0) > 1)
                {
                    ShowSelectDeviceDialog(true);

                    // In the meanwhile, retrieve links data from local file or service
                    ProcessLinksAsync();
                }
                else
                {
                    sendLinkFromIntent();
                }
            }

            getprefs.Dispose();
        }

        private void sendLinkFromIntent()
        {
            // Send link data to the service
            string intentName = Intent.GetStringExtra(Intent.ExtraSubject) ?? "Name not available";
            string intentUrl = Intent.GetStringExtra(Intent.ExtraText) ?? "url not available";

            // Get only the url (usefull when link comes from Google Maps)
            int firstHttp = intentUrl.IndexOf("http");
            intentUrl = intentUrl.Substring(Math.Max(0, firstHttp));

            if (RestService.SendLink(intentName, intentUrl))
            {
                Toast.MakeText(this, "Added " + intentName, ToastLength.Short).Show();

                // Retrieve all links data
                ProcessLinksAsync(true);
            }
            else
            {
                Toast.MakeText(this, "Ups! Link is not added", ToastLength.Short).Show();

                // Retrieve links data from local file or service
                ProcessLinksAsync();
            }
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            // Update links data from service
            ProcessLinksAsync(true);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            base.OnCreateOptionsMenu(menu);
            MenuInflater.Inflate(Resource.Layout.MainMenu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.add_device:
                    ShowAddDeviceDialog();
                    return true;
                case Resource.Id.select_device:
                    ShowSelectDeviceDialog();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void ShowAddDeviceDialog()
        {
            var transaction = FragmentManager.BeginTransaction();
            var dialogFragment = new AddDeviceDialog();
            dialogFragment.Dismissed += (s, e) =>
            {
                Toast.MakeText(this, e.Text, ToastLength.Short).Show();

                // Update main preferences
                device = e.Device;

                // Get links data
                ResetList();
                ProcessLinksAsync();
            };
            dialogFragment.Show(transaction, GetString(Resource.String.add_device));
        }

        public void ShowSelectDeviceDialog(bool disableDelete = false)
        {
            var transaction = FragmentManager.BeginTransaction();
            var dialogFragment = new SelectDeviceDialog();
            dialogFragment.Dismissed += (s, e) => {

                // Update main preferences
                device = e.Device;

                // If delete is disabled means that we are sending a link from intent
                if (disableDelete)
                {
                    sendLinkFromIntent();
                }
                else
                {
                    Toast.MakeText(this, e.Text, ToastLength.Short).Show();

                    // Get links data
                    ResetList();
                    ProcessLinksAsync();
                }
            };
            dialogFragment.Show(transaction, (disableDelete ? "disableDelete" : null));            
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
                try
                {
                    // Open link on default navigator
                    var uri = Android.Net.Uri.Parse(link.Url);
                    var intent = new Intent(Intent.ActionView, uri);
                    StartActivity(intent);
                }
                catch
                {
                    Toast.MakeText(this, "Sorry, I cannot open this link", ToastLength.Short).Show();
                }
            });

            alert.SetNegativeButton("Delete", (senderAlert, args) =>
            {
                // Delete link
                if (RestService.DeleteLink(link.Id))
                {
                    Toast.MakeText(this, "Link deleted", ToastLength.Short).Show();

                    // Retrieve all links data again
                    ProcessLinksAsync(true);
                }
                else
                {
                    Toast.MakeText(this, "Ups! Link is not deleted", ToastLength.Short).Show();
                }
            });

            //run the alert in UI thread to display in the screen
            RunOnUiThread(() =>
            {
                alert.Show();
            });
        }        

        // Get and parse data from links service
        private async void ProcessLinksAsync(bool getFromService = false)
        {
            JsonValue json = null;
            bool reloadFromService = false;

            if (!getFromService && File.Exists(GetDevicePath()))
            {
                json = GetLinksFromFile();
                reloadFromService = true;
            }

            if (json == null)
            {
                // Get the links information from service 
                json = await RestService.GetLinksAsync(GetDevicePath());

                if (json == null)
                    Toast.MakeText(this, "Ups! Links cannot be loaded from service", ToastLength.Short).Show();
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
                string fileText = File.ReadAllText(GetDevicePath());
                return JsonValue.Parse(fileText);
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                return null;
            }
        }        
        
        // Parse the links data, then write that info to the screen.
        private void ParseAndDisplay(JsonValue json, bool reloadFromService = false)
        {
            // Remove list click event and clear items list
            this.ListView.ItemClick -= listView_ItemClick;
            links.Clear();

            if (json != null && json.Count > 0)
            {
                if (json.GetType() == typeof(JsonObject))
                {
                    // Serialize reveived json
                    foreach (var item in json as JsonObject)
                    {
                        links.Add(new Link(item.Value["id"], item.Value["name"], item.Value["url"]));
                    }
                }
                else if (json.GetType() == typeof(JsonArray))
                {
                    // Serialize reveived json
                    foreach (var item in json as JsonArray)
                    {
                        links.Add(new Link(item["id"], item["name"], item["url"]));
                    }
                }
            }
                        
            // Create the lisk adapter
            var adapter = new LinkAdapter(this, links);

            RunOnUiThread(() =>
            {
                // Link list with adapter
                this.ListAdapter = adapter;

                // Handle the list click
                this.ListView.ItemClick += listView_ItemClick;
            });        
              
            // If it has been loaded from file, update list calling the service
            if (reloadFromService)
            {
                ProcessLinksAsync(true);
            }
        }

        private void ResetList()
        {
            this.ListView.ItemClick -= listView_ItemClick;
            links.Clear();
            RunOnUiThread(() =>
            {
                this.ListAdapter = null;
            });
        }

        private string GetDevicePath()
        {
            return string.Format(linksPath, device);
        }
    }
}

