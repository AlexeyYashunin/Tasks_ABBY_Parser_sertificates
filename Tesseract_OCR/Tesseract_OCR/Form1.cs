using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;

using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using PdfSharp.Ghostscript;

using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using XsPDF.Pdf;

using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Driver;

using RestSharp;
using System.Diagnostics;

using System.Threading.Tasks;
using System.Threading;

namespace Tesseract_OCR {
    public partial class Tesseract_OCR_Window : Form {
        //список pdf файлов в указанной папке
        public List<string> fileNames = null;
        public int curIndFileNames = 0;//текущий индекс в списке файлов, для лога
        public string docFilePath = "";//путь к файлу со списком документов для распознания
        public AutoResetEvent waitHandler = new AutoResetEvent(true);

        //создаем поисковик в CertSys
        TCertSys certSys = new TCertSys();

        //создаем список задач
        Task[] ocrTasks = null;

        TABBYFineReader abbyOCR = null;

        //кол-во тасков, запускаемых одновременно
        public int delimTaskValue = 1;   

        public Tesseract_OCR_Window() {
            InitializeComponent();
        }

        private void OcrBtn_Click(object sender, EventArgs e) {
            try {
                if (docFilePath == "") {
                    folderBrowserDialog.ShowDialog();
                }
                {
                    //if(true)
                    //чистим объекты формы
                    textBox.Text = "";
                    isDoneProcLabel.Text = "";
                    
                    Dictionary<string, string> words = null;

                    //инициализируем словарь транслитеризации
                    initDictionary(ref words);

                    //folderBrowserDialog.SelectedPath = "C:\\Users\\YashuninAM\\Desktop\\[ПО]Разработка\\[ПО]Распознаватель_сертов_и_приложений\\2\\Сертификаты\\Сертификаты\\Test";
                    //folderBrowserDialog.SelectedPath = "C:\\Users\\YashuninAM\\Desktop\\[ПО]Разработка\\[ПО]Распознаватель_сертов_и_приложений\\2\\Сертификаты\\Сертификаты\\Test_debug";
                    //folderBrowserDialog.SelectedPath = @"C:\Users\YashuninAM\Desktop\[ПО]Разработка\[ПО]Распознаватель_сертов_и_приложений\2\Сертификаты\Сертификаты\[ПРОММАШТЕСТ]Test_Debug";
                    //folderBrowserDialog.SelectedPath = @"C:\Users\Alexey\Desktop\[ПРОММАШТЕСТ]Test_Debug";

                    //создаем pdf конвертер
                    TPDFConverter pdfConverter = new TPDFConverter();

                    //для записи Image'й в файл
                    TFileWriter fileWriter = new TFileWriter();

                    //создаем графические фильтры
                    TCropTool cropTool = new TCropTool();
                    TResizeTool resizeTool = new TResizeTool();

                    //создаем поисковик
                    TTeseract_OCR tesOCR = new TTeseract_OCR();

                    //забираем кол-во тасков
                    if (Tasks_amount_txtbox.Text != "") {
                        delimTaskValue = Convert.ToInt32(Tasks_amount_txtbox.Text);

                        abbyOCR = new TABBYFineReader(delimTaskValue);//ставим значение от delimTaskValue (в коде ниже)
                    } else {
                        abbyOCR = new TABBYFineReader(delimTaskValue);//ставим значение от delimTaskValue (в коде ниже)
                    }

                    //контейнеры для обработанных изображений
                    List<Image> imagePdfPages = null;
                    List<Image> imageCropPages = new List<Image>();
                    List<Image> imageScaledPages = new List<Image>();

                    //задаем выходной формат изображения
                    System.Drawing.Imaging.ImageFormat imgFormat = System.Drawing.Imaging.ImageFormat.Png;

                    //удаляем старые выходные данные
                    string currentPath = System.IO.Directory.GetCurrentDirectory();

                    //сохраняем путь к папке входных данных
                    string inputFolder = folderBrowserDialog.SelectedPath;

                    //получаем название папки для дальнейшего экспорта содержимого
                    string folderName = "";

                    if (docFilePath == "") {
                        folderName = Path.GetFileName(Path.GetDirectoryName(folderBrowserDialog.SelectedPath + "\\"));
                    } else {
                        string filePath = (File.ReadAllLines(docFilePath)[0]);
                        folderName = Path.GetFileName(Path.GetDirectoryName(filePath));
                    }

                    string outFilePath = currentPath + "\\[OUT]Data\\" + folderName;
                    //string outFilePath = @"\\msk-dc01-elma1p\elmabin\cert archive\\" + folderName;

                    //создаем папку для выходных документов pdf
                    System.IO.Directory.CreateDirectory(outFilePath);

                    //создаем папку для текущего документа
                    System.IO.Directory.CreateDirectory(currentPath + "\\[OUT]Data\\");

                    //пишем список оставшихся файлов для распознания, если упало        
                    string outCrashFolder = currentPath + "\\[Crash]Log";

                    if (System.IO.Directory.Exists(outCrashFolder)) {
                        System.IO.Directory.Delete(outCrashFolder, true);
                    }

                    System.IO.Directory.CreateDirectory(outCrashFolder);

                    //составляем список pdf файлов
                    if (docFilePath != "") {
                        string[] lines = File.ReadAllLines(docFilePath, Encoding.UTF8);

                        fileNames = new List<string>();

                        for (int i = 0; i < lines.Length; i++) {
                            fileNames.Add(lines[i]);
                        }
                    } else {
                        fileNames = getPdfFiles(inputFolder);
                    }

                    //устанавливаем предел прогресс-бара
                    processProgressBar.Maximum = fileNames.Count;
                                  
                    //рендерим, распознаем, сохраняем документы
                    for (int i = 0; i < fileNames.Count; i++) {
                        //пишем текущий файл в документ для последующего учета
                        if(File.Exists(currentPath + "\\[OUT]Data\\Current_file.txt")==true){
                            File.Delete(currentPath + "\\[OUT]Data\\Current_file.txt");
                        }

                        System.IO.File.WriteAllText(currentPath + "\\[OUT]Data\\Current_file.txt", fileNames[i]);

                        //сохраняем индекс для аварийного случая
                        curIndFileNames = i;

                        //чтобы система успевала обработать запросы и других COM-библиотек
                        Application.DoEvents();

                        //читаем pdf документ и рендерим в imag'ы
                        try {
                            imagePdfPages = pdfConverter.pdfToImage(fileNames[i], 300, 3.0f);

                            //throw new System.ArgumentException("Parameter cannot be null", "original");
                        } catch (System.Exception error) {
                            //сохраняем название проблемного файла                            

                            saveErrorFile(error, fileNames[i]);

                            continue;
                        }

                        ////////////////////////////////////////////////////////////////////////////
                        //вырезаем верхнюю часть страницы(20%) и сохраняем отдельно                
                        float cropValue = 0.09f;
                        float cropHeightOffset = 0.08f;

                        for (int j = 0; j < imagePdfPages.Count; j++) {
                            Image img = imagePdfPages[j];

                            int imgCropHeight = Convert.ToInt32((img.Height) * (cropValue));
                            int imgCropHeightOffset = Convert.ToInt32((img.Height) * (cropHeightOffset));

                            Rectangle imgCropRect = new Rectangle(0, imgCropHeightOffset, img.Width, imgCropHeight);

                            img = cropTool.cropImage(img, imgCropRect);

                            imagePdfPages[j] = img;
                        }

                        ////////////////////////////////////////////////////////////////////////////
                        //увеличиваем размер входного изображения(т.е масштабируем)
                        float scaleValue = 1.5f;// начинаем с 50%

                        for (int j = 0; j < imagePdfPages.Count; j++) {
                            Image img = imagePdfPages[j];

                            int newWidth = Convert.ToInt32((img.Width) * (scaleValue));
                            int newHeight = Convert.ToInt32((img.Height) * (scaleValue));

                            Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                            imageScaledPages.Add((Image)imgBitmap);
                        }

                        //создаем список задач
                        ocrTasks = new Task[imagePdfPages.Count];

                        //проходим по страницам документа
                        List<pdfPageInfo> infoPages = new List<pdfPageInfo>(imagePdfPages.Count);//информация по страницам pdf

                        //распознаем страницы
                        for (int j = 0; j < imageScaledPages.Count; j++) {
                            object arg = j;

                            ocrTasks[j] = new Task(() => ocrImage(ref imagePdfPages, ref imageScaledPages, ref infoPages, arg));                                                       
                        }

                        /*//кол-во тасков, запускаемых одновременно
                        int delimTaskValue=2;*/                       

                        //запускаем таски
                        for (int j = 0; j < ocrTasks.Length; j++) {
                            if(((j+1)%delimTaskValue==0)){
                                Task[] delimTasks = new Task[delimTaskValue];
                                
                                for(int k=0;k<delimTaskValue;k++){
                                    delimTasks[k] = ocrTasks[(j+1) - delimTaskValue + k];                                    
                                }

                                for (int k = 0; k < delimTaskValue; k++) {
                                    delimTasks[k].Start();
                                }

                                //ждем завершения всех тасков
                                Task.WaitAll(delimTasks);

                                //удаляем следы FineReader'а
                                abbyOCR.destroyFRProcess();
                            }                           

                            //ocrTasks[j].Start();
                        }

                        //разбираем остаточную страницу(если есть)
                        int ostatokPages = (ocrTasks.Length % 2);

                        if (ostatokPages != 0) {
                            ocrTasks[ocrTasks.Length - 1].Start();
                            ocrTasks[ocrTasks.Length - 1].Wait();
                        }

                        //ждем завершения всех тасков
                        //Task.WaitAll(ocrTasks);

                        //если документ состоит из сертификата и приложений, то сохраняем
                        if (infoPages.Count > 0) {
                            //сохраняем документы
                            fileWriter.exportSertificatesAndPrilozenia(fileNames[i], outFilePath, infoPages);
                        }

                        //throw new ArgumentNullException();//для отладки лога
                        //пишем лог
                        if (textBox.Text == "") {
                            string textBoxStrTemp = "[" + (i + 1) + "] = " + fileNames[i] + "\n";
                            textBox.Text = textBoxStrTemp.Replace("\n", Environment.NewLine);
                        } else {
                            string textBoxStr = textBox.Text + "\n[" + (i + 1) + "] = " + fileNames[i] + "\n";
                            textBox.Text = textBoxStr.Replace("\n", Environment.NewLine);
                        }

                        processProgressBar.Value += 1;

                        //обновляем форму
                        this.Refresh();
                        Application.DoEvents();

                        //чистим infoPages
                        infoPages.Clear();
                        
                        //чистим контейнеры с изображениями, поскольку размеры контейнеров равны, то берем один из двух
                        for (int j = 0; j < imagePdfPages.Count; j++) {
                            imagePdfPages[j].Dispose();
                            imageScaledPages[j].Dispose();
                        }

                        imagePdfPages.Clear();
                        imageScaledPages.Clear();

                        //удаляем временные файлы FineReader'а
                        abbyOCR.destroyOutFolders();

                        //удаляем следы FineReader'а
                        abbyOCR.destroyFRProcess();

                        //txtResult.Text = infoPage.fullNumber_;
                    }

                    //когда процесс закончен, то выводим уведомления
                    isDoneProcLabel.Text = "Done";
                    MessageBox.Show("Done");
                }
            } catch (Exception error) {
                saveErrorFile(error, "");
            }
        }

