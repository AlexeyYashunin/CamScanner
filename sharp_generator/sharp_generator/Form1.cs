using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using AForge.Video.DirectShow;
using AForge.Video;
using AForge.Imaging.Filters;
using System.Threading;
using PdfSharp.Pdf.IO;
using System.IO;

using XsPDF.Pdf;

using iTextSharp;
using iTextSharp.text.pdf;

using Spire;
using PdfSharp.Drawing;
using System.Text.RegularExpressions;
using PdfSharp;
using System.Drawing.Drawing2D;

namespace sharp_generator {
    public partial class Form1 : Form {
        //private BackgroundWorker backgroundWorker1;//для фоновых операций в отдельном потоке
        private Image currentFrame;//текущий фрейм с usb камеры

        private Thread demoThread = null;

        camScannerUSB camScannerUSB_ = null;

        string outFilePath_ = "";//[out] путь до файла
        string outFolderPath_ = "";//[out] путь до папки с файлом
        string outTempFolderPath_ = "";//[out] путь до папки с временными файлами

        int amountOfFiles_ = 0;

        int checkIsUpdated = 0;//если добавлено два одинаковых элемента, то скидываем этот счетчик и не вызываем событие для того же элемента

        bool isSnapshoted = false;

        //RotateBilinear rotateImage = new RotateBilinear(90);
        RotateBilinear rotateImage = new RotateBilinear(270);

        delegate void StringArgReturningVoidDelegate(Image currentFrame);

        public enum EOutPathType { INPUT_ARG = 0, CURRENT_DIR = 1, NONE = 2 };

        public enum EImageFormat { JPG = 0 };

        EOutPathType eOutPathType = EOutPathType.NONE;

        public Form1(string outFilePath) {
            InitializeComponent();     

            /*this.Size = new Size(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);

            pbVideoStream.Size = new System.Drawing.Size(Convert.ToInt32(SystemInformation.VirtualScreen.Width*0.4), Convert.ToInt32(SystemInformation.VirtualScreen.Height*0.7));*/

            backgroundWorker1.WorkerSupportsCancellation = true;

            camScannerUSB_ = new camScannerUSB();

            for(int i = 0; i < camScannerUSB_.res_сur_DOKO.Count; i++) {
                cbResolutions.Items.Add(camScannerUSB_.res_сur_DOKO[i].Item1);
               
                //lbResolutions.Items.Add(camScannerUSB_.res_сur_DOKO[i].Item1);
            }

            //string curRes = camScannerUSB_.videoSource.VideoResolution.FrameSize.Width.ToString() + "x" + camScannerUSB_.videoSource.VideoResolution.FrameSize.Height.ToString();
            string curRes = camScannerUSB_.saveResolution.FrameSize.Width.ToString() + "x" + camScannerUSB_.saveResolution.FrameSize.Height.ToString();

            cbResolutions.SelectedIndex = cbResolutions.FindStringExact(curRes);

            /*Size size = new Size(camScannerUSB_.videoSource.VideoCapabilities[0].FrameSize.Height, camScannerUSB_.videoSource.VideoCapabilities[0].FrameSize.Width);

            pbVideoStream.Size = size;*/

            /*pbVideoStream.Width = camScannerUSB_.videoSource.VideoCapabilities[0].FrameSize.Width;
            pbVideoStream.Height = camScannerUSB_.videoSource.VideoCapabilities[0].FrameSize.Height;*/

            //.............................................
            //Тест

            //string fn = @"C:\Users\YashuninAM\Desktop\Generator\Sharp_Data_Calculation\sharp_generator\sharp_generator\bin\Debug\0.pdf";

            outTempFolderPath_ = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;

            if(Environment.OSVersion.Version.Major >= 6) {
                outTempFolderPath_ = Directory.GetParent(outTempFolderPath_).ToString() + "\\" + "Documents";
            }

            Clear();

            string outFolderPath_ = "";

            try {
                outFolderPath_ = Path.GetFullPath(outFilePath);
                outFolderPath_ = Directory.GetParent(outFolderPath_).FullName;
            } catch(System.Exception ex) {
            }

            if(outFilePath == "") {
                //MessageBox.Show("Input argument is empty or output directory isn't exist\nOutput directory set to default", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                outFilePath_ = System.IO.Directory.GetCurrentDirectory();
                eOutPathType = EOutPathType.CURRENT_DIR;

                string outPdfFolderPath = outFilePath_ + "\\" + "[OUT]Pdf";

                //Удаляем папку [OUT]Pdf
                try {
                    if(System.IO.Directory.Exists(outPdfFolderPath)) {
                        System.IO.Directory.Delete(outPdfFolderPath, true);
                    }
                } catch(System.Exception ex) {
                }
            } else if((outFilePath != "") || (Directory.Exists(outFolderPath_) == false)) {
                System.IO.Directory.CreateDirectory(outFolderPath_);

                //даем права на запись в папку
                File.SetAttributes(outFolderPath_, FileAttributes.Normal);

                outFilePath_ = outFilePath;

                eOutPathType = EOutPathType.INPUT_ARG;
            } else {
                outFilePath_ = outFilePath;

                eOutPathType = EOutPathType.INPUT_ARG;
            }

            /*//добавляем столбец(скрытый) для отображения картинок в listView
            ColumnHeader columnHeader1 = new ColumnHeader();
            columnHeader1.Text = "Images";

            lvScanImages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

            lvScanImages.Columns.AddRange(new ColumnHeader[] { columnHeader1 });*/
        }

