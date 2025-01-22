using System;
using Avalonia.Media.Imaging;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using System.IO;

// This class performs bitmap operations
// Original author: Jeffrey Bostoen
// Ported by: Eric "Goosifer" Galvan

namespace GrAPE.Models {
    public class DirectBitmap : IDisposable {
        private GCHandle _bitsHandle;
        private int[] _bits;
        private Bitmap _bitmap;
        private int _height;
        private int _width;
        private bool _disposed;

        // -------------------------------------------------------------------- Bitmap Structure

        // Bitmap object
        public Bitmap Bitmap {
            get => _bitmap;
            set => _bitmap = value;
        }

        // Bits
        public int[] Bits {
            get => _bits;
            set => _bits = value;
        }

        // Disposed
        public bool Disposed {
            get => _disposed;
            set => _disposed = value;
        }

        // Height
        public int Height {
            get => _height;
            set => _height = value;
        }

        // Width
        public int Width {
            get => _width;
            set => _width = value;
        }

        protected GCHandle BitsHandle {
            get => _bitsHandle;
            set => _bitsHandle = value;
        }

        // -------------------------------------------------------------------- Constructors

        // Initializes new instance with width and height
        public DirectBitmap(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            // Allocate the pixel array and pin it
            // Pinning allows the GC to move the array around
            // GC = Garbage Collector
            this.Bits = new int[width * height];
            this.BitsHandle = GCHandle.Alloc(this.Bits, GCHandleType.Pinned);

            // WriteableBitmap using the pixel buffer
            this.Bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96), // Default DPI
                PixelFormat.Bgra8888, // Equivalent to 32bpp PArgb
                AlphaFormat.Premul // Premultiplied Alpha format
            );
        }

        /// <summary>
        /// Initializes a new instance from an existing Avalonia Bitmap.
        /// </summary>
        public DirectBitmap(Bitmap objBitmap)
        {
            this.Width = objBitmap.PixelSize.Width;
            this.Height = objBitmap.PixelSize.Height;

            this.Bits = new int[this.Width * this.Height];
            this.BitsHandle = GCHandle.Alloc(this.Bits, GCHandleType.Pinned);

            this.Bitmap = new WriteableBitmap(
                new PixelSize(this.Width, this.Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul
            );

            // Copy pixel data from the source bitmap
            using (var framebuffer = this.Bitmap.Lock())
            {
                for (int y = 0; y < this.Height; y++)
                {
                    for (int x = 0; x < this.Width; x++)
                    {
                        var color = objBitmap.GetPixel(x, y);
                        SetPixel(x, y, color);
                    }
                }
            }
        }

        // -------------------------------------------------------------------- Methods

        // Gets pixel on DirectBitmap
        public Avalonia.Media.Color GetPixel(int x, int y) {
            int index = x + (y * Width);
            int col = Bits[index];
            return Avalonia.Media.Color.FromArgb((byte)((col >> 24) & 0xFF), (byte)((col >> 16) & 0xFF), (byte)((col >> 8) & 0xFF), (byte)(col & 0xFF));
        }

         /// <summary>
        /// Sets a pixel in the bitmap.
        /// </summary>
        public void SetPixel(int x, int y, Avalonia.Media.Color color)
        {
            using (var framebuffer = Bitmap.Lock())
            {
                unsafe
                {
                    int bytesPerPixel = 4;
                    int pixelIndex = (y * framebuffer.RowBytes) + (x * bytesPerPixel);
                    byte* buffer = (byte*)framebuffer.Address;

                    buffer[pixelIndex] = color.B;
                    buffer[pixelIndex + 1] = color.G;
                    buffer[pixelIndex + 2] = color.R;
                    buffer[pixelIndex + 3] = color.A; // Premultiplied alpha expected
                }
            }
        }


        // -------------------------------------------------------------------- Methods

        // Sets pixel on DirectBitmap
        public void SetPixel(int x, int y, Color color) {
            int index = x + (y * Width);
            int col = color.ToArgb;
            Bits[index] = col;
        }

        // Gets pixel on DirectBitmap
        public Color GetPixel(int x, int y) {
            int index = x + (y * Width);
            int col = Bits[index];
            return Color.FromArgb(col);
        }

        // Disposes object
        public void Dispose() {
            if (Disposed) {
                return;
            }

            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();

            GC.SuppressFinalize(this);

        }
    }
}