        private void ocrImage(ref List<Image> imagePdfPages, ref List<Image> imageScaledPages, ref List<pdfPageInfo> infoPages, object indexOfPage) {
            ///////////////////////////////////////////////////////////////////////////
            //распознаем изображение               

            TesseractEngine ocrRus = new TesseractEngine("./tessdata", "rus", EngineMode.Default);
            TesseractEngine ocrEng = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
            
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            //Regex regexOrgSertTypeSeriaShortNumberRus = new Regex(@"[А-Я]{2}[0-9]{2}\W[0-9]{5}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexOrgSertTypeSeriaShortNumberRus = new Regex(@"[А-Я]{2}[0-9]{2}[А-Я]{1}[0-9]{5}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            //Regex regexSeriaNumber = new Regex(@"[0-9]{7}", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.RightToLeft);
            Regex regexSeriaNumber = new Regex(@"[0-9]{7}", RegexOptions.IgnoreCase);

            //берем страницу
            Bitmap imgBitmapOcrRus = (Bitmap)imageScaledPages[(int)indexOfPage];
            Bitmap imgBitmapOcrEng = (Bitmap)imageScaledPages[(int)indexOfPage];

            //ставим методику распознавания страницы
            PageSegMode pSegMode = PageSegMode.SparseText;

            string pdfTextRus = "";
            string pdfTextEng = "";

            //waitHandler.WaitOne();

            //удаляем процесс, чтобы не было ошибки
            using (var pageRus = ocrRus.Process(imgBitmapOcrRus, pSegMode)) {
                pdfTextRus = pageRus.GetText();
            }

            //удаляем процесс, чтобы не было ошибки
            using (var pageEng = ocrEng.Process(imgBitmapOcrEng, pSegMode)) {
                pdfTextEng = pageEng.GetText();
            }

            //waitHandler.Set();

            if (isSertificate(pdfTextRus))//сертификат
            //if (abbyOCR.isSertificateViaRescaling(imagePdfPages[(int)indexOfPage], 0.1f, 3))//сертификат
            {
                //////////////
                //tesOCR.getDataViaRescaling(imagePdfPages[j], ref infoPage, ref words, 0.1f, 10);
                pdfPageInfo infoPageABBY = abbyOCR.getDataViaRescaling(ref ocrTasks[(int)indexOfPage], imagePdfPages[(int)indexOfPage], 0.1f, 4);
                infoPageABBY.numberOfPDFPage_ = (int)indexOfPage;

                //////////////////////////////////////////////////

                //infoPages[(int)indexOfPage]=infoPageABBY;

                if ((int)indexOfPage != 0) {
                    ocrTasks[(int)indexOfPage - 1].Wait();
                }         

                infoPages.Insert((int)indexOfPage,infoPageABBY);
            } else {//приложение
                //ждем завершения предыдущего такска, чтобы взять оттуда инфу
                //ocrTasks[(int)indexOfPage - 1].Wait();

                pdfPageInfo infoPageABBY = abbyOCR.getDataViaRescaling(ref ocrTasks[(int)indexOfPage], imagePdfPages[(int)indexOfPage], 0.1f, 4);
                infoPageABBY.numberOfPDFPage_ = (int)indexOfPage;
                infoPageABBY.typeOfPage_ = typeOfPage.PRILOZENIE;

                //////////////////////////////////////////////////

                //infoPages[(int)indexOfPage]=infoPageABBY;

                if ((int)indexOfPage != 0) {
                    ocrTasks[(int)indexOfPage - 1].Wait();
                }   

                infoPages.Insert((int)indexOfPage, infoPageABBY);

                /*//ждем завершения предыдущего такска, чтобы взять оттуда инфу
                ocrTasks[(int)indexOfPage - 1].Wait();

                pdfPageInfo infoPage = infoPages[infoPages.Count - 1];

                //если серт не распознали, то и приложение не распознаем
                if (infoPage.isTesseracted_ == false) {
                    infoPage.numberOfPDFPage_ = (int)indexOfPage;
                    infoPage.isTesseracted_ = false;
                    infoPage.typeOfPage_ = typeOfPage.PRILOZENIE;
                    infoPage.textRus_ = pdfTextRus;
                    infoPage.textEng_ = pdfTextEng;
                    infoPage.seriaNumber_ = "";

                    //infoPage.ocrMethod_ = ocrMethod.TESSERACT;
                    infoPage.ocrMethod_ = ocrMethod.ABBY;

                    infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.typeOfSert_ + "-" + infoPage.country_ + "." + infoPage.orgSert_ + "." + infoPage.typeSeria_ + "." + infoPage.shortNumber_ + "." + infoPage.seriaNumber_;
                    infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();//ставим верхний регистр

                    //infoPages[(int)indexOfPage] = infoPage;
                    infoPages.Insert((int)indexOfPage, infoPage);

                    //освобождаем ресурсы
                    imgBitmapOcrRus.Dispose();
                    imgBitmapOcrEng.Dispose();
                }

                //если до этого было приложение, то seriaNumber+1
                if (infoPage.typeOfPage_ == typeOfPage.PRILOZENIE) {
                    infoPage.numberOfPDFPage_ = (int)indexOfPage;
                    infoPage.textRus_ = pdfTextRus;
                    infoPage.textEng_ = pdfTextEng;

                    int sertSeriaNumber = Convert.ToInt32(infoPage.seriaNumber_);
                    string clearSeriaNumber = "0000000";
                    string prilSeriaNumber = Convert.ToString(sertSeriaNumber + 1);

                    int amountOfSymbolsSSN = prilSeriaNumber.Length;
                    int amountClearSN = clearSeriaNumber.Length;

                    //clearSeriaNumber=clearSeriaNumber.Remove(2, 5).Insert(2, prilSeriaNumber);
                    clearSeriaNumber = clearSeriaNumber.Remove(amountClearSN - amountOfSymbolsSSN, amountOfSymbolsSSN).Insert(amountClearSN - amountOfSymbolsSSN, prilSeriaNumber);

                    infoPage.seriaNumber_ = clearSeriaNumber;
                    infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.typeOfSert_ + "-" + infoPage.country_ + "." + infoPage.orgSert_ + "." + infoPage.typeSeria_ + "." + infoPage.shortNumber_ + "." + infoPage.seriaNumber_;
                    //infoPage.ocrMethod_ = ocrMethod.TESSERACT;
                    infoPage.ocrMethod_ = ocrMethod.ABBY;
                    infoPage.isTesseracted_ = true;

                    infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();//ставим верхний регистр

                    //infoPages[(int)indexOfPage] = infoPage;
                    infoPages.Insert((int)indexOfPage, infoPage);
                }

                infoPage.numberOfPDFPage_ = (int)indexOfPage;
                infoPage.isTesseracted_ = false;
                infoPage.typeOfPage_ = typeOfPage.PRILOZENIE;
                //infoPage.ocrMethod_ = ocrMethod.TESSERACT;
                infoPage.ocrMethod_ = ocrMethod.ABBY;
                infoPage.textRus_ = pdfTextRus;
                infoPage.textEng_ = pdfTextEng;

                //infoPage.seriaNumber_ = tesOCR.getSeriaNumber(pdfTextRus, pdfTextEng);
                infoPage.seriaNumber_ = abbyOCR.getSeriaNumberViaRescaling(imagePdfPages[(int)indexOfPage], 0.1f, 3);
                infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.typeOfSert_ + "-" + infoPage.country_ + "." + infoPage.orgSert_ + "." + infoPage.typeSeria_ + "." + infoPage.shortNumber_ + "." + infoPage.seriaNumber_;

                if (infoPage.seriaNumber_ == "") {
                    infoPage.isTesseracted_ = false;
                } else {
                    infoPage.isTesseracted_ = true;
                }

                infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();//ставим верхний регистр

                //infoPages[(int)indexOfPage] = infoPage;
                infoPages.Insert((int)indexOfPage, infoPage);*/
            }

            //освобождаем ресурсы
            imgBitmapOcrRus.Dispose();
            imgBitmapOcrEng.Dispose();
        }

