
using Android.Support.V4.App;
using Android.OS;
using Android.Content;
using Android.Gms.Maps;
using FriendsZone.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Android.Widget;
using Android.Locations;
using Android.Gms.Maps.Model;
//using Android.App;

namespace FriendsZone.Droid.Activities
{
    [Android.App.Activity(Label = "Mapa")]
    public class MapActivity : FragmentActivity
    {
        GoogleMap map;

        List<int> groupIdList;
        List<KeyValuePair<Marker, int>> markerIds;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Map);

            SupportMapFragment mapFrag = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            if (mapFrag != null)
            {
                map = mapFrag.Map;
                if (map != null)
                {
                    map.UiSettings.ZoomControlsEnabled = true;
                    map.UiSettings.CompassEnabled = true;
                    map.MapType = GoogleMap.MapTypeHybrid;
                }
            }

            // inicjuje list� grup, kt�rych miejsca b�d� wy�wietlone.
           groupIdList = new List<int>();
            if(String.IsNullOrEmpty(Intent.GetStringExtra("SPOT")))
            {
                getUsersGroups();
                map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(53.021219, 18.608724), 7));
            }
            else
            {
                var destinySpot = JsonConvert.DeserializeObject<Spot>(Intent.GetStringExtra("SPOT"));

                groupIdList.Add(destinySpot.gid);
                map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(destinySpot.latitude, destinySpot.longitude), 20));
            }

            map.MapLongClick += (object sender, GoogleMap.MapLongClickEventArgs e) =>
            {
                Console.WriteLine(String.Format("Lat: {0}, Long: {1}", e.P0.Latitude, e.P0.Longitude));
                Intent spotDetailsIntent = new Intent(this, typeof(Activities.SpotDetailsActivity));
                spotDetailsIntent.PutExtra("LAT", e.P0.Latitude);
                spotDetailsIntent.PutExtra("LONG", e.P0.Longitude);
                if(Intent.GetIntExtra("GROUP_ID", -1) != -1) {
                    spotDetailsIntent.PutExtra("GROUP_ID", Intent.GetIntExtra("GROUP_ID", -1));
                }
                StartActivity(spotDetailsIntent);
            };

            map.InfoWindowClick += (object sender, GoogleMap.InfoWindowClickEventArgs e) =>
            {
                int spotId = -1;
                foreach(KeyValuePair<Marker, int> kv in markerIds)
                {
                    if (e.P0.Equals(kv.Key)) spotId = kv.Value;
                }

                if(spotId == -1)
                {
                    Toast.MakeText(
                        this,
                        "B��d, Prosz� zg�osi� si� do Dominika",
                        ToastLength.Long).Show();
                    return;
                }

                Intent spotDetailsIntent = new Intent(this, typeof(Activities.SpotDetailsActivity));
                spotDetailsIntent.PutExtra("SPOT_ID", spotId);
                StartActivity(spotDetailsIntent);
            };
        }

        protected override void OnResume()
        {
            base.OnResume();
            Console.WriteLine("RESUME");
            drawMarkers();
        }

        // je�li nie ma podanej grupy, pobierze wszystkie
        private void getUsersGroups()
        {
            string url = string.Format("http://www.friendszone.cba.pl/api/get_user_groups.php?uid={0}",
                    this.GetSharedPreferences("User.data", FileCreationMode.Private).GetString("Email", ""));

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            StreamReader reader = new StreamReader(response.GetResponseStream(), UTF8Encoding.UTF8);
            String json = reader.ReadToEnd();

            Helpers.JsonMsg jsonMsg = JsonConvert.DeserializeObject<Helpers.JsonMsg>(json);

            groupIdList.Clear();
            foreach(Group g in JsonConvert.DeserializeObject<List<Group>>(jsonMsg.msg))
            {
                groupIdList.Add(g.Id);
            }
        }

        private void drawMarkers()
        {
            markerIds = new List<KeyValuePair<Marker, int>>();
            map.Clear();
            foreach (int groupId in groupIdList)
            {
                List<Spot> spotsList = getGroupSpots(groupId);
                float groupColor = getGroupColor(groupId);

                foreach(Spot s in spotsList)
                {
                    Marker marker = map.AddMarker(new MarkerOptions()
                     .SetPosition(new LatLng(s.latitude, s.longitude))
                     .SetTitle(s.name)
                     .SetSnippet(s.description)
                     .InvokeIcon(BitmapDescriptorFactory
                     .DefaultMarker(groupColor)));

                    markerIds.Add(new KeyValuePair<Marker, int>(marker, s.id));
                }
            }
        }

        private List<Spot> getGroupSpots(int groupId)
        {
            List<Spot> spotsList = new List<Spot>();

            string url = string.Format("http://www.friendszone.cba.pl/api/get_group_spots.php?gid={0}",
                    groupId);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            StreamReader reader = new StreamReader(response.GetResponseStream(), UTF8Encoding.UTF8);
            String json = reader.ReadToEnd();

            Helpers.JsonMsg jsonMsg = JsonConvert.DeserializeObject<Helpers.JsonMsg>(json);

            foreach (Spot s in JsonConvert.DeserializeObject<List<Spot>>(jsonMsg.msg))
            {
                spotsList.Add(s);
            }

            return spotsList;
        }

        private float getGroupColor(int groupId)
        {
            string url = string.Format("http://www.friendszone.cba.pl/api/get_group_color.php?gid={0}&uid={1}",
                    groupId,
                    this.GetSharedPreferences("User.data", FileCreationMode.Private).GetString("Email", ""));

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            StreamReader reader = new StreamReader(response.GetResponseStream(), UTF8Encoding.UTF8);
            String json = reader.ReadToEnd();

            Helpers.JsonMsg jsonMsg = JsonConvert.DeserializeObject<Helpers.JsonMsg>(json);

            return float.Parse(jsonMsg.msg);
        }
    }
}