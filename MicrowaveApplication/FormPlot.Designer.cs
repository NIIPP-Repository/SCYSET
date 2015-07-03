namespace MicrowaveApplication
{
    partial class FormPlot
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPlot));
            this.pictureBox_plot = new System.Windows.Forms.PictureBox();
            this.contextMenuOnPlot = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.измерениеToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.отбраковкаToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.зумToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.отбраковатьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.методGCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.методIGCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.методQCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.методQCleftDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.методQCrightUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.методQCrightDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.зумироватьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.автомасштабToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.просмотрГруппыToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.печатьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.printDocument1 = new System.Drawing.Printing.PrintDocument();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_plot)).BeginInit();
            this.contextMenuOnPlot.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox_plot
            // 
            this.pictureBox_plot.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox_plot.ContextMenuStrip = this.contextMenuOnPlot;
            this.pictureBox_plot.Location = new System.Drawing.Point(12, 12);
            this.pictureBox_plot.Name = "pictureBox_plot";
            this.pictureBox_plot.Size = new System.Drawing.Size(820, 635);
            this.pictureBox_plot.TabIndex = 0;
            this.pictureBox_plot.TabStop = false;
            this.pictureBox_plot.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox_plot_MouseClick);
            this.pictureBox_plot.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox_plot_MouseMove);
            // 
            // contextMenuOnPlot
            // 
            this.contextMenuOnPlot.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.измерениеToolStripMenuItem,
            this.отбраковкаToolStripMenuItem,
            this.зумToolStripMenuItem,
            this.toolStripSeparator1,
            this.отбраковатьToolStripMenuItem,
            this.зумироватьToolStripMenuItem,
            this.автомасштабToolStripMenuItem,
            this.просмотрГруппыToolStripMenuItem,
            this.печатьToolStripMenuItem});
            this.contextMenuOnPlot.Name = "contextMenuStrip1";
            this.contextMenuOnPlot.Size = new System.Drawing.Size(176, 186);
            // 
            // измерениеToolStripMenuItem
            // 
            this.измерениеToolStripMenuItem.Name = "измерениеToolStripMenuItem";
            this.измерениеToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.измерениеToolStripMenuItem.Text = "Измерение";
            this.измерениеToolStripMenuItem.Click += new System.EventHandler(this.измерениеToolStripMenuItem_Click);
            // 
            // отбраковкаToolStripMenuItem
            // 
            this.отбраковкаToolStripMenuItem.Name = "отбраковкаToolStripMenuItem";
            this.отбраковкаToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.отбраковкаToolStripMenuItem.Text = "Отбраковка";
            this.отбраковкаToolStripMenuItem.Click += new System.EventHandler(this.отбраковкаToolStripMenuItem_Click);
            // 
            // зумToolStripMenuItem
            // 
            this.зумToolStripMenuItem.Name = "зумToolStripMenuItem";
            this.зумToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.зумToolStripMenuItem.Text = "Зум";
            this.зумToolStripMenuItem.Click += new System.EventHandler(this.зумToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(172, 6);
            // 
            // отбраковатьToolStripMenuItem
            // 
            this.отбраковатьToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.методGCToolStripMenuItem,
            this.методIGCToolStripMenuItem,
            this.методQCToolStripMenuItem,
            this.методQCleftDownToolStripMenuItem,
            this.методQCrightUpToolStripMenuItem,
            this.методQCrightDownToolStripMenuItem});
            this.отбраковатьToolStripMenuItem.Name = "отбраковатьToolStripMenuItem";
            this.отбраковатьToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.отбраковатьToolStripMenuItem.Text = "Отбраковать";
            this.отбраковатьToolStripMenuItem.Click += new System.EventHandler(this.отбраковатьToolStripMenuItem_Click);
            // 
            // методGCToolStripMenuItem
            // 
            this.методGCToolStripMenuItem.Name = "методGCToolStripMenuItem";
            this.методGCToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.методGCToolStripMenuItem.Text = "метод GC";
            this.методGCToolStripMenuItem.Click += new System.EventHandler(this.методGCToolStripMenuItem_Click);
            // 
            // методIGCToolStripMenuItem
            // 
            this.методIGCToolStripMenuItem.Name = "методIGCToolStripMenuItem";
            this.методIGCToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.методIGCToolStripMenuItem.Text = "метод IGC";
            this.методIGCToolStripMenuItem.Click += new System.EventHandler(this.методIGCToolStripMenuItem_Click);
            // 
            // методQCToolStripMenuItem
            // 
            this.методQCToolStripMenuItem.Name = "методQCToolStripMenuItem";
            this.методQCToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.методQCToolStripMenuItem.Text = "метод QC (left - up)";
            this.методQCToolStripMenuItem.Click += new System.EventHandler(this.методQCToolStripMenuItem_Click);
            // 
            // методQCleftDownToolStripMenuItem
            // 
            this.методQCleftDownToolStripMenuItem.Name = "методQCleftDownToolStripMenuItem";
            this.методQCleftDownToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.методQCleftDownToolStripMenuItem.Text = "метод QC (left - down)";
            this.методQCleftDownToolStripMenuItem.Click += new System.EventHandler(this.методQCleftDownToolStripMenuItem_Click);
            // 
            // методQCrightUpToolStripMenuItem
            // 
            this.методQCrightUpToolStripMenuItem.Name = "методQCrightUpToolStripMenuItem";
            this.методQCrightUpToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.методQCrightUpToolStripMenuItem.Text = "метод QC (right - up)";
            this.методQCrightUpToolStripMenuItem.Click += new System.EventHandler(this.методQCrightUpToolStripMenuItem_Click);
            // 
            // методQCrightDownToolStripMenuItem
            // 
            this.методQCrightDownToolStripMenuItem.Name = "методQCrightDownToolStripMenuItem";
            this.методQCrightDownToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.методQCrightDownToolStripMenuItem.Text = "метод QC (right - down)";
            this.методQCrightDownToolStripMenuItem.Click += new System.EventHandler(this.методQCrightDownToolStripMenuItem_Click);
            // 
            // зумироватьToolStripMenuItem
            // 
            this.зумироватьToolStripMenuItem.Name = "зумироватьToolStripMenuItem";
            this.зумироватьToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.зумироватьToolStripMenuItem.Text = "Зумировать";
            this.зумироватьToolStripMenuItem.Click += new System.EventHandler(this.зумироватьToolStripMenuItem_Click);
            // 
            // автомасштабToolStripMenuItem
            // 
            this.автомасштабToolStripMenuItem.Name = "автомасштабToolStripMenuItem";
            this.автомасштабToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.автомасштабToolStripMenuItem.Text = "Автомасштаб";
            this.автомасштабToolStripMenuItem.Click += new System.EventHandler(this.автомасштабToolStripMenuItem_Click);
            // 
            // просмотрГруппыToolStripMenuItem
            // 
            this.просмотрГруппыToolStripMenuItem.Name = "просмотрГруппыToolStripMenuItem";
            this.просмотрГруппыToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.просмотрГруппыToolStripMenuItem.Text = "Просмотр группы";
            this.просмотрГруппыToolStripMenuItem.Click += new System.EventHandler(this.просмотрГруппыToolStripMenuItem_Click);
            // 
            // печатьToolStripMenuItem
            // 
            this.печатьToolStripMenuItem.Name = "печатьToolStripMenuItem";
            this.печатьToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.печатьToolStripMenuItem.Text = "Печать";
            this.печатьToolStripMenuItem.Click += new System.EventHandler(this.печатьToolStripMenuItem_Click);
            // 
            // printDocument1
            // 
            this.printDocument1.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument1_PrintPage);
            // 
            // FormPlot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(999, 657);
            this.Controls.Add(this.pictureBox_plot);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormPlot";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Визуализация экспериментальных данных";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.plot_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_plot)).EndInit();
            this.contextMenuOnPlot.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox_plot;
        private System.Windows.Forms.ContextMenuStrip contextMenuOnPlot;
        private System.Windows.Forms.ToolStripMenuItem отбраковкаToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem зумToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem печатьToolStripMenuItem;
        private System.Drawing.Printing.PrintDocument printDocument1;
        private System.Windows.Forms.ToolStripMenuItem отбраковатьToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem зумироватьToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem автомасштабToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem измерениеToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem просмотрГруппыToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem методGCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem методIGCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem методQCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem методQCleftDownToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem методQCrightUpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem методQCrightDownToolStripMenuItem;

    }
}