        public struct pdfPageInfo {
            public string fullNumber_;//полный номер сертификата(в одну строку поля)
            public string techReglament_;//технический регламент(статичное поле)(ТС RU)
            public string shortNumber_;//5тизначный номер
            public string typeSeria_;//тип серии
            public string seriaNumber_;//номер серии
            public string country_;//страна
            public string typeOfSert_;//тип сертификата
            public string orgSert_;///орган по сертификации
            public typeOfPage typeOfPage_;//тип страницы(серт/приложение)
            public bool isTesseracted_;//распознан документ или нет
            public int numberOfPDFPage_;//номер страницы в документе
            public string textRus_;
            public string textEng_;
            public ocrMethod ocrMethod_;//метод распознавания
        }

        public enum typeOfPage { SERTIFICATE = 0, PRILOZENIE = 1 };

        public enum ocrMethod { CERTSYS = 0, MONGODB = 1, TESSERACT = 2, ABBY = 3, NONE = 4 };
        public void saveErrorFile(Exception error, string fileName) {
            //пишем список оставшихся файлов для распознания, если упало
            string currentPath = System.IO.Directory.GetCurrentDirectory();

            string outCrashFolder = currentPath + "\\[Crash]Log";

            /*if(System.IO.Directory.Exists(outCrashFolder)) {
                System.IO.Directory.Delete(outCrashFolder, true);
            }

            System.IO.Directory.CreateDirectory(outCrashFolder);*/

            if ((fileNames.Count > 0) && (curIndFileNames > 0)) {
                fileNames.RemoveRange(0, curIndFileNames - 1);//откатываемся на один назад, чтобы затереть проблемный
            }

            System.IO.File.WriteAllLines(outCrashFolder + "\\[Crash]Remaining_files.txt", fileNames.ToArray());
            //System.IO.File.AppendAllText(outCrashFolder + "\\[Crash]Log.txt", fileName + ";" + DateTime.Now + ";" + error.Source + ";" + error.Data + ";" + error.Message + "\n");//выводим ошибку в файл, если файл создан, то дописываем в конец
            System.IO.File.AppendAllText(outCrashFolder + "\\[Crash]Log.txt", fileName + Environment.NewLine);//выводим ошибку в файл, если файл создан, то дописываем в конец
            System.IO.File.AppendAllText(outCrashFolder + "\\[Crash]Errors.txt", error.Message + Environment.NewLine);//выводим ошибку в файл, если файл создан, то дописываем в конец
        }

