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

namespace linkame.Models
{
    public class Link
    {
        public Link (int id, string name, string url)
        {
            Id = id;
            Name = name;
            Url = url;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }
    }
}