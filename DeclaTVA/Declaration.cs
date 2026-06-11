using System;
using System.Collections.Generic;

namespace DeclaTVA
{
    public class Declaration
    {
        public int Id { get; set; }
        public string Societe { get; set; } = string.Empty;
        public string IdentifiantFiscal { get; set; } = string.Empty;
        public string ICE { get; set; } = string.Empty;
        public int Regime { get; set; }
        public int Periode { get; set; }
        public string Annee { get; set; } = string.Empty;
        public DateTime DateImport { get; set; } = DateTime.Now;
        public List<LigneTva> Lignes { get; set; } = new();
    }
}