        public List<string> getPdfFiles(string inputFolder) {
            List<string> fileNames = new List<string>();

            {
                //берем список имен файлов в папке
                string[] fNames = System.IO.Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories);

                //убираем архивные файлы
                for (int i = 0; i < fNames.Length; i++) {
                    string extension = Path.GetExtension(fNames[i]);

                    if (extension == ".pdf") {
                        fileNames.Add(fNames[i]);
                    }
                }
            }

            return fileNames;
        }

        //проверка на сертификат
        public bool isSertificate(string pageText) {
            List<Regex> regexPrilozenieRus = new List<Regex>();

            regexPrilozenieRus.Add(new Regex(@"\w*ПРИ\w*", RegexOptions.Singleline));//ПРИложение
            regexPrilozenieRus.Add(new Regex(@"\w*ОЖЕ\w*", RegexOptions.Singleline));//прилОЖЕние
            regexPrilozenieRus.Add(new Regex(@"\w*АТУ\w*", RegexOptions.Singleline));//сертификАТУ
            regexPrilozenieRus.Add(new Regex(@"\w*ОЖ\w*", RegexOptions.Singleline));//сертификАТУ
            //regexPrilozenieRus.Add(new Regex(@"\w*ТУ\w*", RegexOptions.IgnoreCase | RegexOptions.Singleline));//сертификАТУ

            /*//доп. маски
            regexPrilozenieRus.Add(new Regex(@"\w*СОО\w*", RegexOptions.IgnoreCase | RegexOptions.Singleline));//сертификАТУ
            regexPrilozenieRus.Add(new Regex(@"\w*ТВЕТ\w*", RegexOptions.IgnoreCase | RegexOptions.Singleline));//сертификАТУ
            regexPrilozenieRus.Add(new Regex(@"\w*СТВИЯ\w*", RegexOptions.IgnoreCase | RegexOptions.Singleline));//сертификАТУ*/

            int countMatches = regexPrilozenieRus.Count;//если одну или две маски найдет, то значит - приложение

            for (int i = 0; i < regexPrilozenieRus.Count; i++) {
                MatchCollection matchesPrilozenieRus = regexPrilozenieRus[i].Matches(pageText);

                if (matchesPrilozenieRus.Count > 0) {
                    if (countMatches > 0) {
                        countMatches--;
                    } else {
                        break;
                    }

                }
            }

            //если хоть раз нашел маску, то приложение
            if (countMatches < regexPrilozenieRus.Count && countMatches >= 0) {
                return false;
            }

            return true;
        }

