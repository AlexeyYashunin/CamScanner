namespace sharp_generator {
    partial class Form2 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.pbSnapshot = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbSnapshot)).BeginInit();
            this.SuspendLayout();
            // 
            // pbSnapshot
            // 
            this.pbSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbSnapshot.Enabled = false;
            this.pbSnapshot.Location = new System.Drawing.Point(0, 0);
            this.pbSnapshot.Name = "pbSnapshot";
            this.pbSnapshot.Size = new System.Drawing.Size(588, 526);
            this.pbSnapshot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbSnapshot.TabIndex = 1;
            this.pbSnapshot.TabStop = false;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(588, 526);
            this.Controls.Add(this.pbSnapshot);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form2";
            this.Text = "Preview image";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.pbSnapshot)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbSnapshot;
    }
}