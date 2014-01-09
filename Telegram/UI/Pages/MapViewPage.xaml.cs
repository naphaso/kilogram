using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Telegram.MTProto;
using Telegram.Utils;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI {
    public partial class MapViewPage : PhoneApplicationPage {
        MapLayer myPositionLayer = new MapLayer();
        MapOverlay myMapOverlay = new MapOverlay();

        private GeoCoordinate userGeoCoordinate;
        private GeoCoordinate myGeoCoordinate;

        private GeoPointConstructor point; 
        public int ZoomLevel { get; set; }
        public MapViewPage() {
            InitializeComponent();

            GetMyMapLocationAsync();
            myPositionLayer.Add(myMapOverlay);
            Map.Layers.Add(myPositionLayer);
            ZoomLevel = 16;
            Map.ZoomLevel = ZoomLevel;

            DataContext = MediaTransitionHelper.Instance.From;

            MessageMedia media = MediaTransitionHelper.Instance.Media;
            MessageMediaGeoConstructor geoMedia = (MessageMediaGeoConstructor) media;

            point = (GeoPointConstructor) geoMedia.geo;

            userGeoCoordinate = new GeoCoordinate(point.lat, point.lng);
            AddPushpin(userGeoCoordinate, MediaTransitionHelper.Instance.From.FullName);

            CalculateDistanceBetweenMeAndUser();
            UserInfoClick(this, null);
        }

        private void CalculateDistanceBetweenMeAndUser() {
            if (userGeoCoordinate == null || myGeoCoordinate == null)
                return;

            double meters = myGeoCoordinate.GetDistanceTo(userGeoCoordinate);
            DistanceTextBlock.Text = GetDistanceString(meters) + " away";
        }

        public static string GetDistanceString(double meters) {
            double distance = meters;
            string dstring = "m";
            
            if (meters > 1000) {
                dstring = "km";
                distance = distance/1000;
            }

            return string.Format("{0:0.0}", distance) + dstring;
        }

        private async void GetMyMapLocationAsync() {
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;

            try {
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(10)
                    );

                Map.Center = new GeoCoordinate(geoposition.Coordinate.Latitude, geoposition.Coordinate.Longitude);
                Map.ZoomLevel = ZoomLevel;

                myGeoCoordinate = geoposition.Coordinate.ToGeoCoordinate();
                     
                myMapOverlay.GeoCoordinate = myGeoCoordinate;
                myMapOverlay.Content = new Pushpin() {Content = "You"};
                CalculateDistanceBetweenMeAndUser();

            } catch (Exception ex) {
                if ((uint)ex.HResult == 0x80004004) {
                    // the application does not have the right capability or the location master switch is off
                    Debug.WriteLine("Geoposition not allowed or hadrware disabled");
                }
                //else
                {
                    // something else happened acquring the location
                    Debug.WriteLine("Geoposition is not available. Failure.");

                }
            }
        }

        private void OnExpandMenuTap(object sender, GestureEventArgs e) {

        }

        private void OnSendClick(object sender, RoutedEventArgs e) {

        }

        private void OnSaveClick(object sender, RoutedEventArgs e) {

        }

        private void OnShareClick(object sender, RoutedEventArgs e) {

        }

        private void OnBrowseClick(object sender, RoutedEventArgs e) {

        }

        private void OnCenterClick(object sender, EventArgs e) {
            GetMyMapLocationAsync();
        }

        private void AddPushpin(GeoCoordinate coordinate, string name) {
            Pushpin pushpin = new Pushpin();
            pushpin.GeoCoordinate = coordinate;
            pushpin.Content = name;

            MapOverlay newOverlay = new MapOverlay();
            newOverlay.Content = pushpin;
            newOverlay.GeoCoordinate = coordinate;

            MapLayer newLayer = new MapLayer();
            newLayer.Add(newOverlay);

            Map.Layers.Add(newLayer);

        }

        private void Map_OnTap(object sender, GestureEventArgs e) {
            e.Handled = true;

            Point clickLocation = e.GetPosition(Map);
            GeoCoordinate coordinate = Map.ConvertViewportPointToGeoCoordinate(clickLocation);
            
//            AddPushpin(coordinate);
        }

        private void UserInfoClick(object sender, GestureEventArgs e) {
            if (userGeoCoordinate != null)
                Map.Center = userGeoCoordinate;
        }
    }
}