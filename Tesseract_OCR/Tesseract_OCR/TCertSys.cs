using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract_OCR;

namespace Tesseract_OCR
{
    public class TCertSys
    {
        private TTeseract_OCR tesOCR=null;
        public Dictionary<string, string> words = null;

        public TCertSys()
        {
            tesOCR = new TTeseract_OCR();//создаем поисковик

            initDictionary(ref words);//инициализируем словарь транслитеризации

        }

        public bool isDocExist(string seriaNumber)
        {
            //ищем сертификат по серии в certsys

            string sertificateCertSys = "";

            if (seriaNumber != "")
            {
                sertificateCertSys = ConsoleApplication10.Program.LoadCertNumber(seriaNumber);

                if (sertificateCertSys!="")
                {
                    return true;
                }else{
                    return false;
                }
            }
            else
            {
                return false;
            }            
        }

        public Tesseract_OCR_Window.pdfPageInfo getSertificate(string seriaNumber)
        {
            //структура страницы pdf
            Tesseract_OCR_Window.pdfPageInfo infoPage;

            infoPage.fullNumber_ = "";
            infoPage.techReglament_ = "ТС ";
            infoPage.shortNumber_ = "";
            infoPage.typeSeria_ = "";
            infoPage.seriaNumber_ = "";
            infoPage.typeOfSert_ = "С";
            infoPage.orgSert_ = "";
            infoPage.country_ = "";
            infoPage.typeOfPage_ = Tesseract_OCR_Window.typeOfPage.SERTIFICATE;
            infoPage.isTesseracted_ = false;
            infoPage.numberOfPDFPage_ = -1;
            infoPage.ocrMethod_ = Tesseract_OCR_Window.ocrMethod.NONE;
            infoPage.textRus_ = "";
            infoPage.textEng_ = "";

            if (isDocExist(seriaNumber)==false)
            {
                return infoPage;
            }
            
            /////////////////////////////////////////////////////
            //ищем сертификат по серии в certsys

            string sertificateCertSys = ConsoleApplication10.Program.LoadCertNumber(infoPage.seriaNumber_);

            infoPage.fullNumber_ = sertificateCertSys + "." + infoPage.seriaNumber_;

            //полный номер для 2х языков
            string fullNumberRUS = "ТС " + " " + infoPage.fullNumber_;
            string fullNumberENG = "TC " + " " + infoPage.fullNumber_;

            //разрезаем полный номер
            infoPage.shortNumber_ = tesOCR.getShortNumber(fullNumberRUS);
            infoPage.typeSeria_ = tesOCR.getTypeSeria(fullNumberRUS, fullNumberENG, ref words);
            infoPage.country_ = tesOCR.getCountry(fullNumberENG);
            infoPage.orgSert_ = tesOCR.getOrgSert(fullNumberRUS);

            //добавляем "ТС " перед полным номером
            infoPage.fullNumber_ = infoPage.techReglament_ + infoPage.fullNumber_;

            infoPage.ocrMethod_ = Tesseract_OCR_Window.ocrMethod.CERTSYS;

            infoPage.isTesseracted_ = true;

            infoPage.fullNumber_ = infoPage.fullNumber_.ToUpper();//ставим верхний регистр

            return infoPage;
        }

        public List<Tesseract_OCR_Window.pdfPageInfo> getPrilozenia(Tesseract_OCR_Window.pdfPageInfo sertificate)
        {
            List<Tesseract_OCR_Window.pdfPageInfo> prilozeniaList = new List<Tesseract_OCR_Window.pdfPageInfo>();

            //тащим приложения
            var sertAndPril = ConsoleApplication10.Program.LoadCert(sertificate.orgSert_, sertificate.shortNumber_);

            string prilozenia = sertAndPril.Info.prilblanks;

            string[] separPrilozenia = prilozenia.Split(',');

            List<string> separPrilozeniaList = new List<string>();

            //проверка на наличие приложения
            if (separPrilozenia.Length > 0)
            {
                if (separPrilozenia[0] == "n")
                {
                    return prilozeniaList;
                }
            }

            //сохраняем приложения
            for (int k = 0; k < separPrilozenia.Length; k++)
            {
                //чистим приложения
                string pattern = @"[\W| ]*";
                string replacement = "";

                separPrilozenia[k] = Regex.Replace(separPrilozenia[k], pattern, replacement);

                int sertSeriaNumber = Convert.ToInt32(separPrilozenia[k]);
                string clearSeriaNumber = "0000000";
                string prilSeriaNumber = Convert.ToString(sertSeriaNumber);

                int amountOfSymbolsSSN = prilSeriaNumber.Length;
                int amountClearSN = clearSeriaNumber.Length;

                //clearSeriaNumber=clearSeriaNumber.Remove(2, 5).Insert(2, prilSeriaNumber);
                clearSeriaNumber = clearSeriaNumber.Remove(amountClearSN - amountOfSymbolsSSN, amountOfSymbolsSSN).Insert(amountClearSN - amountOfSymbolsSSN, prilSeriaNumber);

                separPrilozeniaList.Add(clearSeriaNumber);
            }

            //сортируем
            separPrilozeniaList.Sort();

            for (int k = 0; k < separPrilozeniaList.Count; k++)
            {
                //структура страницы pdf
                Tesseract_OCR.Tesseract_OCR_Window.pdfPageInfo infoPrilPage;

                infoPrilPage = sertificate;

                infoPrilPage.numberOfPDFPage_ += k;
                infoPrilPage.isTesseracted_ = false;
                infoPrilPage.typeOfPage_ = Tesseract_OCR.Tesseract_OCR_Window.typeOfPage.PRILOZENIE;
                infoPrilPage.textRus_ = "";
                infoPrilPage.textEng_ = "";

                infoPrilPage.seriaNumber_ = separPrilozeniaList[k];
                infoPrilPage.fullNumber_ = infoPrilPage.techReglament_ + "RU " + infoPrilPage.typeOfSert_ + "-" + infoPrilPage.country_ + "." + infoPrilPage.orgSert_ + "." + infoPrilPage.typeSeria_ + "." + infoPrilPage.shortNumber_ + "." + infoPrilPage.seriaNumber_;
                
                if (infoPrilPage.seriaNumber_ == "")
                {
                    infoPrilPage.isTesseracted_ = false;
                }
                else
                {
                    infoPrilPage.isTesseracted_ = true;
                }

                infoPrilPage.fullNumber_ = infoPrilPage.fullNumber_.ToUpper();//ставим верхний регистр

                prilozeniaList.Add(infoPrilPage);
            }

            return prilozeniaList;
        }

        public void initDictionary(ref Dictionary<string, string> dictionaryRusEng)
        {
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
    }
}
