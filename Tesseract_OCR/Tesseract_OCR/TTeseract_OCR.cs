using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;

namespace Tesseract_OCR
{
    class TTeseract_OCR
    {
        public string getSeriaNumber(string inputRusString,string inputEngString)
        {
            Regex regexSeriaNumber = new Regex(@"\W[0-9]{7}\W", RegexOptions.IgnoreCase);

            MatchCollection matchesSeriaNumberEng = regexSeriaNumber.Matches(inputEngString);
            MatchCollection matchesSeriaNumberRus = regexSeriaNumber.Matches(inputRusString);
            
            //[ENG]проверяем серию
            if (matchesSeriaNumberEng.Count > 0)
            {
                int countNull = 0;

                for (int k = 0; k < matchesSeriaNumberEng.Count; k++)
                {
                    for (int b = 0; b < matchesSeriaNumberEng[k].Value.Length; b++)
                    {
                        if (matchesSeriaNumberEng[k].Value[b] == '0')
                        {
                            countNull++;
                        }
                    }

                }

                //если >= 1, то запоминаем
                if (countNull >= 1)
                {
                    //чистим на всякий случай
                    string pattern = @"[\W| ]*";
                    string replacement = "";

                    string seriaNumberEng = matchesSeriaNumberEng[0].Value;

                    seriaNumberEng = Regex.Replace(seriaNumberEng, pattern, replacement);

                    return seriaNumberEng;
                }
            }

            //[RUS]проверяем серию
            if (matchesSeriaNumberRus.Count > 0)
            {
                int countNull = 0;

                for (int k = 0; k < matchesSeriaNumberRus.Count; k++)
                {
                    for (int b = 0; b < matchesSeriaNumberRus[k].Value.Length; b++)
                    {
                        if (matchesSeriaNumberRus[k].Value[b] == '0')
                        {
                            countNull++;
                        }
                    }

                }

                //если >= 1, то запоминаем
                if (countNull >= 1)
                {
                    //чистим на всякий случай
                    string pattern = @"[\W| ]*";
                    string replacement = "";

                    string seriaNumberRus = matchesSeriaNumberRus[0].Value;

                    seriaNumberRus = Regex.Replace(seriaNumberRus, pattern, replacement);

                    return seriaNumberRus;
                }
            }

            return "";
        }

        //получение номера серии через с помощью масштабирования страницы с разным шагом(исп-ть, если не находит на исходном масштабе)
        //sourceImage = входное изображение, stepScale = шаг масштабирования, amountOfSteps = кол-во проходов рескейлинга
        public string getSeriaNumberViaRescaling(Image sourceImage, float stepScale, int amountOfSteps)
        {
            //подключаем распознаватель
            TesseractEngine ocrRus = new TesseractEngine("./tessdata", "rus", EngineMode.Default);
            TesseractEngine ocrEng = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            //ставим методику распознавания страницы
            PageSegMode pSegMode = PageSegMode.SparseText;

            string seriaNumber = "";

            float scaleValue = 1.0f;//исходный масштаб

            List<string> seriaList = new List<string>();

            for (int k = 1; k <= amountOfSteps; k++)
            {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                string rescaledPageTextRus = "";
                string rescaledPageTextEng = "";

                //удаляем процесс, чтобы не было ошибки
                using (var pageRus = ocrRus.Process(imgBitmap, pSegMode))
                {
                    rescaledPageTextRus = pageRus.GetText();
                }

                //удаляем процесс, чтобы не было ошибки
                using (var pageEng = ocrEng.Process(imgBitmap, pSegMode))
                {
                    rescaledPageTextEng = pageEng.GetText();
                }

                seriaList.Add(getSeriaNumber(rescaledPageTextRus, rescaledPageTextEng));
            }

            seriaNumber = highestMatch(seriaList);

            ocrRus.Dispose();
            ocrEng.Dispose();           

            return seriaNumber;
        }