        private void Form1_Load(object sender, EventArgs e) {
            string curFolderPath = System.IO.Directory.GetCurrentDirectory();
            string outFolderPath = curFolderPath + "\\" + "[OUT]Image";

            //чистим если папка под вывод существует
            if(System.IO.Directory.Exists(outFolderPath)) {
                System.IO.Directory.Delete(outFolderPath, true);
            }

            camScannerUSB_.videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);//set NewFrame event handler
            camScannerUSB_.videoSource.Start();//start the video source
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) {
            this.demoThread = new Thread(new ThreadStart(this.ThreadProcSafe));

            this.demoThread.Start();
        }

        private void ThreadProcSafe() {
            this.SetFrame(currentFrame);
        }

        private void SetFrame(Image currentFrame) {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.

            try {
                if(this.pbVideoStream.InvokeRequired) {
                    StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetFrame);
                    this.Invoke(d, new object[] { currentFrame });
                } else {
                    if(currentFrame != null) {
                        Application.DoEvents();

                        //Image tempImg = (Image)currentFrame;
                        //tempImg = rotateImage.Apply((Bitmap)pbVideoStream.Image);

                        /*if((rotateImage.Angle == 270) || (rotateImage.Angle == 90)) {
                            if(currentFrame.Height == camScannerUSB_.midResolution.FrameSize.Width) {
                                if(pbVideoStream.Image != null) {
                                    pbVideoStream.Image.Dispose();
                                }

                                this.pbVideoStream.Image = currentFrame;
                            }
                        } else if((rotateImage.Angle == 180) || (rotateImage.Angle == 0)) {
                            if(currentFrame.Height == camScannerUSB_.midResolution.FrameSize.Height) {
                                if(pbVideoStream.Image != null) {
                                    pbVideoStream.Image.Dispose();
                                }

                                this.pbVideoStream.Image = currentFrame;
                            }
                        }*/

                        this.pbVideoStream.Image = currentFrame;
                        //pbVideoStream.Image.RotateFlip(RotateFlipType.Rotate90FlipXY);
                        //pbVideoStream.Image = rotateImage.Apply((Bitmap)pbVideoStream.Image);
                    }
                }
            } catch(System.Exception ex) {
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs) {
            /*if(currentFrame != null) {
                currentFrame.Dispose();
                currentFrame = null;
            }*/

            //currentFrame = (Image)eventArgs.Frame.Clone();

            /*if(pbVideoStream.Image!=null) {
                pbVideoStream.Image.Dispose();
            }*/
            
            currentFrame = rotateImage.Apply((Bitmap)eventArgs.Frame);
            eventArgs.Frame.Dispose();

            //this.backgroundWorker1.RunWorkerAsync();

            if(!backgroundWorker1.IsBusy) {
                backgroundWorker1.RunWorkerAsync();
            }

            /* else {
                MessageBox.Show("Can't run the worker twice!");
            }*/
        }

        private void Clear() {
            //string curFolderPath = System.IO.Directory.GetCurrentDirectory();
            string curFolderPath = outTempFolderPath_;
            string outFolderPath = curFolderPath + "\\" + "[OUT]Image";

            bool isAnyImgChecked = false;

            List<int> indItemsToDel = new List<int>();

            int ind = 0;

            foreach(ListViewItem item in lvScanImages.Items) {
                if(item.Checked) {
                    File.Delete(outFolderPath + "\\" + item.Text);

                    item.Remove();

                    //indItemsToDel.Add(ind);

                    isAnyImgChecked = true;

                    ind++;
                }
            }

            //amountOfFiles_ -= ind;

            if(isAnyImgChecked) {
                return;
            }

            /*if(isAnyImgChecked && (indItemsToDel.Count > 0)) {
                for(int i = 0; i < indItemsToDel.Count; i++) {
                    / *ilScanImages.Images[i].Dispose();
                    ilScanImages.Images.RemoveAt(i);* /
                    ilScanImages.Images[indItemsToDel[i]].Dispose();
                    ilScanImages.Images.RemoveAt(indItemsToDel[i]);
                }

                return;
            }*/

            //чистим imageList
            foreach(Image item in ilScanImages.Images) {
                item.Dispose();
            }

            ilScanImages.Images.Clear();

            //чистим listView
            foreach(ListViewItem item in lvScanImages.Items) {
                item.Remove();
            }

            lvScanImages.Clear();

            try {
                if(System.IO.Directory.Exists(outFolderPath)) {
                    System.IO.Directory.Delete(outFolderPath, true);
                }
            } catch(System.Exception ex) {
            }

            /*bool isDeleted = false;
            do {
                try {
                    if(System.IO.Directory.Exists(outFolderPath)) {
                        System.IO.Directory.Delete(outFolderPath, true);
                    }

                    isDeleted = true;
                } catch(System.Exception ex) {
                    continue;
                }
            } while(isDeleted == false);*/

            amountOfFiles_ = 0;
        }

