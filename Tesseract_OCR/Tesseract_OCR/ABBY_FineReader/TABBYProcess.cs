using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tesseract_OCR.ABBY_FineReader {
    public class TABBYProcess {
        string abbyFilePath = @"C:\Program Files (x86)\ABBYY FineReader 12\";
        public Image srcImage;
        public string outImagePath;
        public string outImageFolder;
        public string outTextOCRPath;
        public string outTextOCRFolder;
        public bool isDone;
        public int procID;
        public float timerModifier;

        public TABBYProcess(Image img,int id=0) {
            srcImage = img;
            procID = id;

            timerModifier = 0;

            isDone=false;

            ////////////////////////////////////////////////////////////////////

            string currentPath = System.IO.Directory.GetCurrentDirectory();

            outImageFolder = @"C:\[Temp]Abby_images\";

            if ((System.IO.Directory.Exists(outImageFolder)) == false) {
                System.IO.Directory.CreateDirectory(outImageFolder);
            }

            //даем права на запись в папку
            File.SetAttributes(outImageFolder, FileAttributes.Normal);

            //outImagePath = outImageFolder + "[" + procID + "]Image.png";

            Random rnd = new Random();

            outImagePath = outImageFolder + "[" + procID + rnd.Next() + "]Image.png";

            srcImage.Save(outImagePath);

            outTextOCRFolder = "C:\\[Temp]Text_OCR\\";

            if ((System.IO.Directory.Exists(outTextOCRFolder)) == false) {
                System.IO.Directory.CreateDirectory(outTextOCRFolder);
            }

            //даем права на запись в папку
            File.SetAttributes(outTextOCRFolder, FileAttributes.Normal);

            outTextOCRPath = outTextOCRFolder + "[" + procID + rnd.Next() + "]Text_OCR.txt";

            ////////////////////////////////////////////////////////////////////
        }

        public void Start() {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = @"/c cd " + abbyFilePath + " & FineCmd.exe " + outImagePath + " /lang Mixed /out " + outTextOCRPath + " /quit";

            process.StartInfo = startInfo;
            process.Start();
            /*//ждем окончания работы
            process.WaitForExit(30000 + Convert.ToInt32(5000 * timerModifier));
            process.Close();

            if ((File.Exists(outTextOCRPath)) == false) {
                timerModifier += 1.0f;

                Start();//[рекурсия]перезапускаем процесс
            }*/
        }

        //если распознали, то закончили
        public void Refresh() {
            if (System.IO.File.Exists(outTextOCRPath)) {
                isDone = true;
            }
        }

        public void Destroy(){
            //удаляем созданные файлы после использования
            if (System.IO.File.Exists(outImagePath)) {
                System.IO.File.Delete(outImagePath);
            }

            if (System.IO.File.Exists(outTextOCRPath)) {
                System.IO.File.Delete(outTextOCRPath);
            }
        }
    }
}
