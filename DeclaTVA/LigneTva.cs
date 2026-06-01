using System;
using System.Collections.Generic;
using System.Text;

namespace DeclaTVA
{
    public class LigneTva
    {
        //clé primaire
        public int Id { get; set; }

        //clé etrangère avec Declaration
        public int DeclarationId { get; set; }
        public Declaration? Declaration { get; set; }

        //données
        public string NumFacture { get; set; } = string.Empty;
        public string DateFacture { get; set; } = string.Empty;
        public string Fournisseur { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string MontantHT { get; set; } = string.Empty;
        public string MontantTVA { get; set; } = string.Empty;
        public string MontantTTC { get; set; } = string.Empty;
    }
}
