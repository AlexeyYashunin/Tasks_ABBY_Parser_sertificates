namespace Tesseract_OCR
{
    partial class Tesseract_OCR_Window
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.OcrBtn = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.textBox = new System.Windows.Forms.TextBox();
            this.isDoneProcLabel = new System.Windows.Forms.Label();
            this.processProgressBar = new System.Windows.Forms.ProgressBar();
            this.docButton = new System.Windows.Forms.Button();
            this.docLabel = new System.Windows.Forms.Label();
            this.Tasks_amount_lbl = new System.Windows.Forms.Label();
            this.Tasks_amount_txtbox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // OcrBtn
            // 
            this.OcrBtn.Location = new System.Drawing.Point(114, 332);
            this.OcrBtn.Name = "OcrBtn";
            this.OcrBtn.Size = new System.Drawing.Size(131, 40);
            this.OcrBtn.TabIndex = 0;
            this.OcrBtn.Text = "OCR";
            this.OcrBtn.UseVisualStyleBackColor = true;
            this.OcrBtn.Click += new System.EventHandler(this.OcrBtn_Click);
            // 
            // textBox
            // 
            this.textBox.Location = new System.Drawing.Point(12, 12);
            this.textBox.Multiline = true;
            this.textBox.Name = "textBox";
            this.textBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox.Size = new System.Drawing.Size(331, 244);
            this.textBox.TabIndex = 1;
            this.textBox.TextChanged += new System.EventHandler(this.textBox_TextChanged);
            // 
            // isDoneProcLabel
            // 
            this.isDoneProcLabel.AutoSize = true;
            this.isDoneProcLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.isDoneProcLabel.Location = new System.Drawing.Point(262, 341);
            this.isDoneProcLabel.Name = "isDoneProcLabel";
            this.isDoneProcLabel.Size = new System.Drawing.Size(18, 26);
            this.isDoneProcLabel.TabIndex = 2;
            this.isDoneProcLabel.Text = " ";
            // 
            // processProgressBar
            // 
            this.processProgressBar.Location = new System.Drawing.Point(12, 278);
            this.processProgressBar.Name = "processProgressBar";
            this.processProgressBar.Size = new System.Drawing.Size(331, 30);
            this.processProgressBar.TabIndex = 3;
            // 
            // docButton
            // 
            this.docButton.Location = new System.Drawing.Point(20, 341);
            this.docButton.Name = "docButton";
            this.docButton.Size = new System.Drawing.Size(69, 23);
            this.docButton.TabIndex = 5;
            this.docButton.Text = "Document";
            this.docButton.UseVisualStyleBackColor = true;
            this.docButton.Click += new System.EventHandler(this.docButton_Click);
            // 
            // docLabel
            // 
            this.docLabel.AutoSize = true;
            this.docLabel.Location = new System.Drawing.Point(12, 384);
            this.docLabel.Name = "docLabel";
            this.docLabel.Size = new System.Drawing.Size(19, 13);
            this.docLabel.TabIndex = 6;
            this.docLabel.Text = "    ";
            // 
            // Tasks_amount_lbl
            // 
            this.Tasks_amount_lbl.AutoSize = true;
            this.Tasks_amount_lbl.Location = new System.Drawing.Point(12, 382);
            this.Tasks_amount_lbl.Name = "Tasks_amount_lbl";
            this.Tasks_amount_lbl.Size = new System.Drawing.Size(36, 13);
            this.Tasks_amount_lbl.TabIndex = 7;
            this.Tasks_amount_lbl.Text = "Tasks";
            // 
            // Tasks_amount_txtbox
            // 
            this.Tasks_amount_txtbox.Location = new System.Drawing.Point(55, 379);
            this.Tasks_amount_txtbox.Name = "Tasks_amount_txtbox";
            this.Tasks_amount_txtbox.Size = new System.Drawing.Size(38, 20);
            this.Tasks_amount_txtbox.TabIndex = 8;
            this.Tasks_amount_txtbox.Text = "1";
            // 
            // Tesseract_OCR_Window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(356, 406);
            this.Controls.Add(this.Tasks_amount_txtbox);
            this.Controls.Add(this.Tasks_amount_lbl);
            this.Controls.Add(this.docLabel);
            this.Controls.Add(this.docButton);
            this.Controls.Add(this.processProgressBar);
            this.Controls.Add(this.isDoneProcLabel);
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.OcrBtn);
            this.Name = "Tesseract_OCR_Window";
            this.Text = "Tesseract_OCR";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OcrBtn;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.Label isDoneProcLabel;
        private System.Windows.Forms.ProgressBar processProgressBar;
        private System.Windows.Forms.Button docButton;
        private System.Windows.Forms.Label docLabel;
        private System.Windows.Forms.Label Tasks_amount_lbl;
        private System.Windows.Forms.TextBox Tasks_amount_txtbox;
    }
}

