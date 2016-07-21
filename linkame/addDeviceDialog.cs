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
    [Activity(Label = "Add device")]
    public class AddDeviceDialog : DialogFragment
    {
        public event DialogEventHandler Dismissed;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.Dialog.SetTitle(this.Tag);

            var view = inflater.Inflate(Resource.Layout.AddDevice, container, false);                   

            var textView = view.FindViewById<TextView>(Resource.Id.tvDevice);

            // Show keyboard on capital letters
            textView.InputType = Android.Text.InputTypes.TextFlagCapCharacters;
            textView.RequestFocus();
            this.Dialog.Window.SetSoftInputMode(SoftInput.StateVisible);

            // Limit name length to 10
            var textView2 = view.FindViewById<TextView>(Resource.Id.tvDeviceName);
            textView2.SetFilters(new Android.Text.IInputFilter[] { new Android.Text.InputFilterLengthFilter(10) });

            view.FindViewById<Button>(Resource.Id.btSave).Click += delegate
            {
                int devicesNumber = 0;

                if (string.IsNullOrWhiteSpace(textView.Text))
                    return;

                string key = RestService.GetDeviceKey(textView.Text);
                if (string.IsNullOrWhiteSpace(key))
                {
                    Toast.MakeText(Activity, "Cannot get device from server", ToastLength.Short).Show();
                    return;
                }

                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this.Dialog.Context);
                ISharedPreferencesEditor editor = prefs.Edit();

                // Get current devices number
                devicesNumber = prefs.GetInt("devicesnum", devicesNumber);

                // Check if already exists
                int devicePosition = 0;
                List<string> devices = new List<string>();
                for (int i = 1; i <= devicesNumber; i++)
                {
                    string dev = prefs.GetString("device" + i, string.Empty);
                    if (dev == textView.Text)
                    {
                        devicePosition = i;
                        break;
                    }
                }

                if (devicePosition == 0)
                {
                    // Add new device
                    editor.PutInt("devicesnum", ++devicesNumber);
                    devicePosition = devicesNumber;
                }

                editor.PutString("device" + devicePosition, textView.Text);
                editor.PutString("key" + devicePosition, key);
                editor.PutString("name" + devicePosition, textView2.Text);

                // Select current device
                editor.PutString("device", textView.Text);
                editor.PutString("key", key);

                editor.Apply();

                if (null != Dismissed)
                    Dismissed(this, new DialogEventArgs { Text = String.Format("Device {0} saved", textView.Text), Device = textView.Text });

                Dismiss();
            };

            return view;
        }
    }
}