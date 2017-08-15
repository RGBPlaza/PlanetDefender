using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using SkiaSharp;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using SkiaSharp.Views.Forms;

namespace Planet_Defender
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            MainCanvas.PaintSurface += MainCanvas_PaintSurface;
            App.IsMobile = Device.Idiom != TargetIdiom.Desktop;
        }

        public const int FireButtonHeight = 150;
        public const int FireButtonWidth = 150;

        public static SKImageInfo CanvasInfo;
        private static bool gameRunning;

        public static List<Bullet> FlyingBullets = new List<Bullet>();

        private void MainCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // Retrieve info
            SKCanvas canvas = e.Surface.Canvas;
            CanvasInfo = e.Info;

            // Clear for new frame
            canvas.Clear();

            // Draw the planet
            canvas.DrawCircle(CanvasInfo.Width / 2, CanvasInfo.Height / 2, (CanvasInfo.Height / 5) < 120 ? (CanvasInfo.Height / 5) : 120, new SKPaint() { Color = new SKColor(48, 120, 64), IsStroke = false, IsAntialias = true });

            // Draw the fire button
            if(App.IsMobile)    
                canvas.DrawRect(new SKRect(0, CanvasInfo.Height - FireButtonHeight, FireButtonWidth, CanvasInfo.Height), new SKPaint() { Color = SKColors.OrangeRed, IsStroke = true });

            try
            {
                foreach (Bullet b in FlyingBullets)
                {
                    float relX = b.Displacement * (float)Math.Sin(b.Angle);
                    float relY = b.Displacement * (float)Math.Cos(b.Angle);

                    float absoluteX = relX + b.ReleasedFrom.Item1;
                    float absoluteY = relY + b.ReleasedFrom.Item2;

                    if (0 <= absoluteX && absoluteX <= CanvasInfo.Width && 0 <= absoluteY && absoluteY <= CanvasInfo.Height)
                    {
                        canvas.DrawCircle(absoluteX, absoluteY, 8, new SKPaint() { Color = new SKColor(128, 128, 128) });
                        b.Displacement += 16;
                    }
                    else
                        FlyingBullets.Remove(b);    // Bullet is off screen so it is removed

                }
            }
            catch { };

            // Initialise values
            const string crosshairResourceID = "Planet_Defender.crosshair.png";
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            // Draw Crosshair
            using (Stream stream = assembly.GetManifestResourceStream(crosshairResourceID))
            using (SKManagedStream skStream = new SKManagedStream(stream))
            {
                SKBitmap crosshair = SKBitmap.Decode(skStream);
                canvas.DrawBitmap(crosshair, new SKRect((float)CursorCoords.Item1 - 18, (float)CursorCoords.Item2 - 18, (float)CursorCoords.Item1 + 18, (float)CursorCoords.Item2 + 18));
            }

            // Draw ship at correct angle
            if (Ship.IsFlying)
            {
                const string resourceID = "Planet_Defender.rocket.png";

                // Load image
                using (Stream stream = assembly.GetManifestResourceStream(resourceID))
                using (SKManagedStream skStream = new SKManagedStream(stream))
                {
                    SKBitmap rocketBitmap = SKBitmap.Decode(skStream);

                    canvas.RotateRadians(Ship.GraphicRotation, Ship.X, Ship.Y);
                    canvas.DrawBitmap(rocketBitmap, new SKRect(Ship.X - 16, Ship.Y - 28, Ship.X + 16, Ship.Y + 28));
                }

            }
            else
            {
                // Location of ship initialisation
                Ship.SetAbsolutePosition(Tuple.Create<double, double>(500, CanvasInfo.Height / 2), CanvasInfo);
                Ship.IsFlying = true;
            }

        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            Ship.SetAbsolutePosition(Tuple.Create<double, double>(Ship.X, Ship.Y), CanvasInfo);
        }

        private Tuple<double, double> CursorCoords = Tuple.Create<double, double>(500, CanvasInfo.Height / 2);

        protected override void OnAppearing()
        {
            base.OnAppearing();
            gameRunning = true;

            // Hide cursor on UWP
            MessagingCenter.Send<object>(this, "HideCursor");

            // Event-Driven update of the cursor location
            MessagingCenter.Subscribe<object, Tuple<double, double>>(this, "MouseMoved", (s, coord) =>
            {
                if (!App.IsMobile)
                    CursorCoords = coord;   // Desktop Mode
            });

            MessagingCenter.Subscribe<object, Tuple<double, double>>(this, "MouseDrag", (s, distances) =>
            {
                CursorCoords = Tuple.Create(CursorCoords.Item1 + distances.Item1, CursorCoords.Item2 + distances.Item2);   // Mobile Mode
            });

            // When user clicks or screen is pressed
            MessagingCenter.Subscribe<object, Tuple<double, double>>(this, "MouseDown", (s, coord) =>
            {
                if (!App.IsMobile || (App.IsMobile && coord.Item1 < FireButtonWidth && coord.Item2 > (CanvasInfo.Height - FireButtonHeight)))
                {
                    if (Ship.EquippedGun.CanFire)
                    {
                        Ship.EquippedGun.IsFiring = true;
                        Ship.EquippedGun.FireBullet();
                        Ship.EquippedGun.CanFire = false;
                        Device.StartTimer(TimeSpan.FromSeconds(60 / Ship.EquippedGun.ShotSpeed), () =>
                        {
                            Ship.EquippedGun.CanFire = true;
                            if (Ship.EquippedGun.IsFiring)
                            {
                              Ship.EquippedGun.FireBullet();
                              Ship.EquippedGun.CanFire = false;
                            }
                            return Ship.EquippedGun.IsFiring;
                      });
                    }
                }

            });

            // When click or finger is released
            MessagingCenter.Subscribe<object, Tuple<double, double>>(this, "MouseUp", (s, coord) =>
            {
                if (!App.IsMobile || (App.IsMobile && coord.Item1 < FireButtonWidth && coord.Item2 > (CanvasInfo.Height - FireButtonHeight)))
                    Ship.EquippedGun.IsFiring = false;
            });

            Ship.EquipGun(new SpreadGun());

            // GameLoop
            Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
            {

                // Update location of ship sprite
                Ship.SetFramePosition(CursorCoords, CanvasInfo);

                // Update rotation of ship sprite
                Ship.SetRotation(CursorCoords, CanvasInfo);

                // Update the canvas
                MainCanvas.InvalidateSurface();

                return gameRunning;
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            gameRunning = false;
        }
    }

    public static class Ship
    {
        // Equation of locus for ship: (X - (info.Width / 2)) ^ 2 + (Y - (info.Height / 2)) ^ 2 == 32400
        public static bool IsFlying = false;
        public static float X { get; set; } = 0;
        public static float Y { get; set; } = 0;
        public static float GraphicRotation { get; set; } = 0;
        public static double ShotAngle { get; set; }
        public static Gun EquippedGun { get; private set; }

        public static void SetRotation(Tuple<double, double> cursorCoord, SKImageInfo canvasInfo)
        {
            // Calculate the angle of the cursor point relative to the rocket
            var angleFromCursor = Math.Atan((Y - cursorCoord.Item2) / (X - cursorCoord.Item1));

            // Calculate the angle of the rocket relative to the cursor point
            var angleToCursor = Math.Atan((cursorCoord.Item1 - X) / (cursorCoord.Item2 - Y));

            // Set ship rotation
            if (cursorCoord.Item1 > X)
            {
                GraphicRotation = (float)angleFromCursor + (float)Math.PI / 2;   // Cursor is on the right hand side of the ship
            }
            else
            {
                GraphicRotation = (float)angleFromCursor - (float)Math.PI / 2;   // Cursor is on the  left hand side of the ship
            }

            // Set angle on which bullets are shot
            ShotAngle = angleToCursor - Math.PI;

            // Pythagoras theorem to work out displacements from the centre
            var shipCentreDisplacement = Math.Sqrt(Math.Pow(X - (canvasInfo.Width/2), 2) + Math.Pow(Y - (canvasInfo.Height / 2), 2));
            var cursorCentreDisplacement = Math.Sqrt(Math.Pow(cursorCoord.Item1 - (canvasInfo.Width / 2), 2) + Math.Pow(cursorCoord.Item2 - (canvasInfo.Height / 2), 2));
            
            // If the cursor is closer to the centre, the shot angle must be flipped
            if (cursorCentreDisplacement < shipCentreDisplacement)
                ShotAngle += Math.PI;

            if (cursorCoord.Item2 > canvasInfo.Height / 2)         // Cursor in in the lower half of the screen and cursor is below ship
                ShotAngle += Math.PI;
            else if (cursorCoord.Item2 == canvasInfo.Height / 2 && cursorCoord.Item2 > Y)   // Cursor in in the vertical centre of the screen
            {              
                ShotAngle = angleToCursor + Math.PI;
                if (cursorCoord.Item1 > canvasInfo.Width / 2)
                    ShotAngle += Math.PI;
            }

        }

        public static void SetFramePosition(Tuple<double, double> cursorCoord, SKImageInfo canvasInfo)
        {
            // Initialisation
            float destX;
            float destY;
            float changeX;
            float changeY;

            // Calculate angle of cursor point relative to the centre
            var cursorCentreAngle = Math.Atan(((canvasInfo.Width / 2) - cursorCoord.Item1) / ((canvasInfo.Height / 2) - cursorCoord.Item2));

            // Calculate vertical distance relative to the centre
            var relY = 220 * Math.Cos(cursorCentreAngle);

            // Calculate horizontal distance relative to the centre
            var relX = 220 * Math.Sin(cursorCentreAngle);

            // Calculate absolute distances
            if (cursorCoord.Item2 > (canvasInfo.Height / 2))
            {
                // Cursor is in lower half of the screen
                destX = (float)((canvasInfo.Width / 2) + relX);
                destY = (float)((canvasInfo.Height / 2) + relY);
            }
            else
            {
                // Cursor is in upper half of the screen
                destX = (float)((canvasInfo.Width / 2) - relX);
                destY = (float)((canvasInfo.Height / 2) - relY);
            }

            // Calculate difference between current and destination coordinates
            float diffX = destX - X;
            float diffY = destY - Y;

            // Calculate distances to be moved in this frame
            changeX = diffX / 25;
            changeY = diffY / 25;

            // Increment ship coordinates by calculated distances
            X += changeX;
            Y += changeY;

        }

        public static void SetAbsolutePosition(Tuple<double, double> cursorCoord, SKImageInfo canvasInfo)
        {

            // Calculate angle of cursor point relative to the centre
            var cursorCentreAngle = Math.Atan(((canvasInfo.Width / 2) - cursorCoord.Item1) / ((canvasInfo.Height / 2) - cursorCoord.Item2));

            // Calculate vertical distance relative to the centre
            var relY = 220 * Math.Cos(cursorCentreAngle);

            // Calculate horizontal distance relative to the centre
            var relX = 220 * Math.Sin(cursorCentreAngle);

            // If the cursor is in the lower half of the screen
            if (cursorCoord.Item2 > (canvasInfo.Height / 2))
            {
                X = (float)((canvasInfo.Width / 2) + relX);
                Y = (float)((canvasInfo.Height / 2) + relY);
            }
            else
            {
                X = (float)((canvasInfo.Width / 2) - relX);
                Y = (float)((canvasInfo.Height / 2) - relY);
            }
        }

        public static void EquipGun(Gun gun)
        {
            EquippedGun = gun;
        }
    }

    public abstract class Gun
    {
        public int ClipSize { get; set; }
        public int BulletsRemaining { get; set; }
        public int BulletSpeed { get; set; }
        public TimeSpan ReloadTime { get; set; }
        public double ShotSpeed { get; set; }               // In shots/min
        public bool CanFire { get; set; } = true;
        public bool IsFiring { get; set; } = false;
        public bool IsReloading { get; set; } = false;

        public virtual void FireBullet()
        {
            if (BulletsRemaining > 0)
            {   
                // Fire bullet then decrement ammo
                MainPage.FlyingBullets.Add(new Bullet() { ReleasedFrom = Tuple.Create(Ship.X, Ship.Y), Angle = Ship.ShotAngle, Speed = BulletSpeed });
                BulletsRemaining--;
            }
            else 
            {
                // Reload - time delay
                if (!IsReloading)
                {
                    IsReloading = true;
                    Device.StartTimer(ReloadTime, () =>
                     {
                         BulletsRemaining = ClipSize;
                         IsReloading = false;
                         return false;
                     });
                }
            }
        }
    }
    
    public class BasicGun : Gun
    {
        public BasicGun()
        {
            ClipSize = 12;
            BulletsRemaining = ClipSize;
            BulletSpeed = 1;
            ReloadTime = TimeSpan.FromSeconds(5);
            ShotSpeed = 200;
        }
    }

    public class MachineGun : Gun
    {
        public MachineGun()
        {
            ClipSize = 20;
            BulletsRemaining = ClipSize;
            BulletSpeed = 2;
            ReloadTime = TimeSpan.FromSeconds(5);
            ShotSpeed = 800;
        }
    }

    public class SpreadGun : Gun
    {
        public SpreadGun()
        {
            ClipSize = 8;
            BulletsRemaining = ClipSize;
            BulletSpeed = 1;
            ReloadTime = TimeSpan.FromSeconds(6);
            ShotSpeed = 200;
        }

        public override void FireBullet()
        {
            if (BulletsRemaining > 0)
            {
                // Fire bullet then decrement ammo
                MainPage.FlyingBullets.Add(new Bullet() { ReleasedFrom = Tuple.Create(Ship.X, Ship.Y), Angle = Ship.ShotAngle - (Math.PI / 8), Speed = BulletSpeed });
                MainPage.FlyingBullets.Add(new Bullet() { ReleasedFrom = Tuple.Create(Ship.X, Ship.Y), Angle = Ship.ShotAngle - (Math.PI / 16), Speed = BulletSpeed });
                MainPage.FlyingBullets.Add(new Bullet() { ReleasedFrom = Tuple.Create(Ship.X, Ship.Y), Angle = Ship.ShotAngle, Speed = BulletSpeed });
                MainPage.FlyingBullets.Add(new Bullet() { ReleasedFrom = Tuple.Create(Ship.X, Ship.Y), Angle = Ship.ShotAngle + (Math.PI / 16), Speed = BulletSpeed });
                MainPage.FlyingBullets.Add(new Bullet() { ReleasedFrom = Tuple.Create(Ship.X, Ship.Y), Angle = Ship.ShotAngle + (Math.PI / 8), Speed = BulletSpeed });
                BulletsRemaining--;
            }
            else
            {
                // Reload - time delay
                if (!IsReloading)
                {
                    IsReloading = true;
                    Device.StartTimer(ReloadTime, () =>
                    {
                        BulletsRemaining = ClipSize;
                        IsReloading = false;
                        return false;
                    });
                }
            }
        }

    }

    public class Bullet
    {
        public int Speed { get; set; }
        public Tuple<float, float> ReleasedFrom { get; set; }
        public double Angle;
        public int Displacement { get; set; } = 0;
    }

}
