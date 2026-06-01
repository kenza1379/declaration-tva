using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace DeclaTVA
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<LigneTva> _lignesValides = new();
        private readonly DatabaseService _db;

        public MainWindow()
        {
            InitializeComponent();
            _db = App.Services.GetService<DatabaseService>();
            _db.InitialiserDatabase();
        }

        public ObservableCollection<ErreurValidation> listErreurs = new ObservableCollection<ErreurValidation>();

        private void btnParcourir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Fichiers Excel|*.xlsx";

            if (dialog.ShowDialog() == true)
            {
                txtFichier.Text = dialog.FileName;
                ChargerFichier(dialog.FileName);
            }
        }

        private void ChargerFichier(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook(filePath);
                LireAccueil(workbook);
                LireTva(workbook);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}");
            }
        }

        private void LireAccueil(XLWorkbook workbook)
        {
            if (!workbook.TryGetWorksheet("Accueil", out var sheetAccueil)) return;

            foreach (var row in sheetAccueil.RowsUsed())
            {
                string label = row.Cell(3).GetString().Trim();
                string valeur = row.Cell(8).GetString().Trim();

                if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(valeur)) continue;

                if (label.Contains("Société")) lblSociete.Text = valeur;
                else if (label.Contains("Identifiant Fiscal")) lblIF.Text = valeur;
                else if (label.Contains("Commun")) lblICE.Text = valeur;
                else if (label.Contains("Régime")) lblRegime.Text = valeur;
                else if (label.Contains("Période")) lblPeriode.Text = valeur;
                else if (label.Contains("Année")) lblAnnee.Text = valeur;
            }
        }

        private void LireTva(XLWorkbook workbook)
        {
            if (!workbook.TryGetWorksheet("Tva", out var sheetTVA)) return;

            var lignes = new ObservableCollection<LigneTva>();

            listErreurs.Clear();

            foreach (var row in sheetTVA.RowsUsed())
            {
                bool ligneValide = true;

                if (row.RowNumber() < 4) continue;

                int numeroLigne = row.RowNumber();

                string ordre = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(ordre)) continue;


                string numFacture = row.Cell(2).GetString().Trim();
                string dateFacture = FormatDate(row.Cell(3));
                string fournisseur = row.Cell(6).GetString().Trim();
                string designation = row.Cell(7).GetString().Trim();
                double HT = ToDouble(row.Cell(8));
                double TVA = ToDouble(row.Cell(10));
                double TTC = ToDouble(row.Cell(11));
                

                //Validation des données 
                
                //numero de facture obligatoire
                if(string.IsNullOrEmpty(numFacture))
                {
                    listErreurs.Add(new ErreurValidation {
                        ligne = numeroLigne,
                        champ = "numFacture",
                        message = "Numéro de facture obligatoire"
                    });
                    ligneValide = false;
                }

                //numero de fournisseur obligatoire
                if(string.IsNullOrEmpty(fournisseur))
                {
                    listErreurs.Add(new ErreurValidation {
                        ligne = numeroLigne,
                        champ = "Fournisseur",
                        message = "Fournisseur obligatoire"

                    });
                    ligneValide = false;
                }

                //numero de désignation obligatoire
                if (string.IsNullOrEmpty(designation))
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "Designation",
                        message = "Designation obligatoire"

                    });
                    ligneValide = false;
                }

                //date valide
                if (!DateTime.TryParseExact(dateFacture, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "DateFacture",
                        message = "Date facture invalide"
                    });
                    ligneValide = false;
                }

                //montant inferieur a 0
                if(TVA<0 || HT<0 || TTC<0)
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "Montant",
                        message = "montant négatif invalide"
                    });
                    ligneValide = false;
                }

                //HT + TVA différent du TTC
                if (Math.Abs((HT + TVA) - TTC) > 0.01) //marge d'erreur
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "MontantTTC",
                        message = "HT + TVA différent du TTC"
                    });
                    ligneValide = false;
                }

                //ajout 
                if(ligneValide == true)
                {
                    lignes.Add(new LigneTva
                    {
                        NumFacture = numFacture,
                        DateFacture = dateFacture,
                        Fournisseur = fournisseur,
                        Designation = designation,
                        MontantHT = HT.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        MontantTVA = TVA.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        MontantTTC = TTC.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
                }

            }
            _lignesValides = lignes;
            dgTva.ItemsSource = lignes;
            btnEnregistrer.IsEnabled = lignes.Count > 0;
            dgErreurs.ItemsSource = listErreurs;

        }

        private double ToDouble(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number) return cell.GetDouble();
            if (double.TryParse(cell.GetString(), out double v)) return v;
            return 0;
        }

        private string FormatDate(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime().ToString("dd/MM/yyyy");
            string s = cell.GetString().Trim();
            if (DateTime.TryParse(s, out DateTime d))
                return d.ToString("dd/MM/yyyy");
            return s;
        }

        private void btnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int id = _db.EnregistrerDeclaration(
                    lblSociete.Text, lblIF.Text, lblICE.Text,
                    lblRegime.Text, lblPeriode.Text, lblAnnee.Text,
                    _lignesValides);

                MessageBox.Show($"Déclaration enregistrée avec succès : (Id = {id})",
                                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                btnEnregistrer.IsEnabled = false;
            }
            catch (Exception exp)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {exp.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}