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
using linkame.Models;

namespace linkame.Adapters
{
    public class LinkAdapter : BaseAdapter<Link>
    {
        private readonly List<Link> _links;
        private readonly Activity _activity;

        public LinkAdapter(Activity activity, IEnumerable<Link> links)
        {
            _links = links.OrderByDescending(s => s.Id).ToList(); // s.Name to order by name
            _activity = activity;
        }

        public override Link this[int position]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override int Count
        {
            get { return _links.Count; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;

            if (view == null)
            {
                view = _activity.LayoutInflater.Inflate(Android.Resource.Layout.SimpleExpandableListItem2, null);
            }

            var link = _links[position];

            TextView text1 = view.FindViewById<TextView>(Android.Resource.Id.Text1);
            text1.Text = link.Name;

            TextView text2 = view.FindViewById<TextView>(Android.Resource.Id.Text2);
            text2.Text = link.Url;

            return view;
        }
    }
}