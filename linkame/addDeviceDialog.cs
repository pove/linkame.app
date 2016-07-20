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

            textView.InputType = Android.Text.InputTypes.TextFlagCapCharacters;
            textView.RequestFocus();
            this.Dialog.Window.SetSoftInputMode(SoftInput.StateVisible);

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

                // Get current devices number
                devicesNumber = prefs.GetInt("devicesnum", devicesNumber);

                ISharedPreferencesEditor editor = prefs.Edit();
                // Add new device
                editor.PutInt("devicesnum", ++devicesNumber);
                editor.PutString("device" + devicesNumber, textView.Text);
                editor.PutString("key" + devicesNumber, key);

                // Select current device
                editor.PutString("device", textView.Text);
                editor.PutString("key", key);

                editor.Apply();

                if (null != Dismissed)
                    Dismissed(this, new DialogEventArgs { Text = String.Format("Device {0} saved", textView.Text) });

                Dismiss();
            };

            return view;
        }
    }
}