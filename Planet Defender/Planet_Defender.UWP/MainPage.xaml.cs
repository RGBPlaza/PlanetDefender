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
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Cross, 0);
            });

            LoadApplication(new Planet_Defender.App());
        }

        private void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            MessagingCenter.Send<object, Tuple<double,double>>(this, "MouseMoved", Tuple.Create<double,double>(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
        }

        private void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseDown", Tuple.Create<double, double>(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
        }

        private void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            MessagingCenter.Send<object, Tuple<double, double>>(this, "MouseUp", Tuple.Create<double, double>(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y));
        }
    }
}