        private string highestMatch(List<string> dataList)
        {
            //если пустые данные, то выходим
            if(dataList.Count==0){
                return "";
            }

            List<KeyValuePair<string, int>> valueChecksList = new List<KeyValuePair<string, int>>();

            //проходим по списку серий и ищем правильную
            for (int i = 0; i < dataList.Count; i++)
            {
                //проверяем серию
                if (valueChecksList.Count == 0)
                {
                    valueChecksList.Add(new KeyValuePair<string, int>(dataList[i], 1));
                    continue;
                }

                bool isFoundValue = false;

                for (int j = 0; j < valueChecksList.Count; j++)
                {
                    if (dataList[i] == valueChecksList[j].Key)
                    {
                        valueChecksList[j] = new KeyValuePair<string, int>(valueChecksList[j].Key, valueChecksList[j].Value + 1);

                        isFoundValue = true;
                    }
                }

                if (isFoundValue == false)
                {
                    valueChecksList.Add(new KeyValuePair<string, int>(dataList[i], 1));
                }
            }

            //проходим и выбираем точную серию
            int rightSeriaValue = 0;
            int rightIndSeria = 0;

            for (int i = 0; i < valueChecksList.Count; i++)
            {
                if (rightIndSeria == 0)
                {
                    rightSeriaValue = valueChecksList[i].Value;
                    rightIndSeria = i;
                }
                else
                {
                    if (rightSeriaValue < valueChecksList[i].Value)
                    {
                        rightSeriaValue = valueChecksList[i].Value;
                        rightIndSeria = i;
                    }
                }
            }

            string highestMatchValue = valueChecksList[rightIndSeria].Key;

            return highestMatchValue;
        }

        //получение всех данные по странице с помощью масштабирования страницы с разным шагом(исп-ть, если не находит на исходном масштабе)
        //sourceImage = входное изображение, stepScale = шаг масштабирования, amountOfSteps = кол-во проходов рескейлинга,
        //infoPage = ссылка на структуру для заполнения
        public void getData(Image sourceImage, ref Tesseract_OCR_Window.pdfPageInfo infoPage, ref Dictionary<string, string> replaceWords, float stepScale,int amountOfSteps)
        {
            //подключаем распознаватель
            TesseractEngine ocrRus = new TesseractEngine("./tessdata", "rus", EngineMode.Default);
            TesseractEngine ocrEng = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            //ставим методику распознавания страницы
            PageSegMode pSegMode = PageSegMode.SparseText;

            string seriaNumber = "";//серия
            string country = "";//страна
            string orgSert = "";//организация
            string shortNumber = "";//short number
            string typeSeria = "";//тип серии

            float scaleValue = 1.0f;//исходный масштаб

            for (int k = 0; k <= amountOfSteps; k++)
            {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                string rescaledPageTextRus = "";
                string rescaledPageTextEng = "";

                //удаляем процесс, чтобы не было ошибки
                using (var pageRus = ocrRus.Process(imgBitmap, pSegMode))
                {
                    rescaledPageTextRus = pageRus.GetText();
                }

                //удаляем процесс, чтобы не было ошибки
                using (var pageEng = ocrEng.Process(imgBitmap, pSegMode))
                {
                    rescaledPageTextEng = pageEng.GetText();
                }

                //собираем данные для структуры infoPage
                if(infoPage.seriaNumber_ == "")
                {
                    seriaNumber = getSeriaNumber(rescaledPageTextRus, rescaledPageTextEng);//серия
                    
                    if (seriaNumber != "")
                    {
                        infoPage.seriaNumber_ = seriaNumber;
                    }
                }

                if (infoPage.country_ == "")
                {
                    country = getCountry(rescaledPageTextEng);
                    
                    if (country != "")
                    {
                        infoPage.country_ = country;
                    }
                }

                if (infoPage.orgSert_ == "")
                {
                    orgSert = getOrgSert(rescaledPageTextRus);
                                        
                    if (orgSert != "")
                    {
                        infoPage.orgSert_ = orgSert;
                    }
                }

                if (infoPage.shortNumber_ == "")
                {
                    shortNumber = getShortNumber(rescaledPageTextRus);
                                        
                    if (shortNumber != "")
                    {
                        infoPage.shortNumber_ = shortNumber;
                    }
                }

                if (infoPage.typeSeria_ == "")
                {
                    typeSeria = getTypeSeria(rescaledPageTextRus, rescaledPageTextEng, ref replaceWords);
                                       
                    if (typeSeria != "")
                    {
                        infoPage.typeSeria_ = typeSeria;
                    }
                }

            }

            ocrRus.Dispose();
            ocrEng.Dispose();
        }

