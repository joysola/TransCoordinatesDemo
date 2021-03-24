using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TestScaleImgEssence.WinFrm
{
    public class WinFrmDraw
    {
        /// <summary>
        /// 直线
        /// </summary>
        public class MyLine
        {
            private Color penColor;
            private float penWidth;
            private AnnotationType type;
            private Rectangle[] corners;
            private bool adjusting;
            private double length;

            public double Length { get => length; set => length = value; }
            public Color PenColor { get => penColor; set => penColor = value; }
            public float PenWidth { get => penWidth; set => penWidth = value; }
            public Point Point0 { get; set; }
            public Point Point1 { get; set; }
            public Point Point2 { get; set; }
            public Point Point3 { get; set; }
            public AnnotationType Type { get => type; set => type = value; }
            public bool Adjusting { get => adjusting; set => adjusting = value; }
            public Rectangle[] Corners { get => corners; set => corners = value; }


            public MyLine(Point s, Point e, Color cc, float ww, AnnotationType type)
            {
                this.Point0 = s;
                this.Point2 = e;
                this.Type = type;

                PenColor = cc;
                PenWidth = ww;

                Corners = new Rectangle[2];
            }

            public Rectangle[] CalcCorners(Point start, Point end)
            {
                if (Corners != null)
                {
                    Corners[0] = new Rectangle(start.X - 8, start.Y - 8, 16, 16);//left-top
                    Corners[1] = new Rectangle(end.X - 8, end.Y - 8, 16, 16);    //right-bottom
                }

                return Corners;
            }
            /// <summary>
            /// 都是写入绝对坐标
            /// </summary>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            /// <param name="p2"></param>
            /// <param name="p3"></param>
            /// <param name="unit"></param>
            public void Location(Point p0, Point p1, Point p2, Point p3, double unit)
            {
                this.Point0 = p0;
                this.Point2 = p2;

                double w = Math.Abs(this.Point0.X - this.Point2.X) * unit;
                double h = Math.Abs(this.Point0.Y - this.Point2.Y) * unit;

                this.Length = Math.Round(Math.Sqrt(w * w + h * h), 2); //um
            }

            public void Draw(Graphics g, Point p0, Point p1, Point p2, Point p3, Point offSet)
            {
                Point pp1 = new Point(p0.X + offSet.X, p0.Y + offSet.Y);
                Point pp2 = new Point(p2.X + offSet.X, p2.Y + offSet.Y);

                Pen pen = new Pen(PenColor, PenWidth);
                g.DrawLine(pen, pp1, pp2);

                if (this.Type == AnnotationType.MeasureLine)
                {
                    g.DrawLine(pen, new Point(p0.X + offSet.X - 8, p0.Y + offSet.Y), new Point(p0.X + offSet.X + 8, p0.Y + offSet.Y));
                    g.DrawLine(pen, new Point(p0.X + offSet.X, p0.Y + offSet.Y - 8), new Point(p0.X + offSet.X, p0.Y + offSet.Y + 8));
                    g.DrawLine(pen, new Point(p2.X + offSet.X - 8, p2.Y + offSet.Y), new Point(p2.X + offSet.X + 8, p2.Y + offSet.Y));
                    g.DrawLine(pen, new Point(p2.X + offSet.X, p2.Y + offSet.Y - 8), new Point(p2.X + offSet.X, p2.Y + offSet.Y + 8));
                }

                if (Adjusting)
                {
                    p0.Offset(offSet);
                    p2.Offset(offSet);
                    Rectangle[] crt = CalcCorners(p0, p2);
                    Pen p = new Pen(Color.LightSlateGray, 1.5f);
                    g.DrawRectangles(p, crt);
                    g.FillRectangles(new SolidBrush(Color.LightYellow), crt);
                }

                return;
            }

            public string Detail(bool mm = false)
            {
                if (Length > 10000)
                    mm = true;

                string strDetail = "";
                if (Length > 0)
                {
                    if (mm)
                        strDetail = "长: " + Math.Round(this.Length / 1000, 2).ToString() + "mm";
                    else
                        strDetail = "长: " + this.Length.ToString() + "um";
                }

                return strDetail;
            }
        }




        private static ConcurrentDictionary<int, Bitmap> PicDict = new ConcurrentDictionary<int, Bitmap>();

        /// <summary>
        /// 画图
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="level"></param>
        /// <param name="colStart"></param>
        /// <param name="colEnd"></param>
        /// <param name="rowStart"></param>
        /// <param name="rowEnd"></param>
        /// <param name="picCount"></param>
        /// <param name="fullPageBitmap"></param>
        /// <returns></returns>
        public static Bitmap JoinImage(String basePath, int level, float colStart, float colEnd, float rowStart, float rowEnd, out int picCount, Bitmap fullPageBitmap = null)
        {

            int cs = Convert.ToInt16(Math.Ceiling(colStart));
            int ce = Convert.ToInt16(Math.Ceiling(colEnd));
            int rs = Convert.ToInt16(Math.Ceiling(rowStart));
            int re = Convert.ToInt16(Math.Ceiling(rowEnd));


            int imgWidth = (ce - cs) * Constants.PicW;
            int imgHeight = (re - rs) * Constants.PicH;

            if (imgWidth <= 0 || imgHeight <= 0)
            {
                picCount = 0;
                return null;
            }
            return JoinImage(basePath, level, imgWidth, imgHeight, cs, ce, rs, re, out picCount, fullPageBitmap);
        }
        /// <summary>
        /// 画图
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="level"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="colStart"></param>
        /// <param name="colEnd"></param>
        /// <param name="rowStart"></param>
        /// <param name="rowEnd"></param>
        /// <param name="picCount"></param>
        /// <param name="fullPageBitmap"></param>
        /// <returns></returns>
        private static Bitmap JoinImage(string basePath, int level, int imgWidth, int imgHeight, int colStart, int colEnd, int rowStart, int rowEnd, out int picCount, Bitmap fullPageBitmap = null)
        {
            picCount = 0;
            var newTag = new WinFrmAlgorithm();
            bool reused = false;


            var cropedTag = new WinFrmAlgorithm();

            newTag.ColStart = colStart;
            newTag.ColEnd = colEnd;
            newTag.RowStart = rowStart;
            newTag.RowEnd = rowEnd;
            newTag.Level = level;


            //if (fullPageBitmap != null)
            //{
            //    Bitmap ret = CropImage(fullPageBitmap, newTag.MapRectangle);
            //    return ret;
            //}


            Bitmap bitmap = new Bitmap(imgWidth, imgHeight);


            Graphics graphics = Graphics.FromImage(bitmap);
            float w = (float)(2);
            Pen pen = new Pen(Color.Red, w);

            int num = 0;
            for (int i = colStart; i <= colEnd; i++)
            {
                int num2 = 0;

                for (int j = rowStart; j <= rowEnd; j++)
                {
                    bool skipped = false;
                    if (reused && (
                        (j > cropedTag.RowStart && j < cropedTag.RowEnd) &&
                        (i > cropedTag.ColStart && i < cropedTag.ColEnd)))
                    {
                        skipped = true;
                    }

                    if (!skipped)
                    {
                        string fileName = "\\" + level + "\\" + i + "\\" + j + ".jpg";

                        string fullFileName = basePath + fileName;

                        if (File.Exists(fullFileName))
                        {
                            int hash = fullFileName.GetHashCode();

                            Bitmap image = null;
                            if (!PicDict.TryGetValue(hash, out image))
                            {
                                image = (Bitmap)Image.FromFile(fullFileName);
                                PicDict.GetOrAdd(hash, image);
                            }

                            graphics.DrawImage(image, num * Constants.PicW, num2 * Constants.PicH, Constants.PicW,
                               Constants.PicH);

                            picCount++;
                        }
                        else
                        {
                            graphics.FillRectangle(Brushes.White, num * Constants.PicW, num2 * Constants.PicH, Constants.PicW, Constants.PicH);
                        }
                    }

                    num2++;
                }
                num++;
            }

            bitmap.Tag = newTag;
            return bitmap;
        }
    }
}
