using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using Tesseract_OCR.ABBY_FineReader;

namespace Tesseract_OCR {
    public class TABBYFineReader {
        Dictionary<string, string> words = null;
        string abbyFilePath = @"C:\Program Files (x86)\ABBYY FineReader 12\";
        public int timeABBYOCR=25000;
        public int destroyTimeInMinutes = 5;//кол-во минут до завершения процессов ABBY
        public AutoResetEvent waitHandler = new AutoResetEvent(true);
        public int delimTaskValue = 1;//кол-во тасков, запускаемых одновременно

        public TABBYFineReader(int amountOfTasks = 1) {
            //инициализируем словарь транслитеризации
            initDictionary(ref words);

            delimTaskValue = amountOfTasks;

            //ищем путь к файлу FineCmd.exe
            //abbyFilePath = getAbbyFilePath();            
        }

        /*private string getAbbyFilePath() {
            //берем список имен файлов в папке
            string[] fNames = System.IO.Directory.GetFiles(@"C:\", "*", SearchOption.AllDirectories);
            
            for(int i=0;i<fNames.Length;i++){
                if(Path.GetFileName(fNames[i]) == "FineCmd.exe") {
                    return fNames[i];
                }
            }

            return "";
        }*/

        //getDataViaRescaling - проходит несколько раз, изменяя масштаб страницы и накапливает статистику значений полей, в которых
        //затем ищет наилучшее соответствие
        public Tesseract_OCR_Window.pdfPageInfo getDataViaRescalingOld(Image sourceImage, string filePath, float stepScale, int amountOfSteps) {
            Tesseract_OCR_Window.pdfPageInfo infoPage;//структура страницы pdf

            infoPage.fullNumber_ = "";
            infoPage.techReglament_ = "ТС ";
            infoPage.shortNumber_ = "";
            infoPage.typeSeria_ = "";
            infoPage.seriaNumber_ = "";
            infoPage.typeOfSert_ = "С";
            infoPage.orgSert_ = "";
            infoPage.country_ = "";
            infoPage.typeOfPage_ = Tesseract_OCR.Tesseract_OCR_Window.typeOfPage.SERTIFICATE;
            infoPage.isTesseracted_ = false;
            infoPage.numberOfPDFPage_ = -1;
            infoPage.ocrMethod_ = Tesseract_OCR.Tesseract_OCR_Window.ocrMethod.ABBY;
            infoPage.textRus_ = "";
            infoPage.textEng_ = "";

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            float scaleValue = 1.0f;//исходный масштаб

            List<string> seriaNumberList = new List<string>();
            List<string> countryList = new List<string>();
            List<string> orgSertList = new List<string>();
            List<string> shortNumberList = new List<string>();
            List<string> typeSeriaList = new List<string>();

            float timerModifier = 0;//увеличивает таймер, по мере получения ошибки выполнения fineReader'а

            for (int k = 0; k <= amountOfSteps; k++) {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                string currentPath = System.IO.Directory.GetCurrentDirectory();

                //string outImageFolder = currentPath + "\\[Temp]Abby_images\\";
                string outImageFolder = @"C:\[Temp]Abby_images\";

                if (System.IO.Directory.Exists(outImageFolder)) {
                    System.IO.Directory.Delete(outImageFolder, true);
                }

                System.IO.Directory.CreateDirectory(outImageFolder);

                //даем права на запись в папку
                File.SetAttributes(outImageFolder, FileAttributes.Normal);

                //string outImagePath = currentPath + "\\[Temp]Abby_images\\[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";
                string outImagePath = outImageFolder + "[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";

                rescaledPage.Save(outImagePath);

                string text = "";

                //string outTextOCRFolder = currentPath + "\\[Temp]Text_OCR\\";
                string outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

                if (System.IO.Directory.Exists(outTextOCRFolder)) {
                    System.IO.Directory.Delete(outTextOCRFolder, true);
                }

                System.IO.Directory.CreateDirectory(outTextOCRFolder);

                //даем права на запись в папку
                File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

                /*//ждем пока изображение не появится в папке для недопущения ошибки finereader'а
                while ((File.Exists(outImagePath)) == false) {
                }*/

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = @"/c cd " + abbyFilePath + " & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
                //startInfo.Arguments = @"/k cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe";
                //startInfo.Arguments = @"cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Abby_images\[Rescaled]Image_1786x227.png /lang Mixed /out C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Text_OCR\Text_OCR.txt /quit";
                process.StartInfo = startInfo;
                process.Start();
                //process.WaitForExit();
                //process.WaitForExit(30000);
                process.WaitForExit(30000 + Convert.ToInt32(5000 * timerModifier));

                System.Diagnostics.Process[] procListFineReader = System.Diagnostics.Process.GetProcessesByName("FineReader");
                System.Diagnostics.Process[] procListFineExec = System.Diagnostics.Process.GetProcessesByName("FineExec");
                System.Diagnostics.Process[] procListFineCmd = System.Diagnostics.Process.GetProcessesByName("FineCmd");

                foreach (var proc in procListFineReader) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                foreach (var proc in procListFineExec) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                foreach (var proc in procListFineCmd) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                //даем 8 сек, если ошибка, то перезапускаем
                //System.Threading.Thread.Sleep(20000);
                //process.Close();

                /*System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcessesByName("FineReader");

                process.Close();

                if (processList.Length > 0) {
                    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("FineReader")) {
                        proc.Close();
                        proc.CloseMainWindow();
                    }

                    if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                        k -= 1;
                        timerModifier += 1.0f;

                        continue;
                    }
                }*/

                //обнуляем модификатор таймера
                //timerModifier = 0;

                //process.Close();

                //ждем завершения процесса
                //System.Threading.Thread.Sleep(10000);

                if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                    k -= 1;
                    timerModifier += 1.0f;

                    continue;
                }

                text = String.Join(" ", File.ReadAllLines(outTextOCRFolder + "Text_OCR.txt", Encoding.UTF8));

                //удаляем папки после использования
                if (System.IO.Directory.Exists(outImageFolder))
                {
                    System.IO.Directory.Delete(outImageFolder, true);
                }

                if (System.IO.Directory.Exists(outTextOCRFolder))
                {
                    System.IO.Directory.Delete(outTextOCRFolder, true);
                }

                //набираем статистику для последующего поиска наибольшего соответствия
                string seriaNumber = getSeriaNumber(text, text);
                string country = getCountry(text);
                string orgSert = getOrgSert(text);
                string shortNumber = getShortNumber(text);
                string typeSeria = getTypeSeria(text);

                if (seriaNumber != "") {
                    seriaNumberList.Add(seriaNumber);//серия
                }

                if (country != "") {
                    countryList.Add(country);//страна
                }

                if (orgSert != "") {
                    orgSertList.Add(orgSert);//орган по сертификации
                }

                if (shortNumber != "") {
                    shortNumberList.Add(shortNumber);//shortNumber
                }

                if (typeSeria != "") {
                    typeSeriaList.Add(typeSeria);//тип серии
                }
            }

            //собираем данные для структуры infoPage
            infoPage.seriaNumber_ = highestMatch(seriaNumberList);
            infoPage.country_ = highestMatch(countryList);
            infoPage.orgSert_ = highestMatch(orgSertList);
            infoPage.shortNumber_ = highestMatch(shortNumberList);
            infoPage.typeSeria_ = highestMatch(typeSeriaList);

            //если не нашли orgSert, то ищем в пути папки
            if (infoPage.orgSert_ == "") {
                infoPage.orgSert_ = getOrgSertViaFilePath(filePath);
            }

            //если в certsys нету, то добавляем RU
            infoPage.techReglament_ += "RU ";

            infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.typeOfSert_ + "-" + infoPage.country_ + "." + infoPage.orgSert_ + "." + infoPage.typeSeria_ + "." + infoPage.shortNumber_ + "." + infoPage.seriaNumber_;

            //ставим верхний регистр
            infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();

