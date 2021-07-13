using ETLSoftware.DataTransform;
using System;
using System.Threading;
using System.Windows.Forms;

namespace ETLSoftware
{
    public partial class MainForm : Form, DataTransformator.DataTransformatorListener
    {
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// percorso del file
        /// </summary>
        string pathFile;

        /// <summary>
        /// utilizzato per interrompere la procedura di importazione, su richiesta
        /// </summary>
        bool interrompiProcedura;

        private void btnCaricaFile_Click(object sender, EventArgs e)
        {
            //mostro un dialog che fa selezionare il file da importare
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                if (!openFileDialog.FileName.EndsWith(".xlsx"))
                {
                    this.lblNomeFile.Text = "Selezionare un file con estensione .xlsx";
                }
                else
                {
                    //resetto labels
                    this.lblRisultato.Text = "";
                    this.lblStato.Text = "";

                    interrompiProcedura = false;
                    pathFile = openFileDialog.FileName;

                    //mostro nome file
                    this.lblNomeFile.Text = "File selezionato: " + pathFile;

                    btnInterrompi.Visible = true;

                    //operazioni di importazione eseguite su un altro thread per non bloccare il thread grafico
                    Thread thread = new Thread(new ThreadStart(CaricaDati));
                    thread.Start();
                }
            }
        }

        private void CaricaDati()
        {
            try
            {
                DataTransformator.ReadAndStoreData(pathFile, this);

                if (this.InvokeRequired)
                {
                    // We're on a thread other than the GUI thread
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.lblRisultato.Text = "Operazione effettuata con successo!";
                    }));
                }
                else
                {
                    this.lblRisultato.Text = "Operazione effettuata con successo!";
                }
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired)
                {
                    // We're on a thread other than the GUI thread
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.lblRisultato.Text = "Si è verificato un errore\nMessaggio: " + ex.Message + "\nStacktrace: " + ex.StackTrace;
                    }));
                }
                else
                {
                    this.lblRisultato.Text = "Si è verificato un errore\nMessaggio: " + ex.Message + "\nStacktrace: " + ex.StackTrace;
                }
            }
        }

        public void OnNextRow(int rowIndex, int totalRows)
        {
            if (this.InvokeRequired)
            {
                // We're on a thread other than the GUI thread
                this.Invoke(new MethodInvoker(() =>
                {
                    this.lblStato.Text = string.Format("Righe elaborate: {0} su {1}", rowIndex, totalRows);
                }));
            }
            else
            {
                this.lblStato.Text = string.Format("Righe elaborate: {0} su {1}", rowIndex, totalRows);
            }
        }

        /// <summary>
        /// Porta al link del server jaspersoft
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkJaspersoft_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://localhost:8080/jasperserver-pro/flow.html?_flowId=homeFlow");
        }

        private void btnInterrompi_Click(object sender, EventArgs e)
        {
            interrompiProcedura = true;
        }

        public bool GetInterrompi()
        {
            return interrompiProcedura;
        }
    }
}