        private void switchVidMaxResFlow() {
            demoThread.Interrupt();
            camScannerUSB_.videoSource.Stop();
            camScannerUSB_.setVidMaxRes();
            camScannerUSB_.videoSource.Start();            

            if((rotateImage.Angle == 270) || (rotateImage.Angle == 90)) {
                do {
                } while(currentFrame.Height != camScannerUSB_.maxResolution.FrameSize.Width);
            } else if((rotateImage.Angle == 180) || (rotateImage.Angle == 0)) {
                do {
                } while(currentFrame.Height != camScannerUSB_.maxResolution.FrameSize.Height);
            }
        }

        private void switchVidMinResFlow() {
            demoThread.Interrupt();
            camScannerUSB_.videoSource.Stop();
            camScannerUSB_.setVidMinRes();
            camScannerUSB_.videoSource.Start();

            if((rotateImage.Angle == 270) || (rotateImage.Angle == 90)) {
                do {
                } while(currentFrame.Height != camScannerUSB_.minResolution.FrameSize.Width);
            } else if((rotateImage.Angle == 180) || (rotateImage.Angle == 0)) {
                do {
                } while(currentFrame.Height != camScannerUSB_.minResolution.FrameSize.Height);
            }
        }

        private void switchVidMidResFlow() {
            demoThread.Interrupt();
            camScannerUSB_.videoSource.Stop();
            camScannerUSB_.setVidMidRes();
            camScannerUSB_.videoSource.Start();

            if((rotateImage.Angle == 270) || (rotateImage.Angle == 90)) {
                do {
                } while(currentFrame.Height != camScannerUSB_.midResolution.FrameSize.Width);
            } else if((rotateImage.Angle == 180) || (rotateImage.Angle == 0)) {
                do {
                } while(currentFrame.Height != camScannerUSB_.midResolution.FrameSize.Height);
            }
        }

        private void switchVidResFlow(int indOfRes) {
            try{
                demoThread.Interrupt();
                camScannerUSB_.videoSource.Stop();
                camScannerUSB_.setVidRes(indOfRes);
                camScannerUSB_.videoSource.Start();

                if((rotateImage.Angle == 270) || (rotateImage.Angle == 90)) {
                    do {
                    } while(currentFrame.Height != camScannerUSB_.resolutions[indOfRes].FrameSize.Width);
                } else if((rotateImage.Angle == 180) || (rotateImage.Angle == 0)) {
                    do {
                    } while(currentFrame.Height != camScannerUSB_.resolutions[indOfRes].FrameSize.Height);
                }
            } catch (System.Exception ex){
            }
        }

