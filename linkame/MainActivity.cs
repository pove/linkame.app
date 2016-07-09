﻿using System;
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
    [Activity(Label = "Linkame", MainLauncher = true, Icon = "@drawable/icon",
        FinishOnTaskLaunch = true, // to prevent recreate the last link again when reopen
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)] // to prevent recreate the last link again when orientation changed
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain")]
    public class MainActivity : ListActivity
    {
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
                // If not, retrieve links data from local file or service
                ProcessLinksAsync();
            }
            else
            {
                // If so, send link data to the service
                string intentName = Intent.GetStringExtra(Intent.ExtraSubject) ?? "Name not available";
                string intentUrl = Intent.GetStringExtra(Intent.ExtraText) ?? "url not available";
                if (RestService.SendLink(intentName, intentUrl))
                {
                    Toast.MakeText(this, "Added " + intentName, ToastLength.Short).Show();

                    // Retrieve all links data
                    ProcessLinksAsync(true);
                }
                else
                {
                    Toast.MakeText(this, "Ups! Link is not added", ToastLength.Short).Show();
                }
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
            }
            return base.OnOptionsItemSelected(item);
        }

        public void ShowAddDeviceDialog()
        {
            var transaction = FragmentManager.BeginTransaction();
            var dialogFragment = new AddDeviceDialog();            
            dialogFragment.Show(transaction, "Add device");
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

            if (!getFromService && File.Exists(linksPath))
            {
                json = GetLinksFromFile();
                reloadFromService = true;
            }

            if (json == null)
            {
                // Get the links information from service 
                json = await RestService.GetLinksAsync(linksPath);

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
                string fileText = File.ReadAllText(linksPath);
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

