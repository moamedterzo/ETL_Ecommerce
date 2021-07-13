using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETLSoftware.DataExtract
{
    /// <summary>
    /// Classe che si occupa di leggere e restituire i dati delle righe da un file excel
    /// </summary>
    public class ExcelReader
    {
        private string PathFile { get; set; }

        private ISheet mainSheet { get; set; }

        /// <summary>
        /// Utilizzato a fini informativi per l'utente
        /// </summary>
        public int NumeroTotaleRighe { get; set; }

        public ExcelReader(string pathFile)
        {
            this.PathFile = pathFile;

            IWorkbook workbook = new XSSFWorkbook(this.PathFile);

            //il primo foglio di lavoro è quello che contiene i dati
            mainSheet = workbook.GetSheetAt(0);

            //controllo riga intestazione
            IRow rigaIntestazione = mainSheet.GetRow(0);
            if (rigaIntestazione.GetCell(0).StringCellValue != "IdOrdine"
                || rigaIntestazione.GetCell(1).StringCellValue != "DataOrdine"
                || rigaIntestazione.GetCell(2).StringCellValue != "CodStatoFattura"
                || rigaIntestazione.GetCell(3).StringCellValue != "CodProvinciaFattura"
                || rigaIntestazione.GetCell(4).StringCellValue != "Sesso"
                || rigaIntestazione.GetCell(5).StringCellValue != "Quantita"
                || rigaIntestazione.GetCell(6).StringCellValue != "Prezzo"
                || rigaIntestazione.GetCell(7).StringCellValue != "NomeDes"
                || rigaIntestazione.GetCell(8).StringCellValue != "LinguaCollezione"
                || rigaIntestazione.GetCell(9).StringCellValue != "LinguaColore"
                || rigaIntestazione.GetCell(10).StringCellValue != "NomeSes"
                || rigaIntestazione.GetCell(11).StringCellValue != "PagamentoOrdine"
                || rigaIntestazione.GetCell(12).StringCellValue != "ValoreTagliaEffettivo"
                || rigaIntestazione.GetCell(13).StringCellValue != "NomeCat"
                || rigaIntestazione.GetCell(14).StringCellValue != "NomeMac")
            {
                throw new Exception("Intestazione del file specificato errata, si prega di controllare!");
            }

            //ottengo numero righe
            this.NumeroTotaleRighe = mainSheet.LastRowNum;
        }

        public IEnumerable<Row> GetFileRows()
        {       
            for (int i = 1; i <= mainSheet.LastRowNum; i++)
            {
                IRow rowInput = mainSheet.GetRow(i);

                yield return new Row(rowInput);
            }
        }
    }
}