            //проверка на распознавание всех полей, если не распознана хотя бы одна часть номера, то в брак
            if (infoPage.country_ == "" || infoPage.fullNumber_ == "" || infoPage.orgSert_ == "" || infoPage.seriaNumber_ == "" || infoPage.shortNumber_ == "" || infoPage.techReglament_ == ""
                || infoPage.typeOfSert_ == "" || infoPage.typeSeria_ == "") {
                infoPage.isTesseracted_ = false;
            } else {
                infoPage.isTesseracted_ = true;
            }

            return infoPage;
        }

        public Tesseract_OCR_Window.pdfPageInfo getDataViaRescaling(ref Task curTask, Image sourceImage, float stepScale, int amountOfSteps) {
            Tesseract_OCR_Window.pdfPageInfo infoPage;//структура страницы pdf

            infoPage.fullNumber_ = "";
            infoPage.techReglament_ = "ТС ";
            infoPage.shortNumber_ = "";
            infoPage.typeSeria_ = "";
            infoPage.seriaNumber_ = "";
            infoPage.typeOfSert_ = "С";
            infoPage.orgSert_ = "";
            infoPage.country_ = "";
            infoPage.typeOfPage_ = Tesseract_OCR.Tesseract_OCR_Window.typeOfPage.SERTIFICATE;
            infoPage.isTesseracted_ = false;
            infoPage.numberOfPDFPage_ = -1;
            infoPage.ocrMethod_ = Tesseract_OCR.Tesseract_OCR_Window.ocrMethod.ABBY;
            infoPage.textRus_ = "";
            infoPage.textEng_ = "";

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            float scaleValue = 1.0f;//исходный масштаб

            List<string> seriaNumberList = new List<string>();
            List<string> countryList = new List<string>();
            List<string> orgSertList = new List<string>();
            List<string> shortNumberList = new List<string>();
            List<string> typeSeriaList = new List<string>();

            List<TABBYProcess> abbyProcessList = new List<TABBYProcess>();

            for (int k = 0; k <= amountOfSteps; k++) {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                int hashValue = Task.CurrentId.Value + newWidth + newHeight + k;

                //abbyProcessList.Add(new TABBYProcess(rescaledPage, Task.CurrentId.Value + k));                          
                abbyProcessList.Add(new TABBYProcess(rescaledPage, hashValue));
            }

            bool isAllProcDone=false;
            float timerModifier = 1.0f;

            do {
                //запускаем процессы распознавания
                for (int i = 0; i < abbyProcessList.Count; i++) {
                    if (abbyProcessList[i].isDone == false) {
                        abbyProcessList[i].Start();
                    }
                }
                
                //засыпаем на 30 сек.
                //System.Threading.Thread.CurrentThread.
                //System.Threading.Thread.Sleep(amountOfSteps * timeABBYOCR + Convert.ToInt32(5000 * timerModifier));
                curTask.Wait((amountOfSteps * timeABBYOCR + Convert.ToInt32(5000 * timerModifier)) * delimTaskValue);
                //System.Threading.Thread.Sleep(amountOfSteps * timeABBYOCR + (amountOfSteps * timeABBYOCR) * Task.CurrentId.Value + Convert.ToInt32(5000 * timerModifier));
                //Task.Delay(amountOfSteps * timeABBYOCR + Convert.ToInt32(5000 * timerModifier));
                //System.Threading.Thread.Sleep(amountOfSteps * timeABBYOCR + Convert.ToInt32(5000 * timerModifier));

                /*//удаляем следы FineReader'а
                //закомментил, т.к. тут иногда будет стопориться, поэтому освобождаю в конце
                destroyFRProcess();*/
                
                //проверяем состояния процессов
                int countDone = 0;

                for (int i = 0; i < abbyProcessList.Count; i++) {
                    abbyProcessList[i].Refresh();

                    if(abbyProcessList[i].isDone==true){
                        countDone++;
                    }
                }

                if (countDone == abbyProcessList.Count) {
                    isAllProcDone = true;
                } else {
                    timerModifier += 1.0f;
                }       
            } while (isAllProcDone == false);

            for(int i=0;i<abbyProcessList.Count;i++){
                waitHandler.WaitOne();
                string text = String.Join(" ", File.ReadAllLines(abbyProcessList[i].outTextOCRPath, Encoding.UTF8));
                waitHandler.Set();

                //набираем статистику для последующего поиска наибольшего соответствия
                string seriaNumber = getSeriaNumber(text, text);
                string country = getCountry(text);
                string orgSert = getOrgSert(text);
                string shortNumber = getShortNumber(text);
                string typeSeria = getTypeSeria(text);

                if (seriaNumber != "") {
                    seriaNumberList.Add(seriaNumber);//серия
                }

                if (country != "") {
                    countryList.Add(country);//страна
                }

                if (orgSert != "") {
                    orgSertList.Add(orgSert);//орган по сертификации
                }

                if (shortNumber != "") {
                    shortNumberList.Add(shortNumber);//shortNumber
                }

                if (typeSeria != "") {
                    typeSeriaList.Add(typeSeria);//тип серии
                }

                //удаляем все папки, созданные процессом
                abbyProcessList[i].Destroy();                
            }
 
            //собираем данные для структуры infoPage
            infoPage.seriaNumber_ = highestMatch(seriaNumberList);
            infoPage.country_ = highestMatch(countryList);
            infoPage.orgSert_ = highestMatch(orgSertList);
            infoPage.shortNumber_ = highestMatch(shortNumberList);
            infoPage.typeSeria_ = highestMatch(typeSeriaList);

            /*//если не нашли orgSert, то ищем в пути папки
            if (infoPage.orgSert_ == "") {
                infoPage.orgSert_ = "МЮ62";
                //infoPage.orgSert_ = getOrgSertViaFilePath(filePath);
            }*/

            infoPage.orgSert_ = "МЮ62";

            //если в certsys нету, то добавляем RU
            infoPage.techReglament_ += "RU ";

            infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.typeOfSert_ + "-" + infoPage.country_ + "." + infoPage.orgSert_ + "." + infoPage.typeSeria_ + "." + infoPage.shortNumber_ + "." + infoPage.seriaNumber_;

            //ставим верхний регистр
            infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();

            //проверка на распознавание всех полей, если не распознана хотя бы одна часть номера, то в брак
            if (infoPage.country_ == "" || infoPage.fullNumber_ == "" || infoPage.orgSert_ == "" || infoPage.seriaNumber_ == "" || infoPage.shortNumber_ == "" || infoPage.techReglament_ == ""
                || infoPage.typeOfSert_ == "" || infoPage.typeSeria_ == "") {
                infoPage.isTesseracted_ = false;
            } else {
                infoPage.isTesseracted_ = true;
            }

            //удаляем следы FineReader'а
            //destroyFRProcess();

            //как только все таски отработали, удаляем временные папки
            //нужно также по причине связки тасков с id в контейнере и добавлением
            /*Task.WaitAll();

            if (System.IO.Directory.Exists(abbyProcessList[0].outImageFolder)) {
                System.IO.Directory.Delete(abbyProcessList[0].outImageFolder, true);
            }

            if (System.IO.Directory.Exists(abbyProcessList[0].outTextOCRFolder)) {
                System.IO.Directory.Delete(abbyProcessList[0].outTextOCRFolder, true);
            }*/

            return infoPage;
        }

        public void destroyOutFolders() {
            string outImageFolder = @"C:\[Temp]Abby_images\";
            string outTextOCRFolder = @"C:\[Temp]Text_OCR\";

            if (System.IO.Directory.Exists(outImageFolder)) {
                System.IO.Directory.Delete(outImageFolder, true);
            }

            if (System.IO.Directory.Exists(outTextOCRFolder)) {
                System.IO.Directory.Delete(outTextOCRFolder, true);
            }
        }

