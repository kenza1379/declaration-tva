using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace DeclaTVA
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<LigneTva> _lignesValides = new();
        private readonly DatabaseService _db;
        private readonly EdiService _edi = new EdiService();

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
                btnGenererEdi.IsEnabled = false;
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
                DateTime? dateFacture = ToDateTime(row.Cell(3));
                string ifFournisseur = row.Cell(4).GetString().Trim();
                string iceFournisseur = row.Cell(5).GetString().Trim();
                string fournisseur = row.Cell(6).GetString().Trim();
                string designation = row.Cell(7).GetString().Trim();
                decimal HT = ToDecimal(row.Cell(8));
                decimal tauxTVA = ToDecimal(row.Cell(9));
                decimal TVA = ToDecimal(row.Cell(10));
                decimal TTC = ToDecimal(row.Cell(11));
                DateTime? datePaiement = ToDateTime(row.Cell(12));
                int modePaiement = ToModePaiement(row.Cell(13));
                decimal prorata = ToDecimal(row.Cell(14));

                if (string.IsNullOrEmpty(numFacture))
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "NumFacture",
                        message = "Numéro de facture obligatoire"
                    });
                    ligneValide = false;
                }

                if (string.IsNullOrEmpty(fournisseur))
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "Fournisseur",
                        message = "Fournisseur obligatoire"
                    });
                    ligneValide = false;
                }

                if (string.IsNullOrEmpty(designation))
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "Designation",
                        message = "Désignation obligatoire"
                    });
                    ligneValide = false;
                }

                if (dateFacture == null)
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "DateFacture",
                        message = "Date facture invalide"
                    });
                    ligneValide = false;
                }

                if (datePaiement == null)
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "DatePaiement",
                        message = "Date paiement invalide"
                    });
                    ligneValide = false;
                }

                if (TVA < 0 || HT < 0 || TTC < 0)
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "Montant",
                        message = "Montant négatif invalide"
                    });
                    ligneValide = false;
                }

                if (Math.Abs((HT + TVA) - TTC) > 0.01m)
                {
                    listErreurs.Add(new ErreurValidation
                    {
                        ligne = numeroLigne,
                        champ = "MontantTTC",
                        message = "HT + TVA différent du TTC"
                    });
                    ligneValide = false;
                }

                if (ligneValide)
                {
                    lignes.Add(new LigneTva
                    {
                        NumFacture = numFacture,
                        DateFacture = dateFacture!.Value,
                        IFFournisseur = ifFournisseur,
                        ICEFournisseur = iceFournisseur,
                        Fournisseur = fournisseur,
                        Designation = designation,
                        MontantHT = HT,
                        TauxTVA = tauxTVA,
                        MontantTVA = TVA,
                        MontantTTC = TTC,
                        DatePaiement = datePaiement!.Value,
                        ModePaiement = modePaiement,
                        Prorata = prorata
                    });
                }
            }

            _lignesValides = lignes;
            dgTva.ItemsSource = lignes;
            btnEnregistrer.IsEnabled = lignes.Count > 0;
            dgErreurs.ItemsSource = listErreurs;
        }

        private decimal ToDecimal(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
            {
                return (decimal)cell.GetDouble();
            }
                
            if (decimal.TryParse(cell.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal v))
            {
                return v;
            }
                
            return 0;
        }

        private DateTime? ToDateTime(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                return cell.GetDateTime();
            }

            string s = cell.GetString().Trim();
            if (DateTime.TryParseExact(s, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime d))
            {
                return d;
            }
                
            if (DateTime.TryParse(s, out DateTime d2))
            {
                return d2;
            }
                
            return null;
        }

        private int ToModePaiement(IXLCell cell)
        {
            
            string val = cell.GetString().Trim().ToLower();
            return val switch
            {
                "espèces" or "especes" => 1,
                "chèque" or "cheque" or "chèques" or "cheques" => 2,
                "prélèvement" or "prelevement" => 3,
                "virement" => 4,
                "effet" => 5,
                "compensation" => 6,
                _ when int.TryParse(val, out int i) => i,
                _ => 7 //autres
            };
        }

        private void btnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int regime = lblRegime.Text switch
                {
                    "Mensuel" => 1,
                    "Trimestriel" => 2,
                    _ => 0
                };

                if (!int.TryParse(lblPeriode.Text, out int periode))
                {
                    MessageBox.Show("Période invalide.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Validation période selon régime
                bool periodeValide = regime switch
                {
                    1 => periode >= 1 && periode <= 12,  // Mensuel
                    2 => periode >= 1 && periode <= 4,   // Trimestriel
                    _ => periode == 1                    // Cessation
                };

                if (!periodeValide)
                {
                    MessageBox.Show($"Période invalide pour le régime {lblRegime.Text}.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string annee = lblAnnee.Text;

                int id = _db.EnregistrerDeclaration(
                    lblSociete.Text, lblIF.Text, lblICE.Text,
                    regime, periode, annee,
                    _lignesValides);

                MessageBox.Show($"Déclaration enregistrée avec succès (Id = {id})",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                btnEnregistrer.IsEnabled = false;
                btnGenererEdi.IsEnabled = true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString(), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGenererEdi_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Fichier ZIP|*.zip";
            dialog.FileName = $"releveDeduction_{lblIF.Text}_{lblAnnee.Text}_{lblPeriode.Text}.zip";

            if (dialog.ShowDialog() == true)
            {
                int regime = lblRegime.Text switch
                {
                    "Mensuel" => 1,
                    "Trimestriel" => 2,
                    _ => 1
                };

                string dossier = Path.GetDirectoryName(dialog.FileName)!;
                string zipPath = _edi.GenererEtZipper(lblIF.Text, lblAnnee.Text, lblPeriode.Text, regime, _lignesValides, dossier);

                MessageBox.Show($"Fichier EDI généré : {zipPath}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}