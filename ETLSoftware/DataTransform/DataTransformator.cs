using ETLSoftware.DataExtract;
using System;
using System.Collections.Generic;
using System.Linq;
using ETLSoftware.DWLayer;
using ETLSoftware.Utility;
using System.Text.RegularExpressions;
using System.Diagnostics;

using DBContext = ETLSoftware.DWLayer.DWVenditeEntities;

namespace ETLSoftware.DataTransform
{
    public class DataTransformator
    {
        private static readonly string NON_SPECIFICATO_MASCHILE = "Non specificato";
        private static readonly string NON_SPECIFICATO_FEMMINILE = "Non specificata";

        private static readonly string NOME_NAZIONE_ITALIA = "Italia";

        //classe che scrive su file dei log
        static Logger logger = new Logger();

        //Range delle dimensioni Data
        static DateTime? dimenDataMinima = null;
        static DateTime? dimenDataMassima = null;


        /// <summary>
        /// Metodo principale che legge i dati e li memorizza nel datawarehouse
        /// </summary>
        public static void ReadAndStoreData(string pathFile, DataTransformatorListener listener)
        {
            //collegamento al datawarehouse
            using (var context = new DBContext())
            {
                //Ottengo il range della dimensione Data minima e massima,
                //in modo da aggiungerne delle nuove qualora vengano inclusi Fatti che non rientrino nel range
                if (context.DimenData.Any())
                {
                    dimenDataMinima = context.DimenData.Select(d => d.DataValore).OrderBy(d => d).FirstOrDefault();
                    dimenDataMassima = context.DimenData.Select(d => d.DataValore).OrderByDescending(d => d).FirstOrDefault();
                }

                //lista degli ordini da eliminare a causa di inesattezze dei dati
                List<int> idOrdiniDaEliminare = new List<int>();

                //lettura dei dati dal file excel
                ExcelReader excelReader = new ExcelReader(pathFile);

                //variabili che conterranno le varie informazioni delle righe
                bool firstRow = true;
                bool okCheckInconsistenze;
                List<FattoVendita> venditeStessoOrdine;
                FattoVendita checkExistFattoVendita = null;

                int? idDimenStato;
                string nomeDimenStato;
                int idDimenData;
                int idDimenProvincia;
                int idDimenSessoCliente;
                int idDimenNomeDesign;
                int idDimenLinguaCollezione;
                int idDimenLinguaColore;
                int idDimenSessoAbbigliamento;
                int idDimenTipoPagamento;
                int idDimenValoreTaglia;

                int[] valoriCategMacrocateg;
                int idDimenCategoria;
                int idDimenMacrocategoria;

                int quantita;
                decimal prezzoTotale;

#if(DEBUG)
                //utilizzato per monitorare le prestazioni
                Stopwatch stopwatch = new Stopwatch();
#endif

                foreach (var row in excelReader.GetFileRows())
                {
                    //se avviene un'eccezione a livello della singola riga, non termino la procedura, bensì continuo avanti effettuando il log dell'eccezione
                    try
                    {
#if (DEBUG)
                        stopwatch.Reset();
                        stopwatch.Restart();
#endif
                        //ogni 100 righe notifico il listener
                        if (row.Index % 100 == 0)
                        {
                            listener.OnNextRow(row.Index, excelReader.NumeroTotaleRighe);
                        }

                        if (listener.GetInterrompi())
                        {
                            //interruzione su richiesta
                            break;
                        }


                        if (CheckValoriObbligatoriRiga(context, row))
                        {
                            //OTTENIMENTO ID DIMENSIONI
                            idDimenStato = getIDDimenStato(context, row.CodiceStatoFatturaz, out nomeDimenStato);
                            if (idDimenStato.HasValue)
                            {
                                idDimenData = getIDDimenData(context, row.DataOrdine.Value);
                                idDimenProvincia = getIDDimenProvincia(context, row.ProvinciaFatturaz, nomeDimenStato);

                                //Controllo se esiste un altro fatto avente lo stesso IDOrdine, ma differente Data, Stato o Provincia
                                //In questo caso non viene importato nessun fatto dell'ordine, in quanto non è possibile stabilire con esattezza quali siano le informazioni corrette
                                okCheckInconsistenze = true;

                                venditeStessoOrdine = context.FattoVendita.Where(f => f.IDOrdine == row.IDOrdine.Value).ToList();
                                foreach (FattoVendita fattoVenditaStessoOrdine in venditeStessoOrdine)
                                {
                                    if (idDimenData != fattoVenditaStessoOrdine.IDData
                                        || idDimenStato != fattoVenditaStessoOrdine.IDStato
                                        || idDimenProvincia != fattoVenditaStessoOrdine.IDProvincia)
                                    {
                                        //memorizzo in una lista l'ID dell'ordine, alla fine dell'importazione dei dati verranno rimosse le vendite aventi lo stesso IDOrdine
                                        if (!idOrdiniDaEliminare.Contains(row.IDOrdine.Value))
                                        {
                                            idOrdiniDaEliminare.Add(row.IDOrdine.Value);
                                        }

                                        okCheckInconsistenze = false;
                                        break;
                                    }
                                }

                                if (!okCheckInconsistenze)
                                {
                                    //è inutile proseguire, non verrà inserito questo fatto nel DB
                                    logger.WriteLog("Riga non importata - Inconsistenze con altre righe dello stesso ordine: " + row.ToString());
                                    continue;
                                }

                                idDimenSessoCliente = getIDDimenSessoCliente(context, row.SessoCliente);
                                idDimenNomeDesign = getIDDimenNomeDesign(context, row.NomeDesign);
                                idDimenLinguaCollezione = getIDDimenLinguaCollezione(context, row.LinguaCollezione);
                                idDimenLinguaColore = getIDDimenLinguaColore(context, row.LinguaColore);
                                idDimenSessoAbbigliamento = getIDDimenSessoAbbigliamento(context, row.SessoAbbigliamento);
                                idDimenTipoPagamento = getIDDimenTipoPagamento(context, row.TipoPagamento);
                                idDimenValoreTaglia = getIDDimenValoreTaglia(context, row.ValoreTagliaEffettivo);

                                valoriCategMacrocateg = getIDDimenNomeCategoriaMacrocategoria(context, row.NomeCategoria, row.NomeMacrocategoria);
                                idDimenCategoria = valoriCategMacrocateg[0];
                                idDimenMacrocategoria = valoriCategMacrocateg[1];

                                //CREAZIONE FATTO
                                //controllo se esiste già un fatto avente le stesse dimensioni (caso dei resi aventi misure negative)
                                checkExistFattoVendita = context.FattoVendita.FirstOrDefault(f =>

                                    f.IDMacrocategoria == idDimenMacrocategoria &&
                                    f.IDCategoria == idDimenCategoria &&
                                    f.IDLinguaCollezione == idDimenLinguaCollezione &&
                                    f.IDLinguaColore == idDimenLinguaColore &&

                                    f.IDNomeDesign == idDimenNomeDesign &&
                                    f.IDSessoAbbigliamento == idDimenSessoAbbigliamento &&
                                    f.IDSessoCliente == idDimenSessoCliente &&

                                    f.IDValoreTaglia == idDimenValoreTaglia &&
                                    f.IDTipoPagamento == idDimenTipoPagamento &&

                                    f.IDData == idDimenData &&
                                    f.IDOrdine == row.IDOrdine.Value);


                                //CONTROLLO SOLTATO PER LA PRIMA RIGA IMPORTATA SE ESISTE GIA' UN FATTO CONTENENTE LE STESSE DIMENSIONI
                                //in caso affermativo, vuol dire che il file di importazione è stato già elaborato in precedenza, e pertanto la procedura avrà termine
                                if (firstRow)
                                {
                                    firstRow = false;

                                    if (checkExistFattoVendita != null)
                                    {
                                        //ERRORE, la procedura viene terminata
                                        throw new Exception("Procedura terminata, il file da importare è stato già processato in precedenza");
                                    }
                                }

                                //calcolo misure
                                quantita = (int)(row.QuantitaVenduta.Value * -1);
                                prezzoTotale = row.PrezzoUnitario.Value * quantita;

                                //se la quantità specificata è 0, la considero come se fosse un'unità
                                if (quantita == 0)
                                    quantita = 1;

#if (DEBUG)
                                stopwatch.Stop();
                                logger.WriteLog(string.Format("Riga {0} before insert, tempo:\t {1}", row.Index, stopwatch.ElapsedMilliseconds));
                                stopwatch.Restart();
#endif

                                //se esiste già un fatto vendita con le stesse dimensioni, provvedo a sommare le misure quantità e prezzo
                                if (checkExistFattoVendita != null)
                                {
                                    //Qualora la quantità sia negativa, vorrà dire che è un Reso, pertanto verranno sommato valori negativi
                                    checkExistFattoVendita.QuantitaVenduta += quantita;
                                    checkExistFattoVendita.PrezzoTotale += prezzoTotale;
                                    context.SaveChanges();
                                }
                                else
                                {
                                    //aggiunta nel db
                                    context.createFattoVendita(idDimenMacrocategoria, idDimenCategoria,
                                        idDimenSessoCliente, idDimenData, idDimenLinguaCollezione, idDimenLinguaColore,
                                        idDimenNomeDesign, idDimenProvincia, idDimenSessoAbbigliamento, idDimenStato, idDimenTipoPagamento,
                                        idDimenValoreTaglia, row.IDOrdine, prezzoTotale, quantita);
                                }
                            }
                            else
                            {
                                //id stato non mappato 
                                logger.WriteLog("Riga non importata - IDStato presente ma errato: " + row.ToString());
                            }
#if(DEBUG)
                            stopwatch.Stop();
                            logger.WriteLog(string.Format("Riga {0} creaz fatto, tempo:\t {1}", row.Index, stopwatch.ElapsedMilliseconds));
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        //log eccezione
                        logger.WriteLog("Riga non importata - eccezione avvenuta: " + ex.Message + "\n" + ex.StackTrace);
                    }

                    //dopo 2500 righe...
                    if (row.Index % 2500 == 0)
                    {
                        //pulizia della memoria
                        GC.Collect();
                    }
                }

                //notifico successo elaborazione righe
                listener.OnNextRow(excelReader.NumeroTotaleRighe, excelReader.NumeroTotaleRighe);

                //RIMOZIONE DATI INCONSISTENTI
                //Vengono eliminati i fatti Vendita inconsistenti
                EliminaOrdiniInconsistenti(idOrdiniDaEliminare, context);

                //AGGIUSTAMENTI FINALI
                //1. Potrebbe succedere che alcune dimensioni di tipo Categoria abbiano come Macrocategoria "Non specificato"
                //Viene controllato se esistano altre dimensioni con lo stesso nome categoria, e nel caso viene impostato il valore della loro macrocategoria
                SistemaNomeMacrocategorie(context);

                //2. Elimino i fatti che possiedono Prezzo pari a 0, in quanto sarebbero solamente dei record inutili
                EliminaFattiVenditaInutili(context);

                //3. determino l'ordinamento delle collezioni, in modo da poter mostrare grafici ordinati in base alla temporalità
                SetOrdinamentoCollezioni(context);
            }

        }

        #region OTTENIMENTO DIMENSIONI

        /// <summary>
        /// Ottenimento dimensione data
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_codiceProvincia"></param>
        /// <returns></returns>
        private static int getIDDimenData(DBContext context, DateTime data)
        {
            DimenData dimenData;

            //se non è valorizzato il range delle date, vuol dire che ci troviamo al primo inserimento di date
            if (!dimenDataMassima.HasValue)
            {
                dimenDataMassima = data;
                dimenDataMinima = data;

                //creazione dimensione e salvataggio
                dimenData = new DimenData()
                {
                    DataValore = data
                };
                context.DimenData.Add(dimenData);
                context.SaveChanges();
            }
            else
            {
                //il range esiste, controllo che la data passata rientri nel range
                if (dimenDataMinima.Value.Date > data.Date)
                {
                    //creazione dimensione e salvataggio delle date antecedenti alla data minima
                    dimenData = new DimenData()
                    {
                        DataValore = data
                    };
                    context.DimenData.Add(dimenData);
                    context.SaveChanges();


                    //aggiungere nuove dimensioni data (tante date quanti sono i giorni di differenza)
                    DateTime dataDaInserire = data.AddDays(1);

                    while (dimenDataMinima.Value.Date > dataDaInserire)
                    {
                        context.DimenData.Add(new DimenData()
                        {
                            DataValore = dataDaInserire
                        });
                        context.SaveChanges();

                        //prossima data
                        dataDaInserire = dataDaInserire.AddDays(1);
                    }

                    //aggiornamento data minima
                    dimenDataMinima = data;
                }
                else if (dimenDataMassima.Value.Date < data.Date)
                {
                    //creazione dimensione e salvataggio delle date superiori alla data massima
                    dimenData = new DimenData()
                    {
                        DataValore = data
                    };
                    context.DimenData.Add(dimenData);
                    context.SaveChanges();


                    //aggiungere nuove dimensioni data (tante date quanti sono i giorni di differenza)
                    DateTime dataDaInserire = data.AddDays(-1);

                    while (dimenDataMassima.Value.Date < dataDaInserire)
                    {
                        context.DimenData.Add(new DimenData()
                        {
                            DataValore = dataDaInserire
                        });
                        context.SaveChanges();

                        //prossima data
                        dataDaInserire = dataDaInserire.AddDays(-1);
                    }

                    //aggiornamento data massima
                    dimenDataMassima = data;
                }
                else
                {
                    //la data rientra nel range, pertanto cerco la dimensione nel DB
                    //la ricerco tramite la stored procedure
                    return context.getIDDimenData(data).First().Value;
                }
            }

            return dimenData.ID;
        }


        /// <summary>
        /// Ottenimento dimensione provincia
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_codiceProvincia"></param>
        /// <returns></returns>
        private static int getIDDimenProvincia(DBContext context, string _codiceProvincia, string nomeDimenStato)
        {
            string codiceProvincia = string.IsNullOrEmpty(_codiceProvincia) ? NON_SPECIFICATO_FEMMINILE : _codiceProvincia.ToUpper();
            
            //controllo se la provincia sia corretta
            //per gli acquisti effettuati in italia si può effettuare un controllo circa la validità o meno della provincia, controllando i dati già inseriti
            //cosa che per gli stati esteri non si può effettuare, e si prende sempre per buono che la provincia inserita sia valida
            DimenProvincia dimenProvincia = context.DimenProvincia.FirstOrDefault(d => d.Nome == codiceProvincia && d.NomeStato.ToUpper() == nomeDimenStato);

            if (dimenProvincia == null)
            {
                if (nomeDimenStato == NOME_NAZIONE_ITALIA)
                {
                    //CASO ITALIA
                    //se non esiste prendo come riferimento la provincia "Non specificata"
                    dimenProvincia = context.DimenProvincia.First(d => d.NomeStato.ToUpper() == nomeDimenStato && d.Nome == NON_SPECIFICATO_FEMMINILE);
                }
                else
                {
                    //CASO ESTERO
                    //creazione dimensione e salvataggio
                    dimenProvincia = new DimenProvincia()
                    {
                        Nome = codiceProvincia,
                        NomeStato = nomeDimenStato
                    };
                    context.DimenProvincia.Add(dimenProvincia);
                    context.SaveChanges();
                }
            }

            return dimenProvincia.ID;
        }

        /// <summary>
        /// Ottenimento dimensione stato
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_sessoCliente"></param>
        /// <returns></returns>
        private static int? getIDDimenStato(DBContext context, string _codiceStato, out string nomeDimenStato)
        {
            DimenStato dimenStato = context.DimenStato.FirstOrDefault(c => c.CodiceISO == _codiceStato.ToUpper());
            if (dimenStato == null)
            {
                nomeDimenStato = null;
                return null;
            }
            else
            {
                nomeDimenStato = dimenStato.Nome;
                return dimenStato.ID;
            }
        }

        /// <summary>
        /// Ottenimento dimensione sesso cliente
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_sessoCliente"></param>
        /// <returns></returns>
        private static int getIDDimenSessoCliente(DBContext context, int? _sessoCliente)
        {
            string sessoCliente = _sessoCliente == 3 ? "Maschio" : _sessoCliente == 4 ? "Femmina" : NON_SPECIFICATO_MASCHILE;

            DimenSessoCliente dimenSessoCliente = context.DimenSessoCliente.FirstOrDefault(c => c.Nome == sessoCliente);
            if (dimenSessoCliente == null)
            {
                //creazione dimensione e salvataggio
                dimenSessoCliente = new DimenSessoCliente()
                {
                    Nome = sessoCliente
                };
                context.DimenSessoCliente.Add(dimenSessoCliente);
                context.SaveChanges();
            }

            return dimenSessoCliente.ID;
        }

        /// <summary>
        /// Ottenimento dimensione nome design
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_nomeDesign"></param>
        /// <returns></returns>
        private static int getIDDimenNomeDesign(DBContext context, string _nomeDesign)
        {
            string nomeDesign = string.IsNullOrEmpty(_nomeDesign) ? NON_SPECIFICATO_MASCHILE : _nomeDesign;

            DimenNomeDesign dimenNomeDesign = context.DimenNomeDesign.FirstOrDefault(d => d.Nome == nomeDesign);
            if (dimenNomeDesign == null)
            {
                //creazione dimensione e salvataggio
                dimenNomeDesign = new DimenNomeDesign()
                {
                    Nome = nomeDesign
                };
                context.DimenNomeDesign.Add(dimenNomeDesign);
                context.SaveChanges();
            }

            return dimenNomeDesign.ID;
        }

        /// <summary>
        /// Ottenimento dimensione lingua colore
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_linguaColore"></param>
        /// <returns></returns>
        private static int getIDDimenLinguaColore(DBContext context, string _linguaColore)
        {
            string linguaColore;

            if (string.IsNullOrEmpty(_linguaColore))
            {
                linguaColore = NON_SPECIFICATO_FEMMINILE;
            }
            else
            {
                //vengono tolti eventuali caratteri come '-' oppure i doppi spazi oppure spazi vuoti all'inizio e fine della parola
                linguaColore = _linguaColore.Replace("-", " ").Trim();

                //in maniera ricorsiva vengono eliminati i doppi spazi, fintantoché la stringa non risulterà del tutto "pulita"
                string oldLinguaColore = linguaColore;

                while ((linguaColore = linguaColore.Replace("  ", " ")) != oldLinguaColore)
                {
                    oldLinguaColore = linguaColore;
                }

                //la stringa conterrà soltanto il primo carattere maiuscolo, il resto sarà minuscolo
                linguaColore = linguaColore[0].ToString().ToUpper() + linguaColore.Substring(1, linguaColore.Length - 1).ToLower();
            }

            DimenLinguaColore dimenLinguaColore = context.DimenLinguaColore.FirstOrDefault(d => d.Nome == linguaColore);
            if (dimenLinguaColore == null)
            {
                //creazione dimensione e salvataggio
                dimenLinguaColore = new DimenLinguaColore()
                {
                    Nome = linguaColore
                };
                context.DimenLinguaColore.Add(dimenLinguaColore);
                context.SaveChanges();
            }

            return dimenLinguaColore.ID;
        }

        /// <summary>
        /// Ottenimento dimensione sesso abbigliamento
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_sessoAbbigliamento"></param>
        /// <returns></returns>
        private static int getIDDimenSessoAbbigliamento(DBContext context, string _sessoAbbigliamento)
        {
            string sessoAbbigliamento = string.IsNullOrEmpty(_sessoAbbigliamento) ? NON_SPECIFICATO_MASCHILE : _sessoAbbigliamento;

            DimenSessoAbbigliamento dimenSessoAbbigliamento = context.DimenSessoAbbigliamento.FirstOrDefault(d => d.Nome == sessoAbbigliamento);
            if (dimenSessoAbbigliamento == null)
            {
                //creazione dimensione e salvataggio
                dimenSessoAbbigliamento = new DimenSessoAbbigliamento()
                {
                    Nome = sessoAbbigliamento
                };
                context.DimenSessoAbbigliamento.Add(dimenSessoAbbigliamento);
                context.SaveChanges();
            }

            return dimenSessoAbbigliamento.ID;
        }




        /// <summary>
        /// Ottenimento dimensione lingua collezione
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_linguaCollezione"></param>
        /// <returns></returns>
        private static int getIDDimenLinguaCollezione(DBContext context, string _linguaCollezione)
        {
            string linguaCollezione = string.IsNullOrEmpty(_linguaCollezione) ? NON_SPECIFICATO_FEMMINILE : _linguaCollezione;

            DimenLinguaCollezione dimenLinguaCollezione = context.DimenLinguaCollezione.FirstOrDefault(d => d.Nome == linguaCollezione);
            if (dimenLinguaCollezione == null)
            {
                //creazione dimensione e salvataggio
                dimenLinguaCollezione = new DimenLinguaCollezione()
                {
                    Nome = linguaCollezione,
                    Ordinamento = 0 //ordinamento calcolato in seguito
                };
                context.DimenLinguaCollezione.Add(dimenLinguaCollezione);
                context.SaveChanges();
            }

            return dimenLinguaCollezione.ID;
        }


        /// <summary>
        /// Ottenimento dimensione tipo pagamento
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_tipoPagamento"></param>
        /// <returns></returns>
        private static int getIDDimenTipoPagamento(DBContext context, string _tipoPagamento)
        {
            string tipoPagamento = string.IsNullOrEmpty(_tipoPagamento) ? NON_SPECIFICATO_MASCHILE : _tipoPagamento;

            DimenTipoPagamento dimenTipoPagamento = context.DimenTipoPagamento.FirstOrDefault(d => d.Nome == tipoPagamento);
            if (dimenTipoPagamento == null)
            {
                //creazione dimensione e salvataggio
                dimenTipoPagamento = new DimenTipoPagamento()
                {
                    Nome = tipoPagamento
                };
                context.DimenTipoPagamento.Add(dimenTipoPagamento);
                context.SaveChanges();
            }

            return dimenTipoPagamento.ID;
        }


        /// <summary>
        /// Ottenimento dimensione valore taglia
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_valoreTaglia"></param>
        /// <returns></returns>
        private static int getIDDimenValoreTaglia(DBContext context, string _valoreTaglia)
        {
            string valoreTaglia;

            if (string.IsNullOrWhiteSpace(_valoreTaglia))
            {
                valoreTaglia = NON_SPECIFICATO_FEMMINILE;
            }
            else
            {
                //vengono effettuate correzioni dei caratteri
                valoreTaglia = _valoreTaglia.ToUpper().Trim();

                //in maniera ricorsiva vengono eliminati i doppi spazi, fintantoché la stringa non risulterà del tutto "pulita"
                string oldLinguaColore = _valoreTaglia.ToUpper();

                while ((valoreTaglia = valoreTaglia.Replace("  ", " ")) != oldLinguaColore)
                {
                    oldLinguaColore = valoreTaglia;
                }

            }

            DimenValoreTaglia dimenValoreTaglia = context.DimenValoreTaglia.FirstOrDefault(d => d.Nome == valoreTaglia);
            if (dimenValoreTaglia == null)
            {
                //creazione dimensione e salvataggio
                dimenValoreTaglia = new DimenValoreTaglia()
                {
                    Nome = valoreTaglia
                };
                context.DimenValoreTaglia.Add(dimenValoreTaglia);
                context.SaveChanges();
            }

            return dimenValoreTaglia.ID;
        }


        /// <summary>
        ///  Ottenimento dimensione categoria e macrocategoria
        /// </summary>
        /// <param name="context"></param>
        /// <param name="_nomeCategoria"></param>
        /// <param name="_nomeMacrocategoria"></param>
        /// <returns>Primo valore=IDCategoria; Secondo valore=IDMacrocategoria</returns>
        private static int[] getIDDimenNomeCategoriaMacrocategoria(DBContext context, string _nomeCategoria, string _nomeMacrocategoria)
        {
            string nomeCategoria = string.IsNullOrEmpty(_nomeCategoria) ? NON_SPECIFICATO_FEMMINILE : _nomeCategoria;

            //ottengo la dimensione categoria basandomi sia sul nome della categoria che su quello della macrocategoria
            DimenCategoria dimenCategoria;
            if (string.IsNullOrWhiteSpace(_nomeMacrocategoria))
            {
                //utilizzo solo la categoria per la ricerca cercandone una che abbia il NomeMacrocategoria impostato
                dimenCategoria = context.DimenCategoria.FirstOrDefault(d => d.Nome == nomeCategoria && d.NomeMacrocategoria != NON_SPECIFICATO_FEMMINILE);

                //se non esiste provo a vedere se esiste quella con macrocategoria non specificata   
                if (dimenCategoria == null)
                {
                    dimenCategoria = context.DimenCategoria.FirstOrDefault(d => d.Nome == nomeCategoria);
                }
            }
            else
            {
                //utilizzo anche la macrocategoria per la ricerca
                //in questo modo se non esiste una categoria con la macrocategoria specificata, verrà creata
                dimenCategoria = context.DimenCategoria.FirstOrDefault(d => d.Nome == nomeCategoria && d.NomeMacrocategoria == _nomeMacrocategoria);
            }

            //eventuale creazione dimensione categoria e determinazione nomeMacrocategoria
            string nomeMacrocategoria = string.IsNullOrEmpty(_nomeMacrocategoria) ? NON_SPECIFICATO_FEMMINILE : _nomeMacrocategoria;

            if (dimenCategoria == null)
            {
                //creazione dimensione e salvataggio
                dimenCategoria = new DimenCategoria()
                {
                    Nome = nomeCategoria,
                    NomeMacrocategoria = nomeMacrocategoria
                };
                context.DimenCategoria.Add(dimenCategoria);
                context.SaveChanges();
            }
            else if (nomeMacrocategoria == NON_SPECIFICATO_FEMMINILE)
            {
                //il nome della macrocategoria viene preso dalla categoria
                //In questo modo se ci dovesse essere un campo vuoto per la macrocategoria, lo si può ottenere dalla categoria di riferimento
                nomeMacrocategoria = dimenCategoria.NomeMacrocategoria;
            }

            //Ottengo la dimensione macrocategoria
            DimenMacrocategoria dimenNomeMacrocategoria = context.DimenMacrocategoria.FirstOrDefault(d => d.Nome == nomeMacrocategoria);
            if (dimenNomeMacrocategoria == null)
            {
                //creazione dimensione e salvataggio
                dimenNomeMacrocategoria = new DimenMacrocategoria()
                {
                    Nome = nomeMacrocategoria
                };
                context.DimenMacrocategoria.Add(dimenNomeMacrocategoria);
                context.SaveChanges();
            }

            return new int[] { dimenCategoria.ID, dimenNomeMacrocategoria.ID };
        }


        #endregion

        /// <summary>
        /// Elimina i fatti vendita aventi ordini inconsistenti
        /// </summary>
        /// <param name="idOrdiniDaEliminare"></param>
        /// <param name="context"></param>
        private static void EliminaOrdiniInconsistenti(List<int> idOrdiniDaEliminare, DBContext context)
        {
            foreach (int idOrdine in idOrdiniDaEliminare)
            {
                context.FattoVendita.RemoveRange(context.FattoVendita.Where(f => f.IDOrdine == idOrdine));
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Imposta il valore NomeMacrocategoria per le Categorie che possiedono tale valore pari a "Non specificata"
        /// </summary>
        /// <param name="context"></param>
        private static void SistemaNomeMacrocategorie(DBContext context)
        {
            //ottenimento categoria da sistemare
            var categorieDaSistemare = context.DimenCategoria.Where(c => c.NomeMacrocategoria == NON_SPECIFICATO_FEMMINILE).ToList();

            foreach (var categoriaDaSistemare in categorieDaSistemare)
            {
                //cerco una categoria che abbia lo stesso nome delle categorie da sistemare, e che abbia la macrocategoria impostata
                DimenCategoria dimenCategoriaCorretta = context.DimenCategoria.FirstOrDefault(c => c.Nome == categoriaDaSistemare.Nome && c.NomeMacrocategoria != NON_SPECIFICATO_FEMMINILE);

                if (dimenCategoriaCorretta != null)
                {
                    //aggiorno tutti i fatti che hanno come dimensione la Categoria da sistemare
                    List<FattoVendita> fattiVendita = context.FattoVendita.Where(f => f.IDCategoria == categoriaDaSistemare.ID).ToList();
                    foreach (FattoVendita fattoVenditaDaSistemare in fattiVendita)
                    {
                        //controllo se esista un fatto con la stessa chiave primaria, e nel caso sommo le misure
                        FattoVendita checkExistFattoVendita = context.FattoVendita.FirstOrDefault(f =>

                                   f.IDMacrocategoria == fattoVenditaDaSistemare.IDMacrocategoria &&
                                   f.IDCategoria == dimenCategoriaCorretta.ID && //CATEGORIA CORRETTA
                                   f.IDLinguaCollezione == fattoVenditaDaSistemare.IDLinguaCollezione &&
                                   f.IDLinguaColore == fattoVenditaDaSistemare.IDLinguaColore &&

                                   f.IDNomeDesign == fattoVenditaDaSistemare.IDNomeDesign &&
                                   f.IDSessoAbbigliamento == fattoVenditaDaSistemare.IDSessoAbbigliamento &&
                                   f.IDSessoCliente == fattoVenditaDaSistemare.IDSessoCliente &&

                                   f.IDValoreTaglia == fattoVenditaDaSistemare.IDValoreTaglia &&
                                   f.IDTipoPagamento == fattoVenditaDaSistemare.IDTipoPagamento &&

                                   f.IDData == fattoVenditaDaSistemare.IDData &&
                                   f.IDOrdine == fattoVenditaDaSistemare.IDOrdine);

                        if (checkExistFattoVendita != null)
                        {
                            checkExistFattoVendita.QuantitaVenduta += fattoVenditaDaSistemare.QuantitaVenduta;
                            checkExistFattoVendita.PrezzoTotale += fattoVenditaDaSistemare.PrezzoTotale;
                        }
                        else
                        {
                            //creo un nuovo fatto ed elimino quello errato
                            //creo nuovo fatto vendita
                            //può avere misure negative qualora il fatto sia un reso piuttosto che una vendita
                            FattoVendita nuovoFattoVendita = new FattoVendita()
                            {
                                IDMacrocategoria = fattoVenditaDaSistemare.IDMacrocategoria,
                                IDCategoria = dimenCategoriaCorretta.ID, //CATEGORIA CORRETTA

                                IDLinguaCollezione = fattoVenditaDaSistemare.IDLinguaCollezione,
                                IDLinguaColore = fattoVenditaDaSistemare.IDLinguaColore,
                                IDNomeDesign = fattoVenditaDaSistemare.IDNomeDesign,
                                IDSessoAbbigliamento = fattoVenditaDaSistemare.IDSessoAbbigliamento,
                                IDSessoCliente = fattoVenditaDaSistemare.IDSessoCliente,
                                IDValoreTaglia = fattoVenditaDaSistemare.IDValoreTaglia,
                                IDTipoPagamento = fattoVenditaDaSistemare.IDTipoPagamento,

                                IDStato = fattoVenditaDaSistemare.IDStato,
                                IDProvincia = fattoVenditaDaSistemare.IDProvincia,

                                IDData = fattoVenditaDaSistemare.IDData,
                                IDOrdine = fattoVenditaDaSistemare.IDOrdine,

                                PrezzoTotale = fattoVenditaDaSistemare.PrezzoTotale,
                                QuantitaVenduta = fattoVenditaDaSistemare.QuantitaVenduta
                            };

                            //aggiunta nel db
                            context.FattoVendita.Add(nuovoFattoVendita);
                        }

                        //elimino il vecchio fatto
                        context.FattoVendita.Remove(fattoVenditaDaSistemare);
                        context.SaveChanges();
                    }

                    //eliminazione categoria ormai inutile
                    context.DimenCategoria.Remove(categoriaDaSistemare);
                    context.SaveChanges();
                }
            }

            //eliminazione macrocategoria "Non specificata" se quest'ultima non possiede dei fatti associati
            DimenMacrocategoria dimenMacrocategoria = context.DimenMacrocategoria.FirstOrDefault(m => m.Nome == NON_SPECIFICATO_FEMMINILE);
            if (dimenMacrocategoria != null)
            {
                if (!dimenMacrocategoria.FattoVendita.Any())
                {
                    //eliminazione nel db
                    context.DimenMacrocategoria.Remove(dimenMacrocategoria);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Rimuove i fatti Vendita aventi Prezzo pari a 0
        /// </summary>
        /// <param name="context"></param>
        private static void EliminaFattiVenditaInutili(DBContext context)
        {
            context.FattoVendita.RemoveRange(context.FattoVendita.Where(f => f.PrezzoTotale == 0));
            context.SaveChanges();
        }

        /// <summary>
        /// Imposta l'ordinamento temporale delle collezioni
        /// </summary>
        /// <param name="context"></param>
        private static void SetOrdinamentoCollezioni(DBContext context)
        {
            List<DimenLinguaCollezione> lingueCollezioni = context.DimenLinguaCollezione.ToList();
            List<KeyValuePair<int, KeyValuePair<int, bool>>> infoLingueCollezioni = new List<KeyValuePair<int, KeyValuePair<int, bool>>>();

            foreach (DimenLinguaCollezione linguaCollezione in lingueCollezioni)
            {
                //true se è primavera estate, false se è autunno inverno
                bool isAutunnoInverno;

                //anno della collezione
                int anno;

                if (linguaCollezione.Nome.Contains(" pre 2011"))
                {
                    //è una collezione che viene prima del 2011
                    //non è un problema se si imposta come anno 2010, l'importante è l'ordinamento con gli anni delle altre collezioni
                    anno = 2010;
                }
                else
                {
                    //prendo il numero presente nella stringa
                    anno = int.Parse(Regex.Match(linguaCollezione.Nome, @"\d+").Value);
                }

                if (linguaCollezione.Nome.ToLower().Contains("primavera"))
                {
                    isAutunnoInverno = false;
                }
                else
                {
                    isAutunnoInverno = true;
                }

                //aggiunta valore
                infoLingueCollezioni.Add(new KeyValuePair<int, KeyValuePair<int, bool>>(linguaCollezione.ID, new KeyValuePair<int, bool>(anno, isAutunnoInverno)));
            }

            //una volta ottenuta per ogni collezione anno e stagione, provvedo ad ordinare i valori
            //Nello stesso anno la collezione primavera/estate viene prima di quella autunno/inverno
            infoLingueCollezioni = infoLingueCollezioni.OrderBy(l => l.Value.Key).ThenBy(l => l.Value.Value).ToList();

            //imposto l'ordinamento nel DB
            for (short i = 0; i < infoLingueCollezioni.Count; i++)
            {
                lingueCollezioni.First(c => c.ID == infoLingueCollezioni[i].Key).Ordinamento = i;
            }

            context.SaveChanges();
        }


        /// <summary>
        /// Metodo che restituisce "true" se la riga contiene i valori obbligatori necessari
        /// Inoltre scrive nel file di log il messaggio di non importazione della riga
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static bool CheckValoriObbligatoriRiga(DBContext context, Row row)
        {
            if (!row.IDOrdine.HasValue)
            {
                logger.WriteLog("Riga non importata - IDOrdine mancante: " + row.ToString());
                return false;
            }
            else if (!row.DataOrdine.HasValue)
            {
                logger.WriteLog("Riga non importata - DataOrdine mancante: " + row.ToString());
                return false;
            }
            else if (row.DataOrdine.Value.Date > DateTime.Today)
            {
                logger.WriteLog("Riga non importata - DataOrdine maggiore della data attuale (informazione mendace): " + row.ToString());
                return false;
            }
            else if (!row.QuantitaVenduta.HasValue)
            {
                logger.WriteLog("Riga non importata - Quantità mancante: " + row.ToString());
                return false;
            }
            else if (!row.PrezzoUnitario.HasValue)
            {
                logger.WriteLog("Riga non importata - Prezzo mancante: " + row.ToString());
                return false;
            }
            else if (row.PrezzoUnitario.Value < 0)
            {
                logger.WriteLog("Riga non importata - Prezzo minore di 0 (informazione mendace): " + row.ToString());
                return false;
            }
            else if (string.IsNullOrWhiteSpace(row.CodiceStatoFatturaz))
            {
                logger.WriteLog("Riga non importata - CodStatoFatturazione mancante: " + row.ToString());
                return false;
            }
            else
            {
                //ok, tutti i controlli sono stati superati
                return true;
            }

        }

        public interface DataTransformatorListener
        {
            void OnNextRow(int rowIndex, int totalRows);

            bool GetInterrompi();
        }
    }
}