        private void btnScan_Click(object sender, EventArgs e) {
            /*int index = this.cbResolutions.SelectedIndex;

            try{
                switchVidResFlow(camScannerUSB_.res_сur_DOKO.ElementAt(index).Item2);
                //Thread.Sleep(2000);
                //Thread.Sleep(1000);
            } catch (System.Exception ex){}*/

            //Thread.Sleep(2000);
            //Thread.Sleep(300);
            //Thread.Sleep(100);

            if(currentFrame != null) {
                //string curFolderPath = System.IO.Directory.GetCurrentDirectory();
                string curFolderPath = outTempFolderPath_;
                //string curFolderPath = outFilePath_;
                string outFolderPath = curFolderPath + "\\" + "[OUT]Image";
                //string fileName = "[" + (amountOfFiles_++) + "]Image.png";

                //............................................
                /*string fileName = "";

                if(checkIsUpdated==1){
                    fileName = (amountOfFiles_--) + ".png";
                } else {
                    fileName = (amountOfFiles_++) + ".png";
                }*/
                //............................................

                string fileName = "";

                //if((lvScanImages.Items.Count>0) && (lvScanImages.Items.Count==amountOfFiles_)){
                if(lvScanImages.Items.Count > 0) {
                    string lvfileName = Path.GetFileNameWithoutExtension(lvScanImages.Items[lvScanImages.Items.Count - 1].Text);
                    fileName = (Convert.ToInt32(lvfileName) + 1) + ".png";
                    //fileName = (amountOfFiles_) + ".png";
                } else {
                    fileName = "0.png";
                    //fileName = (amountOfFiles_++) + ".png";
                }

                //string fileName = (amountOfFiles_++) + ".png";
                string outFilePath = outFolderPath + "\\" + fileName;

                if(!(System.IO.Directory.Exists(outFolderPath))) {
                    System.IO.Directory.CreateDirectory(outFolderPath);

                    //даем права на запись в папку
                    File.SetAttributes(outFolderPath, FileAttributes.Normal);
                }

                //System.Drawing.Imaging.ImageFormat imgFormat = System.Drawing.Imaging.ImageFormat.Png;
                System.Drawing.Imaging.ImageFormat imgFormat = System.Drawing.Imaging.ImageFormat.Png;

                //currentFrame.Save(outFolderPath + "\\[" + (amountOfFiles++) + "]Image",imgFormat);

                //currentFrame.RotateFlip(RotateFlipType.Rotate90FlipXY);

                Image dubCurrentFrame = (Image)currentFrame.Clone();

                /*while(camScannerUSB_.videoSource.VideoResolution.FrameSize.Width != dubCurrentFrame.Height) {
                    //dubCurrentFrame.RotateFlip(RotateFlipType.Rotate90FlipXY);
                    dubCurrentFrame = rotateImage.Apply((Bitmap)dubCurrentFrame);
                }*/

                dubCurrentFrame.Save(outFilePath);

                //сохраняем картинку в исходном размере
                /*Image tempImage = (Image)currentFrame.Clone();
                tempImage.Save(outFilePath);
                tempImage.Dispose();*/

                //...................................................
                /*if(checkIsUpdated==0) {
                    currentFrame.Save(outFilePath);
                } else {
                    Image dubCurrentFrame = (Image)currentFrame.Clone();
                    dubCurrentFrame.Save(outFilePath);
                    dubCurrentFrame.Dispose();
                }*/

                //currentFrame.Save(outFilePath);
                //dubCurrentFrame.Save(outFilePath);

                //...................................................

                //XSize size = PageSizeConverter.ToSize(PdfSharp.PageSize.A4);

                //меняем размер картинки под A4 как в pdf
                /*Image resizedImage = Image.FromFile(outFilePath);
                resizedImage = (Image)resizeImage(resizedImage, Convert.ToInt32(size.Width), Convert.ToInt32(size.Height));*/

                /*Image resizedImage = (Image)currentFrame.Clone();
                resizedImage = (Image)resizeImage(resizedImage, Convert.ToInt32(size.Width), Convert.ToInt32(size.Height));
          
                resizedImage.Save(outFilePath);*/

                /*using(var image = Image.FromFile(outFilePath)) {
                    ilScanImages.Images.Add(image);
                }*/

                //ilScanImages.Images.Add(currentFrame);
                ilScanImages.Images.Add(dubCurrentFrame);
                //ilScanImages.Images.Add(Image.FromFile(outFilePath));
                //ilScanImages.Images.Add(Image.FromFile(outFilePath));

                //set the amall and large ImageList properties of listview
                /*lvScanImages.LargeImageList = ilScanImages;
                lvScanImages.SmallImageList = ilScanImages;*/

                //..........................................

                lvScanImages.View = View.LargeIcon;

                ilScanImages.ImageSize = new Size(128, 128);
                ilScanImages.ColorDepth = ColorDepth.Depth32Bit;

                lvScanImages.LargeImageList = ilScanImages;

                //..........................................

                //lvScanImages.Items.Add(new ListViewItem() { ImageIndex = amountOfFiles_, Text = fileName });
                //lvScanImages.Items.Add(new ListViewItem() { ImageIndex = (lvScanImages.Items.Count ), Text = fileName });
                //lvScanImages.Items.Add(new ListViewItem() { ImageIndex = ilScanImages.Images.Count - 1, Text = fileName });

                ListViewItem lvItem = new ListViewItem();
                lvItem.ImageIndex = ilScanImages.Images.Count - 1;
                lvItem.Text = fileName;
                lvItem.SubItems.Add("Angle");
                
                lvScanImages.Items.Add(lvItem);

                lvScanImages.Items[lvScanImages.Items.Count - 1].SubItems[1].Text = Convert.ToString(rotateImage.Angle);

                //..........................................
                //Тест

                /*lvScanImages.View = View.LargeIcon;
                ilScanImages.ImageSize = new Size(128, 128);

                ilScanImages.ColorDepth = ColorDepth.Depth32Bit;

                lvScanImages.LargeImageList = ilScanImages;

                lvScanImages.Items.Add(new ListViewItem() { ImageIndex = amountOfFiles_, Text = fileName });   */

                //..........................................

                //lvScanImages.View = View.LargeIcon;
                //.........................................

                /*string fileTempName = "[TEMP]"+(amountOfFiles_) + ".png";

                lvScanImages.Items.Add(new ListViewItem() { ImageIndex = amountOfFiles_+1, Text = fileTempName });
                lvScanImages.Items[lvScanImages.Items.Count - 1].Remove();*/

                //.........................................

                //обновляем на форме listView
                lvScanImages.Refresh();

                if(checkIsUpdated == 1) {
                    checkIsUpdated = 0;

                    //удаляем измененную картинку из папки

                    string outImageFolderPath = curFolderPath + "\\" + "[OUT]Image";

                    ilScanImages.Images[ilScanImages.Images.Count - 1].Dispose();
                    ilScanImages.Images.RemoveAt(ilScanImages.Images.Count - 1);

                    //File.Delete(outImageFolderPath + "\\" + lvScanImages.Items[lvScanImages.Items.Count - 1].Text);

                    bool isDeleted = false;

                    string fPathDublicate = outImageFolderPath + "\\" + lvScanImages.Items[lvScanImages.Items.Count - 1].Text;

                    lvScanImages.Items[lvScanImages.Items.Count - 1].Remove();

                    Application.DoEvents();

                    do {
                        try {
                            //File.Delete(fPathDublicate);

                            isDeleted = true;
                        } catch(System.Exception ex) {
                            continue;
                        }
                    } while(isDeleted == false);

                    //amountOfFiles_--;

                    /*if(lvScanImages.Items.Count > 0) {
                        lvScanImages.Items[lvScanImages.Items.Count - 1].ImageIndex -= 1;
                    }    */

                    /*try{
                        switchVidMidResFlow();
                        //Thread.Sleep(2000);
                    } catch (System.Exception ex){}*/

                    return;
                } else {
                    checkIsUpdated++;
                    //amountOfFiles_--;
                    btnScan_Click(sender, e);
                }

                /*/цепляем страницу серта
                PdfSharp.Pdf.PdfPage pageImage;
                
                //создаем pdf документ
                PdfSharp.Pdf.PdfDocument pdfScanImages = new PdfSharp.Pdf.PdfDocument();
                pdfScanImages = new PdfSharp.Pdf.PdfDocument();

                //добавляем в pdf документ сертификат
                pdfScanImages.AddPage(pageSert);

                //сохраняем pdf файл
                pdfDocSert.Save(outSertFilePath);

                //lvScanImages.Items.Add()*/
            }
        }

