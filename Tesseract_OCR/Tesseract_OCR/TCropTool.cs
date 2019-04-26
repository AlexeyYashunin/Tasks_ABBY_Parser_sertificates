using System;
using System.Collections.Generic;
using System.Drawing;


namespace Tesseract_OCR
{
    class TCropTool
    {
        //кадрирует изображение
        public Image cropImage(Image img, Rectangle cropArea){
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }
    }
}