        public void initDictionary(ref Dictionary<string, string> dictionaryRusEng) {
            dictionaryRusEng = new Dictionary<string, string>();

            dictionaryRusEng.Add("а", "a");
            dictionaryRusEng.Add("б", "b");
            dictionaryRusEng.Add("в", "v");
            dictionaryRusEng.Add("г", "g");
            dictionaryRusEng.Add("д", "d");
            dictionaryRusEng.Add("е", "e");
            dictionaryRusEng.Add("ё", "yo");
            dictionaryRusEng.Add("ж", "zh");
            dictionaryRusEng.Add("з", "z");
            dictionaryRusEng.Add("й", "j");
            dictionaryRusEng.Add("к", "k");
            dictionaryRusEng.Add("л", "l");
            dictionaryRusEng.Add("м", "m");
            dictionaryRusEng.Add("н", "n");
            dictionaryRusEng.Add("о", "o");
            dictionaryRusEng.Add("р", "p");
            dictionaryRusEng.Add("т", "t");
            dictionaryRusEng.Add("и", "u");
            dictionaryRusEng.Add("ф", "f");
            dictionaryRusEng.Add("х", "h");
            dictionaryRusEng.Add("с", "c");
            dictionaryRusEng.Add("ч", "ch");
            dictionaryRusEng.Add("ш", "sh");
            dictionaryRusEng.Add("щ", "sch");
            dictionaryRusEng.Add("ъ", "j");
            dictionaryRusEng.Add("ы", "i");
            dictionaryRusEng.Add("ь", "j");
            dictionaryRusEng.Add("ю", "yu");
            dictionaryRusEng.Add("я", "ya");
            dictionaryRusEng.Add("А", "A");
            dictionaryRusEng.Add("В", "B");
            dictionaryRusEng.Add("Г", "G");
            dictionaryRusEng.Add("Д", "D");
            dictionaryRusEng.Add("Е", "E");
            dictionaryRusEng.Add("Ё", "Yo");
            dictionaryRusEng.Add("Ж", "Zh");
            dictionaryRusEng.Add("З", "Z");
            dictionaryRusEng.Add("Й", "J");
            dictionaryRusEng.Add("К", "K");
            dictionaryRusEng.Add("Л", "L");
            dictionaryRusEng.Add("М", "M");
            dictionaryRusEng.Add("О", "O");
            dictionaryRusEng.Add("Р", "P");
            dictionaryRusEng.Add("Т", "T");
            dictionaryRusEng.Add("У", "U");
            dictionaryRusEng.Add("Ф", "F");
            dictionaryRusEng.Add("Н", "H");
            dictionaryRusEng.Add("С", "C");
            dictionaryRusEng.Add("Ч", "Ch");
            dictionaryRusEng.Add("Ш", "Sh");
            dictionaryRusEng.Add("Щ", "Sch");
            dictionaryRusEng.Add("Ъ", "J");
            dictionaryRusEng.Add("Ы", "I");
            dictionaryRusEng.Add("Ь", "J");
            dictionaryRusEng.Add("Ю", "Yu");
            dictionaryRusEng.Add("Я", "Ya");
        }

        private void textBox_TextChanged(object sender, EventArgs e) {
            if (textBox.Visible) {
                textBox.SelectionStart = textBox.TextLength;
                textBox.ScrollToCaret();
            }
        }

        private void docButton_Click(object sender, EventArgs e) {
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                docFilePath = openFileDialog.FileName;//берем файлик со списком доков для распознания
                docLabel.Text = Path.GetFileName(openFileDialog.FileName);//отображаем на форме
            }
        }
    }
}