        private void btnClear_Click(object sender, EventArgs e) {
            Clear();
        }

        private void btnSave_Click(object sender, EventArgs e) {
            demoThread.Interrupt();

            if(lvScanImages.Items.Count == 0) {
                return;
            }

            string curFolderPath = "";
            string outPdfFolderPath = "";
            string outImageFolderPath = "";

            if(eOutPathType == EOutPathType.CURRENT_DIR) {
                //curFolderPath = System.IO.Directory.GetCurrentDirectory();
                curFolderPath = outTempFolderPath_;
                outPdfFolderPath = curFolderPath + "\\" + "[OUT]Pdf";
                outImageFolderPath = curFolderPath + "\\" + "[OUT]Image";
            } else {
                //curFolderPath = System.IO.Directory.GetCurrentDirectory();
                curFolderPath = outTempFolderPath_;
                outImageFolderPath = curFolderPath + "\\" + "[OUT]Image";
            }

            int i = 0;

            bool isAnyImgChecked = false;

            PdfSharp.Pdf.PdfDocument doc = new PdfSharp.Pdf.PdfDocument();

            if(isAnyImgChecked == false) {
                foreach(ListViewItem item in lvScanImages.Items) {
                    PdfSharp.Pdf.PdfPage pdfPage = new PdfSharp.Pdf.PdfPage();

                    XSize size = PageSizeConverter.ToSize(PdfSharp.PageSize.A4);

                    pdfPage.Orientation = PageOrientation.Portrait;

                    //........................................
                    //размер страницы = A4
                    /*pdfPage.Width = size.Width;
                    pdfPage.Height = size.Height;*/
                    //........................................

                    /*if((item.SubItems[1].Text == "270") || (item.SubItems[1].Text == "90")) {
                        pdfPage.Orientation = PageOrientation.Portrait;

                        pdfPage.Width = size.Width;
                        pdfPage.Height = size.Height;
                    } else if((item.SubItems[1].Text == "180") || (item.SubItems[1].Text == "0")) {
                        //pdfPage.Orientation = PageOrientation.Landscape;
                        pdfPage.Orientation = PageOrientation.Portrait;

                        pdfPage.Width = size.Height;
                        pdfPage.Height = size.Width;
                    } */

                    string compressedFilePath = "";

                    if((item.SubItems[1].Text == "270") || (item.SubItems[1].Text == "90")) {
                        compressedFilePath = saveImage(outImageFolderPath + "\\" + item.Text, EImageFormat.JPG);
                    } else if((item.SubItems[1].Text == "180") || (item.SubItems[1].Text == "0")) {
                        Image rotImage = Image.FromFile(outImageFolderPath + "\\" + item.Text);
                        RotateBilinear rotImgTemp = new RotateBilinear(90);
                        rotImage = rotImgTemp.Apply((Bitmap)rotImage);
                        rotImage.Save(outImageFolderPath + "\\" + "[ROTATED]" + item.Text);

                        compressedFilePath = saveImage(outImageFolderPath + "\\" + "[ROTATED]" + item.Text, EImageFormat.JPG);
                    }

                    pdfPage.TrimMargins.Top = 5;
                    pdfPage.TrimMargins.Right = 5;
                    pdfPage.TrimMargins.Bottom = 5;
                    pdfPage.TrimMargins.Left = 5;

                    //string compressedFilePath = saveImage(outImageFolderPath + "\\" + item.Text, EImageFormat.JPG);
                    //compressedFilePath = saveImage(outImageFolderPath + "\\" + "[ROTATED]" + item.Text, EImageFormat.JPG);

                    //var image = iTextSharp.text.Image.GetInstance(pdfPage, System.Drawing.Imaging.ImageFormat.Png);
                    //iTextSharp.text.Image resizedImage = iTextSharp.text.Image.GetInstance(outImageFolderPath + "\\" + item.Text);

                    iTextSharp.text.Image resizedImage = iTextSharp.text.Image.GetInstance(compressedFilePath);

                    //размер страницы = размер фото
                    pdfPage.Width = resizedImage.Width;
                    pdfPage.Height = resizedImage.Height;
                    //...................................

                    //iTextSharp.text.Image resizedImage = stretchAspectRatioPic(compressedFilePath,(int)pdfPage.Width,(int)pdfPage.Height);

                    /*resizedImage.RotationDegrees=90;
                    resizedImage.Rotate();*/

                    //iTextSharp.text.Rectangle pSize = new iTextSharp.text.Rectangle(0,(float)pdfPage.Height,(float)pdfPage.Width,0);

                    //...................................
                    /*resizedImage.SetAbsolutePosition(0, 0);
                    resizedImage.ScaleAbsoluteHeight((float)pdfPage.Height);
                    resizedImage.ScaleAbsoluteWidth((float)pdfPage.Width);*/
                    //...................................

                    //размер страницы = A4
                    //iTextSharp.text.Document docITextSharp = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

                    //под размер фото
                    iTextSharp.text.Rectangle pgSize = new iTextSharp.text.Rectangle(0, 0, resizedImage.Width, resizedImage.Height);
                    iTextSharp.text.Document docITextSharp = new iTextSharp.text.Document(pgSize,0,0,0,0);

                    PdfWriter writer = PdfWriter.GetInstance(docITextSharp, new FileStream(outImageFolderPath + "\\" + "[Resized]" + Path.GetFileName(compressedFilePath), FileMode.Create));
                    writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_5);
                    writer.CompressionLevel = PdfStream.BEST_COMPRESSION;

                    docITextSharp.Open();
                    iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(resizedImage);
                    docITextSharp.Add(pdfImage);
                    docITextSharp.Close();

                    //_directContent.AddImage(image);
                    doc.Pages.Add(pdfPage);

                    //_doc.NewPage();           
                    //doc.Pages.Add(pdfPage);

                    XGraphics xgr = XGraphics.FromPdfPage(doc.Pages[i++]);

                    //подгружаем обратно уже измененную картинку
                    //XImage img = XImage.FromFile(outImageFolderPath + "\\" + item.Text);

                    XImage img = XImage.FromFile(outImageFolderPath + "\\" + "[Resized]" + Path.GetFileName(compressedFilePath));
                    //XImage img = XImage.FromFile(compressedFilePath);

                    xgr.DrawImage(img, 0, 0);

                    if((item.SubItems[1].Text == "180") || (item.SubItems[1].Text == "0")) {
                        doc.Pages[doc.Pages.Count - 1].Rotate = 90;
                    }

                    //освобождаем рисовалку
                    img.Dispose();
                    xgr.Dispose();

                    isAnyImgChecked = true;

                    item.Remove();
                    ilScanImages.Images[i - 1].Dispose();

                    //удаляем измененную картинку из папки
                    //File.Delete(outImageFolderPath + "\\" + "[Resized]" + item.Text);
                    File.Delete(outImageFolderPath + "\\" + "[Resized]" + Path.GetFileName(compressedFilePath));
                    File.Delete(compressedFilePath);
                }
            }

