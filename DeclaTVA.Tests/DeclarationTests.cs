using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using Xunit;

namespace DeclaTVA.Tests
{
    public class DatabaseServiceTests
    {
        // Crée un contexte en mémoire (pas besoin de SQL Server pour les tests)
        private DeclaTvaContext CreerContexteEnMemoire()
        {
            var options = new DbContextOptionsBuilder<DeclaTvaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // base unique par test
                .Options;

            return new DeclaTvaContext(options);
        }

        [Fact]
        public void EnregistrerDeclaration_RetourneUnIdPositif()
        {
            // Arrange
            using var context = CreerContexteEnMemoire();
            var service = new DatabaseService(context);

            var lignes = new ObservableCollection<LigneTva>
            {
                new LigneTva
                {
                    NumFacture = "F001",
                    DateFacture = "01/01/2024",
                    Fournisseur = "Fournisseur Test",
                    Designation = "Produit A",
                    MontantHT = "100",
                    MontantTVA = "20",
                    MontantTTC = "120"
                }
            };

            // Act
            int id = service.EnregistrerDeclaration(
                "Société Test", "IF123", "ICE456",
                "Normal", "Mensuel", "2024", lignes);

            // Assert
            Assert.True(id > 0);
        }

        [Fact]
        public void EnregistrerDeclaration_SaveLesLignesEnBase()
        {
            
            using var context = CreerContexteEnMemoire();
            var service = new DatabaseService(context);

            var lignes = new ObservableCollection<LigneTva>
            {
                new LigneTva { NumFacture = "F001", DateFacture = "01/01/2024", Fournisseur = "FournA", Designation = "Désig A", MontantHT = "100", MontantTVA = "20", MontantTTC = "120" },
                new LigneTva { NumFacture = "F002", DateFacture = "02/01/2024", Fournisseur = "FournB", Designation = "Désig B", MontantHT = "200", MontantTVA = "40", MontantTTC = "240" }
            };

            
            int id = service.EnregistrerDeclaration("Société", "IF", "ICE", "Normal", "Mensuel", "2024", lignes);

            
            var declaration = context.Declarations.Find(id);
            Assert.NotNull(declaration);
            Assert.Equal(2, context.LignesTVA.Count());
        }

        [Fact]
        public void EnregistrerDeclaration_AvecListeVide_ReussitQuandMeme()
        {
            
            using var context = CreerContexteEnMemoire();
            var service = new DatabaseService(context);
            var lignesVides = new ObservableCollection<LigneTva>();

            
            int id = service.EnregistrerDeclaration("Société", "IF", "ICE", "Normal", "Mensuel", "2024", lignesVides);

            
            Assert.True(id > 0);
            Assert.Equal(0, context.LignesTVA.Count());
        }

        [Fact]
        public void EnregistrerDeclaration_AvecChampsNuls_NeLancePasException()
        {
           
            using var context = CreerContexteEnMemoire();
            var service = new DatabaseService(context);

            
            var exception = Record.Exception(() =>
                service.EnregistrerDeclaration(null, null, null, null, null, null, new ObservableCollection<LigneTva>()));

            Assert.Null(exception);
        }
    }

    public class ValidationTests
    {
        // Tests des règles de validation métier (HT + TVA = TTC, montants négatifs, etc.)

        [Theory]
        [InlineData(100, 20, 120, true)]   // valide
        [InlineData(200, 40, 240, true)]   // valide
        [InlineData(100, 20, 130, false)]  // TTC incorrect
        [InlineData(100, 20, 115, false)]  // TTC incorrect
        public void Validation_HTplusTVA_EgalTTC(double ht, double tva, double ttc, bool attenduValide)
        {
            // La marge d'erreur tolérée dans l'application est 0.01
            bool estValide = Math.Abs((ht + tva) - ttc) <= 0.01;
            Assert.Equal(attenduValide, estValide);
        }

        [Theory]
        [InlineData(100, 20, 120, false)]   // tous positifs → pas d'erreur
        [InlineData(-10, 20, 10,  true)]    // HT négatif → erreur
        [InlineData(100, -5, 95,  true)]    // TVA négative → erreur
        [InlineData(100, 20, -1,  true)]    // TTC négatif → erreur
        public void Validation_MontantNegatif_DetecteErreur(double ht, double tva, double ttc, bool attenduErreur)
        {
            bool aErreur = ht < 0 || tva < 0 || ttc < 0;
            Assert.Equal(attenduErreur, aErreur);
        }

        [Theory]
        [InlineData("01/01/2024", true)]
        [InlineData("31/12/2023", true)]
        [InlineData("2024-01-01", false)]   // mauvais format
        [InlineData("abc",        false)]   // invalide
        [InlineData("",           false)]   // vide
        public void Validation_FormatDate_ddMMyyyy(string date, bool attenduValide)
        {
            bool estValide = DateTime.TryParseExact(
                date,
                "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _);

            Assert.Equal(attenduValide, estValide);
        }
    }
}
