using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace sharp_generator {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new Form1(@"C:\Test\test.pdf"));          

           if(args.Length == 0) {
                Application.Run(new Form1(""));
            } else {
                Application.Run(new Form1(args[0]));
            }
        }
    }
}