        public void destroyFRProcess() {
            //удаляем процессы FineReader'а
            System.Diagnostics.Process[] procListFineReader = System.Diagnostics.Process.GetProcessesByName("FineReader");
            System.Diagnostics.Process[] procListFineExec = System.Diagnostics.Process.GetProcessesByName("FineExec");
            System.Diagnostics.Process[] procListFineCmd = System.Diagnostics.Process.GetProcessesByName("FineCmd");

            foreach (var proc in procListFineReader) {
                TimeSpan runTime = DateTime.Now - proc.StartTime;

                if ((proc.HasExited == false) && (runTime.TotalMinutes > destroyTimeInMinutes)) {
                    proc.Kill();
                }
            }

            foreach (var proc in procListFineExec) {
                TimeSpan runTime = DateTime.Now - proc.StartTime;

                if ((proc.HasExited == false) && (runTime.TotalMinutes > destroyTimeInMinutes)) {
                    proc.Kill();
                }
            }

            foreach (var proc in procListFineCmd) {
                TimeSpan runTime = DateTime.Now - proc.StartTime;

                if ((proc.HasExited == false) && (runTime.TotalMinutes > destroyTimeInMinutes)) {
                    proc.Kill();
                }
            }
        }

        private string getOrgSertViaFilePath(string filePath) {
            Regex regexPrommash = new Regex(@"ПРОММАШТЕСТ", RegexOptions.IgnoreCase | RegexOptions.Singleline);//проммаш тест

            MatchCollection matchesPrommash = regexPrommash.Matches(filePath);

            if (matchesPrommash.Count > 0) {
                return "МЮ62";
            } else {
                return "";
            }
        }

        //getData - проходит один раз
        public Tesseract_OCR_Window.pdfPageInfo getData(Image sourceImage) {
            Tesseract_OCR_Window.pdfPageInfo infoPage;//структура страницы pdf

            infoPage.fullNumber_ = "";
            infoPage.techReglament_ = "ТС ";
            infoPage.shortNumber_ = "";
            infoPage.typeSeria_ = "";
            infoPage.seriaNumber_ = "";
            infoPage.typeOfSert_ = "С";
            infoPage.orgSert_ = "";
            infoPage.country_ = "";
            infoPage.typeOfPage_ = Tesseract_OCR.Tesseract_OCR_Window.typeOfPage.SERTIFICATE;
            infoPage.isTesseracted_ = false;
            infoPage.numberOfPDFPage_ = -1;
            infoPage.ocrMethod_ = Tesseract_OCR.Tesseract_OCR_Window.ocrMethod.ABBY;
            infoPage.textRus_ = "";
            infoPage.textEng_ = "";

            string currentPath = System.IO.Directory.GetCurrentDirectory();

            //string outImageFolder = currentPath + "\\[Temp]Abby_images\\";
            string outImageFolder = @"C:\[Temp]Abby_images\";

            if (System.IO.Directory.Exists(outImageFolder)) {
                System.IO.Directory.Delete(outImageFolder, true);
            }

            System.IO.Directory.CreateDirectory(outImageFolder);

            //даем права на запись в папку
            File.SetAttributes(outImageFolder, FileAttributes.Normal);

            //string outImagePath = currentPath + "\\[Temp]Abby_images\\[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";
            string outImagePath = outImageFolder + "[Source_image]Image.png";

            sourceImage.Save(outImagePath);

            string text = "";

            //string outTextOCRFolder = currentPath + "\\[Temp]Text_OCR\\";
            string outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

            if (System.IO.Directory.Exists(outTextOCRFolder)) {
                System.IO.Directory.Delete(outTextOCRFolder, true);
            }

            System.IO.Directory.CreateDirectory(outTextOCRFolder);

            //даем права на запись в папку
            File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = @"/c cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
            //startInfo.Arguments = @"/k cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe";
            //startInfo.Arguments = @"cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Abby_images\[Rescaled]Image_1786x227.png /lang Mixed /out C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Text_OCR\Text_OCR.txt /quit";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            process.Close();

            text = String.Join(" ", File.ReadAllLines(outTextOCRFolder + "Text_OCR.txt", Encoding.UTF8));

            //набираем статистику для последующего поиска наибольшего соответствия
            string seriaNumber = getSeriaNumber(text, text);
            string country = getCountry(text);
            string orgSert = getOrgSert(text);
            string shortNumber = getShortNumber(text);
            string typeSeria = getTypeSeria(text);

            //если в certsys нету, то добавляем RU
            infoPage.techReglament_ += "RU ";

            infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.typeOfSert_ + "-" + infoPage.country_ + "." + infoPage.orgSert_ + "." + infoPage.typeSeria_ + "." + infoPage.shortNumber_ + "." + infoPage.seriaNumber_;

            //ставим верхний регистр
            infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();

            //проверка на распознавание всех полей, если не распознана хотя бы одна часть номера, то в брак
            if (infoPage.country_ == "" || infoPage.fullNumber_ == "" || infoPage.orgSert_ == "" || infoPage.seriaNumber_ == "" || infoPage.shortNumber_ == "" || infoPage.techReglament_ == ""
                || infoPage.typeOfSert_ == "" || infoPage.typeSeria_ == "") {
                infoPage.isTesseracted_ = false;
            } else {
                infoPage.isTesseracted_ = true;
            }

            return infoPage;
        }

        public string getSeriaNumber(string inputString, string inputEngString) {
            Regex regexSeriaNumber = new Regex(@"[0-9]{7}", RegexOptions.IgnoreCase);

            MatchCollection matchesSeriaNumber = regexSeriaNumber.Matches(inputString);

            //[ENG][RUS]проверяем серию
            if (matchesSeriaNumber.Count > 0) {
                int countNull = 0;

                for (int k = 0; k < matchesSeriaNumber.Count; k++) {
                    for (int b = 0; b < matchesSeriaNumber[k].Value.Length; b++) {
                        if (matchesSeriaNumber[k].Value[b] == '0') {
                            countNull++;
                        }
                    }

                }

                //если >= 1, то запоминаем
                if (countNull >= 1) {
                    //чистим на всякий случай
                    string pattern = @"[\W| ]*";
                    string replacement = "";

                    string seriaNumber = matchesSeriaNumber[0].Value;

                    seriaNumber = Regex.Replace(seriaNumber, pattern, replacement);

                    return seriaNumber;
                }
            }

            return "";
        }

        public string getSeriaNumber(Image sourceImage) {
            string currentPath = System.IO.Directory.GetCurrentDirectory();

            //string outImageFolder = currentPath + "\\[Temp]Abby_images\\";
            string outImageFolder = @"C:\[Temp]Abby_images\";

            if (System.IO.Directory.Exists(outImageFolder)) {
                System.IO.Directory.Delete(outImageFolder, true);
            }

            System.IO.Directory.CreateDirectory(outImageFolder);

            //даем права на запись в папку
            File.SetAttributes(outImageFolder, FileAttributes.Normal);

            //string outImagePath = currentPath + "\\[Temp]Abby_images\\[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";
            string outImagePath = outImageFolder + "[Rescaled]Image.png";

            sourceImage.Save(outImagePath);

            //string outTextOCRFolder = currentPath + "\\[Temp]Text_OCR\\";
            string outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

            if (System.IO.Directory.Exists(outTextOCRFolder)) {
                System.IO.Directory.Delete(outTextOCRFolder, true);
            }

            System.IO.Directory.CreateDirectory(outTextOCRFolder);

            //даем права на запись в папку
            File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = @"/c cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
            //startInfo.Arguments = @"/k cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe";
            //startInfo.Arguments = @"cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Abby_images\[Rescaled]Image_1786x227.png /lang Mixed /out C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Text_OCR\Text_OCR.txt /quit";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            process.Close();

            string text = String.Join(" ", File.ReadAllLines(outTextOCRFolder + "Text_OCR.txt", Encoding.UTF8));

            //удаляем папки после использования
            if (System.IO.Directory.Exists(outImageFolder)) {
                System.IO.Directory.Delete(outImageFolder, true);
            }

            if (System.IO.Directory.Exists(outTextOCRFolder)) {
                System.IO.Directory.Delete(outTextOCRFolder, true);
            }

            Regex regexSeriaNumber = new Regex(@"[0-9]{7}", RegexOptions.IgnoreCase);

            MatchCollection matchesSeriaNumber = regexSeriaNumber.Matches(text);

            //[ENG][RUS]проверяем серию
            if (matchesSeriaNumber.Count > 0) {
                int countNull = 0;

                for (int k = 0; k < matchesSeriaNumber.Count; k++) {
                    for (int b = 0; b < matchesSeriaNumber[k].Value.Length; b++) {
                        if (matchesSeriaNumber[k].Value[b] == '0') {
                            countNull++;
                        }
                    }

                }

                //если >= 1, то запоминаем
                if (countNull >= 1) {
                    //чистим на всякий случай
                    string pattern = @"[\W| ]*";
                    string replacement = "";

                    string seriaNumber = matchesSeriaNumber[0].Value;

                    seriaNumber = Regex.Replace(seriaNumber, pattern, replacement);

                    return seriaNumber;
                }
            }

            return "";
        }

