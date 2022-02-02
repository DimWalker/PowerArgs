﻿using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace PowerArgs.Cli;
    /// <summary>
    /// A data structure representing a 2d image that can be pained in
    /// a console window
    /// </summary>
    public class ConsoleBitmap
    {
        private static ChunkPool chunkPool = new ChunkPool();
        private static ConsolePixelPool pixelPool = new ConsolePixelPool();
        private static List<Chunk> chunksOnLine = new List<Chunk>();
        private static PaintBuffer paintBuilder = new PaintBuffer();


        // larger is faster, but may cause gaps
        private const float DrawPrecision = .5f;

        /// <summary>
        /// The width of the image, in number of character pixels
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of the image, in number of character pixels
        /// </summary>
        public int Height { get; private set; }


        /// <summary>
        /// The character to draw when calling the various Draw methods
        /// </summary>
        public ConsoleCharacter Pen { get; set; }

        /// <summary>
        /// The console to target when the Paint method is called 
        /// </summary>
        public IConsoleProvider Console { get; set; }

        private ConsolePixel[][] pixels;

        private int lastBufferWidth;


        /// <summary>
        /// Creates a new ConsoleBitmap
        /// </summary>
        /// <param name="w">the width of the image</param>
        /// <param name="h">the height of the image</param>
        public ConsoleBitmap(int w, int h) : this(new Size(w, h)) { }

        /// <summary>
        /// Creates a new ConsoleBitmap
        /// </summary>
        /// <param name="bounds">the area of the image</param>
        public ConsoleBitmap(Size bounds)
        {
            this.Width = bounds.Width;
            this.Height = bounds.Height;
            this.Console = ConsoleProvider.Current;
            this.lastBufferWidth = this.Console.BufferWidth;
            this.Pen = new ConsoleCharacter('*');
            pixels = new ConsolePixel[this.Width][];
            for (int x = 0; x < this.Width; x++)
            {
                pixels[x] = new ConsolePixel[this.Height];
                for (int y = 0; y < pixels[x].Length; y++)
                {
                var p = pixelPool.Rent();
                p.Value = new ConsoleCharacter(' ');
                    pixels[x][y] = p;
                }
            }
        }

    ~ConsoleBitmap()
    {
        Return();
    }

    private void Return()
    {
        if (pixels == null) return;
        for (int x = 0; x < this.Width; x++)
        {
            for (int y = 0; y < pixels[x].Length; y++)
            {
                pixelPool.Return(pixels[x][y]);
            }
        }
        pixels = null;
    }

    /// <summary>
    /// Converts this ConsoleBitmap to a ConsoleString
    /// </summary>
    /// <param name="trimMode">if false (the default), unformatted whitespace at the end of each line will be included as whitespace in the return value. If true, that whitespace will be trimmed from the return value.</param>
    /// <returns>the bitmap as a ConsoleString</returns>
    public ConsoleString ToConsoleString(bool trimMode = false)
        {
            List<ConsoleCharacter> chars = new List<ConsoleCharacter>();
            for (var y = 0; y < this.Height; y++)
            {
                for (var x = 0; x < this.Width; x++)
                {
                    if (trimMode && IsRestOfLineWhitespaceWithDefaultBackground(x, y))
                    {
                        break;
                    }
                    else
                    {
                        var pixel = this.GetPixel(x, y);
                        var pixelValue = pixel.Value;
                        chars.Add(pixelValue);
                    }
                }
                if (y < this.Height - 1)
                {
                    chars.Add(new ConsoleCharacter('\n'));
                }
            }

            return new ConsoleString(chars);
        }

        private bool IsRestOfLineWhitespaceWithDefaultBackground(int xStart, int y)
        {
            var defaultBg = new ConsoleCharacter(' ').BackgroundColor;

            for (var x = xStart; x < this.Width; x++)
            {
                if (char.IsWhiteSpace(this.GetPixel(x, y).Value.Value) && this.GetPixel(x, y).Value.BackgroundColor == defaultBg)
                {
                    // this is whitespace
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resizes this image, preserving the data in the pixels that remain in the new area
        /// </summary>
        /// <param name="w">the new width</param>
        /// <param name="h">the new height</param>
        public void Resize(int w, int h)
        {
            if (w == Width && h == Height) return;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                pixelPool.Return(pixels[x][y]);
            }
        }
        var newPixels = new ConsolePixel[w][];
            for (int x = 0; x < w; x++)
            {
                newPixels[x] = new ConsolePixel[h];
                for (int y = 0; y < newPixels[x].Length; y++)
                {
                    var newPix = pixelPool.Rent();
                    newPix.Value = new ConsoleCharacter(' ');
                    newPixels[x][y] = newPix;   
                }
            }

            pixels = newPixels;
            this.Width = w;
            this.Height = h;
            this.Invalidate();
        }

        /// <summary>
        /// Gets the pixel at the given location
        /// </summary>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        /// <returns>the pixel at the given location</returns>
        public ConsolePixel GetPixel(int x, int y)
        {
            return pixels[x][y];
        }

        /// <summary>
        /// Creates a snapshot of the cursor position
        /// </summary>
        /// <returns>a snapshot of the cursor positon</returns>
        public ConsoleSnapshot CreateSnapshot()
        {
            var snapshot = new ConsoleSnapshot(0, 0, Console);
            return snapshot;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Draws the given string onto the bitmap
        /// </summary>
        /// <param name="str">the value to write</param>
        /// <param name="x">the x coordinate to draw the string's fist character</param>
        /// <param name="y">the y coordinate to draw the string's first character </param>
        /// <param name="vert">if true, draw vertically, else draw horizontally</param>
        public void DrawString(string str, int x, int y, bool vert = false)
        {
            DrawString(new ConsoleString(str), x, y, vert);
        }

        /// <summary>
        /// Draws a filled in rectangle bounded by the given coordinates
        /// using the current pen
        /// </summary>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        public void FillRect(int x, int y, int w, int h)
        {
            var maxX = Math.Min(x + w, Width);
            var maxY = Math.Min(y + h, Height);

            var minX = Math.Max(x, 0);
            var minY = Math.Max(y, 0);


            Span<ConsolePixel[]> xSpan = pixels.AsSpan().Slice(minX, maxX - minX);

            for (int xd = 0; xd < xSpan.Length; xd++)
            {
                var ySpan = xSpan[xd].AsSpan(minY, maxY - minY);
                for (var yd = 0; yd < ySpan.Length; yd++)
                {
                    ySpan[yd].Value = Pen;
                }
            }
        }

        /// <summary>
        /// Draws a filled in rectangle bounded by the given coordinates
        /// using the current pen, without performing bounds checks
        /// </summary>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        public void FillRectUnsafe(int x, int y, int w, int h)
        {
            var maxX = x + w;
            var maxY = y + h;

            Span<ConsolePixel[]> xSpan = pixels.AsSpan().Slice(x, maxX - x);

            for (int xd = 0; xd < xSpan.Length; xd++)
            {
                var ySpan = xSpan[xd].AsSpan(y, maxY - y);
                for (var yd = 0; yd < ySpan.Length; yd++)
                {
                    ySpan[yd].Value = Pen;
                }
            }
        }

        /// <summary>
        /// Draws an unfilled in rectangle bounded by the given coordinates
        /// using the current pen
        /// </summary>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        public void DrawRect(int x, int y, int w, int h)
        {
            var maxX = Math.Min(x + w, Width);
            var maxY = Math.Min(y + h, Height);
            var minX = Math.Max(x, 0);
            var minY = Math.Max(y, 0);

            var xEndIndex = maxX - 1;
            var yEndIndex = maxY - 1;

            // left vertical line
            for (var yd = minY; yd < maxY; yd++)
            {
                Compose(minX, yd, Pen);
            }

            // right vertical line
            for (var yd = minY; yd < maxY; yd++)
            {
                Compose(xEndIndex, yd, Pen);
            }

            var xSpan = pixels.AsSpan(minX, maxX - minX);
            // top horizontal line
            for (int xd = 0; xd < xSpan.Length; xd++)
            {
                xSpan[xd][minY].Value = Pen;
            }

            // bottom horizontal line
            for (int xd = 0; xd < xSpan.Length; xd++)
            {
                xSpan[xd][yEndIndex].Value = Pen;
            }
        }

        /// <summary>
        /// Draws the given string onto the bitmap
        /// </summary>
        /// <param name="str">the value to write</param>
        /// <param name="x">the x coordinate to draw the string's fist character</param>
        /// <param name="y">the y coordinate to draw the string's first character </param>
        /// <param name="vert">if true, draw vertically, else draw horizontally</param>
        public void DrawString(ConsoleString str, int x, int y, bool vert = false)
        {
            var xStart = x;


            var span = str.AsSpan();
            for(var i = 0; i < span.Length; i++)
            {
                var character = span[i];
                if (character.Value == '\n')
                {
                    y++;
                    x = xStart;
                }
                else if (character.Value == '\r')
                {
                    // ignore
                }
                else if (IsInBounds(x, y))
                {
                    Compose(x, y, character);
                    if (vert) y++;
                    else x++;
                }
            }
        }

        /// <summary>
        /// Draw a single pixel value at the given point using the current pen, skipping the bounds check. Only call this if you've
        /// already done a bounds check
        /// </summary>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        public void DrawPointUnsafe(int x, int y)
        {
            Compose(x, y, Pen);
        }

        /// <summary>
        /// Draw a single pixel value at the given point using the current pen
        /// </summary>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        public void DrawPoint(int x, int y)
        {
            if (IsInBounds(x, y))
            {
                Compose(x, y, Pen);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Compose(int x, int y, in ConsoleCharacter pen)
        {
            pixels[x][y].Value = pen;
        }

        [ThreadStatic]
        internal static Point[] LineBuffer;

        /// <summary>
        /// Draw a line segment between the given points
        /// </summary>
        /// <param name="x1">the x coordinate of the first point</param>
        /// <param name="y1">the y coordinate of the first point</param>
        /// <param name="x2">the x coordinate of the second point</param>
        /// <param name="y2">the y coordinate of the second point</param>
        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            var len = DefineLineBuffered(x1, y1, x2, y2);
            Point point;
            for (var i = 0; i < len; i++)
            {
                point = LineBuffer[i];
                if (IsInBounds(point.X, point.Y))
                {
                    Compose(point.X, point.Y, Pen);
                }
            }
        }

        public static int DefineLineBuffered(int x1, int y1, int x2, int y2, Point[] buffer = null)
        {
            LineBuffer = LineBuffer ?? new Point[10000];
            buffer = buffer ?? LineBuffer;

            var ret = 0;
            if (x1 == x2)
            {
                int yMin = y1 >= y2 ? y2 : y1;
                int yMax = y1 >= y2 ? y1 : y2;
                for (int y = yMin; y < yMax; y++)
                {
                    buffer[ret++] = new Point(x1, y);
                }
            }
            else if (y1 == y2)
            {
                int xMin = x1 >= x2 ? x2 : x1;
                int xMax = x1 >= x2 ? x1 : x2;
                for (int x = xMin; x < xMax; x++)
                {
                    buffer[ret++] = new Point(x, y1);
                }
            }
            else
            {
                float slope = ((float)y2 - y1) / ((float)x2 - x1);

                int dx = Math.Abs(x1 - x2);
                int dy = Math.Abs(y1 - y2);

                Point last = new Point();
                if (dy > dx)
                {
                    for (float x = x1; x < x2; x += DrawPrecision)
                    {
                        float y = slope + (x - x1) + y1;
                        int xInt = ConsoleMath.Round(x);
                        int yInt = ConsoleMath.Round(y);
                        var p = new Point(xInt, yInt);
                        if (p.Equals(last) == false)
                        {
                            buffer[ret++] = p;
                            last = p;
                        }
                    }

                    for (float x = x2; x < x1; x += DrawPrecision)
                    {
                        float y = slope + (x - x1) + y1;
                        int xInt = ConsoleMath.Round(x);
                        int yInt = ConsoleMath.Round(y);
                        var p = new Point(xInt, yInt);
                        if (p.Equals(last) == false)
                        {
                            buffer[ret++] = p;
                            last = p;
                        }
                    }
                }
                else
                {
                    for (float y = y1; y < y2; y += DrawPrecision)
                    {
                        float x = ((y - y1) / slope) + x1;
                        int xInt = ConsoleMath.Round(x);
                        int yInt = ConsoleMath.Round(y);
                        var p = new Point(xInt, yInt);
                        if (p.Equals(last) == false)
                        {
                            buffer[ret++] = p;
                            last = p;
                        }
                    }

                    for (float y = y2; y < y1; y += DrawPrecision)
                    {
                        float x = ((y - y1) / slope) + x1;
                        int xInt = ConsoleMath.Round(x);
                        int yInt = ConsoleMath.Round(y);
                        var p = new Point(xInt, yInt);
                        if (p.Equals(last) == false)
                        {
                            buffer[ret++] = p;
                            last = p;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Makes a copy of this bitmap
        /// </summary>
        /// <returns>a copy of this bitmap</returns>
        public ConsoleBitmap Clone()
        {
            var ret = new ConsoleBitmap(Width, Height);
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    ret.Pen = this.GetPixel(x, y).Value;
                    ret.DrawPoint(x, y);
                }
            }
            return ret;
        }

        public void CloneTo(ConsoleBitmap ret)
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    ret.Pen = this.GetPixel(x, y).Value;
                    ret.DrawPoint(x, y);
                }
            }
        }

       

        private bool wasFancy;
        public void Paint()
        {
            if (ConsoleProvider.Fancy != wasFancy)
            {
                this.Invalidate();
                wasFancy = ConsoleProvider.Fancy;

                if (ConsoleProvider.Fancy)
                {
                    Console.Write(Ansi.Cursor.Hide + Ansi.Text.BlinkOff);
                }
            }

            if (ConsoleProvider.Fancy)
            {
                PaintNew();
            }
            else
            {
                PaintOld();
            }
        }

        /// <summary>
        /// Paints this image to the current Console
        /// </summary>
        public void PaintOld()
        {
            if (Console.WindowHeight == 0) return;

            var changed = false;
            if (lastBufferWidth != this.Console.BufferWidth)
            {
                lastBufferWidth = this.Console.BufferWidth;
                Invalidate();
                this.Console.Clear();
                changed = true;
            }
            try
            {
                Chunk currentChunk = null;
                var chunksOnLine = new List<Chunk>();
                ConsolePixel pixel;
                char val;
                RGB fg;
                RGB bg;
                bool pixelChanged;
                for (int y = 0; y < Height; y++)
                {
                    var changeOnLine = false;
                    for (int x = 0; x < Width; x++)
                    {
                        pixel = pixels[x][y];
                        pixelChanged = pixel.HasChanged;
                        changeOnLine = changeOnLine || pixelChanged;
                        val = pixel.Value.Value;
                        fg = pixel.Value.ForegroundColor;
                        bg = pixel.Value.BackgroundColor;
                        if (currentChunk == null)
                        {
                            // first pixel always gets added to the current empty chunk
                            currentChunk = new Chunk(Width);
                            currentChunk.FG = fg;
                            currentChunk.BG = bg;
                            currentChunk.HasChanged = pixelChanged;
                            currentChunk.Add(val);
                        }
                        else if (currentChunk.HasChanged == false && pixelChanged == false)
                        {
                            // characters that have not changed get chunked even if their styles differ
                            currentChunk.Add(val);
                        }
                        else if (currentChunk.HasChanged && pixelChanged && fg == currentChunk.FG && bg == currentChunk.BG)
                        {
                            // characters that have changed only get chunked if their styles match to minimize the number of writes
                            currentChunk.Add(val);
                        }
                        else
                        {
                            // either the styles of consecutive changing characters differ or we've gone from a non changed character to a changed one
                            // in either case we end the current chunk and start a new one
                            chunksOnLine.Add(currentChunk);
                            currentChunk = new Chunk(Width);
                            currentChunk.FG = fg;
                            currentChunk.BG = bg;
                            currentChunk.HasChanged = pixelChanged;
                            currentChunk.Add(val);
                        }
                        pixel.LastDrawnValue = pixel.Value;
                    }

                    if (currentChunk.Length > 0)
                    {
                        chunksOnLine.Add(currentChunk);
                    }

                    currentChunk = null;

                    if (changeOnLine)
                    {
                        Console.CursorTop = y; // we know there will be a change on this line so move the cursor top
                        var left = 0;
                        var leftChanged = true;
                        for (var i = 0; i < chunksOnLine.Count; i++)
                        {
                            var chunk = chunksOnLine[i];
                            if (chunk.HasChanged)
                            {
                                if (leftChanged)
                                {
                                    Console.CursorLeft = left;
                                    leftChanged = false;
                                }

                                Console.ForegroundColor = chunk.FG;
                                Console.BackgroundColor = chunk.BG;
                                Console.Write(chunk.ToString());
                                left += chunk.Length;
                                changed = true;
                            }
                            else
                            {
                                left += chunk.Length;
                                leftChanged = true;
                            }
                        }
                    }
                    chunksOnLine.Clear();
                }

                if (changed)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = 0;
                    Console.ForegroundColor = ConsoleString.DefaultForegroundColor;
                    Console.BackgroundColor = ConsoleString.DefaultBackgroundColor;
                }
            }
            catch (IOException)
            {
                Invalidate();
                PaintOld();
            }
            catch (ArgumentOutOfRangeException)
            {
                Invalidate();
                PaintOld();
            }
        }

        public void Dump(string dest)
        {
            using (Bitmap b = new Bitmap(Width * 10, Height * 20))
            using (var g = Graphics.FromImage(b))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var pix = GetPixel(x, y);
                        var bgColor = Color.FromArgb(pix.Value.BackgroundColor.R, pix.Value.BackgroundColor.G, pix.Value.BackgroundColor.B);
                        var fgColor = Color.FromArgb(pix.Value.ForegroundColor.R, pix.Value.ForegroundColor.G, pix.Value.ForegroundColor.B);
                        var imgX = x * 10;
                        var imgY = y * 20;
                        g.FillRectangle(new SolidBrush(bgColor), imgX, imgY, 10, 20);
                        g.DrawString(pix.Value.Value.ToString(), new Font("Consolas", 12), new SolidBrush(fgColor), imgX-2, imgY);
                    }
                }
                b.Save(dest, ImageFormat.Png);
            }
        }


        public void PaintNew()
        {
            if (Console.WindowHeight == 0) return;

            if (lastBufferWidth != this.Console.BufferWidth)
            {
                lastBufferWidth = this.Console.BufferWidth;
                Invalidate();
                this.Console.Clear();
            }

            try
            {
                paintBuilder.Clear();
                Chunk currentChunk = null;
                char val;
                RGB fg;
                RGB bg;
                bool underlined;
                ConsolePixel pixel;
                bool changeOnLine;
                bool pixelChanged;
                for (int y = 0; y < Height; y++)
                {
                    changeOnLine = false;
                    for (int x = 0; x < Width; x++)
                    {
                        pixel = pixels[x][y];
                        pixelChanged = pixel.HasChanged;
                        changeOnLine = changeOnLine || pixelChanged;

                        val = pixel.Value.Value;
                        fg = pixel.Value.ForegroundColor;
                        bg = pixel.Value.BackgroundColor;
                        underlined = pixel.Value.IsUnderlined;

                        if (currentChunk == null)
                        {
                            // first pixel always gets added to the current empty chunk
                            currentChunk = chunkPool.Get(Width);
                            currentChunk.FG = fg;
                            currentChunk.BG = bg;
                            currentChunk.Underlined = underlined;
                            currentChunk.HasChanged = pixelChanged;
                            currentChunk.Add(val);
                        }
                        else if (currentChunk.HasChanged == false && pixelChanged == false)
                        {
                            // characters that have not changed get chunked even if their styles differ
                            currentChunk.Add(val);
                        }
                        else if (currentChunk.HasChanged && pixelChanged && fg == currentChunk.FG && bg == currentChunk.BG && underlined == currentChunk.Underlined)
                        {
                            // characters that have changed only get chunked if their styles match to minimize the number of writes
                            currentChunk.Add(val);
                        }
                        else
                        {
                            chunksOnLine.Add(currentChunk);
                            currentChunk = chunkPool.Get(Width);
                            currentChunk.FG = fg;
                            currentChunk.BG = bg;
                            currentChunk.Underlined = underlined;
                            currentChunk.HasChanged = pixelChanged;
                            currentChunk.Add(val);
                        }
                        pixel.LastDrawnValue = pixel.Value;
                    }

                    if (currentChunk.Length > 0)
                    {
                        chunksOnLine.Add(currentChunk);
                    }

                    currentChunk = null;

                    if (changeOnLine)
                    {
                        var left = 0;
                        for (var i = 0; i < chunksOnLine.Count; i++)
                        {
                            var chunk = chunksOnLine[i];
                            if (chunk.HasChanged)
                            {

                                if (chunk.Underlined)
                                {
                                    paintBuilder.Append(Ansi.Text.UnderlinedOn);
                                }

                                Ansi.Cursor.Move.ToLocation(left + 1, y + 1, paintBuilder);
                                Ansi.Color.Foreground.Rgb(chunk.FG, paintBuilder);
                                Ansi.Color.Background.Rgb(chunk.BG, paintBuilder);
                                paintBuilder.Append(chunk);
                                if (chunk.Underlined)
                                {
                                    paintBuilder.Append(Ansi.Text.UnderlinedOff);
                                }
                            }

                            left += chunk.Length;
                        }
                    }

                    foreach(var chunk in chunksOnLine)
                    {
                        chunkPool.Return(chunk);
                    }
                    chunksOnLine.Clear();
                }
                Ansi.Cursor.Move.ToLocation(Width-1, Height-1, paintBuilder);
                Console.Write(paintBuilder.Buffer, paintBuilder.Length);
            }
            catch (IOException)
            {
                Invalidate();
                PaintNew();
            }
            catch (ArgumentOutOfRangeException)
            {
                Invalidate();
                PaintNew();
            }
        }

        /// <summary>
        /// Clears the cached paint state of each pixel so that
        /// all pixels will forcefully be painted the next time Paint
        /// is called
        /// </summary>
        public void Invalidate()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var pixel = pixels[x][y];
                    pixel.LastDrawnValue = default;
                }
            }
        }

        /// <summary>
        /// Gets a string representation of this image 
        /// </summary>
        /// <returns>a string representation of this image</returns>
        public override string ToString() => ToConsoleString().ToString();

        /// <summary>
        /// Returns true if the given object is a ConsoleBitmap with
        /// equivalent values as this bitmap, false otherwise
        /// </summary>
        /// <param name="obj">the object to compare</param>
        /// <returns>true if the given object is a ConsoleBitmap with
        /// equivalent values as this bitmap, false otherwise</returns>
        public override bool Equals(Object obj)
        {
            var other = obj as ConsoleBitmap;
            if (other == null) return false;

            if (this.Width != other.Width || this.Height != other.Height)
            {
                return false;
            }

            for (var x = 0; x < this.Width; x++)
            {
                for (var y = 0; y < this.Height; y++)
                {
                    var thisVal = this.GetPixel(x, y).Value;
                    var otherVal = other.GetPixel(x, y).Value;
                    if (thisVal.Value != otherVal.Value) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a hashcode for this bitmap
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // new style methods that don't require you to set the pen before drawing

        /// <summary>
        /// Draws a line between the two points using the given character as a pen
        /// </summary>
        /// <param name="character">the temporary pen</param>
        /// <param name="x1">the x coordinate of the first point</param>
        /// <param name="y1">the y coordinate of the first point</param>
        /// <param name="x2">the x coordinate of the second point</param>
        /// <param name="y2">the y coordinate of the second point</param>
        /// <returns>this ConsoleBitmap</returns>
        public ConsoleBitmap DrawLine(in ConsoleCharacter character, int x1, int y1, int x2, int y2)
        {
            var oldPen = this.Pen;
            try
            {
                this.Pen = character;
                DrawLine(x1, y1, x2, y2);
            }
            finally
            {
                this.Pen = oldPen;
            }
            return this;
        }

        /// <summary>
        /// Draws an unfilled rectangle at the given coordinates using the specified character
        /// as a temporary pen
        /// </summary>
        /// <param name="character">the temporary character</param>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        /// <returns>this ConsoleBitmap</returns>
        public ConsoleBitmap DrawRect(in ConsoleCharacter character, int x = 0, int y = 0, int w = -1, int h = -1)
        {
            var oldPen = this.Pen;
            try
            {
                w = w < 0 ? Width : w;
                h = h < 0 ? Height : h;
                this.Pen = character;
                DrawRect(x, y, w, h);
            }
            finally
            {
                this.Pen = oldPen;
            }
            return this;
        }

        /// <summary>
        /// Draws a filled rectangle at the given coordinates using the specified character
        /// as a temporary pen
        /// </summary>
        /// <param name="character">the temporary character</param>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        /// <returns>this ConsoleBitmap</returns>
        public ConsoleBitmap FillRect(in ConsoleCharacter character, int x = 0, int y = 0, int w = -1, int h = -1)
        {
            var oldPen = this.Pen;
            try
            {
                w = w < 0 ? Width : w;
                h = h < 0 ? Height : h;
                this.Pen = character;
                FillRect(x, y, w, h);
            }
            finally
            {
                this.Pen = oldPen;
            }
            return this;
        }

        /// <summary>
        /// Draws a filled rectangle at the given coordinates using a space
        /// as a temporary pen
        /// </summary>
        /// <param name="color">the background color of the space character</param>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        /// <returns>this ConsoleBitmap</returns>
        public ConsoleBitmap FillRect(in RGB color, int x = 0, int y = 0, int w = -1, int h = -1) =>
            FillRect(new ConsoleCharacter(' ', null, color), x, y, w, h);

        /// <summary>
        /// Draws a single pixel at the given coordinates using the specified character
        /// as a temporary pen
        /// </summary>
        /// <param name="character">the temporary pen</param>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        /// <returns>this ConsoleBitmap</returns>
        public ConsoleBitmap DrawPoint(ConsoleCharacter character, int x, int y)
        {
            var oldPen = this.Pen;
            try
            {
                this.Pen = character;
                DrawPoint(x, y);
            }
            finally
            {
                this.Pen = oldPen;
            }

            return this;
        }

        public ConsoleBitmap DrawPointUnsafe(in ConsoleCharacter character, int x, int y)
        {
            var oldPen = this.Pen;
            try
            {
                this.Pen = character;
                DrawPointUnsafe(x, y);
            }
            finally
            {
                this.Pen = oldPen;
            }

            return this;
        }
    }


internal class Chunk
{
    public RGB FG;
    public RGB BG;
    public bool HasChanged;
    public short Length;
    public char[] buffer;
    public bool Underlined;
    public int BufferLength => buffer.Length;
    public Chunk(int maxWidth)
    {
        buffer = new char[maxWidth];
    }

    public void Clear()
    {
        Length = 0;
        FG = default;
        BG = default;
        Underlined = default;
        HasChanged = false;
    }

    public void Add(char c) => buffer[Length++] = c;
    public override string ToString() => new string(buffer, 0, Length);
}

internal class PaintBuffer
{
    public char[] Buffer = new char[120*80];
    public int Length;

    internal void Append(Chunk c)
    {
        EnsureBigEnough(Length + c.Length);

        var span = c.buffer.AsSpan();
        for (var i = 0; i < c.Length; i++)
        {
            Buffer[Length++] = span[i];
        }
    }

    public void Append(char c)
    {
        EnsureBigEnough(Length + 1);
        Buffer[Length++] = c;
    }

    public void Append(string chars)
    {
        EnsureBigEnough(Length + chars.Length);

        var span = chars.AsSpan();
        for(var i = 0; i < span.Length; i++)
        {
            Buffer[Length++]=span[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureBigEnough(int newLen)
    {
        while (newLen > Buffer.Length)
        {
            var newBuffer = new char[Buffer.Length * 2];
            Array.Copy(Buffer, 0, newBuffer, 0, Buffer.Length);
            Buffer = newBuffer;
            newLen = Buffer.Length;
        }
    }

    public void Clear()
    {
        Length = 0;
    }
}

internal class ChunkPool
{
    Dictionary<int, List<Chunk>> pool = new Dictionary<int, List<Chunk>>();
    public Chunk Get(int w)
    {
        if(pool.TryGetValue(w, out List<Chunk> chunks) == false || chunks.None())
        {
            return new Chunk(w);
        }
        else
        {
            var ret = chunks[0];
            chunks.RemoveAt(0);
            return ret;
        }
    }

    public void Return(Chunk obj)
    {
        if (pool.TryGetValue(obj.BufferLength, out List<Chunk> chunks) == false)
        {
            chunks = new List<Chunk>();
            pool.Add(obj.BufferLength, chunks);
        }
        obj.Clear();
        chunks.Add(obj);
    }
}

internal class ConsolePixelPool
{

    private List<ConsolePixel> pool;

#if DEBUG
        private long hits;
        private long misses;
        private long returns;
        private double HitRate => ConsoleMath.Round(100 * hits / (hits + misses));
#endif

    public ConsolePixelPool()
    {
        pool = new List<ConsolePixel>();
    }

    public ConsolePixel Rent()
    {
        lock (pool)
        {
            if (pool.Count > 0)
            {
#if DEBUG
                hits++;
#endif
                var ret = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                ret.Value = default;
                ret.LastDrawnValue = default;
                return ret;
            }
            else
            {
#if DEBUG
                misses++;
#endif
                return new ConsolePixel();
            }
        }
    }

    public void Return(ConsolePixel p)
    {
#if DEBUG
        returns++;
#endif
        lock (pool)
        {
            pool.Add(p);
        }
    }
}