        public void getDataViaRescaling(Image sourceImage, ref Tesseract_OCR_Window.pdfPageInfo infoPage, ref Dictionary<string, string> replaceWords, float stepScale, int amountOfSteps)
        {
            //подключаем распознаватель
            TesseractEngine ocrRus = new TesseractEngine("./tessdata", "rus", EngineMode.Default);
            TesseractEngine ocrEng = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

            //создаем графический фильтр
            TResizeTool resizeTool = new TResizeTool();

            //ставим методику распознавания страницы
            PageSegMode pSegMode = PageSegMode.SparseText;

            float scaleValue = 1.0f;//исходный масштаб

            List<string> seriaNumberList = new List<string>();
            List<string> countryList = new List<string>();
            List<string> orgSertList = new List<string>();
            List<string> shortNumberList = new List<string>();
            List<string> typeSeriaList = new List<string>();

            for (int k = 0; k <= amountOfSteps; k++)
            {
                //по индексу отмасш-й страницы берем исходную(без масштаба)
                Image img = sourceImage;

                int newWidth = Convert.ToInt32((img.Width) * ((k * stepScale) + scaleValue));
                int newHeight = Convert.ToInt32((img.Height) * ((k * stepScale) + scaleValue));

                Bitmap imgBitmap = resizeTool.resizeImage(img, newWidth, newHeight);

                Image rescaledPage = ((Image)imgBitmap);

                string rescaledPageTextRus = "";
                string rescaledPageTextEng = "";

                //удаляем процесс, чтобы не было ошибки
                using (var pageRus = ocrRus.Process(imgBitmap, pSegMode))
                {
                    rescaledPageTextRus = pageRus.GetText();
                }

                //удаляем процесс, чтобы не было ошибки
                using (var pageEng = ocrEng.Process(imgBitmap, pSegMode))
                {
                    rescaledPageTextEng = pageEng.GetText();
                }

                //набираем статистику для последующего поиска наибольшего соответствия
                string seriaNumber = getSeriaNumber(rescaledPageTextRus, rescaledPageTextEng);
                string country = getCountry(rescaledPageTextEng);
                string orgSert = getOrgSert(rescaledPageTextRus);
                string shortNumber = getShortNumber(rescaledPageTextRus);
                string typeSeria = getTypeSeria(rescaledPageTextRus, rescaledPageTextEng, ref replaceWords);

                if (seriaNumber!=""){
                    seriaNumberList.Add(seriaNumber);//серия
                }

                if (country != "")
                {
                    countryList.Add(country);//страна
                }

                if (orgSert != "")
                {
                    orgSertList.Add(orgSert);//орган по сертификации
                }

                if (shortNumber != "")
                {
                    shortNumberList.Add(shortNumber);//shortNumber
                }

                if (typeSeria != "")
                {
                    typeSeriaList.Add(typeSeria);//тип серии
                }                
            }
            
            ocrRus.Dispose();
            ocrEng.Dispose();

            //собираем данные для структуры infoPage
            infoPage.seriaNumber_ = highestMatch(seriaNumberList);
            infoPage.country_ = highestMatch(countryList);
            infoPage.orgSert_ = highestMatch(orgSertList);
            infoPage.shortNumber_ = highestMatch(shortNumberList);
            infoPage.typeSeria_ = highestMatch(typeSeriaList);
        }