        public string getSeriaNumberViaRescalingOld(Image sourceImage, float stepScale, int amountOfSteps) {
            Tesseract_OCR_Window.pdfPageInfo infoPage;//структура страницы pdf

            infoPage.fullNumber_ = "";
            infoPage.techReglament_ = "ТС ";
            infoPage.shortNumber_ = "";
            infoPage.typeSeria_ = "";
            infoPage.seriaNumber_ = "";
            infoPage.typeOfSert_ = "С";
            infoPage.orgSert_ = "";
            infoPage.country_ = "";
            infoPage.typeOfPage_ = Tesseract_OCR.Tesseract_OCR_Window.typeOfPage.SERTIFICATE;
            infoPage.isTesseracted_ = false;
            infoPage.numberOfPDFPage_ = -1;
            infoPage.ocrMethod_ = Tesseract_OCR.Tesseract_OCR_Window.ocrMethod.ABBY;
            infoPage.textRus_ = "";
            infoPage.textEng_ = "";

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            float scaleValue = 1.0f;//исходный масштаб

            List<string> seriaNumberList = new List<string>();

            float timerModifier = 0;//увеличивает таймер, по мере получения ошибки выполнения fineReader'а

            for (int k = 0; k <= amountOfSteps; k++) {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                string currentPath = System.IO.Directory.GetCurrentDirectory();

                //string outImageFolder = currentPath + "\\[Temp]Abby_images\\";
                string outImageFolder = @"C:\[Temp]Abby_images\";

                if (System.IO.Directory.Exists(outImageFolder)) {
                    System.IO.Directory.Delete(outImageFolder, true);
                }

                System.IO.Directory.CreateDirectory(outImageFolder);

                //даем права на запись в папку
                File.SetAttributes(outImageFolder, FileAttributes.Normal);

                //string outImagePath = currentPath + "\\[Temp]Abby_images\\[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";
                string outImagePath = outImageFolder + "[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";

                rescaledPage.Save(outImagePath);

                string text = "";

                //string outTextOCRFolder = currentPath + "\\[Temp]Text_OCR\\";
                string outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

                if (System.IO.Directory.Exists(outTextOCRFolder)) {
                    System.IO.Directory.Delete(outTextOCRFolder, true);
                }

                System.IO.Directory.CreateDirectory(outTextOCRFolder);

                //даем права на запись в папку
                File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

                /*//ждем пока изображение не появится в папке для недопущения ошибки finereader'а
                while ((File.Exists(outImagePath)) == false) {
                }*/

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = @"/c cd " + abbyFilePath + " & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
                //startInfo.Arguments = @"/k cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe";
                //startInfo.Arguments = @"cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Abby_images\[Rescaled]Image_1786x227.png /lang Mixed /out C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Text_OCR\Text_OCR.txt /quit";
                process.StartInfo = startInfo;
                process.Start();
                //process.WaitForExit();
                //process.WaitForExit(30000);
                process.WaitForExit(30000 + Convert.ToInt32(5000 * timerModifier));

                //System.Threading.Thread.Sleep(20000);
                //process.Refresh();

                /*if (process.HasExited==false) {
                    process.CloseMainWindow();
                    process.Close();
                }*/

                System.Diagnostics.Process[] procListFineReader = System.Diagnostics.Process.GetProcessesByName("FineReader");
                System.Diagnostics.Process[] procListFineExec = System.Diagnostics.Process.GetProcessesByName("FineExec");
                System.Diagnostics.Process[] procListFineCmd = System.Diagnostics.Process.GetProcessesByName("FineCmd");

                foreach (var proc in procListFineReader) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                foreach (var proc in procListFineExec) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                foreach (var proc in procListFineCmd) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                //даем 8 сек, если ошибка, то перезапускаем
                /*System.Threading.Thread.Sleep(7000 + Convert.ToInt32(1000 * timerModifier));

                System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcessesByName("FineReader");

                process.Close();

                if (processList.Length > 0) {
                    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("FineReader")) {
                        proc.Close();
                        proc.CloseMainWindow();                        
                    }

                    if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                        k -= 1;
                        timerModifier += 1.0f;

                        continue;
                    }
                }*/

                //обнуляем модификатор таймера
                //timerModifier = 0;
                                
                //process.Close();

                //ждем завершения процесса
                //System.Threading.Thread.Sleep(10000);
                /*do{
                    process.Refresh();
                }while(process.Responding);*/

                if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                    k -= 1;
                    timerModifier += 1.0f;

                    continue;
                }

                text = String.Join(" ", File.ReadAllLines(outTextOCRFolder + "Text_OCR.txt", Encoding.UTF8));

                //удаляем папки после использования
                if (System.IO.Directory.Exists(outImageFolder))
                {
                    System.IO.Directory.Delete(outImageFolder, true);
                }

                if (System.IO.Directory.Exists(outTextOCRFolder))
                {
                    System.IO.Directory.Delete(outTextOCRFolder, true);
                }

                //набираем статистику для последующего поиска наибольшего соответствия
                string seriaNumber = getSeriaNumber(text, text);

                if (seriaNumber != "") {
                    seriaNumberList.Add(seriaNumber);//серия
                }
            }

            //собираем данные для структуры infoPage
            string seriaNumberHighestMatch = highestMatch(seriaNumberList);

