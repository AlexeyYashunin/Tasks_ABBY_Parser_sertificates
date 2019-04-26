using System;
using System.Collections.Generic;
using System.Drawing;

using PdfSharp.Pdf.IO;
using System.IO;

namespace Tesseract_OCR
{
    class TFileWriter
    {
        public void imageWriter(string filePath, string fileName, List<Image> imagePages, System.Drawing.Imaging.ImageFormat imgFormat)
        {
            //создаем путь для выходных обработанных изображений
            System.IO.Directory.CreateDirectory(filePath);

            //экспортируем изображения           
            for (int i = 0; i < imagePages.Count; i++)
            {
                imagePages[i].Save(filePath + "[" + i + "]" + fileName, imgFormat);
            }
        }

        public void exportSertificatesAndPrilozenia(string sourcePDFFilePath, string outFolderPath, List<Tesseract_OCR.Tesseract_OCR_Window.pdfPageInfo> infoPages)
        {
            //создаем путь для выходных обработанных изображений
            System.IO.Directory.CreateDirectory(outFolderPath);

            // open and load the file
            using (PdfSharp.Pdf.PdfDocument inputDocument = PdfReader.Open(sourcePDFFilePath, PdfDocumentOpenMode.Import))
            {
                //создаем pdf документ
                PdfSharp.Pdf.PdfDocument pdfDocSertAndPril = null;

                // process and save pages one by one
                //for (int i = 0; i < infoPages.Count; i++)
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    if ((infoPages[i].typeOfPage_ == Tesseract_OCR_Window.typeOfPage.SERTIFICATE) || (infoPages[i].typeOfPage_ == Tesseract_OCR_Window.typeOfPage.PRILOZENIE))
                    {
                        //путь к выходной папке серта
                        string outSertFolder = "";

                        //путь к выходному файлу серта
                        string outSertFilePath = "";

                        //путь к выходному файлу (серт + приложения к нему)
                        string outSertAndPrilFilePath = "";

                        //раскидываем по папкам "распознано/не распознано"
                        if (infoPages[i].isTesseracted_ == true)
                        {
                            outSertFolder= outFolderPath + "\\" + "[+]Tesseracted" + "\\" + infoPages[i].fullNumber_;
                            outSertFilePath= outFolderPath + "\\" + "[+]Tesseracted" + "\\" + infoPages[i].fullNumber_ + "\\" + infoPages[i].fullNumber_ + ".pdf";

                            outSertAndPrilFilePath = outFolderPath + "\\" + "[+]Tesseracted" + "\\" + infoPages[i].fullNumber_ + "\\" + "[FULL]"+ infoPages[i].fullNumber_ + ".pdf";
                        }
                        else{
                            outSertFolder = outFolderPath + "\\" + "[-]Tesseracted" + "\\" + infoPages[i].fullNumber_;
                            outSertFilePath = outFolderPath + "\\" + "[-]Tesseracted" + "\\" + infoPages[i].fullNumber_ + "\\" + infoPages[i].fullNumber_ + ".pdf";

                            outSertAndPrilFilePath = outFolderPath + "\\" + "[-]Tesseracted" + "\\" + infoPages[i].fullNumber_ + "\\" + "[FULL]" + infoPages[i].fullNumber_ + ".pdf";
                        }

                        if (System.IO.Directory.Exists(outSertFolder)){
                            int indexOfNewFolder = 2;

                            do{
                                //раскидываем по папкам "распознано/не распознано"
                                if (infoPages[i].isTesseracted_ == true)
                                {
                                    outSertFolder = outFolderPath + "\\" + "[+]Tesseracted" + "\\[" + indexOfNewFolder + "]" + infoPages[i].fullNumber_;
                                    outSertFilePath = outFolderPath + "\\" + "[+]Tesseracted" + "\\[" + indexOfNewFolder + "]" + infoPages[i].fullNumber_ + "\\" + infoPages[i].fullNumber_ + ".pdf";

                                    outSertAndPrilFilePath = outFolderPath + "\\" + "[+]Tesseracted" + "\\[" + indexOfNewFolder + "]" + infoPages[i].fullNumber_ + "\\" + "[FULL]" + infoPages[i].fullNumber_ + ".pdf";
                                }
                                else
                                {
                                    outSertFolder = outFolderPath + "\\" + "[-]Tesseracted" + "\\[" + indexOfNewFolder + "]" + infoPages[i].fullNumber_;
                                    outSertFilePath = outFolderPath + "\\" + "[-]Tesseracted" + "\\[" + indexOfNewFolder + "]" + infoPages[i].fullNumber_ + "\\" + infoPages[i].fullNumber_ + ".pdf";

                                    outSertAndPrilFilePath = outFolderPath + "\\" + "[-]Tesseracted" + "\\[" + indexOfNewFolder + "]" + infoPages[i].fullNumber_ + "\\" + "[FULL]" + infoPages[i].fullNumber_ + ".pdf";
                                }

                                indexOfNewFolder++;
                            } while((System.IO.Directory.Exists(outSertFolder)) == true);
                            //если уже есть такая папка, то добавляем префикс
                            System.IO.Directory.CreateDirectory(outSertFolder);
                        }
                        else
                        {
                            //создаем папку с названием сертификата, содержащую сам серт и приложения
                            System.IO.Directory.CreateDirectory(outSertFolder);
                        }                       

                        //даем права на запись в папку
                        File.SetAttributes(outSertFolder, FileAttributes.Normal);

                        //цепляем страницу серта
                        PdfSharp.Pdf.PdfPage pageSert = inputDocument.Pages[i];

                        //создаем pdf документ
                        PdfSharp.Pdf.PdfDocument pdfDocSert = new PdfSharp.Pdf.PdfDocument();
                        pdfDocSertAndPril = new PdfSharp.Pdf.PdfDocument();

                        //добавляем в pdf документ сертификат
                        pdfDocSert.AddPage(pageSert);
                        pdfDocSertAndPril.AddPage(pageSert);

                        //сохраняем pdf файл
                        pdfDocSert.Save(outSertFilePath);
                        
                        //проверка на выход за границу контейнера infoPages
                        //if((i + 1) >= infoPages.Count){
                        if((i + 1) >= inputDocument.PageCount)
                        {
                            pdfDocSertAndPril.Save(outSertAndPrilFilePath);

                            //очищаем
                            pdfDocSertAndPril.Dispose();
                            pdfDocSert.Dispose();

                            continue;
                        }

                        //для смены позиции индекса в документе
                        int updatePositionInDoc = -1;

                        //for (int j = i + 1; j < infoPages.Count; j++)
                        for (int j = i + 1; j < inputDocument.PageCount; j++)
                        {
                            if (infoPages[j].typeOfPage_ == Tesseract_OCR_Window.typeOfPage.PRILOZENIE)
                            {
                                //путь к выходному файлу приложения
                                //string outPrilozenieFilePath = outFolderPath + "\\" + infoPages[i].fullNumber_ + "\\" + infoPages[i].fullNumber_ + "." + infoPages[j].seriaNumber_ + ".pdf";
                                string outPrilozenieFilePath = outSertFolder + "\\" + infoPages[i].fullNumber_ + "." + infoPages[j].seriaNumber_ + ".pdf";

                                //цепляем страницу приложения
                                PdfSharp.Pdf.PdfPage pagePrilozenie = inputDocument.Pages[j];

                                //создаем pdf документ
                                PdfSharp.Pdf.PdfDocument pdfDocPrilozenie = new PdfSharp.Pdf.PdfDocument();

                                //добавляем в pdf документ приложение
                                pdfDocPrilozenie.AddPage(pagePrilozenie);
                                pdfDocSertAndPril.AddPage(pagePrilozenie);

                                //сохраняем pdf файл
                                if(File.Exists(outPrilozenieFilePath)){
                                    int offsetIndex = 1;

                                    do{
                                        outPrilozenieFilePath = outSertFolder + "\\[" + offsetIndex + "]" + infoPages[i].fullNumber_ + "." + infoPages[j].seriaNumber_ + ".pdf";

                                        offsetIndex++;
                                    } while(File.Exists(outPrilozenieFilePath));
                                }

                                pdfDocPrilozenie.Save(outPrilozenieFilePath);

                                //увеличиваем кол-во считанных страниц в документе
                                updatePositionInDoc = j;
                            }
                            else
                            {
                                //если это серт, то отматываемся, т.к. его обработка идет не здесь
                                i = j - 1;

                                break;
                            }
                        }

                        //если меняли значение, то обновляем позицию в документе
                        if(updatePositionInDoc!=-1)
                        {
                            //меняем позицию в документе
                            i = updatePositionInDoc;
                        }                        

                        pdfDocSertAndPril.Save(outSertAndPrilFilePath);

                        //очищаем
                        pdfDocSertAndPril.Dispose();
                        pdfDocSert.Dispose();
                    }
                }
            }
        }
    }
}