        public string getShortNumber(string inputRusString)//берет русскую строку
        {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputRusString);

            Regex regexShortNumber = new Regex(@"\W[0-9]{5}\W", RegexOptions.IgnoreCase);
            MatchCollection matchesShortNumber = regexShortNumber.Matches(inputRusString);

            //[RUS]если одно соответствие, то берем
            /*if (matchesFullNumberRus.Count > 0)
            {
                if (matchesShortNumber.Count == 1)
                {
                    //чистим
                    string pattern = @"[\W| ]*";
                    string replacement = "";

                    string shortNumber = matchesShortNumber[0].Value;

                    shortNumber = Regex.Replace(shortNumber, pattern, replacement);
                                        
                    return shortNumber;
                }
            }*/

            if (matchesShortNumber.Count == 1)
            {
                //чистим
                string pattern = @"[\W| ]*";
                string replacement = "";

                string shortNumber = matchesShortNumber[0].Value;

                shortNumber = Regex.Replace(shortNumber, pattern, replacement);

                return shortNumber;
            }

            return "";    
        }
        
        public string getTypeSeria(string inputRusString,string inputEngString, ref Dictionary<string, string> replaceWords)
        {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputRusString);
            MatchCollection matchesFullNumberEng = regexFullNumberEng.Matches(inputEngString);

            //берем typeSeria
            Regex regexTypeSeria = new Regex(@"[А-Я]{2}[0-9]{2}\W*[А-Я]{1}", RegexOptions.IgnoreCase);
            //Regex regexTypeSeria = new Regex(@"[ |.|]{1}[А-Я]{1}[ |.]{1}", RegexOptions.IgnoreCase);
            
            MatchCollection matchesTypeSeria = regexTypeSeria.Matches(inputRusString);

            string typeSeria = "";

            //[RUS]если одно соответствие, то берем
            /*if (matchesFullNumberRus.Count > 0)
            {
                if (matchesTypeSeria.Count > 0)
                {
                    //чистим
                    typeSeria = matchesTypeSeria[0].Value;

                    string patternTypeSeria = @"[А-Я]{2}[0-9]{2}\W*";
                    //string patternTypeSeria = @"\W*";
                    string replacementTypeSeria = "";

                    typeSeria = Regex.Replace(typeSeria, patternTypeSeria, replacementTypeSeria);
                }
            }*/

            if (matchesTypeSeria.Count > 0)
            {
                //чистим
                typeSeria = matchesTypeSeria[0].Value;

                string patternTypeSeria = @"[А-Я]{2}[0-9]{2}\W*";
                //string patternTypeSeria = @"\W*";
                string replacementTypeSeria = "";

                typeSeria = Regex.Replace(typeSeria, patternTypeSeria, replacementTypeSeria);
            }

            //перепроверяем typeSeria, если нет в рус, то ищем в англ с последующим конвертом
            if (typeSeria == "")
            {
                string pageTextEng = inputEngString;

                pageTextEng = pageTextEng.ToUpper();//ставим верхний регистр

                //берем typeSeria
                Regex regexTypeSeriaEng = new Regex(@"[0-9]{2}\W*[A-Z]{1}\W*[0-9]", RegexOptions.IgnoreCase);
                MatchCollection matchesTypeSeriaEng = regexTypeSeriaEng.Matches(pageTextEng);

                if (matchesTypeSeriaEng.Count > 0)
                {
                    //чистим
                    typeSeria = matchesTypeSeriaEng[0].Value;

                    string patternTypeSeria = @"[\d|\W]*";
                    string replacementTypeSeria = "";

                    typeSeria = Regex.Replace(typeSeria, patternTypeSeria, replacementTypeSeria);

                    foreach (KeyValuePair<string, string> pair in replaceWords)
                    {
                        typeSeria = typeSeria.Replace(pair.Key, pair.Value);
                    }

                    return typeSeria;

                }
            }
            else
            {
                return typeSeria;
            }

