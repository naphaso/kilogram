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
using Telegram.Core.Logging;
using Telegram.MTProto;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI.Pages {
    public partial class MapPickPage : PhoneApplicationPage {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MapPickPage));

        private Pushpin selectedPushpin;

        MapOverlay selectedPushpinOverlay = new MapOverlay();
        MapOverlay myPositionOverlay = new MapOverlay();

        private int returnToModelId = 0;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            string uriParam = "";

            if (NavigationContext.QueryString.TryGetValue("fromModelId", out uriParam)) {
                returnToModelId = int.Parse(uriParam);
            }
            else {
                logger.error("Error: no modeId to return to");
            }
        }

        public MapPickPage() {
            InitializeComponent();

            MapLayer mainLayer = new MapLayer();
            mainLayer.Add(selectedPushpinOverlay);
            mainLayer.Add(myPositionOverlay);
            Map.Layers.Add(mainLayer);

            Map.ZoomLevel = 16;

            GetMyMapLocationAsync();
        }

        private void OnCheckinClick(object sender, EventArgs e) {
            InputGeoPoint point = TL.inputGeoPoint(selectedPushpin.GeoCoordinate.Latitude, selectedPushpin.GeoCoordinate.Longitude);
            InputMedia geoMedia = TL.inputMediaGeoPoint(point);

            PhoneApplicationService.Current.State["MapMedia"] = geoMedia;
            NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?modelId=" + returnToModelId + "&action=sendMedia&content=MapMedia", UriKind.Relative));
        }

        private void OnCenterClick(object sender, EventArgs e) {
            GetMyMapLocationAsync();
        }

        private async void GetMyMapLocationAsync()
        {
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;

            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(10)
                    );

                Map.Center = new GeoCoordinate(geoposition.Coordinate.Latitude, geoposition.Coordinate.Longitude);
                myPositionOverlay.Content = new Pushpin() { Content = "You" };
                myPositionOverlay.GeoCoordinate = geoposition.Coordinate.ToGeoCoordinate();
                Map.ZoomLevel = 16;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
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

        private void Map_OnTap(object sender, GestureEventArgs e) {
            Point clickLocation = e.GetPosition(Map);
            GeoCoordinate coordinate = Map.ConvertViewportPointToGeoCoordinate(clickLocation);

            SetPushpin(coordinate);

            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true; 
        }

        private void SetPushpin(GeoCoordinate coordinate) {
            selectedPushpin = new Pushpin() {Content = "Share", GeoCoordinate = coordinate};

            selectedPushpinOverlay.Content = selectedPushpin;
            selectedPushpinOverlay.GeoCoordinate = coordinate;
        }

        private void OnRoadModeClick(object sender, EventArgs e) {
            Map.CartographicMode = MapCartographicMode.Road;
        }

        private void OnAerialMode(object sender, EventArgs e) {
            Map.CartographicMode = MapCartographicMode.Aerial;
        }

        private void OnHybridMode(object sender, EventArgs e) {
            Map.CartographicMode = MapCartographicMode.Hybrid;
        }

        private void OnTerrainMode(object sender, EventArgs e) {
            Map.CartographicMode = MapCartographicMode.Terrain;
        }
    }
}