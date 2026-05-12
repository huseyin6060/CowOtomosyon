using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CowMaster.Sayfalar
{
    public partial class AracBakim : UserControl
    {
        int secilenId = 0;
        private DataTable dtEkipmanlar = new DataTable();

        public AracBakim()
        {
            InitializeComponent();


            LoadData();
        }

        
        private void LoadData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();

               
                    string query = @"SELECT *, 
                        (MarkaModel + ' ' + EkipmanTuru) AS ekipman_ad, 
                        ISNULL(MevcutSaat, 0) AS mevcut_saat,
                        (ISNULL(BakimSaati, 0) - ISNULL(MevcutSaat, 0)) AS kalan_saat,
                        CASE 
                            WHEN ISNULL(BakimSaati, 0) > 0 THEN (ISNULL(MevcutSaat, 0) * 100 / BakimSaati) 
                            ELSE 0 
                        END AS bakim_yuzde,
                        CASE 
                            WHEN ISNULL(BakimSaati, 0) = 0 THEN '#43A047' -- Periyot yoksa yeşil
                            WHEN (BakimSaati - MevcutSaat) <= 0 THEN '#C62828' -- Bakım geçmiş (Kırmızı)
                            WHEN (BakimSaati - MevcutSaat) <= 50 AND MevcutSaat > 0 THEN '#EF6C00' -- Bakım yaklaşmış (Turuncu)
                            ELSE '#43A047' -- Güvenli (Yeşil)
                        END AS durum_renk
                        FROM EkipmanYonetimi";

                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    dtEkipmanlar = new DataTable();
                    da.Fill(dtEkipmanlar);

                    dgEkipmanlar.ItemsSource = dtEkipmanlar.DefaultView;
                    HesaplaIstatistikler(dtEkipmanlar);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri yükleme hatası: " + ex.Message);
            }
        }

        private void HesaplaIstatistikler(DataTable dt)
        {
            txtToplamEkipman.Text = dt.Rows.Count.ToString() + " Adet";

            int yaklasanBakimSayisi = 0;
            int aktifCalisanSayisi = 0;

            foreach (DataRow row in dt.Rows)
            {
                
                double mevcut = row["MevcutSaat"] != DBNull.Value ? Convert.ToDouble(row["MevcutSaat"]) : 0;
                double periyot = row["BakimSaati"] != DBNull.Value ? Convert.ToDouble(row["BakimSaati"]) : 0;
                double kalan = periyot - mevcut;

     
                if (periyot > 0 && mevcut > 0 && kalan <= 50 && kalan > 0)
                {
                    yaklasanBakimSayisi++;
                }

                if (periyot == 0 || kalan > 50 || mevcut == 0)
                {
                    aktifCalisanSayisi++;
                }
            }

            txtYaklasanBakim.Text = yaklasanBakimSayisi.ToString() + " Araç";
            txtAktifUnite.Text = aktifCalisanSayisi.ToString() + " Ünite";
        }

        private void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAracAd.Text))
            {
                MessageBox.Show("Lütfen bir araç adı giriniz.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(@"INSERT INTO EkipmanYonetimi 
                        (EkipmanTuru, MarkaModel, SatinAlmaTarihi, MevcutSaat, BakimSaati)
                        VALUES (@t, @m, @s, @ms, @bs)", conn);

                    cmd.Parameters.AddWithValue("@t", cmbKategori.Text);
                    cmd.Parameters.AddWithValue("@m", txtAracAd.Text);
                    cmd.Parameters.AddWithValue("@s", (object)dpAlimTarihi.SelectedDate! ?? DBNull.Value);

            
                    double.TryParse(txtMevcutSaat.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double ms);
                    double.TryParse(txtBakimPeriyodu.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double bs);

                    cmd.Parameters.AddWithValue("@ms", ms);
                    cmd.Parameters.AddWithValue("@bs", bs);

                    cmd.ExecuteNonQuery();
                }
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası: " + ex.Message);
            }
        }

      
        private void BtnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (secilenId == 0) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(@"UPDATE EkipmanYonetimi SET 
                        EkipmanTuru=@t, MarkaModel=@m, SatinAlmaTarihi=@s, MevcutSaat=@ms, BakimSaati=@bs 
                        WHERE Id=@id", conn);

                    cmd.Parameters.AddWithValue("@t", cmbKategori.Text);
                    cmd.Parameters.AddWithValue("@m", txtAracAd.Text);
                    cmd.Parameters.AddWithValue("@s", (object)dpAlimTarihi.SelectedDate! ?? DBNull.Value);

                    double.TryParse(txtMevcutSaat.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double ms);
                    double.TryParse(txtBakimPeriyodu.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double bs);

                    cmd.Parameters.AddWithValue("@ms", ms);
                    cmd.Parameters.AddWithValue("@bs", bs);
                    cmd.Parameters.AddWithValue("@id", secilenId);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
                ClearForm();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        
        private void BtnSil_Click(object sender, RoutedEventArgs e)
        {
            if (secilenId == 0) return;
            if (MessageBox.Show("Silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.No) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM EkipmanYonetimi WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", secilenId);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
                ClearForm();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }


        private void dgEkipmanlar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEkipmanlar.SelectedItem is not DataRowView row)
                return;

            secilenId = Convert.ToInt32(row["Id"]);
            txtAracAd.Text = row["MarkaModel"]?.ToString();
            txtMevcutSaat.Text = row["MevcutSaat"]?.ToString();
            txtBakimPeriyodu.Text = row["BakimSaati"]?.ToString();

            dpAlimTarihi.SelectedDate =
                row["SatinAlmaTarihi"] == DBNull.Value
                ? null
                : (DateTime?)row["SatinAlmaTarihi"];

            cmbKategori.Text = row["EkipmanTuru"]?.ToString();

            lblPanelBaslik.Text = "Kayıt Düzenle: " + row["MarkaModel"]?.ToString();
        }

        private void BtnBakimOnayla_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn!.DataContext as DataRowView;
            if (row == null) return;

            if (MessageBox.Show("Bakım tamamlandı mı? Mevcut saat sıfırlanacak.", "Bakım Onayı", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("UPDATE EkipmanYonetimi SET MevcutSaat = 0 WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", row["Id"]);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
        }

        private void txtEkipmanAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtEkipmanlar == null) return;
            string filtre = txtEkipmanAra.Text.Replace("'", "''");
            dtEkipmanlar.DefaultView.RowFilter = string.Format("MarkaModel LIKE '%{0}%' OR EkipmanTuru LIKE '%{0}%'", filtre);
        }

        private void btnEkipmanYenile_Click(object sender, RoutedEventArgs e)
        {
            txtEkipmanAra.Clear();
            LoadData();
        }

        private void BtnIptal_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            txtAracAd.Clear(); txtMevcutSaat.Clear(); txtBakimPeriyodu.Clear();
            dpAlimTarihi.SelectedDate = null; cmbKategori.SelectedIndex = -1; secilenId = 0;
            lblPanelBaslik.Text = "Ekipman Kaydı";
        }
    }
}