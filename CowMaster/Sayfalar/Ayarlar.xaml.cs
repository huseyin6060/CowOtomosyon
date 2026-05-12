using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class Ayarlar : UserControl
    {
        public Ayarlar()
        {
            InitializeComponent();

            SayisalButtonOlaylari();
            AyarlariYukle();
        }

       
        private void AyarlariYukle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string kontrol =
                    @"IF OBJECT_ID('Tbl_Ayarlar', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Tbl_Ayarlar(
                            ayar_id INT PRIMARY KEY IDENTITY(1,1),
                            olcu_birimi NVARCHAR(50),
                            para_birimi NVARCHAR(50),
                            ciftlik_adi NVARCHAR(150),
                            hayvan_kapasitesi INT,
                            sutten_kesme_gun INT,
                            gebelik_kontrol_gun INT,
                            kuru_donem_gun INT,
                            bildirimler BIT,
                            otomatik_yedek BIT
                        )

                        INSERT INTO Tbl_Ayarlar
                        (
                            olcu_birimi,
                            para_birimi,
                            ciftlik_adi,
                            hayvan_kapasitesi,
                            sutten_kesme_gun,
                            gebelik_kontrol_gun,
                            kuru_donem_gun,
                            bildirimler,
                            otomatik_yedek
                        )
                        VALUES
                        (
                            'Metrik (kg, litre)',
                            'Türk Lirası (₺)',
                            '',
                            0,
                            65,
                            45,
                            60,
                            1,
                            0
                        )
                    END";

                    SqlCommand cmdKontrol = new SqlCommand(kontrol, conn);
                    cmdKontrol.ExecuteNonQuery();

                    string sql = "SELECT TOP 1 * FROM Tbl_Ayarlar";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();

                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];

                        cmbOlcuBirimi.Text = row["olcu_birimi"].ToString();
                        cmbParaBirimi.Text = row["para_birimi"].ToString();

                        txtCiftlikAdi.Text = row["ciftlik_adi"].ToString();
                        txtCiftlikKapasite.Text = row["hayvan_kapasitesi"].ToString();

                        numSutKesme.Text = row["sutten_kesme_gun"].ToString();
                        numGebelik.Text = row["gebelik_kontrol_gun"].ToString();
                        numKuruDonem.Text = row["kuru_donem_gun"].ToString();

                        tgBildirimler.IsChecked = Convert.ToBoolean(row["bildirimler"]);
                        tgOtomatikYedek.IsChecked = Convert.ToBoolean(row["otomatik_yedek"]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ayarlar yüklenemedi!\n" + ex.Message);
            }
        }

      
        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql =
                    @"UPDATE Tbl_Ayarlar SET
                        olcu_birimi = @olcu,
                        para_birimi = @para,
                        ciftlik_adi = @ciftlik,
                        hayvan_kapasitesi = @kapasite,
                        sutten_kesme_gun = @sut,
                        gebelik_kontrol_gun = @gebelik,
                        kuru_donem_gun = @kuru,
                        bildirimler = @bildirim,
                        otomatik_yedek = @yedek";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@olcu", cmbOlcuBirimi.Text);
                    cmd.Parameters.AddWithValue("@para", cmbParaBirimi.Text);
                    cmd.Parameters.AddWithValue("@ciftlik", txtCiftlikAdi.Text);

                    cmd.Parameters.AddWithValue("@kapasite",
                        string.IsNullOrWhiteSpace(txtCiftlikKapasite.Text)
                        ? 0
                        : Convert.ToInt32(txtCiftlikKapasite.Text));

                    cmd.Parameters.AddWithValue("@sut",
                        Convert.ToInt32(numSutKesme.Text));

                    cmd.Parameters.AddWithValue("@gebelik",
                        Convert.ToInt32(numGebelik.Text));

                    cmd.Parameters.AddWithValue("@kuru",
                        Convert.ToInt32(numKuruDonem.Text));

                    cmd.Parameters.AddWithValue("@bildirim",
                        tgBildirimler.IsChecked == true);

                    cmd.Parameters.AddWithValue("@yedek",
                        tgOtomatikYedek.IsChecked == true);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Ayarlar başarıyla kaydedildi.",
                        "Başarılı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası!\n" + ex.Message);
            }
        }

        
        private void SayisalButtonOlaylari()
        {
            btnSutKesmeArtir.Click += (s, e) => SayiArtir(numSutKesme);
            btnSutKesmeAzalt.Click += (s, e) => SayiAzalt(numSutKesme);

            btnGebelikArtir.Click += (s, e) => SayiArtir(numGebelik);
            btnGebelikAzalt.Click += (s, e) => SayiAzalt(numGebelik);

            btnKuruArtir.Click += (s, e) => SayiArtir(numKuruDonem);
            btnKuruAzalt.Click += (s, e) => SayiAzalt(numKuruDonem);

            btnYedekAl.Click += BtnYedekAl_Click;
        }

      
        private void SayiArtir(TextBox txt)
        {
            int sayi = 0;

            int.TryParse(txt.Text, out sayi);

            sayi++;

            txt.Text = sayi.ToString();
        }

  
        private void SayiAzalt(TextBox txt)
        {
            int sayi = 0;

            int.TryParse(txt.Text, out sayi);

            if (sayi > 0)
                sayi--;

            txt.Text = sayi.ToString();
        }

     
        private void BtnYedekAl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string masaustu =
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                string klasor = Path.Combine(masaustu, "CowMasterYedek");

                if (!Directory.Exists(klasor))
                    Directory.CreateDirectory(klasor);

                string dosyaAdi =
                    "CIFLIKDB_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                    ".bak";

                string tamYol = Path.Combine(klasor, dosyaAdi);

                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string backup =
                    $@"BACKUP DATABASE CIFLIKDB
                    TO DISK = '{tamYol}'
                    WITH FORMAT,
                    MEDIANAME = 'SQLServerBackups',
                    NAME = 'Full Backup of CIFLIKDB'";

                    SqlCommand cmd = new SqlCommand(backup, conn);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show(
                    "Yedek başarıyla oluşturuldu.\n\n" + tamYol,
                    "Başarılı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Yedekleme başarısız!\n" + ex.Message);
            }
        }
    }
}