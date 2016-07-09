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
        private string device;

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
            ISharedPreferences getprefs = PreferenceManager.GetDefaultSharedPreferences(this.Dialog.Context);
            device = getprefs.GetString("device", string.Empty);
            textView.Text = device;

            textView.InputType = Android.Text.InputTypes.TextFlagCapCharacters;
            textView.RequestFocus();
            this.Dialog.Window.SetSoftInputMode(SoftInput.StateVisible);

            view.FindViewById<Button>(Resource.Id.btSave).Click += delegate
            {
                if (string.IsNullOrWhiteSpace(textView.Text) || device == textView.Text)
                    return;



                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this.Dialog.Context);
                ISharedPreferencesEditor editor = prefs.Edit();
                editor.PutString("device", textView.Text);
                editor.Apply();

                Toast.MakeText(Activity, String.Format("Device {0} saved", textView.Text), ToastLength.Short).Show();
                Dismiss();
            };

            return view;
        }
    }
}