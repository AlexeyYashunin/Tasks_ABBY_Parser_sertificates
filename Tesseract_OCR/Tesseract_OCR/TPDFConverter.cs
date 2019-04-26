using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using PdfSharp.Pdf.IO;

using XsPDF.Pdf;

namespace Tesseract_OCR
{
    class TPDFConverter{
        public List<Image> pdfToImage(string pdfFilePath,int dpi,float resModifier){
            // Create a PDF converter instance by loading a local file 
            PdfImageConverter pdfConverter = new PdfImageConverter(pdfFilePath);

            // Set the dpi, the output image will be rendered in such resolution
            pdfConverter.DPI = dpi;

            // the output image will be rendered to grayscale image or not
            pdfConverter.GrayscaleOutput = true;

            // open and load the file
            using (PdfSharp.Pdf.PdfDocument inputDocument = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.Import))
            {
                List<Image> tempImagePdfPages = new List<Image>();

                // process and save pages one by one
                for (int i = 0; i < inputDocument.Pages.Count; i++)
                {
                    PdfSharp.Pdf.PdfPage currentPage = inputDocument.Pages[i];

                    /*// Create instance of Ghostscript wrapper class.
                    GS gs = new GS();*/

                    /*int widthPdfPage = Convert.ToInt32(currentPage.Width.Point);
                    int heightPdfPage = Convert.ToInt32(currentPage.Height.Point);*/

                    int widthPdfPage = Convert.ToInt32(currentPage.Width.Point * resModifier);
                    int heightPdfPage = Convert.ToInt32(currentPage.Height.Point * resModifier);

                    // Convert pdf to png in customized image size
                    Image image = pdfConverter.PageToImage(i, widthPdfPage, heightPdfPage);

                    tempImagePdfPages.Add(image);
                }

                pdfConverter.Dispose();

                return tempImagePdfPages;
            }
        }
    }
}
