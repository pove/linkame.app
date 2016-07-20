using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Preferences;

namespace linkame
{
    public class DialogEventArgs : EventArgs
    {
        public string Text { get; set; }
    }

    public delegate void DialogEventHandler(object sender, DialogEventArgs args);

    [Activity(Label = "Select device")]
    public class SelectDeviceDialog : DialogFragment
    {
        int devicesNumber = 0, currentDevicePosition = 0;
        ISharedPreferences prefs;
        string currentdevice = string.Empty;

        public event DialogEventHandler Dismissed;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.Dialog.SetTitle(GetString(Resource.String.select_device));

            var view = inflater.Inflate(Resource.Layout.SelectDevice, container, false);            

            ListView lv = view.FindViewById<ListView>(Resource.Id.lvDevices);

            prefs = PreferenceManager.GetDefaultSharedPreferences(this.Dialog.Context);

            // Get current devices number
            devicesNumber = prefs.GetInt("devicesnum", devicesNumber);
            // Get current selected device
            currentdevice = prefs.GetString("device", string.Empty);

            List<string> devices = new List<string>();
            for (int i = 1; i <= devicesNumber; i++)
            {
                string dev = prefs.GetString("device" + i, string.Empty);
                if (dev == currentdevice)
                {
                    dev += " (current)";
                    currentDevicePosition = i - 1;
                }

                devices.Add(dev);
            }

            if (devices.Count == 0)
            {
                return null;
            }

            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this.Dialog.Context, Android.Resource.Layout.SimpleListItem1, devices.ToArray());
            lv.Adapter = adapter;
            lv.ItemClick += Lv_ItemClick;
            if (!string.IsNullOrEmpty(this.Tag) && this.Tag.ToString() == "disableDelete")
            {
                view.FindViewById<TextView>(Resource.Id.tvSelectDeviceInfo).Visibility = ViewStates.Gone;
            }
            else
            {
                lv.ItemLongClick += Lv_ItemLongClick;
            }

            return view;
        }

        public override void OnStart()
        {
            base.OnStart();

            if (View == null)
            {
                if (null != Dismissed)
                    Dismissed(this, new DialogEventArgs { Text = "Add a device first" });

                Dismiss();
            }
        }

        private void Lv_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            string selectedDevice = prefs.GetString("device" + (e.Position + 1), string.Empty);

            //set alert for executing the task
            AlertDialog.Builder alert = new AlertDialog.Builder(this.Dialog.Context);
            alert.SetTitle("Delete device");
            alert.SetMessage(string.Format("¿Do you want to delete device {0}?", selectedDevice));

            // Delete selected device
            alert.SetPositiveButton("Delete", (senderAlert, args) =>
            {
                ISharedPreferencesEditor editor = prefs.Edit();
                
                // Move next devices to this position
                for (int i = e.Position + 1; i < devicesNumber; i++)
                {
                    string dev = prefs.GetString("device" + (i + 1), string.Empty);
                    string key = prefs.GetString("key" + (i + 1), string.Empty);

                    editor.PutString("device" + i, dev);
                    editor.PutString("key" + i, key);                    
                }

                // Remove the last one
                editor.Remove("device" + devicesNumber);
                editor.Remove("key" + devicesNumber);
                editor.PutInt("devicesnum", --devicesNumber);
                editor.Apply();

                // If the deleted device is the current device, select the first one
                if (e.Position == currentDevicePosition)
                {
                    editor.PutString("device", prefs.GetString("device1", string.Empty));
                    editor.PutString("key", prefs.GetString("key1", string.Empty));
                    editor.Apply();
                }

                if (null != Dismissed)
                    Dismissed(this, new DialogEventArgs { Text = "Device deleted" });

                Dismiss();
            });

            alert.SetNegativeButton("Cancel", (senderAlert, args) => { });

            //run the alert
            alert.Show();
        }

        private void Lv_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            if (null != Dismissed)
            {
                // Get selected device
                string device = prefs.GetString("device" + (e.Position + 1), string.Empty);
                string key = prefs.GetString("key" + (e.Position + 1), string.Empty);

                ISharedPreferencesEditor editor = prefs.Edit();

                // Select current device
                editor.PutString("device", device);
                editor.PutString("key", key);

                editor.Apply();

                Dismissed(this, new DialogEventArgs { Text = "Selected device " + device });

                Dismiss();
            }
        }
    }
}