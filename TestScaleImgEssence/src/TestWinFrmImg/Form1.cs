using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestScaleImgEssence.WinFrm;

namespace TestWinFrmImg
{
    public partial class Form1 : Form
    {
        public string BaseFilePath { get; set; } = @"K:\Test\Test\TestScaleImgEssence\TestScaleImgEssence\src\411681C03200807002";
        private WinFrmAlgorithm Algorithm { get; } = new WinFrmAlgorithm();
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.MouseMove += Form1_MouseMove;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            var xx = Algorithm.AbsPoint(e.Location, 0, 1);
            this.Tbx1.Text = xx.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i <= 9; i++)
            {
                Algorithm.GetColsRows(i);
            }
            Algorithm.CurrLevel = 9;
            Algorithm.ColStart = 105;
            Algorithm.ColEnd = 124;
            Algorithm.RowStart = 129;
            Algorithm.RowEnd = 144;
            var bitmap = WinFrmDraw.JoinImage(BaseFilePath, 9, Algorithm.ColStart, Algorithm.ColEnd, Algorithm.RowStart, Algorithm.RowEnd, out int xx);
            this.BackgroundImage = bitmap;
        }
    }
}
