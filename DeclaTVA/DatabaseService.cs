using System.Collections.ObjectModel;

namespace DeclaTVA
{
    class DatabaseService
    {
        private readonly DeclaTvaContext context;

        public DatabaseService(DeclaTvaContext context)
        {
            this.context = context;
        }
        public void InitialiserDatabase()
        {
            context.Database.EnsureCreated();
        }

        public int EnregistrerDeclaration(string societe, string identifiantFiscal, string ice, string regime, string periode, string annee, ObservableCollection<LigneTva> lignes)
        {

            var declaration = new Declaration
            {
                Societe = societe ?? "",
                IdentifiantFiscal = identifiantFiscal ?? "",
                ICE = ice ?? "",
                Regime = regime ?? "",
                Periode = periode ?? "",
                Annee = annee ?? "",
                Lignes = new(lignes)   
            };

            context.Declarations.Add(declaration);
            context.SaveChanges();   

            return declaration.Id;   
        }
    }
}