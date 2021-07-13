using NPOI.SS.UserModel;
using System;

namespace ETLSoftware.DataExtract
{
    /// <summary>
    /// Rappresenta le informazioni di una singola riga letta
    /// Il compito di determinarne la validità semantica spetta ad altre classi, così come l'interpretazione dei dati
    /// </summary>
    public class Row
    {
        //Posizione delle colonne all'interno del file excel
        private static readonly int INDEX_ID_ORDINE = 0;
        private static readonly int INDEX_DATA_ORDINE = 1;
        private static readonly int INDEX_STATO_FATTURAZ = 2;
        private static readonly int INDEX_PROVINCIA_FATTURAZ = 3;

        //private static readonly int INDEX_ID_CLIENTE = 4;
        private static readonly int INDEX_SESSO_CLIENTE = 4;
        private static readonly int INDEX_QUANTITA = 5;
        private static readonly int INDEX_PREZZO_UNITARIO = 6;

        private static readonly int INDEX_NOME_DESIGN = 7;
        private static readonly int INDEX_LINGUA_COLLEZIONE = 8;
        private static readonly int INDEX_LINGUA_COLORE = 9;
        private static readonly int INDEX_SESSO_ABBIGLIAMENTO = 10;

        private static readonly int INDEX_TIPO_PAGAMENTO = 11;
        private static readonly int INDEX_VALORE_TAGLIA = 12;
        private static readonly int INDEX_NOME_CATEGORIA = 13;
        private static readonly int INDEX_NOME_MACROCATEGORIA = 14;

        /// <summary>
        /// Dalla riga excel passata vengono estratti i valori
        /// </summary>
        /// <param name="iRow"></param>
        public Row(IRow iRow)
        {
            //posizione della riga
            this.Index = iRow.RowNum;

            //vengono prelevati i vari valori, se il valore non è presente o è scorretto, la proprietà inerente non viene valorizzata
            try
            {
                this.IDOrdine = (int?)iRow.GetCell(INDEX_ID_ORDINE).NumericCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

            try
            {
                this.DataOrdine = iRow.GetCell(INDEX_DATA_ORDINE).DateCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

            try
            {
                this.CodiceStatoFatturaz = iRow.GetCell(INDEX_STATO_FATTURAZ).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

            try
            {
                this.ProvinciaFatturaz = iRow.GetCell(INDEX_PROVINCIA_FATTURAZ).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.SessoCliente = (int?)iRow.GetCell(INDEX_SESSO_CLIENTE).NumericCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

            try
            {
                this.QuantitaVenduta = (int?)iRow.GetCell(INDEX_QUANTITA).NumericCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

            try
            {
                this.PrezzoUnitario = (decimal?)iRow.GetCell(INDEX_PREZZO_UNITARIO).NumericCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.NomeDesign = iRow.GetCell(INDEX_NOME_DESIGN).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.LinguaCollezione = iRow.GetCell(INDEX_LINGUA_COLLEZIONE).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.LinguaColore = iRow.GetCell(INDEX_LINGUA_COLORE).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.SessoAbbigliamento = iRow.GetCell(INDEX_SESSO_ABBIGLIAMENTO).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.TipoPagamento = iRow.GetCell(INDEX_TIPO_PAGAMENTO).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.ValoreTagliaEffettivo = iRow.GetCell(INDEX_VALORE_TAGLIA).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

            //il valore taglia può essere sia un valore testuale che numerico, pertanto cerco di ottenere nuovamente il valore qualora il valore testuale non sia stato preso
            if (string.IsNullOrWhiteSpace(this.ValoreTagliaEffettivo))
            {
                try
                {
                    this.ValoreTagliaEffettivo = iRow.GetCell(INDEX_VALORE_TAGLIA).NumericCellValue.ToString();
                }
                catch (InvalidOperationException) { }
                catch (NullReferenceException) { }
            }

            try
            {
                this.NomeCategoria = iRow.GetCell(INDEX_NOME_CATEGORIA).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }


            try
            {
                this.NomeMacrocategoria = iRow.GetCell(INDEX_NOME_MACROCATEGORIA).StringCellValue;
            }
            catch (InvalidOperationException) { }
            catch (NullReferenceException) { }

        }

        public int? IDOrdine { get; set; }

        public DateTime? DataOrdine { get; set; }

        public string CodiceStatoFatturaz { get; set; }

        public string ProvinciaFatturaz { get; set; }

        public int? SessoCliente { get; set; }

        public int? QuantitaVenduta { get; set; }

        public decimal? PrezzoUnitario { get; set; }

        public string NomeDesign { get; set; }

        public string LinguaCollezione { get; set; }

        public string LinguaColore { get; set; }

        public string SessoAbbigliamento { get; set; }

        public string TipoPagamento { get; set; }

        public string ValoreTagliaEffettivo { get; set; }

        public string NomeCategoria { get; set; }

        public string NomeMacrocategoria { get; set; }

        public int Index { get; private set; }

        public override string ToString()
        {
            return string.Format("riga alla posizione {0}{1}", Index, (IDOrdine.HasValue ? ", id ordine " + IDOrdine.Value : ""));
        }
    }
}
