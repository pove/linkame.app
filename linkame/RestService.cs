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
using System.Threading.Tasks;
using linkame.Models;
using System.Json;
using System.Net;
using System.IO;

namespace linkame
{
    public static class RestService
    {
        // Links rest api url
        private const string url = "http://192.168.0.X/linkame/api/public/";

        // Gets links data from the passed URL.
        public static async Task<JsonValue> GetLinksAsync(string linksPath)
        {
            try
            {
                // Create an HTTP web request using the URL:
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url + "links"));
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
                //Toast.MakeText(this, "Ups! Links cannot be loaded", ToastLength.Short).Show();
                return null;
            }
        }

        // Send link received to the service and retrieve existing links
        public static bool SendLink(string intentName, string intentUrl)
        {
            try
            {
                //string intentName = Intent.GetStringExtra(Intent.ExtraSubject) ?? "name not available";
                //string intentUrl = Intent.GetStringExtra(Intent.ExtraText) ?? "url not available";

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
                        //Toast.MakeText(this, "Added " + intentName, ToastLength.Short).Show();

                        // Retrieve all links data
                        //ProcessLinksAsync(true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                //Toast.MakeText(this, "Ups! Link is not added", ToastLength.Short).Show();
            }

            return false;
        }

        // Send link received to the service and retrieve existing links
        public static bool DeleteLink(int linkId)
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
                    //Toast.MakeText(this, "Link deleted", ToastLength.Short).Show();

                    // Retrieve all links data again
                    //ProcessLinksAsync(true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                //Toast.MakeText(this, "Ups! Link is not deleted", ToastLength.Short).Show();
            }
            return false;
        }
    }
}