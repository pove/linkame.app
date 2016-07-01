using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace linkame
{
    [Activity(Label = "Linkame", MainLauncher = true, Icon = "@drawable/icon")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] {Intent.CategoryDefault, Intent.CategoryBrowsable}, DataMimeType = "text/plain")]
    public class MainActivity : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (!String.IsNullOrEmpty(Intent.GetStringExtra(Intent.ExtraText)))
            {
                string subject = Intent.GetStringExtra(Intent.ExtraSubject) ?? "subject not available";
                Toast.MakeText(this, subject, ToastLength.Long).Show();
            }

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate { button.Text = string.Format("{0} clicks!", count++); };
        }
    }
}

