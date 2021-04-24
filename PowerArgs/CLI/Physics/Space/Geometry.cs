﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public enum Direction
    {
        None = -10000,
        Right = 345,
        RightDown = 15,
        DownRight = 45,
        Down = 75,
        DownLeft = 105,
        LeftDown = 135,
        Left = 165,
        LeftUp = 195,
        UpLeft = 225,
        Up = 255,
        UpRight = 285,
        RightUp = 315
    }

    public interface IRectangularF
    {
        float Left { get; }
        float Top { get; }
        float Width { get; }
        float Height { get; }

        Edge[] Edges { get; }
    }

    public interface ISizeF
    {
        float Width { get; }
        float Height { get; }
    }

    public interface ILocationF
    {
        float Left { get; }
        float Top { get; }
    }

    public static class LocationEx
    {
        public static ILocationF GetRounded(this ILocationF loc)
        {
            return LocationF.Create(Geometry.Round(loc.Left), Geometry.Round(loc.Top));
        }

        public static ILocationF GetFloor(this ILocationF loc)
        {
            return LocationF.Create((int)(loc.Left), (int)(loc.Top));
        }
    }

    public static class LocationF
    {
        private class LocationImpl : ILocationF
        {
            public float Left { get; internal set; }
            public float Top { get; internal set; }

            public override bool Equals(object obj)
            {
                var other = obj as ILocationF;
                if (other == null) return false;
                return Left == other.Left && Top == other.Top;
            }

            public override int GetHashCode()
            {
                return (Left * Top).GetHashCode();
            }

            public override string ToString() => $"{Left},{Top}";
        }

        public static ILocationF Create(float x, float y) => new LocationImpl() { Left = x, Top = y };
    }

    public static class SizeF
    {
        private class SizeImpl : ISizeF
        {
            public float Width { get; internal set; }
            public float Height { get; internal set; }

            public override bool Equals(object obj)
            {
                var other = obj as ISizeF;
                if (other == null) return false;
                return Width == other.Width&& Height== other.Height;
            }
        }

        public static ISizeF Create(float w, float h) => new SizeImpl() { Width = w, Height = h };
    }

    public interface IWriteableRectangularF : IRectangularF
    {
        new float Left { get; set; }

        new float Top { get; set; }

        new float Width { get; set; }

        new float Height { get; set; }
    }

    public class RectangularF : IWriteableRectangularF
    {
        public float Left { get; set; }

        public float Top { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public object Tag { get; private set; }

        public Edge[] Edges { get; private set; }

        private RectangularF(float x, float y, float w, float h, object tag = null)
        {
            this.Left = x;
            this.Top = y;
            this.Width = w;
            this.Height = h;
            this.Tag = tag;
            Edges = new Edge[4];
            Edges[0] = new Edge();
            Edges[1] = new Edge();
            Edges[2] = new Edge();
            Edges[3] = new Edge();
            Geometry.UpdateEdges(this, Edges);
        }

        public override bool Equals(object obj)
        {
            var other = obj as IRectangularF;
            if (other == null) return false;
            return Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;
        }

        public override string ToString() => $"X={Left}, Y={Top}, W={Width}, H={Height}";

        public static IWriteableRectangularF Create(float x, float y, float w, float h) => new RectangularF(x, y, w, h);
        public static IWriteableRectangularF Create(ILocationF location, ISizeF size) => new RectangularF(location.Left, location.Top, size.Width, size.Height);
    }

    public enum Side
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public static class Geometry
    {
        public static Rectangle Offset(this Rectangle rectangle, int dx, int dy) => new Rectangle(rectangle.Left + dx, rectangle.Top + dy, rectangle.Width, rectangle.Height);
        public static IRectangularF Offset(this IRectangularF rectangle, float dx, float dy) => RectangularF.Create(rectangle.Left + dx, rectangle.Top + dy, rectangle.Width, rectangle.Height);
        public static float Right(this IRectangularF rectangle) => rectangle.Left + rectangle.Width;
        public static float Bottom(this IRectangularF rectangle) => rectangle.Top + rectangle.Height;
        public static float CenterX(this IRectangularF rectangular) => rectangular.Left + (rectangular.Width / 2);
        public static float CenterY(this IRectangularF rectangular) => rectangular.Top + (rectangular.Height / 2);
        public static ILocationF Center(this IRectangularF rectangular) => LocationF.Create(rectangular.CenterX(), rectangular.CenterY());
        public static ILocationF TopLeft(this IRectangularF rectangular) => LocationF.Create(rectangular.Left, rectangular.Top);
        public static ILocationF TopRight(this IRectangularF rectangular) => LocationF.Create(rectangular.Right(), rectangular.Top);
        public static ILocationF BottomLeft(this IRectangularF rectangular) => LocationF.Create(rectangular.Left, rectangular.Bottom());
        public static ILocationF BottomRight(this IRectangularF rectangular) => LocationF.Create(rectangular.Right(), rectangular.Bottom());
        public static IRectangularF CopyBounds(this IRectangularF rectangular) => RectangularF.Create(rectangular.Left, rectangular.Top, rectangular.Width, rectangular.Height);
        public static float Hypotenous(this IRectangularF rectangular) => (float)Math.Sqrt(rectangular.Width * rectangular.Width + rectangular.Height * rectangular.Height);
        public static float DiffAngle(this int a, float b) => DiffAngle((float)a, b);
        public static float AddToAngle(this int a, float b) => AddToAngle((float)a, b);
        public static float CalculateNormalizedDistanceTo(ILocationF a, ILocationF b) => NormalizeQuantity(a.CalculateDistanceTo(b), a.CalculateAngleTo(b), true);
        public static float CalculateNormalizedDistanceTo(IRectangularF a, IRectangularF b) => NormalizeQuantity(a.CalculateDistanceTo(b), a.CalculateAngleTo(b), true);
        public static float CalculateDistanceTo(this ILocationF start, ILocationF end) => CalculateDistanceTo(start.Left, start.Top, end.Left, end.Top);
        public static float CalculateDistanceTo(float x1, float y1, float x2, float y2) => (float)Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
        public static Direction GetDirection(float a) => Enums.GetEnumValues<Direction>().OrderBy(slice => ((float)slice + 15).DiffAngle(a)).First();
        public static Direction GetHitDirection(this IRectangularF rectangle, IRectangularF other) => GetDirection(rectangle.CalculateAngleTo(other));
        public static float NormalizeQuantity(this int quantity, float angle, bool reverse = false) => NormalizeQuantity((float)quantity, angle, reverse);
        public static bool Contains(this IRectangularF rectangle, IRectangularF other) => OverlapPercentage(rectangle, other) == 1;
        public static bool Touches(this IRectangularF rectangle, IRectangularF other) => NumberOfPixelsThatOverlap(rectangle, other) > 0;


        public static float Round(float f, int digits) => (float)Math.Round(f, digits, MidpointRounding.AwayFromZero);
        public static float Round(double d, int digits) => (float)Math.Round(d, digits, MidpointRounding.AwayFromZero);

        public static int Round(float f) => (int)Math.Round(f, MidpointRounding.AwayFromZero);
        public static int Round(double d) => (int)Math.Round(d, MidpointRounding.AwayFromZero);

        public static int FindLineCircleIntersections(float cx, float cy, float radius, ILocationF point1, ILocationF point2, out ILocationF intersection1, out ILocationF intersection2)
        {
            float dx, dy, A, B, C, det, t;

            dx = point2.Left - point1.Left;
            dy = point2.Top - point1.Top;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.Left - cx) + dy * (point1.Top - cy));
            C = (point1.Left - cx) * (point1.Left - cx) +
                (point1.Top - cy) * (point1.Top - cy) -
                radius * radius;

            det = B * B - 4 * A * C;
            if ((A <= 0.0000001) || (det < 0))
            {
                // No real solutions.
                intersection1 = LocationF.Create(float.NaN, float.NaN);
                intersection2 = LocationF.Create(float.NaN, float.NaN);
                return 0;
            }
            else if (det == 0)
            {
                // One solution.
                t = -B / (2 * A);
                intersection1 =
                    LocationF.Create(point1.Left + t * dx, point1.Top + t * dy);
                intersection2 = LocationF.Create(float.NaN, float.NaN);
                return 1;
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                intersection1 =
                    LocationF.Create(point1.Left + t * dx, point1.Top + t * dy);
                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                intersection2 =
                    LocationF.Create(point1.Left + t * dx, point1.Top + t * dy);
                return 2;
            }
        }

        public static Side GetSideGivenEdgeIndex(int index)
        {
            if (index == 0) return Side.Top;
            if (index == 1) return Side.Right;
            if (index == 2) return Side.Bottom;
            if (index == 3) return Side.Left;
            throw new NotSupportedException("index must be >=0 && <= 3");
        }

        public static void UpdateEdges(IRectangularF rect, Edge[] edgeBuffer)
        {
            edgeBuffer[0].From = rect.TopLeft();
            edgeBuffer[0].To = rect.TopRight();

            edgeBuffer[1].From = rect.TopRight();
            edgeBuffer[1].To = rect.BottomRight();


            edgeBuffer[2].From = rect.BottomRight();
            edgeBuffer[2].To = rect.BottomLeft();


            edgeBuffer[3].From = rect.BottomLeft();
            edgeBuffer[3].To = rect.TopLeft();
        }

        public static IRectangularF ToRect(this ILocationF loc, float w, float h)
        {
            var left = loc.Left - w / 2;
            var top = loc.Top - h / 2;
            return RectangularF.Create(left, top, w, h);
        }

        public static Direction GetNearestDirectionFromAngle(this float orig)
        {
            var effectiveAngle = (orig % 360).RoundAngleToNearest(45);

            if (effectiveAngle == 0 || effectiveAngle == 360) return Direction.Right;
            else if (effectiveAngle == 45) return Direction.DownRight;
            else if (effectiveAngle == 90) return Direction.Down;
            else if (effectiveAngle == 135) return Direction.DownLeft;
            else if (effectiveAngle == 180) return Direction.Left;
            else if (effectiveAngle == 225) return Direction.UpLeft;
            else if (effectiveAngle == 270) return Direction.Up;
            else if (effectiveAngle == 315) return Direction.UpRight;

            throw new Exception("Unexpected angle: "+ orig);
        }

        public static float GetAngleFromDirection(this Direction d)
        {
            if (d == Direction.Right) return 0;
            else if (d == Direction.DownRight) return 45;
            else if (d == Direction.Down) return 90;
            else if (d == Direction.DownLeft) return 135;
            else if (d == Direction.Left) return 180;
            else if (d == Direction.UpLeft) return 225;
            else if (d == Direction.Up) return 270;
            else if (d == Direction.UpRight) return 315;
            
            throw new Exception("Unexpected direction: " + d);
        }

        public static Direction GetOppositeDirection(this Direction d)
        {
            if (d == Direction.Down) return Direction.Up;
            if (d == Direction.Up) return Direction.Down;
            if (d == Direction.Left) return Direction.Right;
            if (d == Direction.Right) return Direction.Left;
            if (d == Direction.UpLeft) return Direction.DownRight;
            if (d == Direction.DownLeft) return Direction.UpRight;
            if (d == Direction.UpRight) return Direction.DownLeft;
            if (d == Direction.DownRight) return Direction.UpLeft;

            return Direction.None;
        }

        /// <summary>
        /// Finds the angle that is between these two angles
        /// </summary>
        /// <param name="from">the starting angle</param>
        /// <param name="to">the ending angle</param>
        /// <returns>the angle that is between these two angles</returns>
        public static float Bisect(this float from, float to)
        {
            var max = Math.Max(from, to);
            var min = Math.Min(from, to);
            var range = max - min;
            if (range > 180)
            {
                min += 360;
            }
            var ret = (max + min) / 2;
            ret = ret % 360;
            return ret;
        }

        public static char GetArrowPointedAt(float angle)
        {
            if (angle >= 315 || angle < 45)
            {
                return '>';
            }
            else if (angle >= 45 && angle < 135)
            {
                return 'v';
            }
            else if (angle >= 135 && angle < 225)
            {
                return '<';
            }
            else
            {
                return '^';
            }
        }

        public static IRectangularF Round(this IRectangularF rect)
        {
            return RectangularF.Create(
                Geometry.Round(rect.Left),
                Geometry.Round(rect.Top),
                Geometry.Round(rect.Width),
                Geometry.Round(rect.Height)
            );
        }

        public static float GetOppositeAngle(this float angle)
        {
            float ret = angle < 180 ? angle + 180 : angle - 180;
            ret = ret == 360 ? 0 : ret;
            return ret;
        }
        
        public static IRectangularF Resize(this IRectangularF me, float ratio)
        {
            var newW = me.Width * ratio;
            var newH = me.Height * ratio;
            var leftAdjust = (me.Width - newW) / 2;
            var topAdjust = (me.Height - newH) / 2;
            var ret = RectangularF.Create(me.Left + leftAdjust, me.Top + topAdjust, newW, newH);
            return ret;
        }

        public static float ToRadians(this float degrees) => (float)(Math.PI * degrees / 180.0);
        public static float ToDegrees(this float radians) => (float)(radians * (180.0 / Math.PI));

        public static float DiffAngle(this float a, float b)
        {
            var c = Math.Abs(a - b);
            c = c <= 180 ? c : Math.Abs(360 - c);
            if (c == 360) return 0;
            return c;
        }

        public static bool IsClockwiseShortestPathToAngle(this float a, float b)
        {
            var diff = a.DiffAngle(b);
            return a.AddToAngle(diff) == b;
        }

        public static float DiffAngleRaw(this float a, float b)
        {
            var c = a - b;
            return c % 360;
        }

        public static float RoundAngleToNearest(this float a, float nearest)
        {
            return (float)Geometry.Round(a / nearest) * nearest;
        }

        public static float AddToAngle(this float angle, float toAdd)
        {
            var ret = angle + toAdd;
            ret = ret % 360;
            ret = ret >= 0 ? ret : ret + 360;
            if (ret == 360) ret = 0;
            return ret;
        }

        public static bool EqualsAngle(this float angle, float other)
        {
            return angle.DiffAngle(other) == 0;
        }

        public static float CalculateAngleTo(this IRectangularF from, IRectangularF to) => CalculateAngleTo(from.Center(), to.Center());
        public static float CalculateAngleTo(this ILocationF start, ILocationF end)
        {
            float dx = end.Left - start.Left;
            float dy = end.Top - start.Top;
            float d = CalculateDistanceTo(start, end);

            if (dy == 0 && dx > 0) return 0;
            else if (dy == 0) return 180;
            else if (dx == 0 && dy > 0) return 90;
            else if (dx == 0) return 270;

            double radians, increment;
            if (dx >= 0 && dy >= 0)
            {
                // Sin(a) = dy / d
                radians = Math.Asin(dy / d);
                increment = 0;

            }
            else if (dx < 0 && dy > 0)
            {
                // Sin(a) = dx / d
                radians = Math.Asin(-dx / d);
                increment = 90;
            }
            else if (dy < 0 && dx < 0)
            {
                radians = Math.Asin(-dy / d);
                increment = 180;
            }
            else if (dx > 0 && dy < 0)
            {
                radians = Math.Asin(dx / d);
                increment = 270;
            }
            else
            {
                throw new Exception($"Failed to calculate angle from {start?.Left},{start?.Top} to {end?.Left},{end?.Top}");
            }

            var ret = (float)(increment + radians * 180 / Math.PI);

            if (ret == 360) ret = 0;

            return ret;
        }

        public static IRectangularF Grow(this IRectangularF rect, float percentage)
        {
            var center = rect.Center();
            var newW = rect.Width * (1 + percentage);
            var newH = rect.Height * (1 + percentage);
            return RectangularF.Create(center.Left - newW / 2, center.Top - newH / 2, newW, newH);
        }

        public static IRectangularF Shrink(this IRectangularF rect, float percentage)
        {
            var center = rect.Center();
            var newW = rect.Width * (1 - percentage);
            var newH = rect.Height * (1 - percentage);
            return RectangularF.Create(center.Left - newW / 2, center.Top - newH / 2, newW, newH);
        }

        public static float CalculateDistanceTo(this IRectangularF a, IRectangularF b)
        {
            var left = b.Right() < a.Left;
            var right = a.Right() < b.Left;
            var bottom = b.Bottom() < a.Top;
            var top = a.Bottom() < b.Top;
            if (top && left)
                return CalculateDistanceTo(a.Left, a.Bottom(), b.Right(), b.Top);
            else if (left && bottom)
                return CalculateDistanceTo(a.Left, a.Top, b.Right(), b.Bottom());
            else if (bottom && right)
                return CalculateDistanceTo(a.Right(), a.Top, b.Left, b.Bottom());
            else if (right && top)
                return CalculateDistanceTo(a.Right(), a.Bottom(), b.Left, b.Top);
            else if (left)
                return a.Left - b.Right();
            else if (right)
                return b.Left - a.Right();
            else if (bottom)
                return a.Top - b.Bottom();
            else if (top)
                return b.Top - a.Bottom();
            else
                return 0;
        }

        public static float NumberOfPixelsThatOverlap(this IRectangularF rectangle, IRectangularF other)
        {
            var rectangleRight = rectangle.Left + rectangle.Width;
            var otherRight = other.Left + other.Width;
            var rectangleBottom = rectangle.Top + rectangle.Height;
            var otherBottom = other.Top+other.Height;
            var a = Math.Max(0, Math.Min(rectangleRight, otherRight) - Math.Max(rectangle.Left, other.Left));
            if (a == 0) return 0;
            var b = Math.Max(0, Math.Min(rectangleBottom, otherBottom) - Math.Max(rectangle.Top, other.Top));
            return a * b;
        }

        public static float OverlapPercentage(this IRectangularF rectangle, IRectangularF other)
        {
            var numerator = NumberOfPixelsThatOverlap(rectangle, other);
            var denominator = other.Width * other.Height;

            if (numerator == 0) return 0;
            else if (numerator == denominator) return 1;

            var amount = numerator / denominator;
            if (amount < 0) amount = 0;
            else if (amount > 1) amount = 1;

            if(amount > .999)
            {
                amount = 1;
            }

            return amount;
        }

        /// <summary>
        /// In most consoles the recrtangles allocated to characters are about twice as tall as they
        /// are wide. Since we want to treat the console like a uniform grid we'll have to account for that.
        /// 
        /// This method takes in some quantity and an angle and normalizes it so that if the angle were flat (e.g. 0 or 180)
        /// then you'll get back the same quantity you gave in. If the angle is vertical (e.g. 90 or 270) then you will get back
        /// a quantity that is only half of what you gave. The degree to which we normalize the quantity is linear.
        /// </summary>
        /// <param name="quantity">The quantity to normalize</param>
        /// <param name="angle">the angle to use to adjust the quantity</param>
        /// <param name="reverse">if true, grows the quantity instead of shrinking it. This is useful for angle quantities.</param>
        /// <returns></returns>
        public static float NormalizeQuantity(this float quantity, float angle, bool reverse = false)
        {
            float degreesFromFlat;
            if (angle <= 180)
            {
                degreesFromFlat = Math.Min(180 - angle, angle);
            }
            else
            {
                degreesFromFlat = Math.Min(angle - 180, 360 - angle);
            }

            var skewPercentage = 1 + (degreesFromFlat / 90);

            return reverse ? quantity * skewPercentage : quantity / skewPercentage;
        }

        public static IEnumerable<ILocationF> Corners(this IRectangularF rect)
        {
            yield return rect.TopLeft();
            yield return rect.TopRight();
            yield return rect.BottomLeft();
            yield return rect.BottomRight();
        }
    }
}
