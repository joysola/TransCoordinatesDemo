using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestScaleImgEssence.WinFrm
{
    public class WinFrmAlgorithm
    {

        public string BaseFilePath { get; set; } = @"K:\Test\Test\TestScaleImgEssence\TestScaleImgEssence\src\411681C03200807002";
        public int CurrLevel { get; set; }
        public int MaxLevel { get; set; } = 9;


        public int ColStart { get; set; }
        public int ColEnd { get; set; }
        public int RowStart { get; set; }
        public int RowEnd { get; set; }
        public int RowMaxEnd { get; set; }
        public int ColMaxEnd { get; set; }
        public float ToScale { get; set; } = 1;
        public int Level { get; set; }


        /// <summary>
        /// 图像（相对于控件的）偏移
        /// </summary>
        public Point ImgOffset { get; set; }
        /// <summary>
        /// 控件自身的偏移（onpaint方法中，计算结果也是 1，1）
        /// </summary>
        public Point CtrlOffset { get; set; } = new Point(1, 1);
        /// <summary>
        /// 每一层图的信息 字典
        /// </summary>
        private Dictionary<int, ScanPageLevel> LevelColsRows { get; set; } = new Dictionary<int, ScanPageLevel>();

        #region 绝对坐标 计算 控件坐标
        /// <summary>
        ///  根据绝对坐标计算控件坐标
        /// </summary>
        /// <param name="mapr"></param>
        /// <param name="absPoint">绝对坐标</param>
        /// <param name="degreeA">角度</param>
        /// <param name="scale">放大倍率</param>
        /// <returns>控件坐标</returns>
        public Point ImgCtrlPoint(/*MapRectangle mapr,*/ Point absPoint, float degreeA, double scale)
        {
            ScanPageLevel currLCR = GetColsRows(CurrLevel);
            ScanPageLevel maxLCR = GetColsRows(MaxLevel);
            Point p = ImgPoint(currLCR, maxLCR, /*mapr,*/ absPoint, degreeA, scale);
            Point ret0 = new Point(Convert.ToInt32(p.X - ImgOffset.X + CtrlOffset.X),
                Convert.ToInt32(p.Y - ImgOffset.Y + CtrlOffset.Y));
            return ret0;
        }
        /// <summary>
        /// 把绝对坐标转换为控件坐标
        /// </summary>
        /// <param name="currLCR">当前切片当前层数据</param>
        /// <param name="maxLCR">当前切片最大层数据</param>
        /// <param name="mapr">当前视野的地图范围数据</param>
        /// <param name="absPoint">绝对坐标</param>
        /// <param name="degreeA">角度</param>
        /// <param name="scale">放大倍率</param>
        /// <returns>控件坐标</returns>
        public Point ImgPoint(ScanPageLevel currLCR, ScanPageLevel maxLCR,
           /*MapRectangle mapr,*/ Point absPoint, float degreeA, double scale)
        {
            Point[] points = CornerPoints(1, degreeA);

            int turn = Convert.ToInt32(Math.Floor(degreeA / 90)); // 象限
            double degree = degreeA % 90;

            float pitchCol = maxLCR.ColSpan() / currLCR.ColSpan();
            float pitchRow = maxLCR.RowSpam() / currLCR.RowSpam();

            double cosx = Math.Cos(DegreeToRaidens(degree));
            double sinx = Math.Sin(DegreeToRaidens(degree));
            double tanx = Math.Tan(DegreeToRaidens(degree));
            int h = Convert.ToInt32(Height(0));
            int w = Convert.ToInt32(Width(0));

            // 核心
            Point rp = new Point(Convert.ToInt32(absPoint.X / pitchCol - ColStart * Constants.PicW),
                                 Convert.ToInt32(absPoint.Y / pitchRow - RowStart * Constants.PicH));
            double rx = rp.X;
            double ry = rp.Y;
            double rlvPtX = 0, rlvPtY = 0;

            if (turn == 0)
            {
                //第一象限
                rlvPtY = (ry + rx * tanx) * cosx;
                rlvPtX = rx / cosx + (h * cosx - rlvPtY) * tanx;
            }
            else if (turn == 1)
            {
                //第二象限
                rlvPtX = w * sinx + h * cosx - (ry + rx * tanx) * cosx;
                rlvPtY = rx / cosx + (rlvPtX - w * sinx) * tanx;
            }
            else if (turn == 3)
            {
                //第四象限
                double h1 = (rx - ry / cosx * sinx) * cosx;
                rlvPtY = points[0].Y - h1;
                rlvPtX = ry / cosx + h1 * tanx;
            }
            else if (turn == 2)
            {
                //第三象限
                double y1 = (ry + rx * tanx) * cosx;
                double x1 = rx / cosx + (h * cosx - y1) * tanx;
                rlvPtX = w * cosx + h * sinx - x1;
                rlvPtY = w * sinx + h * cosx - y1;
            }

            Point ret0 = new Point(Convert.ToInt32(rlvPtX * scale), Convert.ToInt32(rlvPtY * scale));
            return ret0;
        }

        public ScanPageLevel GetColsRows(int level)
        {
            if (level >= MaxLevel)
            {
                level = MaxLevel;
            }

            if (level <= 0)
            {
                return null;
            }

            if (CalcColsRows(level))
            {
                return this.LevelColsRows[level];
            }
            return null;
        }

        public bool CalcColsRows(int level)
        {
            if (LevelColsRows.ContainsKey(level))
            {
                return true;
            }
            ScanPageLevel pageLevel = new ScanPageLevel
            {
                Level = level
            };
            if (!Directory.Exists(BaseFilePath + "\\" + level))
            {
                return false;
            }

            String file = BaseFilePath + "\\Slide.dat";
            if (File.Exists(file))
            {
                //couny by file
                pageLevel = GenPageLevelByIni(pageLevel);
            }
            else
            {
                // count automate
                pageLevel = GenPageLevelAutomate(pageLevel);
                //return false;
            }

            if (level > 1)
            {
                ScanPageLevel baseScanPageLevel = LevelColsRows[1];
                pageLevel.ToScale = Math.Max(pageLevel.EndCol / baseScanPageLevel.EndCol
                    , pageLevel.EndRow / baseScanPageLevel.EndRow);
            }
            else
            {
                pageLevel.ToScale = Math.Max(pageLevel.EndCol, pageLevel.EndRow);
            }

            pageLevel.ToScale = Constants.PageScale(level);

            LevelColsRows.Add(level, pageLevel);
            return LevelColsRows.ContainsKey(level);
        }

        /// <summary>
        /// 总dat文件读取层级信息
        /// </summary>
        /// <param name="pageLevel"></param>
        /// <returns></returns>
        private ScanPageLevel GenPageLevelByIni(ScanPageLevel pageLevel)
        {
            String file = BaseFilePath + "\\Slide.dat";
            if (File.Exists(file))
            {
                StringBuilder sc = new StringBuilder();
                FileUtils.GetPrivateProfileString("LayerMag", pageLevel.Level + "C", "0", sc, 255, file);

                StringBuilder sr = new StringBuilder();
                FileUtils.GetPrivateProfileString("LayerMag", pageLevel.Level + "R", "0", sr, 255, file);

                pageLevel.StartCol = 0;
                pageLevel.StartRow = 0;
                try
                {
                    pageLevel.EndCol = float.Parse(sc.ToString());
                    pageLevel.EndRow = float.Parse(sr.ToString());
                }
                catch (Exception exp)
                {
                }
            }

            return pageLevel;
        }

       

        /// <summary>
        /// 图层四个角坐标（没有偏移的情况）
        /// </summary>
        /// <param name="scale">放大倍率</param>
        /// <param name="angleA">度数</param>
        /// <returns></returns>
        public Point[] CornerPoints(double scale, double angleA)
        {

            int turn = Convert.ToInt16(Math.Floor(angleA / 90)); // 象限
            double angle = angleA % 90;


            double rowspan = (RowEnd - RowStart) * Constants.PicH * scale;
            double colspan = (ColEnd - ColStart) * Constants.PicW * scale;


            double d1 = rowspan * Math.Sin(DegreeToRaidens(angle));
            double d2 = rowspan * Math.Cos(DegreeToRaidens(angle));

            double d3 = colspan * Math.Cos(DegreeToRaidens(angle));
            double d4 = colspan * Math.Sin(DegreeToRaidens(angle));
            Point[] points = { };

            if (turn == 1 || turn == 3)
            {
                Point p1w = new Point(Convert.ToInt16(d4), 0);
                Point p2w = new Point(0, Convert.ToInt16(d3));
                Point p3w = new Point(Convert.ToInt16(d2 + d4), Convert.ToInt16(d1));
                Point p4w = new Point(Convert.ToInt16(d2), Convert.ToInt16(d3 + d1));
                if (turn == 1)
                {
                    points = new Point[] { p3w, p1w, p2w, p4w };
                }
                else if (turn == 3)
                {
                    points = new Point[] { p2w, p4w, p3w, p1w };
                }
            }
            else
            {
                Point p1 = new Point(Convert.ToInt32(d1), 0);
                Point p2 = new Point(0, Convert.ToInt32(d2));
                Point p3 = new Point(Convert.ToInt32(d3), Convert.ToInt32(d2 + d4));
                Point p4 = new Point(Convert.ToInt32(d3 + d1), Convert.ToInt32(d4));
                if (turn == 0)
                {
                    points = new Point[] { p1, p2, p3, p4 };
                }
                else if (turn == 2)
                {
                    points = new Point[] { p3, p4, p1, p2 };
                }


            }
            return points;

        }
        /// <summary>
        /// 度数转弧度
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public double DegreeToRaidens(double degrees)
        {
            double radians = degrees * Math.PI / 180;
            return radians;
        }

        
        /// <summary>
        /// 图的总高度
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        public double Height(int degree)
        {
            int angle = degree % 90;

            double sinx = Math.Sin(DegreeToRaidens(angle));
            double cosx = Math.Cos(DegreeToRaidens(angle));

            int rl = (RowEnd - RowStart) * Constants.PicH;
            int cl = (ColEnd - ColStart) * Constants.PicW;

            return cl * sinx + rl * cosx;
        }
        /// <summary>
        /// 图的总宽度
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        public double Width(int degree)
        {
            int angle = degree % 90;
            double sinx = Math.Sin(DegreeToRaidens(angle));
            double cosx = Math.Cos(DegreeToRaidens(angle));

            int rl = (RowEnd - RowStart) * Constants.PicH;
            int cl = (ColEnd - ColStart) * Constants.PicW;

            return cl * cosx + rl * sinx;
        }

        public void GetXXX(ScanPageLevel levelColsRows)
        {
            ColStart = 0;
            ColEnd = ScanPageLevel.GetActual(levelColsRows.EndCol);
            ColMaxEnd = ScanPageLevel.GetActual(levelColsRows.EndCol);

            RowStart = 0;
            RowEnd = ScanPageLevel.GetActual(levelColsRows.EndRow);
            RowMaxEnd = ScanPageLevel.GetActual(levelColsRows.EndRow);

            Level = levelColsRows.Level;
            ToScale = levelColsRows.ToScale;
        }

        #region 自动计算PageLevel(暂时用不到)
        /// <summary>
        /// 根据图片文件的rgb值确定是否为有效图片，非常慢！！！！
        /// 只有在slide.dat缺少layermsg时，才不得不调用此方法
        /// </summary>
        /// <param name="pageLevel"></param>
        /// <returns></returns>
        private ScanPageLevel GenPageLevelAutomate(ScanPageLevel pageLevel)
        {
            string[] directories = Directory.GetDirectories(BaseFilePath + "\\" + pageLevel.Level);
            if (directories != null)
            {
                int[] array = new int[directories.Length];
                for (int i = 0; i < directories.Length; i++)
                {
                    array[i] = Convert.ToInt32(new DirectoryInfo(directories[i]).Name);
                }
                Array.Sort(array);
                int item = CalcRows(pageLevel.Level);
                pageLevel.EndCol = array.Length - 1;
                pageLevel.EndRow = item - 1;
                pageLevel = CountColRowDetail(pageLevel);
            }
            return pageLevel;
        }
        private ScanPageLevel CountColRowDetail(ScanPageLevel pageLevel)
        {
            float colStartAbs = pageLevel.EndCol * Constants.PicW;
            float rowStartAbs = pageLevel.EndRow * Constants.PicH;
            float colEndAbs = 0;
            float rowEndAbs = 0;

            for (int col = 0; col <= pageLevel.EndCol; col++)
            {
                for (int row = 0; row <= pageLevel.EndRow; row++)
                {
                    FileInfo file = new FileInfo(BaseFilePath + "\\" + pageLevel.Level + "\\" + col + "\\" + row + ".jpg");
                    Rectangle validRect = ValidRect(file);
                    if (validRect.Size.IsEmpty)
                    {
                        continue;
                    }

                    colStartAbs = Math.Min(validRect.X + col * Constants.PicW, colStartAbs);
                }
                if (colStartAbs < pageLevel.EndCol * Constants.PicW)
                {
                    break;
                }
            }

            for (int col = Convert.ToInt16(pageLevel.EndCol); col >= 0; col--)
            {
                for (int row = 0; row <= pageLevel.EndRow; row++)
                {
                    FileInfo file = new FileInfo(BaseFilePath + "\\" + pageLevel.Level + "\\" + col + "\\" + row + ".jpg");
                    Rectangle validRect = ValidRect(file);
                    if (validRect.Size.IsEmpty)
                    {
                        continue;
                    }

                    colEndAbs = Math.Max(validRect.X + validRect.Size.Width + col * Constants.PicW, colEndAbs);
                }
                if (colEndAbs > 0)
                {
                    break;
                }
            }

            for (int row = 0; row <= pageLevel.EndRow; row++)
            {
                for (int col = 0; col <= pageLevel.EndCol; col++)
                {
                    FileInfo file = new FileInfo(BaseFilePath + "\\" + pageLevel.Level + "\\" + col + "\\" + row + ".jpg");
                    Rectangle validRect = ValidRect(file);
                    if (validRect.Size.IsEmpty)
                    {
                        continue;
                    }

                    rowStartAbs = Math.Min(validRect.Y + row * Constants.PicH, rowStartAbs);
                }
                if (rowStartAbs < pageLevel.EndRow * Constants.PicH)
                {
                    break;
                }
            }

            for (int row = Convert.ToInt16(pageLevel.EndRow); row >= 0; row--)
            {
                for (int col = 0; col <= pageLevel.EndCol; col++)
                {
                    FileInfo file = new FileInfo(BaseFilePath + "\\" + pageLevel.Level + "\\" + col + "\\" + row + ".jpg");
                    Rectangle validRect = ValidRect(file);
                    if (validRect.Size.IsEmpty)
                    {
                        continue;
                    }
                    rowEndAbs = Math.Max(validRect.Y + validRect.Size.Height + row * Constants.PicH, rowEndAbs);
                }
                if (rowEndAbs > 0)
                {
                    break;
                }
            }

            pageLevel = new ScanPageLevel
            {
                EndCol = colEndAbs / Constants.PicW,
                EndRow = colEndAbs / Constants.PicH,
                StartRow = rowStartAbs / Constants.PicH,
                StartCol = colStartAbs / Constants.PicW,
                Level = pageLevel.Level
            };
            return pageLevel;
        }

        /// <summary>
        /// 判断某图像文件是否为切片的有效图片，主要目的是排除切片中的全白图片）
        /// </summary>
        /// <param name="file"></param>
        /// <returns>如果是有效图片则返回所在的行、列</returns>
        public Rectangle ValidRect(FileInfo file)
        {
            if (!file.Exists)
            {
                return new Rectangle();
            }

            Rectangle ret = new Rectangle(0, 0, Constants.PicW, Constants.PicH);
            Image image = Image.FromFile(file.FullName);
            Bitmap map = new Bitmap(image);

            int colStart = Constants.PicW;
            int rowStart = Constants.PicH;
            int colEnd = 0;
            int rowEnd = 0;

            bool allwhite = true;

            Color color0 = map.GetPixel(0, 0);
            Color color1 = map.GetPixel(0, Constants.PicW - 1);
            Color color2 = map.GetPixel(Constants.PicH - 1, 0);
            Color color3 = map.GetPixel(Constants.PicH - 1, Constants.PicW - 1);

            if (color0.ToArgb() < -1 && color1.ToArgb() < -1 && color2.ToArgb() < -1 && color3.ToArgb() < -1)
            {
                //绝大多数都是全有图像的情况，快速跳过
                return new Rectangle(0, 0, Constants.PicW, Constants.PicH);
            }

            for (int x = 0; x < Constants.PicW; x++)
            {
                for (int y = 0; y < Constants.PicH; y++)
                {
                    Color color = map.GetPixel(x, y);
                    if (color.ToArgb() < -1)
                    {
                        allwhite = false;
                        rowStart = Math.Min(y, rowStart);
                        colStart = Math.Min(x, colStart);

                        rowEnd = Math.Max(y, rowEnd);
                        colEnd = Math.Max(x, colEnd);
                    }
                }
            }

            if (allwhite)
            {
                ret = new Rectangle(0, 0, 0, 0);
            }
            else
            {
                ret = new Rectangle(colStart, rowStart, colEnd - colStart + 1, rowEnd - rowStart + 1);
            }

            return ret;
        }
        private int CalcRows(int level)
        {
            int ret = 0;
            if (Directory.Exists(BaseFilePath + "\\" + level))
            {
                string[] directories = Directory.GetDirectories(BaseFilePath + "\\" + level);
                if (directories != null)
                {
                    for (int i = 0; i < directories.Length; i++)
                    {
                        List<string> nameList = new List<string>();
                        FileUtils.DirFiles(directories[i], nameList);
                        foreach (string filename in nameList)
                        {
                            FileInfo f = new FileInfo(directories[i] + "\\" + filename);

                            string fileNoS = f.Name.ToLower().Replace(".jpg", "");
                            try
                            {
                                int fileNo = Convert.ToInt16(fileNoS);

                                ret = Math.Max(ret, fileNo);
                            }
                            catch (Exception e)
                            {
                            }
                        }
                    }
                }
            }
            return ret + 1;
        }
        #endregion 自动计算PageLevel

        #endregion 绝对坐标 计算 控件坐标


        #region 控件坐标 计算 绝对坐标
        /// <summary>
        /// 根据控件坐标计算绝对坐标
        /// </summary>
        /// <param name="imgCtrlPoint">控件坐标</param>
        /// <param name="degreeA">角度</param>
        /// <returns>绝对坐标</returns>
        public Point AbsPoint(Point imgCtrlPoint, float degreeA, double scale)
        {
            Point[] points = CornerPoints(1, degreeA);

            int turn = Convert.ToInt32(Math.Floor(degreeA / 90));
            double degree = degreeA % 90;

            ScanPageLevel currLCR = GetColsRows(CurrLevel);
            ScanPageLevel maxLCR = GetColsRows(MaxLevel);

            float pitchCol = maxLCR.ColSpan() / currLCR.ColSpan();
            float pitchRow = maxLCR.RowSpam() / currLCR.RowSpam();

            Point ret0;

            Point rlvPt = new Point(ImgOffset.X + imgCtrlPoint.X - CtrlOffset.X,
                                    ImgOffset.Y + imgCtrlPoint.Y - CtrlOffset.Y);

            rlvPt = new Point(Convert.ToInt32(rlvPt.X / scale), Convert.ToInt32(rlvPt.Y / scale));

            double cosx = Math.Cos(DegreeToRaidens(degree));
            double sinx = Math.Sin(DegreeToRaidens(degree));
            double tanx = Math.Tan(DegreeToRaidens(degree));
            //TODO tested height degree
            int h = Convert.ToInt32(Height(0));
            int w = Convert.ToInt32(Width(0));

            double rx = 0, ry = 0;
            if (turn == 0)
            {
                //第一象限
                rx = (rlvPt.X - (h * cosx - rlvPt.Y) * tanx) * cosx;
                ry = rlvPt.Y / cosx - rx * tanx;
            }
            else if (turn == 1)
            {
                //第二象限
                rx = (rlvPt.Y - (rlvPt.X - w * sinx) * tanx) * cosx;
                ry = (w * sinx + h * cosx - rlvPt.X) / cosx - rx * tanx;
            }
            else if (turn == 3)
            {
                //第四象限
                double h1 = points[0].Y - rlvPt.Y;
                rx = h1 / cosx + (rlvPt.X - h1 * tanx) * sinx;
                ry = (rlvPt.X - h1 * tanx) * cosx;
            }
            else if (turn == 2)
            {
                //第三象限
                double x1 = w * cosx + h * sinx - rlvPt.X;
                double y1 = w * sinx + h * cosx - rlvPt.Y;

                rx = (x1 - (h * cosx - y1) * tanx) * cosx;
                ry = y1 / cosx - rx * tanx;
            }

            // 核心
            ret0 = new Point(Convert.ToInt32((rx + ColStart * Constants.PicW) * pitchCol),
                             Convert.ToInt32((ry + RowStart * Constants.PicH) * pitchRow));
            return ret0;
        }



        #endregion
    }
}
