using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using AForge.Video.DirectShow;
using AForge.Video;


namespace sharp_generator {
    public class camScannerUSB {
        public VideoCaptureDevice videoSource;//create video source
        public VideoCapabilities [] resolutions;//собираем доступные разрешения usb камеры

        public Image currentFrame;//текущий фрейм с usb камеры

        public string name;//имя устройства

        public VideoCapabilities minResolution;//min разрешение
        public VideoCapabilities midResolution;//mid разрешение
        public VideoCapabilities maxResolution;//max разрешение       
        public VideoCapabilities saveResolution;//save разрешение

        public string[] res_DOKO_X08A3;
        public string[] res_DOKO_X08A4;
        public List<Tuple<string,int>> res_сur_DOKO=new List<Tuple<string, int>>();//разрешения для текущей камеры

        public camScannerUSB() {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);//enumerate video devices
            
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);//create video source
            resolutions = videoSource.VideoCapabilities;//собираем доступные разрешения usb камеры

            /*
            Разрешения камеры(с привязкой к индексу): 

            Модель: DOKO CamScanner X08A4

            Индекс по-умолчаниб = 5

            0 3264x2448
            1 2592x1944
            2 2048x1536
            3 1920x1080
            4 1600x1200
            5 1280x960
            6 1024x768
            7 800x600
            8 640x480
            9 320x240
             */

            /*
            videoDevices[0].MonikerString = "@device:pnp:\\\\?\\usb#vid_2b16&pid_6689&mi_00#7&1ef84f63&0&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\\global"
            videoDevices[0].Name = "Doccamera"
            FilterCategory.VideoInputDevice = {860bb310-5d01-11d0-bd3b-00a0c911ce86}
            
            Заводские данные:
            DOKO Document Camera
            Item: DKOKO CamScanner X08A3
            Serial Number: X08A3-201800072

            0 3264x2448
            1 640x480
            2 800x600
            3 1024x768
            4 1600x1200
            5 2048x1536
            6 1280x720
            7 1920x1080
            8 2592x1944
            9 1280x960
             */

            //настраиваем разрешение снимков и видео
            /*videoSource.SnapshotResolution = videoSource.VideoCapabilities[5];//берем нулевой, т.к. в нем max разрешающая способность
            videoSource.VideoResolution = videoSource.VideoCapabilities[5];//берем нулевой, т.к. в нем max разрешающая способность */

            

            //сохраняем имя камеры
            name = videoDevices[0].Name;

            //ставим разрешения
            if(videoDevices[0].Name == "Doccamera") {//если модель X08A3, то index = 1, res = 640x480
                res_DOKO_X08A3 = new string[10] { "3264x2448", "640x480", "800x600", "1024x768", "1600x1200", "2048x1536", "1280x720", "1920x1080", "2592x1944", "1280x960" };

                res_сur_DOKO.Add(new Tuple<string, int>("640x480", 1));
                res_сur_DOKO.Add(new Tuple<string, int>("800x600", 2));
                res_сur_DOKO.Add(new Tuple<string, int>("1024x768", 3));
                res_сur_DOKO.Add(new Tuple<string, int>("1280x720", 6));
                res_сur_DOKO.Add(new Tuple<string, int>("1280x960", 9));
                res_сur_DOKO.Add(new Tuple<string, int>("1600x1200", 4));
                res_сur_DOKO.Add(new Tuple<string, int>("1920x1080", 7));
                res_сur_DOKO.Add(new Tuple<string, int>("2048x1536", 5));             
                res_сur_DOKO.Add(new Tuple<string, int>("2592x1944", 8));
                res_сur_DOKO.Add(new Tuple<string, int>("3264x2448", 0));

                minResolution = videoSource.VideoCapabilities[1];//640x480
                //midResolution = videoSource.VideoCapabilities[7];//1920x1080
                //midResolution = videoSource.VideoCapabilities[4];//1600x1200
                midResolution = videoSource.VideoCapabilities[9];//1280x960
                maxResolution = videoSource.VideoCapabilities[0];//3264x2448

                saveResolution = videoSource.VideoCapabilities[5];

                //setMinRes();
                setMidRes();
                //setMaxRes();
            } else {// если модель X08A4, то index = 8, res = 640x480
                res_DOKO_X08A4 = new string[10] {"3264x2448","2592x1944","2048x1536","1920x1080","1600x1200","1280x960","1024x768","800x600","640x480","320x240"};

                res_сur_DOKO.Add(new Tuple<string, int>("320x240", 9));
                res_сur_DOKO.Add(new Tuple<string, int>("640x480", 8));
                res_сur_DOKO.Add(new Tuple<string, int>("800x600", 7));
                res_сur_DOKO.Add(new Tuple<string, int>("1024x768", 6));
                res_сur_DOKO.Add(new Tuple<string, int>("1280x960", 5));
                res_сur_DOKO.Add(new Tuple<string, int>("1600x1200", 4));
                res_сur_DOKO.Add(new Tuple<string, int>("1920x1080", 3));
                res_сur_DOKO.Add(new Tuple<string, int>("2048x1536", 2));
                res_сur_DOKO.Add(new Tuple<string, int>("2592x1944", 1));
                res_сur_DOKO.Add(new Tuple<string, int>("3264x2448", 0));

                minResolution = videoSource.VideoCapabilities[8];//640x480
                //midResolution = videoSource.VideoCapabilities[3];//1920x1080
                //midResolution = videoSource.VideoCapabilities[4];//1600x1200
                midResolution = videoSource.VideoCapabilities[5];//1280x960
                maxResolution = videoSource.VideoCapabilities[0];//3264x2448

                saveResolution = videoSource.VideoCapabilities[5];

                //setMinRes();
                setMidRes();
                //setMaxRes();
            }

            //setMinRes();
            //setMidRes();
            //setMaxRes();
        }

        public void setVidRes(int indOfRes) {
            videoSource.VideoResolution = resolutions[indOfRes];

            saveResolution = resolutions[indOfRes];
        }

        public void setVidMaxRes() {
            videoSource.VideoResolution = maxResolution;
        }

        public void setVidMidRes() {
            videoSource.VideoResolution = midResolution;
        }

        public void setVidMinRes() {
            videoSource.VideoResolution = minResolution;
        }

        public void setSnapMaxRes() {
            videoSource.SnapshotResolution = maxResolution;
        }

        public void setSnapMidRes() {
            videoSource.SnapshotResolution = midResolution;
        }

        public void setSnapMinRes() {
            videoSource.SnapshotResolution = minResolution;
        }

        public void setMaxRes() {
            videoSource.SnapshotResolution = maxResolution;
            videoSource.VideoResolution = maxResolution;
        }

        public void setMidRes() {
            videoSource.SnapshotResolution = midResolution;
            videoSource.VideoResolution = midResolution;
        }

        public void setMinRes() {
            videoSource.SnapshotResolution = minResolution;
            videoSource.VideoResolution = minResolution;
        }
    }
}
