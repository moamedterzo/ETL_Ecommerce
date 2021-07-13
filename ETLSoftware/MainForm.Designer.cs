namespace ETLSoftware
{
    partial class MainForm
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
            this.btnCaricaFile = new System.Windows.Forms.Button();
            this.lblRisultato = new System.Windows.Forms.Label();
            this.lblStato = new System.Windows.Forms.Label();
            this.linkJaspersoft = new System.Windows.Forms.LinkLabel();
            this.lblNomeFile = new System.Windows.Forms.Label();
            this.btnInterrompi = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCaricaFile
            // 
            this.btnCaricaFile.Location = new System.Drawing.Point(104, 58);
            this.btnCaricaFile.Name = "btnCaricaFile";
            this.btnCaricaFile.Size = new System.Drawing.Size(151, 40);
            this.btnCaricaFile.TabIndex = 0;
            this.btnCaricaFile.Text = "Carica file";
            this.btnCaricaFile.UseVisualStyleBackColor = true;
            this.btnCaricaFile.Click += new System.EventHandler(this.btnCaricaFile_Click);
            // 
            // lblRisultato
            // 
            this.lblRisultato.AutoSize = true;
            this.lblRisultato.Location = new System.Drawing.Point(27, 231);
            this.lblRisultato.Name = "lblRisultato";
            this.lblRisultato.Size = new System.Drawing.Size(0, 13);
            this.lblRisultato.TabIndex = 1;
            // 
            // lblStato
            // 
            this.lblStato.AutoSize = true;
            this.lblStato.Location = new System.Drawing.Point(27, 171);
            this.lblStato.Name = "lblStato";
            this.lblStato.Size = new System.Drawing.Size(0, 13);
            this.lblStato.TabIndex = 2;
            // 
            // linkJaspersoft
            // 
            this.linkJaspersoft.AutoSize = true;
            this.linkJaspersoft.Location = new System.Drawing.Point(27, 23);
            this.linkJaspersoft.Name = "linkJaspersoft";
            this.linkJaspersoft.Size = new System.Drawing.Size(121, 13);
            this.linkJaspersoft.TabIndex = 3;
            this.linkJaspersoft.TabStop = true;
            this.linkJaspersoft.Text = "Vai sul server Jaspersoft";
            this.linkJaspersoft.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkJaspersoft_LinkClicked);
            // 
            // lblNomeFile
            // 
            this.lblNomeFile.AutoSize = true;
            this.lblNomeFile.Location = new System.Drawing.Point(27, 116);
            this.lblNomeFile.Name = "lblNomeFile";
            this.lblNomeFile.Size = new System.Drawing.Size(0, 13);
            this.lblNomeFile.TabIndex = 4;
            // 
            // btnInterrompi
            // 
            this.btnInterrompi.Location = new System.Drawing.Point(297, 58);
            this.btnInterrompi.Name = "btnInterrompi";
            this.btnInterrompi.Size = new System.Drawing.Size(136, 40);
            this.btnInterrompi.TabIndex = 5;
            this.btnInterrompi.Text = "Interrompi procedura";
            this.btnInterrompi.UseVisualStyleBackColor = true;
            this.btnInterrompi.Visible = false;
            this.btnInterrompi.Click += new System.EventHandler(this.btnInterrompi_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(505, 447);
            this.Controls.Add(this.btnInterrompi);
            this.Controls.Add(this.lblNomeFile);
            this.Controls.Add(this.linkJaspersoft);
            this.Controls.Add(this.lblStato);
            this.Controls.Add(this.lblRisultato);
            this.Controls.Add(this.btnCaricaFile);
            this.Name = "MainForm";
            this.Text = "ETL Software";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCaricaFile;
        private System.Windows.Forms.Label lblRisultato;
        private System.Windows.Forms.Label lblStato;
        private System.Windows.Forms.LinkLabel linkJaspersoft;
        private System.Windows.Forms.Label lblNomeFile;
        private System.Windows.Forms.Button btnInterrompi;
    }
}