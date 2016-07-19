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
using Android.Preferences;

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
                // Check if we have a device saved on preferences
                ISharedPreferences getprefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                string device = getprefs.GetString("device", string.Empty);
                string key = getprefs.GetString("key", string.Empty);

                string urlLinks = url + "links";
                if (!string.IsNullOrWhiteSpace(device) && !string.IsNullOrWhiteSpace(key))
                    urlLinks += string.Format("/{0}?key={1}", device, Uri.EscapeDataString(key));

                // Create an HTTP web request using the URL:
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(urlLinks));
                request.ContentType = "application/json";
                request.Method = "GET";


                // Send the request to the server and wait for the response:
                using (WebResponse response = await request.GetResponseAsync())
                {
                    // Get a stream representation of the HTTP web response:
                    using (Stream stream = response.GetResponseStream())
                    {
                        /*if (stream == null || stream.Length < 5)
                        {
                            return null;
                        }*/

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
            }

            return null;
        }

        // Send link received to the service and retrieve existing links
        public static bool SendLink(string intentName, string intentUrl)
        {
            try
            {
                // Check if we have a device saved on preferences
                ISharedPreferences getprefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                string device = getprefs.GetString("device", string.Empty);
                string key = getprefs.GetString("key", string.Empty);

                string urlLink = url + "link";
                if (!string.IsNullOrWhiteSpace(device) && !string.IsNullOrWhiteSpace(key))
                    urlLink += string.Format("/{0}?key={1}", device, Uri.EscapeDataString(key));

                // Create an HTTP web request using the URL:
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(urlLink));
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
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
            }

            return false;
        }

        // Send link received to the service and retrieve existing links
        public static bool DeleteLink(int linkId)
        {
            try
            {
                // Check if we have a device saved on preferences
                ISharedPreferences getprefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                string device = getprefs.GetString("device", string.Empty);
                string key = getprefs.GetString("key", string.Empty);

                string urlLinks = url + "link/" + linkId;
                if (!string.IsNullOrWhiteSpace(device) && !string.IsNullOrWhiteSpace(key))
                    urlLinks += string.Format("/{0}?key={1}", device, Uri.EscapeDataString(key));

                // Create a simple web request using the URL:
                WebRequest request = WebRequest.Create(urlLinks);
                request.Method = "DELETE";

                // Send the request to the server and wait for the response:
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
            }
            return false;
        }

        // Gets device key
        public static string GetDeviceKey(string device)
        {
            try
            {
                // Create an HTTP web request using the URL:
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url + "device/" + device));
                request.ContentType = "text/plain";
                request.Method = "GET";

                // Send the request to the server and wait for the response:
                using (WebResponse response = request.GetResponse())
                {
                    // Get a stream representation of the HTTP web response:
                    using (Stream stream = response.GetResponseStream())
                    {
                        // Read the received data
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            string result = sr.ReadToEnd();
                            Console.Out.WriteLine("Response: {0}", result);

                            // Return the received key
                            return result;
                        }                        

                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
            }

            return string.Empty;
        }
    }
}