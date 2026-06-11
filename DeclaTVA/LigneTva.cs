using System;
using System.Collections.Generic;
using System.Text;

namespace DeclaTVA
{
    public class LigneTva
    {
        // Clé primaire
        public int Id { get; set; }
        // Clé étrangère
        public int DeclarationId { get; set; }
        public Declaration? Declaration { get; set; }
        // Données
        public string NumFacture { get; set; } = string.Empty;
        public DateTime DateFacture { get; set; }
        public string Designation { get; set; } = string.Empty;
        public decimal MontantHT { get; set; }
        public decimal MontantTVA { get; set; }
        public decimal MontantTTC { get; set; }
        public decimal TauxTVA { get; set; }
        public string Fournisseur { get; set; } = string.Empty;
        public string IFFournisseur { get; set; } = string.Empty;
        public string ICEFournisseur { get; set; } = string.Empty;
        public DateTime DatePaiement { get; set; }
        public int ModePaiement { get; set; }
        public decimal Prorata { get; set; }
    }
}