            //сохраняем pdf, переданный как аргумент
            //если файл был создан ранее, то перезаписываем его
            if(eOutPathType == EOutPathType.INPUT_ARG) {
                doc.Save(outFilePath_);

                doc.Close();

                if(lvScanImages.Items.Count == 0) {
                    //btnClear_Click(sender, e);
                }

                //System.Environment.Exit(0);

                Form1_FormClosed(sender, new FormClosedEventArgs(CloseReason.UserClosing));


                return;
            }

            //Создаем папку [OUT]Pdf
            if(!System.IO.Directory.Exists(outPdfFolderPath)) {
                System.IO.Directory.CreateDirectory(outPdfFolderPath);

                //даем права на запись в папку
                File.SetAttributes(outPdfFolderPath, FileAttributes.Normal);
            }

            //DateTime dTime = DateTime.Now;

            //проверяем номер pdf файла
            string[] pdfFiles = System.IO.Directory.GetFiles(outPdfFolderPath);

            List<string> listPdfFiles = new List<string>();

            for(int j = 0; j < pdfFiles.Length; j++) {
                listPdfFiles.Add(pdfFiles[j]);
            }

            int maxIndex = 0;

            //Regex regexPdfIndex = new Regex(@"[[]{1}\d*[]]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexPdfIndex = new Regex(@"[\\]{1}\d*.pdf", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            for(int j = 0; j < listPdfFiles.Count; j++) {
                if(j == 0) {
                    MatchCollection matchesPdfIndex = regexPdfIndex.Matches(listPdfFiles[j]);

                    if(matchesPdfIndex.Count > 0) {
                        /*Regex regNumbers = new Regex(@"\d*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                        MatchCollection matchesNumbers = regNumbers.Matches(matchesPdfIndex[0].Value);*/

                        string pattern = @"[\\]";
                        string patternSecond = @".pdf";
                        //string pattern = @"\";
                        string replacement = "";

                        string pdfIndex = matchesPdfIndex[0].Value;
                        pdfIndex = Regex.Replace(pdfIndex, pattern, replacement);
                        pdfIndex = Regex.Replace(pdfIndex, patternSecond, replacement);

                        //string pdfIndex = matchesNumbers[0].Value;                     

                        maxIndex = Convert.ToInt32(pdfIndex);
                    }
                } else {
                    MatchCollection matchesPdfIndex = regexPdfIndex.Matches(listPdfFiles[j]);

                    if(matchesPdfIndex.Count > 0) {
                        Regex regNumbers = new Regex(@"\d*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                        MatchCollection matchesNumbers = regNumbers.Matches(matchesPdfIndex[0].Value);

                        /*//чистим
                        string pattern = @"[\w|\W| ]*";
                        //string pattern = @"\d*";
                        string replacement = "";

                        string pdfIndex = matchesPdfIndex[0].Value;*/

                        //pdfIndex = Regex.Replace(pdfIndex, pattern, replacement);

                        string pattern = @"[\\]";
                        string patternSecond = @".pdf";
                        //string pattern = @"\";
                        string replacement = "";

                        string pdfIndex = matchesPdfIndex[0].Value;
                        pdfIndex = Regex.Replace(pdfIndex, pattern, replacement);
                        pdfIndex = Regex.Replace(pdfIndex, patternSecond, replacement);

                        if(maxIndex < (Convert.ToInt32(pdfIndex))) {
                            maxIndex = Convert.ToInt32(pdfIndex);
                        }
                    }
                }
            }

