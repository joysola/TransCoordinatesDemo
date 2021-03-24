using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestScaleImgEssence.WinFrm;

namespace TestScaleImgEssence
{
    class Program
    {
        static void Main(string[] args)
        {
            var winAlg = new WinFrmAlgorithm(); // winform计算类
            // 初始化LevelColsRows字典
            for (int i = 0; i <= 9; i++)
            {
                winAlg.GetColsRows(i);
            }
            Console.ReadKey();
        }
    }
}
