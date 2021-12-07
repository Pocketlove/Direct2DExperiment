using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Threading;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using D3D11 = SharpDX.Direct3D11;
using D3D = SharpDX.Direct3D;
using D2D = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;

namespace Direct2D_Experiment
{
    public partial class Form1 : Form
    {
        DXGI.Device dxgiDevice;

        D3D11.Device d3dDevice;
        SwapChain swapChain;

        D2D.Device d2dDevice;
        D2D.DeviceContext deviceContext;

        DXGI.Surface surface;

        D2D.Bitmap bitmapTarget;
        public static D2D.SolidColorBrush brush;

        D2D.Bitmap image;

        SharpDX.DirectWrite.Factory dwFactory;

        System.Timers.Timer timer;

        Ellipse ellipse;
        SharpDX.RectangleF rectangle;
        RoundedRectangle roundedRectangle;

        SharpDX.DirectWrite.TextFormat textFormat;

        public Form1()
        {
            InitializeRendering();

            InitializeComponent();

            this.SizeChanged += new EventHandler(Render);
        }

        void InitializeRendering()
        {
            SwapChainDescription chainDescription = new SwapChainDescription()
            {
                BufferCount = 1,
                IsWindowed = true,
                ModeDescription = new ModeDescription(1186, 760, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                SwapEffect = SwapEffect.Discard,
                OutputHandle = this.Handle,
                Flags = SwapChainFlags.GdiCompatible,
                Usage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
            };

            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, chainDescription, out d3dDevice, out swapChain);
            dxgiDevice = d3dDevice.QueryInterface<DXGI.Device>();
            d2dDevice = new D2D.Device(dxgiDevice);

            deviceContext = new D2D.DeviceContext(d2dDevice, DeviceContextOptions.EnableMultithreadedOptimizations);

            surface = swapChain.GetBackBuffer<Surface>(0);
            bitmapTarget = new D2D.Bitmap1(deviceContext, surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied), 120, 120, BitmapOptions.Target | BitmapOptions.CannotDraw | BitmapOptions.GdiCompatible));

            brush = new SolidColorBrush(deviceContext, SharpDX.Color.Crimson, new BrushProperties() { Opacity = 1 });

            dwFactory = new SharpDX.DirectWrite.Factory();

            deviceContext.Target = bitmapTarget;

            deviceContext.DotsPerInch = new Size2F(120, 120);
            deviceContext.AntialiasMode = AntialiasMode.PerPrimitive;
            deviceContext.TextAntialiasMode = TextAntialiasMode.Cleartype;
            deviceContext.UnitMode = UnitMode.Pixels;
            deviceContext.StrokeWidth = 3;


            ellipse = new Ellipse(new SharpDX.Vector2(100, 100), 60, 60);
            rectangle = new SharpDX.RectangleF(60, 60, 180, 120);
            roundedRectangle = new RoundedRectangle()
            {
                Rect = new SharpDX.RectangleF(50, 250, 450, 150),
                RadiusX = 10,
                RadiusY = 10
            };

            textFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Verdana", 10);
        }

        void Render(object sender, EventArgs elapsedEventHandler)
        {           
            deviceContext.BeginDraw();
            deviceContext.Clear(SharpDX.Color.White);
            deviceContext.DrawEllipse(ellipse, brush);
            deviceContext.DrawRectangle(rectangle, brush);
            deviceContext.FillRoundedRectangle(roundedRectangle, brush);
            deviceContext.DrawText("flex", textFormat, rectangle, brush);
            deviceContext.EndDraw();

            swapChain.Present(0, PresentFlags.None);

            Thread.Sleep(5);
        }

        void DisposeDX()
        {
            dxgiDevice.Dispose();
            swapChain.Dispose();
            d2dDevice.Dispose();
            deviceContext.Dispose();
            surface.Dispose();
            bitmapTarget.Dispose();
            brush.Dispose();
            timer.Dispose();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            foreach(PictureTest pictureTest in Controls.OfType<PictureTest>())
            {
                Controls.Remove(pictureTest);
                
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisposeDX();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            PictureTest[,] pictureTests = new PictureTest[20,20];

            for(int x = 0; x < 20; x++)
            {
                for(int y = 0; y < 20; y++)
                {
                    pictureTests[x, y] = new PictureTest(d2dDevice, dxgiDevice, dwFactory);

                    pictureTests[x, y].Location = new System.Drawing.Point(x * 50, y * 50);

                    pictureTests[x, y].InitializeRendering();

                    Controls.Add(pictureTests[x, y]);
                    //ActiveForm.SizeChanged += new EventHandler(pictureTests[x, y].Render);

                }
            }

            Thread renderer = new Thread(Renderer);
            renderer.Start();
        }

        private void Renderer()
        {
            while (true)
            {
                foreach (PictureTest picture in Controls.OfType<PictureTest>())
                {
                    picture.Render();
                }
            }
        }

        private class PanelTest : Panel
        {
            public DXGI.Device dxgiDevice { get; set; }
            public D2D.Device d2dDevice { get; set; }

            D2D.DeviceContext deviceContext { get; set; }

            SwapChain swapChain;
            Surface surface;

            D2D.Bitmap bitmapTarget;
            public SharpDX.DirectWrite.Factory dwFactory { get; set; }

            SharpDX.DirectWrite.TextFormat textFormat;

            Ellipse ellipse;
            SharpDX.RectangleF rectangle;
            RoundedRectangle roundedRectangle;

            public PanelTest(D2D.Device d2Device, DXGI.Device dxDevice, SharpDX.DirectWrite.Factory directFactory)
            {
                dxgiDevice = dxDevice;
                d2dDevice = d2Device;
                dwFactory = directFactory;

                Size = new Size(500, 2000);
                Location = new System.Drawing.Point(0, 0);
                BackColor = System.Drawing.Color.Transparent;

                AutoScroll = true;

                this.MouseWheel += new MouseEventHandler(OnScroll);
            }

            public void OnScroll(object sender, MouseEventArgs eventArgs)
            {
                Panel panel = (Panel)sender;

                if (panel.VerticalScroll.Value >= panel.VerticalScroll.Minimum && panel.VerticalScroll.Value <= panel.VerticalScroll.Maximum)
                {
                    ellipse = new Ellipse(new SharpDX.Vector2(ellipse.Point.X, 50 + panel.VerticalScroll.Value / 3), ellipse.RadiusX, ellipse.RadiusY);
                    rectangle = new SharpDX.RectangleF(60, 60, 90, 90);
                    roundedRectangle = new RoundedRectangle()
                    {
                        Rect = new SharpDX.RectangleF(roundedRectangle.Rect.Left, 50 + panel.VerticalScroll.Value / 3, 500, 2000),
                        RadiusX = 10,
                        RadiusY = 10
                    };

                    Render();
                }
            }

            public void InitializeRendering()
            {
                SwapChainDescription chainDescription = new SwapChainDescription()
                {
                    BufferCount = 2,
                    IsWindowed = true,
                    ModeDescription = new ModeDescription(500, 2000, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                    SwapEffect = SwapEffect.FlipSequential,
                    OutputHandle = this.Handle,
                    Flags = SwapChainFlags.GdiCompatible,
                    Usage = Usage.RenderTargetOutput,
                    SampleDescription = new SampleDescription(1, 0),
                };

                swapChain = new SwapChain(new DXGI.Factory2(), dxgiDevice, chainDescription);
                surface = swapChain.GetBackBuffer<Surface>(0);

                deviceContext = new D2D.DeviceContext(d2dDevice, DeviceContextOptions.EnableMultithreadedOptimizations);

                bitmapTarget = new D2D.Bitmap1(deviceContext, surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied), 120, 120, BitmapOptions.Target | BitmapOptions.CannotDraw | BitmapOptions.GdiCompatible));
                deviceContext.Target = bitmapTarget;

                deviceContext.DotsPerInch = new Size2F(120, 120);
                deviceContext.AntialiasMode = AntialiasMode.PerPrimitive;
                deviceContext.TextAntialiasMode = TextAntialiasMode.Cleartype;
                deviceContext.UnitMode = UnitMode.Pixels;
                deviceContext.StrokeWidth = 3;

                brush = new SolidColorBrush(deviceContext, SharpDX.Color.Crimson, new BrushProperties() { Opacity = 1 });

                ellipse = new Ellipse(new SharpDX.Vector2(50, 50), 20, 20);
                rectangle = new SharpDX.RectangleF(60, 60, 90, 90);
                roundedRectangle = new RoundedRectangle()
                {
                    Rect = new SharpDX.RectangleF(50, 20, 500, 2000),
                    RadiusX = 10,
                    RadiusY = 10
                };

                textFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Verdana", 10);

            }

            public void Render()
            {
                deviceContext.BeginDraw();
                deviceContext.Clear(SharpDX.Color.White);
                deviceContext.DrawEllipse(ellipse, brush);
                deviceContext.DrawRectangle(rectangle, brush);
                deviceContext.FillRoundedRectangle(roundedRectangle, brush);
                deviceContext.DrawText("flex", textFormat,
                    rectangle, brush);
                deviceContext.EndDraw();

                swapChain.Present(0, PresentFlags.None);
            }
        }

        private class PictureTest : PictureBox
        {
            public DXGI.Device dxgiDevice { get; set; }
            public D2D.Device d2dDevice { get; set; }

            D2D.DeviceContext deviceContext { get; set; }

            SwapChain swapChain;
            Surface surface;

            D2D.Bitmap bitmapTarget;
            public SharpDX.DirectWrite.Factory dwFactory { get; set; }

            SharpDX.DirectWrite.TextFormat textFormat;

            Ellipse ellipse;
            SharpDX.RectangleF rectangle;
            RoundedRectangle roundedRectangle;

            public PictureTest(D2D.Device d2Device, DXGI.Device dxDevice, SharpDX.DirectWrite.Factory directFactory)
            {
                dxgiDevice = dxDevice;
                d2dDevice = d2Device;
                dwFactory = directFactory;

                Size = new Size(50, 50);
                Location = new System.Drawing.Point(500, 200);
                BackColor = System.Drawing.Color.Transparent;
                Image = null;
            }

            public void InitializeRendering()
            {
                SwapChainDescription chainDescription = new SwapChainDescription()
                {
                    BufferCount = 2,
                    IsWindowed = true,
                    ModeDescription = new ModeDescription(50, 50, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                    SwapEffect = SwapEffect.Sequential,
                    OutputHandle = this.Handle,
                    Flags = SwapChainFlags.GdiCompatible,
                    Usage = Usage.RenderTargetOutput,
                    SampleDescription = new SampleDescription(1, 0),
                };

                swapChain = new SwapChain(new DXGI.Factory2(), dxgiDevice, chainDescription);
                surface = swapChain.GetBackBuffer<Surface>(0);

                deviceContext = new D2D.DeviceContext(d2dDevice, DeviceContextOptions.EnableMultithreadedOptimizations);

                bitmapTarget = new D2D.Bitmap1(deviceContext, surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied), 120, 120, BitmapOptions.Target | BitmapOptions.CannotDraw | BitmapOptions.GdiCompatible));
                deviceContext.Target = bitmapTarget;

                deviceContext.DotsPerInch = new Size2F(120, 120);
                deviceContext.AntialiasMode = AntialiasMode.PerPrimitive;
                deviceContext.TextAntialiasMode = TextAntialiasMode.Cleartype;
                deviceContext.UnitMode = UnitMode.Pixels;
                deviceContext.StrokeWidth = 3;

                brush = new SolidColorBrush(deviceContext, SharpDX.Color.Crimson, new BrushProperties() { Opacity = 1 });

                ellipse = new Ellipse(new SharpDX.Vector2(50, 50), 20, 20);
                rectangle = new SharpDX.RectangleF(60, 60, 90, 90);
                roundedRectangle = new RoundedRectangle()
                {
                    Rect = new SharpDX.RectangleF(20, 20, 70, 50),
                    RadiusX = 10,
                    RadiusY = 10
                };

                textFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Verdana", 10);

            }

            public void Render()
            {
                if (Parent.Width >= Left - Width && Parent.Height >= Top - Height)
                {
                    deviceContext.BeginDraw();
                    deviceContext.Clear(SharpDX.Color.White);
                    deviceContext.DrawEllipse(ellipse, brush);
                    deviceContext.DrawRectangle(rectangle, brush);
                    deviceContext.FillRoundedRectangle(roundedRectangle, brush);
                    deviceContext.DrawText("flex", textFormat,
                        rectangle, brush);
                    deviceContext.EndDraw();

                    swapChain.Present(4, PresentFlags.None);
                }
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            PanelTest panelTest = new PanelTest(d2dDevice, dxgiDevice, dwFactory);
            Label label = new Label();
            label.Location = new System.Drawing.Point(panelTest.Width, panelTest.Height * 2);

            Controls.Add(panelTest);
            panelTest.Controls.Add(label);

            panelTest.InitializeRendering();
            panelTest.Render();
        }
    }
}