            return seriaNumberHighestMatch;
        }

        public string getSeriaNumberViaRescaling(Image sourceImage, float stepScale, int amountOfSteps) {
            Tesseract_OCR_Window.pdfPageInfo infoPage;//структура страницы pdf

            infoPage.fullNumber_ = "";
            infoPage.techReglament_ = "ТС ";
            infoPage.shortNumber_ = "";
            infoPage.typeSeria_ = "";
            infoPage.seriaNumber_ = "";
            infoPage.typeOfSert_ = "С";
            infoPage.orgSert_ = "";
            infoPage.country_ = "";
            infoPage.typeOfPage_ = Tesseract_OCR.Tesseract_OCR_Window.typeOfPage.SERTIFICATE;
            infoPage.isTesseracted_ = false;
            infoPage.numberOfPDFPage_ = -1;
            infoPage.ocrMethod_ = Tesseract_OCR.Tesseract_OCR_Window.ocrMethod.ABBY;
            infoPage.textRus_ = "";
            infoPage.textEng_ = "";

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            float scaleValue = 1.0f;//исходный масштаб

            List<string> seriaNumberList = new List<string>();

            List<TABBYProcess> abbyProcessList = new List<TABBYProcess>();

            for (int k = 0; k <= amountOfSteps; k++) {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                abbyProcessList.Add(new TABBYProcess(rescaledPage, k));
            }

            bool isAllProcDone = false;
            float timerModifier = 0;

            do {
                //запускаем процессы распознавания
                for (int i = 0; i < abbyProcessList.Count; i++) {
                    if (abbyProcessList[i].isDone == false) {
                        abbyProcessList[i].Start();
                    }
                }

                //засыпаем на 30 сек.
                System.Threading.Thread.Sleep(amountOfSteps * timeABBYOCR + Convert.ToInt32(5000 * timerModifier));

                /*//удаляем следы FineReader'а
                //закомментил, т.к. тут иногда будет стопориться, поэтому освобождаю в конце
                destroyFRProcess();*/

                //проверяем состояния процессов
                int countDone = 0;

                for (int i = 0; i < abbyProcessList.Count; i++) {
                    abbyProcessList[i].Refresh();

                    if (abbyProcessList[i].isDone == true) {
                        countDone++;
                    }
                }

                if (countDone == abbyProcessList.Count) {
                    isAllProcDone = true;
                } else {
                    timerModifier += 1.0f;
                }
            } while (isAllProcDone == false);

            for (int i = 0; i < abbyProcessList.Count; i++) {
                string text = String.Join(" ", File.ReadAllLines(abbyProcessList[i].outTextOCRPath, Encoding.UTF8));

                //набираем статистику для последующего поиска наибольшего соответствия
                string seriaNumber = getSeriaNumber(text, text);

                if (seriaNumber != "") {
                    seriaNumberList.Add(seriaNumber);//серия
                }

                //удаляем все папки, созданные процессом
                abbyProcessList[i].Destroy();
            }

            //собираем данные для структуры infoPage
            string seriaNumberHighestMatch = highestMatch(seriaNumberList);

            //удаляем следы FineReader'а
            destroyFRProcess();

            //удаляем временные папки
            if (System.IO.Directory.Exists(abbyProcessList[0].outImageFolder)) {
                System.IO.Directory.Delete(abbyProcessList[0].outImageFolder, true);
            }

            if (System.IO.Directory.Exists(abbyProcessList[0].outTextOCRFolder)) {
                System.IO.Directory.Delete(abbyProcessList[0].outTextOCRFolder, true);
            }

            return seriaNumberHighestMatch;
        }

        //highestMatch - ищет наилучшее соответствие значения в контейнере
        private string highestMatch(List<string> dataList) {
            //если пустые данные, то выходим
            if (dataList.Count == 0) {
                return "";
            }

            List<KeyValuePair<string, int>> valueChecksList = new List<KeyValuePair<string, int>>();

            //проходим по списку серий и ищем правильную
            for (int i = 0; i < dataList.Count; i++) {
                //проверяем серию
                if (valueChecksList.Count == 0) {
                    valueChecksList.Add(new KeyValuePair<string, int>(dataList[i], 1));
                    continue;
                }

                bool isFoundValue = false;

                for (int j = 0; j < valueChecksList.Count; j++) {
                    if (dataList[i] == valueChecksList[j].Key) {
                        valueChecksList[j] = new KeyValuePair<string, int>(valueChecksList[j].Key, valueChecksList[j].Value + 1);

                        isFoundValue = true;
                    }
                }

                if (isFoundValue == false) {
                    valueChecksList.Add(new KeyValuePair<string, int>(dataList[i], 1));
                }
            }

            //проходим и выбираем точную серию
            int rightSeriaValue = 0;
            int rightIndSeria = 0;

            for (int i = 0; i < valueChecksList.Count; i++) {
                if (i == 0) {
                    rightSeriaValue = valueChecksList[i].Value;
                    rightIndSeria = i;
                } else {
                    if (rightSeriaValue < valueChecksList[i].Value) {
                        rightSeriaValue = valueChecksList[i].Value;
                        rightIndSeria = i;
                    }
                }
            }

            string highestMatchValue = valueChecksList[rightIndSeria].Key;

            return highestMatchValue;
        }

        public string getShortNumberOld(string inputString)//берет русскую строку
        {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputString);
            MatchCollection matchesFullNumberEng = regexFullNumberEng.Matches(inputString);

            Regex regexShortNumber = new Regex(@"[.]{1}[0-9]{5}[.| ]*", RegexOptions.IgnoreCase);

            //[ENG][RUS]если одно соответствие, то берем
            string matchFullNumber = "";

            if (matchesFullNumberEng.Count > 0) {
                matchFullNumber = matchesFullNumberEng[0].Value;
            } else if (matchesFullNumberRus.Count > 0) {
                matchFullNumber = matchesFullNumberRus[0].Value;
            } else {
                return "";
            }

            string pageText = matchFullNumber;

            pageText = pageText.ToUpper();//ставим верхний регистр

            MatchCollection matchesShortNumber = regexShortNumber.Matches(pageText);

            if (matchesShortNumber.Count == 1) {
                //чистим
                string pattern = @"[\W| ]*";
                string replacement = "";

                string shortNumber = matchesShortNumber[0].Value;

                shortNumber = Regex.Replace(shortNumber, pattern, replacement);

                return shortNumber;
            }


            return "";
        }

        public string getShortNumber(string inputString)//берет русскую строку
        {
            Regex regexShortNumber = new Regex(@"[.]{1}[0-9]{5}[.| ]*", RegexOptions.IgnoreCase);

            string pageText = inputString;

            pageText = pageText.ToUpper();//ставим верхний регистр

            MatchCollection matchesShortNumber = regexShortNumber.Matches(pageText);

            if (matchesShortNumber.Count == 1) {
                //чистим
                string pattern = @"[\W| ]*";
                string replacement = "";

                string shortNumber = matchesShortNumber[0].Value;

                shortNumber = Regex.Replace(shortNumber, pattern, replacement);

                return shortNumber;
            }


            return "";
        }

        public string getTypeSeriaOld(string inputString) {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputString);
            MatchCollection matchesFullNumberEng = regexFullNumberEng.Matches(inputString);

            //берем typeSeria
            //Regex regexTypeSeria = new Regex(@"[0-9]{2}\W*[А-Я]{1}\W*[0-9]", RegexOptions.IgnoreCase);
            //Regex regexTypeSeria = new Regex(@"[.| ]{1}[А-Я]{1}[.| ]{1}", RegexOptions.IgnoreCase);
            Regex regexTypeSeria = new Regex(@"[.]{1}[А-Я]{1}[.]{1}", RegexOptions.IgnoreCase);
            //Regex regexTypeSeria = new Regex(@"[А-Я]{2}[0-9]{2}\W*[А-Я]{1}", RegexOptions.IgnoreCase);
            //Regex regexTypeSeria = new Regex(@"[ |.|]{1}[А-Я]{1}[ |.]{1}", RegexOptions.IgnoreCase);

            MatchCollection matchesTypeSeria = regexTypeSeria.Matches(inputString);

            string typeSeria = "";

            //[RUS]если одно соответствие, то берем
            if (matchesFullNumberRus.Count > 0) {
                if (matchesTypeSeria.Count > 0) {
                    //чистим
                    typeSeria = matchesTypeSeria[0].Value;

                    string patternTypeSeria = @"[\d|\W]*";
                    //string patternTypeSeria = @"\W*";
                    string replacementTypeSeria = "";

                    typeSeria = Regex.Replace(typeSeria, patternTypeSeria, replacementTypeSeria);
                }
            }

            //перепроверяем typeSeria, если нет в рус, то ищем в англ с последующим конвертом
            if ((typeSeria == "") && (matchesFullNumberEng.Count > 0)) {
                string pageTextEng = matchesFullNumberEng[0].Value;

                pageTextEng = pageTextEng.ToUpper();//ставим верхний регистр

                //берем typeSeria
                //Regex regexTypeSeriaEng = new Regex(@"[0-9]{2}\W*[A-Z]{1}\W*[0-9]", RegexOptions.IgnoreCase);
                Regex regexTypeSeriaEng = new Regex(@"[.| ]{1}[A-Z]{1}[.| ]{1}", RegexOptions.IgnoreCase);
                MatchCollection matchesTypeSeriaEng = regexTypeSeriaEng.Matches(pageTextEng);

                if (matchesTypeSeriaEng.Count > 0) {
                    //чистим
                    typeSeria = matchesTypeSeriaEng[0].Value;

                    string patternTypeSeria = @"[\d|\W]*";
                    string replacementTypeSeria = "";

                    typeSeria = Regex.Replace(typeSeria, patternTypeSeria, replacementTypeSeria);

                    foreach (KeyValuePair<string, string> pair in words) {
                        typeSeria = typeSeria.Replace(pair.Key, pair.Value);
                    }

                    return typeSeria;

                }
            } else {
                return typeSeria;
            }

            return "";
        }

        public string getTypeSeria(string inputString) {
            //берем typeSeria
            //Regex regexTypeSeria = new Regex(@"[0-9]{2}\W*[А-Я]{1}\W*[0-9]", RegexOptions.IgnoreCase);
            //Regex regexTypeSeria = new Regex(@"[.| ]{1}[А-Я]{1}[.| ]{1}", RegexOptions.IgnoreCase);
            Regex regexTypeSeriaEng = new Regex(@"[.]{1}[A-Z]{1}[.]{1}", RegexOptions.IgnoreCase);
            Regex regexTypeSeriaRus = new Regex(@"[.]{1}[А-Я]{1}[.]{1}", RegexOptions.IgnoreCase);

            //Regex regexTypeSeria = new Regex(@"[А-Я]{2}[0-9]{2}\W*[А-Я]{1}", RegexOptions.IgnoreCase);
            //Regex regexTypeSeria = new Regex(@"[ |.|]{1}[А-Я]{1}[ |.]{1}", RegexOptions.IgnoreCase);

            MatchCollection matchesTypeSeriaEng = regexTypeSeriaEng.Matches(inputString);
            MatchCollection matchesTypeSeriaRus = regexTypeSeriaRus.Matches(inputString);

            string typeSeria = "";

            //перепроверяем typeSeria, если нет в рус, то ищем в англ с последующим конвертом
            if (matchesTypeSeriaRus.Count > 0) {
                string pageText = matchesTypeSeriaRus[0].Value;

                pageText = pageText.ToUpper();//ставим верхний регистр

                string patternTypeSeria = @"\W*";
                string replacementTypeSeria = "";

                typeSeria = Regex.Replace(pageText, patternTypeSeria, replacementTypeSeria);

                return typeSeria;

            }

            if (matchesTypeSeriaEng.Count > 0) {
                string pageText = matchesTypeSeriaEng[0].Value;

                pageText = pageText.ToUpper();//ставим верхний регистр

                string patternTypeSeria = @"\W*";
                string replacementTypeSeria = "";

                typeSeria = Regex.Replace(pageText, patternTypeSeria, replacementTypeSeria);

                foreach (KeyValuePair<string, string> pair in words) {
                    //тут наоборот значение на ключ, т.к. нужно англ. на рус.
                    typeSeria = typeSeria.Replace(pair.Value, pair.Key);
                }

                return typeSeria;
            }
            
            return typeSeria;
        }

        public string getCountryOld(string inputEngString) {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputEngString);
            MatchCollection matchesFullNumberEng = regexFullNumberEng.Matches(inputEngString);

            //Regex regexCountry = new Regex(@"[-][ ]*[A-Z]{2}[ |.]*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexCountry = new Regex(@"[-][ ]*[A-Z][ ]*[A-Z]*[ |.]*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            //[ENG][RUS]если одно соответствие, то берем
            string matchFullNumber = "";

            if (matchesFullNumberEng.Count > 0) {
                matchFullNumber = matchesFullNumberEng[0].Value;
            } else if (matchesFullNumberRus.Count > 0) {
                matchFullNumber = matchesFullNumberRus[0].Value;
            } else {
                return "";
            }

            string pageText = matchFullNumber;

            pageText = pageText.ToUpper();//ставим верхний регистр

            MatchCollection matchesCountryEng = regexCountry.Matches(pageText);

            if (matchesCountryEng.Count > 0) {
                string countryTextEng = "";

                if (matchesCountryEng.Count == 1) {
                    countryTextEng = matchesCountryEng[0].Value;
                } else {
                    countryTextEng = matchesCountryEng[1].Value;
                }

                //чистим лишнее
                string pattern = @"\W*[ ]*[0-9]*";
                string replacement = "";

                countryTextEng = Regex.Replace(countryTextEng, pattern, replacement);

                //проверка, если есть англ. буквы, то выкидываем
                Regex regexCheckStringRusAndEng = new Regex(@"[А-Я]{1}[A-Z]{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Regex regexCheckStringEngAndRus = new Regex(@"[A-Z]{1}[А-Я]{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                MatchCollection matchesCheckStringRusAndEng = regexCheckStringRusAndEng.Matches(countryTextEng);
                MatchCollection matchesCheckStringEngAndRus = regexCheckStringEngAndRus.Matches(countryTextEng);

                if ((matchesCheckStringRusAndEng.Count > 0) || (matchesCheckStringEngAndRus.Count > 0)) {
                    countryTextEng = "";
                }

                return countryTextEng;
            }

            return "";
        }

        public string getCountry(string inputString) {
            //Regex regexCountry = new Regex(@"[-][ ]*[A-Z]{2}[ |.]*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexCountry = new Regex(@"[-][ ]*[A-Z]{1}[ ]*[A-Z]{1}[ |.]*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            string pageText = inputString;

            pageText = pageText.ToUpper();//ставим верхний регистр

            MatchCollection matchesCountryEng = regexCountry.Matches(pageText);

            if (matchesCountryEng.Count > 0) {
                string countryTextEng = "";

                if (matchesCountryEng.Count == 1) {
                    countryTextEng = matchesCountryEng[0].Value;
                } else {
                    countryTextEng = matchesCountryEng[1].Value;
                }

                //чистим лишнее
                string pattern = @"\W*[ ]*[0-9]*";
                string replacement = "";

                countryTextEng = Regex.Replace(countryTextEng, pattern, replacement);

                //проверка, если есть англ. буквы, то выкидываем
                Regex regexCheckStringRusAndEng = new Regex(@"[А-Я]{1}[A-Z]{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Regex regexCheckStringEngAndRus = new Regex(@"[A-Z]{1}[А-Я]{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                MatchCollection matchesCheckStringRusAndEng = regexCheckStringRusAndEng.Matches(countryTextEng);
                MatchCollection matchesCheckStringEngAndRus = regexCheckStringEngAndRus.Matches(countryTextEng);

                if ((matchesCheckStringRusAndEng.Count > 0) || (matchesCheckStringEngAndRus.Count > 0)) {
                    countryTextEng = "";
                }

                return countryTextEng;
            }

            return "";
        }

        public string getOrgSertOld(string inputString) {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputString);
            MatchCollection matchesFullNumberEng = regexFullNumberEng.Matches(inputString);

            //[ENG][RUS]если одно соответствие, то берем
            string matchFullNumber = "";

            if (matchesFullNumberEng.Count > 0) {
                matchFullNumber = matchesFullNumberEng[0].Value;
            } else if (matchesFullNumberRus.Count > 0) {
                matchFullNumber = matchesFullNumberRus[0].Value;
            } else {
                return "";
            }

            string pageText = matchFullNumber;

            pageText = pageText.ToUpper();//ставим верхний регистр

            string pattern = @"[\W| ]*";
            string replacement = "";

            //берем orgSert
            Regex regexOrgSert = new Regex(@"\W[А-Я|A-Z]{2}[0-9]{2}\W", RegexOptions.IgnoreCase);
            MatchCollection matchesOrgSert = regexOrgSert.Matches(pageText);

            if (matchesOrgSert.Count == 1) {
                //чистим
                string orgSert = matchesOrgSert[0].Value;

                orgSert = Regex.Replace(orgSert, pattern, replacement);

                foreach (KeyValuePair<string, string> pair in words) {
                    //тут наоборот значение на ключ, т.к. нужно англ. на рус.
                    orgSert = orgSert.Replace(pair.Value, pair.Key);
                }

                //infoPage.orgSert_ = temp.Substring(temp.Length - (shortNumberLength + typeOfSeriaLength + orgSertLength), orgSertLength);
                return orgSert;
            }

            return "";
        }

        public string getOrgSert(string inputString) {
            //берем orgSert
            Regex regexOrgSert = new Regex(@"[.]{1}[А-Я|A-Z]{2}[0-9]{2}[.]{1}", RegexOptions.IgnoreCase);
            MatchCollection matchesOrgSert = regexOrgSert.Matches(inputString);

            if (matchesOrgSert.Count == 1) {
                string pattern = @"\W*";
                string replacement = "";

                //чистим
                string orgSert = matchesOrgSert[0].Value;

                orgSert = Regex.Replace(orgSert, pattern, replacement);

                foreach (KeyValuePair<string, string> pair in words) {
                    //тут наоборот значение на ключ, т.к. нужно англ. на рус.
                    orgSert = orgSert.Replace(pair.Value, pair.Key);
                }

                //infoPage.orgSert_ = temp.Substring(temp.Length - (shortNumberLength + typeOfSeriaLength + orgSertLength), orgSertLength);
                return orgSert;
            }

            return "";
        }

        public bool isSertificate(Image sourceImage) {
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

            string currentPath = System.IO.Directory.GetCurrentDirectory();

            //string outImageFolder = currentPath + "\\[Temp]Abby_images\\";
            string outImageFolder = @"C:\[Temp]Abby_images\";

            if (System.IO.Directory.Exists(outImageFolder)) {
                System.IO.Directory.Delete(outImageFolder, true);
            }

            System.IO.Directory.CreateDirectory(outImageFolder);

            //даем права на запись в папку
            File.SetAttributes(outImageFolder, FileAttributes.Normal);

            //string outImagePath = currentPath + "\\[Temp]Abby_images\\[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";
            string outImagePath = outImageFolder + "[Rescaled]Image.png";

            sourceImage.Save(outImagePath);

            //string outTextOCRFolder = currentPath + "\\[Temp]Text_OCR\\";
            string outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

            if (System.IO.Directory.Exists(outTextOCRFolder)) {
                System.IO.Directory.Delete(outTextOCRFolder, true);
            }

            System.IO.Directory.CreateDirectory(outTextOCRFolder);

            //даем права на запись в папку
            File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = @"/c cd " + abbyFilePath + " & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
            //startInfo.Arguments = @"/c cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
            //startInfo.Arguments = @"/k cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe";
            //startInfo.Arguments = @"cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Abby_images\[Rescaled]Image_1786x227.png /lang Mixed /out C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Text_OCR\Text_OCR.txt /quit";
            process.StartInfo = startInfo;
            process.Start();
            //process.WaitForExit();

            //даем 30 сек, если ошибка, то перезапускаем
            System.Threading.Thread.Sleep(20000);

            foreach (var proc in System.Diagnostics.Process.GetProcessesByName("FineReader")) {
                proc.Close();
            }

            /*//если нет выходоного файла, то запускаем повторно
            if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                process.Close();
                return isSertificate(sourceImage);
            }*/

            process.Close();

            string text = String.Join(" ", File.ReadAllLines(outTextOCRFolder + "Text_OCR.txt", Encoding.UTF8));

            foreach (KeyValuePair<string, string> pair in words) {
                //тут наоборот значение на ключ, т.к. нужно англ. на рус.
                text = text.Replace(pair.Value, pair.Key);
            }

            //удаляем папки после использования
            if (System.IO.Directory.Exists(outImageFolder)) {
                System.IO.Directory.Delete(outImageFolder, true);
            }

            if (System.IO.Directory.Exists(outTextOCRFolder)) {
                System.IO.Directory.Delete(outTextOCRFolder, true);
            }

            int countMatches = regexPrilozenieRus.Count;//если одну или две маски найдет, то значит - приложение

            for (int i = 0; i < regexPrilozenieRus.Count; i++) {
                MatchCollection matchesPrilozenieRus = regexPrilozenieRus[i].Matches(text);

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

        public bool isSertificateViaRescalingOld(Image sourceImage, float stepScale, int amountOfSteps) {
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

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            float scaleValue = 1.0f;//исходный масштаб

            List<string> isSertificateList = new List<string>();

            float timerModifier = 0;//увеличивает таймер, по мере получения ошибки выполнения fineReader'а

            for (int k = 0; k <= amountOfSteps; k++) {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                string currentPath = System.IO.Directory.GetCurrentDirectory();

                //string outImageFolder = currentPath + "\\[Temp]Abby_images\\";
                string outImageFolder = @"C:\[Temp]Abby_images\";

                if (System.IO.Directory.Exists(outImageFolder)) {
                    System.IO.Directory.Delete(outImageFolder, true);
                }

                System.IO.Directory.CreateDirectory(outImageFolder);

                //даем права на запись в папку
                File.SetAttributes(outImageFolder, FileAttributes.Normal);

                //string outImagePath = currentPath + "\\[Temp]Abby_images\\[Rescaled]Image_" + newWidth + "x" + newHeight + ".png";
                string outImagePath = outImageFolder + "[IsSertificate][Rescaled]Image.png";

                sourceImage.Save(outImagePath);

                //string outTextOCRFolder = currentPath + "\\[Temp]Text_OCR\\";
                string outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

                if (System.IO.Directory.Exists(outTextOCRFolder)) {
                    System.IO.Directory.Delete(outTextOCRFolder, true);
                }

                System.IO.Directory.CreateDirectory(outTextOCRFolder);

                //даем права на запись в папку
                File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = @"/c cd " + abbyFilePath + " & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
                //startInfo.Arguments = @"/c cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRFolder + "Text_OCR.txt /quit";
                //startInfo.Arguments = @"/k cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe";
                //startInfo.Arguments = @"cd C:\Program Files (x86)\ABBYY FineReader 12\ & FineCmd.exe C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Abby_images\[Rescaled]Image_1786x227.png /lang Mixed /out C:\Users\Alexey\documents\visual studio 2013\Projects\Tesseract_OCR\Tesseract_OCR\bin\Debug\[Temp]Text_OCR\Text_OCR.txt /quit";
                process.StartInfo = startInfo;
                process.Start();
                //process.WaitForExit();
                //process.WaitForExit(30000);
                process.WaitForExit(30000 + Convert.ToInt32(5000 * timerModifier));


                System.Diagnostics.Process[] procListFineReader = System.Diagnostics.Process.GetProcessesByName("FineReader");
                System.Diagnostics.Process[] procListFineExec = System.Diagnostics.Process.GetProcessesByName("FineExec");
                System.Diagnostics.Process[] procListFineCmd = System.Diagnostics.Process.GetProcessesByName("FineCmd");

                foreach (var proc in procListFineReader) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                foreach (var proc in procListFineExec) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                foreach (var proc in procListFineCmd) {
                    if (proc.HasExited == false) {
                        proc.Kill();
                    }
                }

                //process.WaitForExit(15000 + Convert.ToInt32(2000 * timerModifier));

                //даем 8 сек, если ошибка, то перезапускаем
                /*System.Threading.Thread.Sleep(7000 + Convert.ToInt32(1000 * timerModifier));

                System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcessesByName("FineReader");

                process.Close();

                if(processList.Length>0){
                    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("FineReader")) {
                        proc.Close();
                        proc.CloseMainWindow();                        
                    }

                    if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                        k -= 1;
                        timerModifier += 1.0f;

                        continue;
                    }
                }*/

                /*using (var timer = new System.Threading.Timer(delegate { process.Close(); }, null, 8000, Timeout.Infinite)) {
                    / * //крутимся, пока не истечет таймер
                    do{
                    }while(true);* /
                }*/

                /*//даем 10 сек, если ошибка, то перезапускаем
                System.Threading.Thread.Sleep(8000 + Convert.ToInt32(1000 * timerModifier));

                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("FineReader")) {
                    proc.Close();
                }*/

                /*//если нет выходоного файла, то запускаем повторно
                if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                    process.Close();

                    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("FineReader")) {
                        proc.CloseMainWindow();
                    }

                    k -= 1;
                    timerModifier += 1.0f;                    

                    //удаляем папки после использования
                    if (System.IO.Directory.Exists(outImageFolder)) {
                        System.IO.Directory.Delete(outImageFolder, true);
                    }

                    if (System.IO.Directory.Exists(outTextOCRFolder)) {
                        System.IO.Directory.Delete(outTextOCRFolder, true);
                    }

                    continue;
                }*/

                //обнуляем модификатор таймера
                //timerModifier = 0;

                //process.Close();

                /*//если нет выходоного файла, то запускаем повторно
                if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                    process.Close();
                    return isSertificate(sourceImage);
                }*/

                if ((File.Exists(outTextOCRFolder + "Text_OCR.txt")) == false) {
                    k -= 1;
                    timerModifier += 1.0f;

                    continue;
                }

                string text = String.Join(" ", File.ReadAllLines(outTextOCRFolder + "Text_OCR.txt", Encoding.UTF8));

                foreach (KeyValuePair<string, string> pair in words) {
                    //тут наоборот значение на ключ, т.к. нужно англ. на рус.
                    text = text.Replace(pair.Value, pair.Key);
                }

                //удаляем папки после использования
                if (System.IO.Directory.Exists(outImageFolder)) {
                    System.IO.Directory.Delete(outImageFolder, true);
                }

                if (System.IO.Directory.Exists(outTextOCRFolder)) {
                    System.IO.Directory.Delete(outTextOCRFolder, true);
                }

                int countMatches = regexPrilozenieRus.Count;//если одну или две маски найдет, то значит - приложение

                for (int i = 0; i < regexPrilozenieRus.Count; i++) {
                    MatchCollection matchesPrilozenieRus = regexPrilozenieRus[i].Matches(text);

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
                    isSertificateList.Add("false");
                    //return false;
                }

                isSertificateList.Add("true");
                //return true;
            }

            if (highestMatch(isSertificateList) == "true") {
                return true;
            } else {
                return false;
            }
        }

        public bool isSertificateViaRescaling(Image sourceImage, float stepScale, int amountOfSteps) {
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

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            float scaleValue = 1.0f;//исходный масштаб

            List<string> isSertificateList = new List<string>();

            List<TABBYProcess> abbyProcessList = new List<TABBYProcess>();

            for (int k = 0; k <= amountOfSteps; k++) {
                
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                abbyProcessList.Add(new TABBYProcess(rescaledPage,Task.CurrentId.Value+k));
            }

            bool isAllProcDone = false;
            float timerModifier = 0;

            do {
                //запускаем процессы распознавания
                for (int i = 0; i < abbyProcessList.Count; i++) {
                    if (abbyProcessList[i].isDone == false) {
                        abbyProcessList[i].Start();
                    }
                }

                //засыпаем на 30 сек.
                System.Threading.Thread.Sleep(amountOfSteps* timeABBYOCR + Convert.ToInt32(5000 * timerModifier));

                /*//удаляем следы FineReader'а
                //закомментил, т.к. тут иногда будет стопориться, поэтому освобождаю в конце
                destroyFRProcess();*/

                //проверяем состояния процессов
                int countDone = 0;

                for (int i = 0; i < abbyProcessList.Count; i++) {
                    abbyProcessList[i].Refresh();

                    if (abbyProcessList[i].isDone == true) {
                        countDone++;
                    }
                }

                if (countDone == abbyProcessList.Count) {
                    isAllProcDone = true;
                } else {
                    timerModifier += 1.0f;
                }
            } while (isAllProcDone == false);

            for (int i = 0; i < abbyProcessList.Count; i++) {
                string text = String.Join(" ", File.ReadAllLines(abbyProcessList[i].outTextOCRPath, Encoding.UTF8));

                foreach (KeyValuePair<string, string> pair in words) {
                    //тут наоборот значение на ключ, т.к. нужно англ. на рус.
                    text = text.Replace(pair.Value, pair.Key);
                }

                int countMatches = regexPrilozenieRus.Count;//если одну или две маски найдет, то значит - приложение

                for (int j = 0; j < regexPrilozenieRus.Count; j++) {
                    MatchCollection matchesPrilozenieRus = regexPrilozenieRus[j].Matches(text);

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
                    isSertificateList.Add("false");
                    //return false;
                }

                isSertificateList.Add("true");
                //return true;

                abbyProcessList[i].Destroy();
            }

            //удаляем следы FineReader'а
            destroyFRProcess();

            //как только все таски отработали, удаляем временные папки
            /*Task.WaitAll();

            if (System.IO.Directory.Exists(abbyProcessList[0].outImageFolder)) {
                System.IO.Directory.Delete(abbyProcessList[0].outImageFolder, true);
            }

            if (System.IO.Directory.Exists(abbyProcessList[0].outTextOCRFolder)) {
                System.IO.Directory.Delete(abbyProcessList[0].outTextOCRFolder, true);
            }*/              

            if (highestMatch(isSertificateList) == "true") {
                return true;
            } else {
                return false;
            }
        }

        private void initDictionaryOld(ref Dictionary<string, string> dictionaryRusEng) {
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
            dictionaryRusEng.Add("и", "i");
            dictionaryRusEng.Add("й", "j");
            dictionaryRusEng.Add("к", "k");
            dictionaryRusEng.Add("л", "l");
            dictionaryRusEng.Add("м", "m");
            dictionaryRusEng.Add("н", "n");
            dictionaryRusEng.Add("о", "o");
            dictionaryRusEng.Add("п", "p");
            dictionaryRusEng.Add("р", "r");
            dictionaryRusEng.Add("с", "s");
            dictionaryRusEng.Add("т", "t");
            dictionaryRusEng.Add("у", "u");
            dictionaryRusEng.Add("ф", "f");
            dictionaryRusEng.Add("х", "h");
            dictionaryRusEng.Add("ц", "c");
            dictionaryRusEng.Add("ч", "ch");
            dictionaryRusEng.Add("ш", "sh");
            dictionaryRusEng.Add("щ", "sch");
            dictionaryRusEng.Add("ъ", "j");
            dictionaryRusEng.Add("ы", "i");
            dictionaryRusEng.Add("ь", "j");
            dictionaryRusEng.Add("э", "e");
            dictionaryRusEng.Add("ю", "yu");
            dictionaryRusEng.Add("я", "ya");
            dictionaryRusEng.Add("А", "A");
            dictionaryRusEng.Add("Б", "B");
            dictionaryRusEng.Add("В", "V");
            dictionaryRusEng.Add("Г", "G");
            dictionaryRusEng.Add("Д", "D");
            dictionaryRusEng.Add("Е", "E");
            dictionaryRusEng.Add("Ё", "Yo");
            dictionaryRusEng.Add("Ж", "Zh");
            dictionaryRusEng.Add("З", "Z");
            dictionaryRusEng.Add("И", "I");
            dictionaryRusEng.Add("Й", "J");
            dictionaryRusEng.Add("К", "K");
            dictionaryRusEng.Add("Л", "L");
            dictionaryRusEng.Add("М", "M");
            dictionaryRusEng.Add("Н", "N");
            dictionaryRusEng.Add("О", "O");
            dictionaryRusEng.Add("П", "P");
            dictionaryRusEng.Add("Р", "R");
            dictionaryRusEng.Add("С", "S");
            dictionaryRusEng.Add("Т", "T");
            dictionaryRusEng.Add("У", "U");
            dictionaryRusEng.Add("Ф", "F");
            dictionaryRusEng.Add("Х", "H");
            dictionaryRusEng.Add("Ц", "C");
            dictionaryRusEng.Add("Ч", "Ch");
            dictionaryRusEng.Add("Ш", "Sh");
            dictionaryRusEng.Add("Щ", "Sch");
            dictionaryRusEng.Add("Ъ", "J");
            dictionaryRusEng.Add("Ы", "I");
            dictionaryRusEng.Add("Ь", "J");
            dictionaryRusEng.Add("Э", "E");
            dictionaryRusEng.Add("Ю", "Yu");
            dictionaryRusEng.Add("Я", "Ya");
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
    }
}