            //doc.Save(outPdfFolderPath + "\\" + "[OUT]File.pdf");
            /*doc.Save(outPdfFolderPath + "\\" + dTime.Hour.ToString() + "." + 
            dTime.Minute.ToString() + "." + dTime.Millisecond.ToString() + ".pdf");*/
            if((maxIndex == 0) && (listPdfFiles.Count == 0) && (File.Exists(outPdfFolderPath + "\\" + Convert.ToString(maxIndex) + ".pdf") == false)) {
                //doc.Save(outPdfFolderPath + "\\[" + Convert.ToString(maxIndex) + "]File.pdf");
                doc.Save(outPdfFolderPath + "\\" + Convert.ToString(maxIndex) + ".pdf");
            } else {
                //doc.Save(outPdfFolderPath + "\\[" + Convert.ToString(maxIndex + 1) + "]File.pdf");
                doc.Save(outPdfFolderPath + "\\" + Convert.ToString(maxIndex + 1) + ".pdf");
            }

            doc.Close();

            if(lvScanImages.Items.Count == 0) {
                btnClear_Click(sender, e);
            }

            //......................................

            //btnClear_Click(sender,e);

            /*foreach(ListViewItem item in lvScanImages.Items) {
                if(item.Checked){

                    Image img=new Image();
                    img.
                     
                    PdfSharp.Pdf.PdfDocument inputDocument = PdfReader.Open(item.Text, PdfDocumentOpenMode.Import);

                    pdfDoc.AddPage(inputDocument.Pages[i]);
                }

                
            }*/

