using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TestScaleImgEssence
{
    public class TransAlgorithms
    {
        public void XXX(Visual wpfVisual)
        {
            var transform = PresentationSource.FromVisual(wpfVisual).CompositionTarget.TransformFromDevice;
            var mouse = transform.Transform(GetMousePosition());

            System.Windows.Point GetMousePosition()
            {
                var point = System.Windows.Forms.Control.MousePosition;
                return new System.Windows.Point(point.X, point.Y);
            }
        }
    }
}