            return "";
        }

        public string getCountry(string inputEngString)
        {
            Regex regexFullNumberEng = new Regex(@"TC[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            MatchCollection matchesFullNumberEng = regexFullNumberEng.Matches(inputEngString);

            //Regex regexCountry = new Regex(@"[-|\w][A-Z]{2}[ |.]*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexCountry = new Regex(@"[-][A-Z]{2}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            //[ENG]если одно соответствие, то берем
            /*if (matchesFullNumberEng.Count > 0)
            {
                string pageTextEng = matchesFullNumberEng[0].Value;

                pageTextEng = pageTextEng.ToUpper();//ставим верхний регистр

                MatchCollection matchesCountryEng = regexCountry.Matches(pageTextEng);

                if (matchesCountryEng.Count > 0)
                {
                    string countryTextEng = "";

                    if (matchesCountryEng.Count == 1)
                    {
                        countryTextEng = matchesCountryEng[0].Value;
                    }
                    else
                    {
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

                    if ((matchesCheckStringRusAndEng.Count>0) || (matchesCheckStringEngAndRus.Count > 0))
                    {
                        countryTextEng = "";
                    }

                    return countryTextEng;
                }
            }*/

            string pageTextEng = inputEngString;

            pageTextEng = pageTextEng.ToUpper();//ставим верхний регистр

            MatchCollection matchesCountryEng = regexCountry.Matches(pageTextEng);

            if (matchesCountryEng.Count > 0)
            {
                string countryTextEng = "";

                if (matchesCountryEng.Count == 1)
                {
                    countryTextEng = matchesCountryEng[0].Value;
                }
                else
                {
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

                if ((matchesCheckStringRusAndEng.Count > 0) || (matchesCheckStringEngAndRus.Count > 0))
                {
                    countryTextEng = "";
                }

                return countryTextEng;
            }

            return "";
        }

        public string getOrgSert(string inputRusString)
        {
            Regex regexFullNumberRus = new Regex(@"ТС[^\\n]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            MatchCollection matchesFullNumberRus = regexFullNumberRus.Matches(inputRusString);

/*
            if (matchesFullNumberRus.Count > 0)
            {
                string pageTextRus = matchesFullNumberRus[0].Value;

                string pattern = @"[\W| ]*";
                string replacement = "";

                pageTextRus = pageTextRus.ToUpper();//ставим верхний регистр

                //берем orgSert
                Regex regexOrgSert = new Regex(@"\W[А-Я]{2}[0-9]{2}\W", RegexOptions.IgnoreCase);
                MatchCollection matchesOrgSert = regexOrgSert.Matches(pageTextRus);

                if (matchesOrgSert.Count == 1)
                {
                    //чистим
                    string orgSert = matchesOrgSert[0].Value;

                    orgSert = Regex.Replace(orgSert, pattern, replacement);

                    //infoPage.orgSert_ = temp.Substring(temp.Length - (shortNumberLength + typeOfSeriaLength + orgSertLength), orgSertLength);
                    return orgSert;
                }
            }*/

            string pageTextRus = inputRusString;

            string pattern = @"[\W| ]*";
            string replacement = "";

            pageTextRus = pageTextRus.ToUpper();//ставим верхний регистр

            //берем orgSert
            Regex regexOrgSert = new Regex(@"\W[А-Я]{2}[0-9]{2}\W", RegexOptions.IgnoreCase);
            MatchCollection matchesOrgSert = regexOrgSert.Matches(pageTextRus);

            if (matchesOrgSert.Count == 1)
            {
                //чистим
                string orgSert = matchesOrgSert[0].Value;

                orgSert = Regex.Replace(orgSert, pattern, replacement);

                //infoPage.orgSert_ = temp.Substring(temp.Length - (shortNumberLength + typeOfSeriaLength + orgSertLength), orgSertLength);
                return orgSert;
            }

            return "";
        }
    }
}
