using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
//using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;
using Windows.Devices.Geolocation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace CompassDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Compass compass;
        String appversion = GetAppVersion();
        private ApplicationDataContainer localSettings;
        Geolocator gpswatcher = new Geolocator()
        {
            MovementThreshold = 20
        };

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;

           // String value=localSettings.Values["initSetting"].ToString();
            localSettings = ApplicationData.Current.LocalSettings;
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //DO I NEED TO DIRECT TO THE HELP PAGE?
            if (localSettings.Values.ContainsKey("ok"))
            {
                compass = Compass.GetDefault();
                gpswatcher.DesiredAccuracy = PositionAccuracy.High;

                if (compass == null)
                {
                    await new MessageDialog("您的设备不支持罗盘传感器").ShowAsync();
                    return;
                }
                compass.ReadingChanged += compass_ReadingChanged;

                if (gpswatcher==null)
                {
                    await new MessageDialog("您的设备不支持GPS装置").ShowAsync();
                    return;
                }
                gpswatcher.PositionChanged += gpswatcherPositionChanged;
                gpswatcher.StatusChanged += gpswatcherStatusChanged;
            }

            else
            {
                localSettings.Values["ok"] = "ok";
                Frame.Navigate(typeof(Help),appversion);
            }

        }

        private async void compass_ReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CompassReading reading = args.Reading;
                magneticNorth.Text = String.Format("{0,5:0.00}°\n精度: \n", reading.HeadingMagneticNorth);
                if (reading.HeadingTrueNorth != null)
                {
                    trueNorth.Text = String.Format("{0,5:0}°", reading.HeadingTrueNorth);
                }
                else
                {
                    magneticNorth.Text += "无数据";
                }
                switch (reading.HeadingAccuracy)
                {
                    case MagnetometerAccuracy.Unknown:
                        magneticNorth.Text = "未知";
                        break;
                    case MagnetometerAccuracy.Unreliable:
                        magneticNorth.Text += "低";
                        break;
                    case MagnetometerAccuracy.Approximate:
                        magneticNorth.Text += "中";
                        break;
                    case MagnetometerAccuracy.High:
                        magneticNorth.Text += "高";
                        break;
                    default:
                        magneticNorth.Text += "无数据";
                        break;
                }
                double TrueHeading = reading.HeadingTrueNorth.Value;
                double ReciprocalHeading;
                if ((180 <= TrueHeading) && (TrueHeading <= 360))
                    ReciprocalHeading = TrueHeading - 180;
                else
                    ReciprocalHeading = TrueHeading + 180;
                CompassFace.RenderTransformOrigin = new Point(0.5, 0.5);
                //EllipseGlass.RenderTransformOrigin = new Point(0.5, 0.5);
                RotateTransform transform = new RotateTransform();
                transform.Angle = 360 - TrueHeading;
                CompassFace.RenderTransform = transform;
                // EllipseGlass.RenderTransform = transform;
            });
        }

        async void gpswatcherPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ShowData();
            });       
        }

        async void ShowData()
        {
            try
            {
                Geoposition pos = await gpswatcher.GetGeopositionAsync();
                string Latitude = String.Format("{0,5:0.00}°:", pos.Coordinate.Point.Position.Latitude);
                string Longitude = String.Format("{0,5:0.00}°:", pos.Coordinate.Point.Position.Longitude);
                if (Latitude.StartsWith ("-"))
                { Latitude = "南" + Latitude.Substring(1); }
                else
                { Latitude = "北" + Latitude; }
                if (Longitude.StartsWith("-"))
                { Longitude = "西" + Longitude.Substring(1); }
                else
                { Longitude = "东" + Longitude; }
                GPS.Text = Latitude+" "+ Longitude;
            }
            catch (System.UnauthorizedAccessException)
            {
                GPS.Text = "无数据";
            }
        }

        private void gpswatcherStatusChanged(Geolocator sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case PositionStatus.Disabled:
                    //location is unsupported on this device
                    GPS.Text = "请手动开启定位设置";
                    break;
                case PositionStatus.NoData:
                    // data unavailable
                    GPS.Text = "无数据";
                    break;
                case PositionStatus.NotAvailable:
                    GPS.Text = "您的设备不支持GPS装置";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.
        }

        #region AddInfo
        public static string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            string temp = String.Format("{0}.{0}.{0}.{0}", version.Major, version.Minor, version.Build, version.Revision);
            return temp;
        }

        //Info Page
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            //this.NavigationService.Navigate(new Uri("/Info_Page.xaml", UriKind.Relative));
            Frame.Navigate(typeof(Help), appversion);
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(
    new Uri(string.Format("ms-windows-store:reviewapp?appid=" + "57762822-45ff-4c70-a16c-75369411ef3d")));
        }
        #endregion

    }
}
