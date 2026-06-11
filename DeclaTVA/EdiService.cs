using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace DeclaTVA
{
    class EdiService
    {
        public string GenererEtZipper(string identifiantFiscal, string annee, string periode, int regime, ObservableCollection<LigneTva> lignes, string dossierDestination)
        {
            string xmlContent = GenererXml(identifiantFiscal, annee, periode, regime, lignes);

            string nomFichierXml = $"releveDeduction_{identifiantFiscal}_{annee}_{periode}.xml";
            string nomFichierZip = Path.Combine(dossierDestination, $"releveDeduction_{identifiantFiscal}_{annee}_{periode}.zip");

            string tempXml = Path.Combine(Path.GetTempPath(), nomFichierXml);
            File.WriteAllText(tempXml, xmlContent, System.Text.Encoding.UTF8);

            if (File.Exists(nomFichierZip))
                File.Delete(nomFichierZip);

            using (var zip = ZipFile.Open(nomFichierZip, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempXml, nomFichierXml);
            }

            File.Delete(tempXml);

            return nomFichierZip;
        }

        private string GenererXml(string identifiantFiscal, string annee, string periode, int regime, ObservableCollection<LigneTva> lignes)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = System.Text.Encoding.UTF8
            };

            using var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("DeclarationReleveDeduction");

                writer.WriteElementString("identifiantFiscal", identifiantFiscal ?? "");
                writer.WriteElementString("annee", annee ?? "");
                writer.WriteElementString("periode", periode ?? "");
                writer.WriteElementString("regime", regime.ToString());

                writer.WriteStartElement("releveDeductions");

                int ordre = 1;
                foreach (var ligne in lignes)
                {
                    writer.WriteStartElement("rd");

                    writer.WriteElementString("ord", ordre.ToString());
                    writer.WriteElementString("num", ligne.NumFacture ?? "");
                    writer.WriteElementString("des", ligne.Designation ?? "");
                    writer.WriteElementString("mht", FormatMontant(ligne.MontantHT));
                    writer.WriteElementString("tva", FormatMontant(ligne.MontantTVA));
                    writer.WriteElementString("ttc", FormatMontant(ligne.MontantTTC));

                    writer.WriteStartElement("refF");
                    writer.WriteElementString("if", ligne.IFFournisseur ?? "");
                    writer.WriteElementString("nom", ligne.Fournisseur ?? "");
                    writer.WriteElementString("ice", ligne.ICEFournisseur ?? "");
                    writer.WriteEndElement(); 

                    writer.WriteElementString("dfac", FormatDate(ligne.DateFacture));

                    writer.WriteEndElement(); 
                    ordre++;
                }

                writer.WriteEndElement(); 
                writer.WriteEndElement(); 
                writer.WriteEndDocument();
            }

            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }

        private string FormatMontant(decimal montant)
        {
            return montant.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
    }
}