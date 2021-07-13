//------------------------------------------------------------------------------
// <auto-generated>
//     Codice generato da un modello.
//
//     Le modifiche manuali a questo file potrebbero causare un comportamento imprevisto dell'applicazione.
//     Se il codice viene rigenerato, le modifiche manuali al file verranno sovrascritte.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ETLSoftware.DWLayer
{
    using System;
    using System.Collections.Generic;
    
    public partial class FattoVendita
    {
        public int IDMacrocategoria { get; set; }
        public int IDCategoria { get; set; }
        public int IDSessoCliente { get; set; }
        public int IDData { get; set; }
        public int IDLinguaCollezione { get; set; }
        public int IDLinguaColore { get; set; }
        public int IDNomeDesign { get; set; }
        public int IDProvincia { get; set; }
        public int IDSessoAbbigliamento { get; set; }
        public int IDStato { get; set; }
        public int IDTipoPagamento { get; set; }
        public int IDValoreTaglia { get; set; }
        public int IDOrdine { get; set; }
        public decimal PrezzoTotale { get; set; }
        public int QuantitaVenduta { get; set; }
    
        public virtual DimenCategoria DimenCategoria { get; set; }
        public virtual DimenData DimenData { get; set; }
        public virtual DimenLinguaCollezione DimenLinguaCollezione { get; set; }
        public virtual DimenLinguaColore DimenLinguaColore { get; set; }
        public virtual DimenMacrocategoria DimenMacrocategoria { get; set; }
        public virtual DimenNomeDesign DimenNomeDesign { get; set; }
        public virtual DimenProvincia DimenProvincia { get; set; }
        public virtual DimenSessoAbbigliamento DimenSessoAbbigliamento { get; set; }
        public virtual DimenSessoCliente DimenSessoCliente { get; set; }
        public virtual DimenStato DimenStato { get; set; }
        public virtual DimenTipoPagamento DimenTipoPagamento { get; set; }
        public virtual DimenValoreTaglia DimenValoreTaglia { get; set; }
    }
}