            /*pdfDoc.Save(outFolderPath+"\\"+"[OUT]File.pdf");
            pdfDoc.Dispose();*/
        }

        private iTextSharp.text.Image stretchAspectRatioPic(string filePath,int toWidth,int toHeight) {
            iTextSharp.text.Image pic = iTextSharp.text.Image.GetInstance(filePath);

            if(pic.Height > pic.Width) {
                float percentage = 0.0f;
                percentage = toHeight / pic.Height;
                pic.ScalePercent(percentage * 100);
            } else {
                float percentage = 0.0f;
                percentage = toWidth / pic.Width;
                pic.ScalePercent(percentage * 100);
            }

            pic.Border = iTextSharp.text.Rectangle.BOX;
            pic.BorderColor = iTextSharp.text.BaseColor.BLACK;
            pic.BorderWidth = 3f;

            return pic;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            //останавливаем работу камеры
            if(camScannerUSB_.videoSource.IsRunning) {
                camScannerUSB_.videoSource.Stop();
            }

            //  this.Close();                                                         ЗАКОМЕНТИЛ
            System.Environment.Exit(0);

            /*  ((Form1)Owner).Close();
              Application.Exit();*/
        }

        private bool isFileLocked(string filePath) {
            try {
                using(Stream stream = new FileStream("MyFilename.txt", FileMode.Open)) {
                    return false;
                }
            } catch(IOException) {
                return true;
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap resizeImage(Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using(var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using(var wrapMode = new System.Drawing.Imaging.ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void lvScanImages_MouseDoubleClick(object sender, MouseEventArgs e) {
            string curFolderPath = "";
            string outImageFolderPath = "";

            if(eOutPathType == EOutPathType.CURRENT_DIR) {
                //curFolderPath = System.IO.Directory.GetCurrentDirectory();
                curFolderPath = outTempFolderPath_;
                outImageFolderPath = curFolderPath + "\\" + "[OUT]Image";
            } else {
                //curFolderPath = System.IO.Directory.GetCurrentDirectory();
                curFolderPath = outTempFolderPath_;
                outImageFolderPath = curFolderPath + "\\" + "[OUT]Image";
            }

            ListViewItem theClickedOne = lvScanImages.GetItemAt(e.X, e.Y);
            theClickedOne.Checked = false;

            Form2 form2 = new Form2();
            form2.FormClosing += Form2_Closing;
            form2.setSnapshot(Image.FromFile(outImageFolderPath + "\\" + theClickedOne.Text));
            this.Hide();
            form2.Show();

            //System.Diagnostics.Process procImgPresent = System.Diagnostics.Process.Start(outImageFolderPath + "\\" + theClickedOne.Text);
        }

        private void btnRotate_Click(object sender, EventArgs e) {
            if(rotateImage.Angle < 360) {
                rotateImage.Angle += 90;
            } else {
                rotateImage.Angle = 0;
            }
        }

        private void Form2_Closing(object sender, FormClosingEventArgs e) {
            this.Show();
        }

        private string saveImage(string imgFilePath, EImageFormat eImgFormat) {
            Bitmap myBitmap;

            System.Drawing.Imaging.ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            System.Drawing.Imaging.EncoderParameter myEncoderParameter;
            System.Drawing.Imaging.EncoderParameters myEncoderParameters;

            string fn = imgFilePath;
            string currentPath = Path.GetFullPath(fn);
            currentPath = Directory.GetParent(currentPath).FullName;

            string fileName = "[COMPRESSED]" + Path.GetFileNameWithoutExtension(imgFilePath) + ".jpeg";

            // Create a Bitmap object based on a BMP file.
            myBitmap = new Bitmap(imgFilePath);

            // Get an ImageCodecInfo object that represents the JPEG codec.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Create an Encoder object based on the GUID

            // for the Quality parameter category.
            myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.

            // An EncoderParameters object has an array of EncoderParameter

            // objects. In this case, there is only one

            // EncoderParameter object in the array.
            myEncoderParameters = new System.Drawing.Imaging.EncoderParameters(1);

            /*// Save the bitmap as a JPEG file with quality level 25.
            myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 25L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            myBitmap.Save(currentPath + "Shapes025.jpg", myImageCodecInfo, myEncoderParameters);

            // Save the bitmap as a JPEG file with quality level 50.
            myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 50L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            myBitmap.Save(currentPath + "Shapes050.jpg", myImageCodecInfo, myEncoderParameters);*/

            // Save the bitmap as a JPEG file with quality level 75.
            myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 75L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            myBitmap.Save(currentPath + "\\" + fileName, myImageCodecInfo, myEncoderParameters);

            return (currentPath + "\\" + fileName);
        }

        private static System.Drawing.Imaging.ImageCodecInfo GetEncoderInfo(String mimeType) {
            int j;
            System.Drawing.Imaging.ImageCodecInfo[] encoders;
            encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            for(j = 0; j < encoders.Length; ++j) {
                if(encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }               

        private void cbResolutions_SelectedIndexChanged(object sender, EventArgs e) {
            //lvScanImages.Focus();

            int index = this.cbResolutions.SelectedIndex;
            
            if(index != System.Windows.Forms.ListBox.NoMatches) {
                switchVidResFlow(camScannerUSB_.res_сur_DOKO.ElementAt(index).Item2);
                lvScanImages.Focus();
            }
        }
    }
}