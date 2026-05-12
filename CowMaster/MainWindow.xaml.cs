using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CowMaster.Sayfalar;

namespace CowMaster
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

 
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1500);

            BildirimKontrolleri();
        }

     
        private void SayfaDegistir(UserControl yeniSayfa)
        {
            if (SayfaIcerigi == null) return;

            if (SayfaIcerigi.Content != null &&
                SayfaIcerigi.Content.GetType() == yeniSayfa.GetType())
            {
                SayfaIcerigi.Content = null;
            }
            else
            {
                SayfaIcerigi.Content = yeniSayfa;
            }
        }

     
        private void BildirimKontrolleri()
        {
            DogumYaklasanlariKontrolEt();

            AsiZamaniKontrolEt();

            VeterinerKontrolKontrolEt();

            KritikStokKontrolEt();

            GorevKontrolEt();

            BakimKontrolEt();
        }

    
        private void DogumYaklasanlariKontrolEt()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql = @"
                    SELECT
                        H.kupe_no,
                        H.hayvan_adi,
                        G.tahmini_dogum
                    FROM Tbl_Gebelik G
                    INNER JOIN Tbl_Hayvanlar H
                        ON G.hayvan_id = H.hayvan_id
                    WHERE
                        G.durum = 'Pozitif'
                        AND G.tahmini_dogum IS NOT NULL
                        AND DATEDIFF(DAY, GETDATE(), G.tahmini_dogum)
                        BETWEEN 0 AND 7";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        string kupe =
                            dr["kupe_no"].ToString()!;

                        string ad =
                            dr["hayvan_adi"].ToString()!;

                        DateTime tarih =
                            Convert.ToDateTime(dr["tahmini_dogum"]);

                        int gun =
                            (tarih - DateTime.Now).Days;

                        MessageBox.Show(
                            $"{kupe} - {ad}\n\n" +
                            $"Doğuma {gun} gün kaldı.",
                            "DOĞUM UYARISI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

   
        private void AsiZamaniKontrolEt()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql = @"
                    SELECT
                        H.kupe_no,
                        H.hayvan_adi,
                        A.asi_adi
                    FROM Tbl_AsiTakip A
                    INNER JOIN Tbl_Hayvanlar H
                        ON A.hayvan_id = H.hayvan_id
                    WHERE
                        A.durum = 'Bekliyor'
                        AND DATEDIFF(DAY, GETDATE(), A.tekrar_tarihi)
                        BETWEEN 0 AND 3";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        MessageBox.Show(
                            dr["kupe_no"] + " - " +
                            dr["hayvan_adi"] +
                            "\n\n" +
                            dr["asi_adi"] +
                            " aşı zamanı yaklaştı.",
                            "AŞI UYARISI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void VeterinerKontrolKontrolEt()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql = @"
                    SELECT
                        H.kupe_no,
                        H.hayvan_adi
                    FROM Tbl_VeterinerIslem V
                    INNER JOIN Tbl_Hayvanlar H
                        ON V.hayvan_id = H.hayvan_id
                    WHERE
                        V.sonraki_kontrol IS NOT NULL
                        AND DATEDIFF(DAY, GETDATE(), V.sonraki_kontrol)
                        BETWEEN 0 AND 2";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        MessageBox.Show(
                            dr["kupe_no"] + " - " +
                            dr["hayvan_adi"] +
                            "\n\nVeteriner kontrol zamanı geldi.",
                            "VETERİNER UYARISI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

 
        private void KritikStokKontrolEt()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql = @"
                    SELECT
                        urun_adi,
                        mevcut_stok
                    FROM Tbl_Stok
                    WHERE mevcut_stok <= kritik_stok";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        MessageBox.Show(
                            dr["urun_adi"] +
                            "\n\nKritik stok seviyesine düştü." +
                            "\nMevcut Stok : " +
                            dr["mevcut_stok"],
                            "STOK UYARISI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

     
        private void GorevKontrolEt()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql = @"
                    SELECT
                        gorev_tanimi
                    FROM Gorevler
                    WHERE
                        durum = 'Beklemede'
                        AND DATEDIFF(DAY, GETDATE(), son_tarih)
                        BETWEEN 0 AND 2";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        MessageBox.Show(
                            dr["gorev_tanimi"] +
                            "\n\nGörev süresi yaklaşıyor.",
                            "GÖREV UYARISI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

   
        private void BakimKontrolEt()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(App.connectionString))
                {
                    conn.Open();

                    string sql = @"
                    SELECT
                        MarkaModel
                    FROM EkipmanYonetimi
                    WHERE MevcutSaat >= BakimSaati";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        MessageBox.Show(
                            dr["MarkaModel"] +
                            "\n\nBakım zamanı geldi.",
                            "EKİPMAN UYARISI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

   
        private void Ozet_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new SuruBilgi());
        }

        private void Hayvanlar_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new Hayvanlarım());
        }

        private void Sut_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new SütBilgisi());
        }

        private void Gelir_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new GelirGider());
        }

        private void rasyon_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new Rasyon());
        }

        private void Veteriner_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new VeterinerBilgi());
        }

        private void haftalıkgörev_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new HaftalikGorevler());
        }

        private void tarla_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new Tarla());
        }

        private void ekipman_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new AracBakim());
        }

        private void notlarım_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new SonDurumlar());
        }

        private void Ayarlar_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new Ayarlar());
        }

        private void Musteri_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new MusterilerPage());
        }

        private void Personel_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new PersonellerPage());
        }

        private void HayvanSatıs_Click(object sender, RoutedEventArgs e)
        {
            SayfaDegistir(new HayvanSatis());
        }

        private void BtnKapat_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}