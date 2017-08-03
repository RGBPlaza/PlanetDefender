using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Xamarin.Forms;

namespace Planet_Defender.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            MessagingCenter.Subscribe<object>(this, "HideCursor", (s) =>
            {
                Window.Current.CoreWindow.PointerCursor = null;
            });

            LoadApplication(new Planet_Defender.App());
        }
        
        private double X = 0;
        private double Y = 0;

        private void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            double x = e.GetCurrentPoint(this).Position.X;
            double y = e.GetCurrentPoint(this).Position.Y;

            if (Planet_Defender.App.IsMobile && !(x < Planet_Defender.MainPage.FireButtonWidth && y > (Planet_Defender.MainPage.CanvasInfo.Height - Planet_Defender.MainPage.FireButtonHeight)))
            {
                double distX = x - X;
                double distY = y - Y;
                MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseDrag", Tuple.Create<double, double>(distX, distY));
                X = x;
                Y = y;
            }
            else if (!Planet_Defender.App.IsMobile)
            {
                MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseMoved", Tuple.Create<double, double>(x, y));
            }
        }

        private void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            double x = e.GetCurrentPoint(this).Position.X;
            double y = e.GetCurrentPoint(this).Position.Y;
            if (Planet_Defender.App.IsMobile && !(x < Planet_Defender.MainPage.FireButtonWidth && y > (Planet_Defender.MainPage.CanvasInfo.Height - Planet_Defender.MainPage.FireButtonHeight)))
            {
                X = x;
                Y = y;
            }
            MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseDown", Tuple.Create<double, double>(x, y));
        }

        private void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseUp", Tuple.Create<double, double>(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
        }
    }
}
