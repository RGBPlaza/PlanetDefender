using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Planet_Defender.Droid
{
    [Activity(Label = "Planet_Defender", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.SensorLandscape)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());

            View touchListener = new View(this);
            AddContentView(touchListener, new ViewGroup.LayoutParams(WallpaperDesiredMinimumWidth,WallpaperDesiredMinimumHeight));
            touchListener.Touch += V_Touch;
        }

        private double X = 0;
        private double Y = 0;

        private void V_Touch(object sender, View.TouchEventArgs e)
        {
            switch (e.Event.Action)
            {
            case MotionEventActions.Move:
                {
                    double x = e.Event.GetX();
                    double y = e.Event.GetY();

                    if (!(x < MainPage.FireButtonWidth && y > (MainPage.CanvasInfo.Height - MainPage.FireButtonHeight)))
                    {
                        double distX = x - X;
                        double distY = y - Y;
                        Xamarin.Forms.MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseDrag", Tuple.Create<double, double>(distX, distY));
                        X = x;
                        Y = y;
                    }
                    break;
                }
            case MotionEventActions.Down:
                {
                    double x = e.Event.GetX();
                    double y = e.Event.GetY();
                    if (!(x < MainPage.FireButtonWidth && y > (MainPage.CanvasInfo.Height - MainPage.FireButtonHeight)))
                    {
                        X = x;
                        Y = y;
                    }
                    Xamarin.Forms.MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseDown", Tuple.Create<double, double>(x, y));
                    break;
                }
            case MotionEventActions.Up:
                {
                    Xamarin.Forms.MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseUp", Tuple.Create<double, double>(e.Event.GetX(), e.Event.GetY()));
                    break;
                }
            }
        }
    }
}

