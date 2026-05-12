using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class PersonellerPage : UserControl
    {
        private int seciliPersonelID = 0;

        public PersonellerPage()
        {
            InitializeComponent();
            Listele();
            IstatistikleriGuncelle();
       
            txtPersonelAra.TextChanged += (s, e) => Listele(txtPersonelAra.Text);
            
        }

 
        private void Listele(string arama = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    string sql = "SELECT * FROM Tbl_Personeller WHERE aktif = 1";
                    if (!string.IsNullOrEmpty(arama)) sql += " AND ad_soyad LIKE @ara";
                    sql += " ORDER BY personel_id DESC";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.Parameters.AddWithValue("@ara", "%" + arama + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgPersoneller.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Listeleme Hatası: " + ex.Message); }
        }

        private void IstatistikleriGuncelle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();
       
                    string sql = @"SELECT 
                                 COUNT(*) AS toplam, 
                                 SUM(maas) AS maas,
                                 (SELECT COUNT(*) FROM Tbl_Personeller WHERE aktif=1 AND ehliyet_varmi=1) AS ehliyet
                                 FROM Tbl_Personeller 
                                 WHERE aktif = 1";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        txtToplamPersonel.Text = dr["toplam"].ToString();
                        txtAktifPersonel.Text = dr["toplam"].ToString(); 
                        txtToplamMaas.Text = string.Format("{0:N2} ₺", dr["maas"] == DBNull.Value ? 0 : dr["maas"]);
                        txtEhliyetli.Text = dr["ehliyet"].ToString();
                    }
                    else
                    {
                        txtToplamPersonel.Text = "0";
                        txtAktifPersonel.Text = "0";
                        txtToplamMaas.Text = "0,00 ₺";
                        txtEhliyetli.Text = "0";
                    }
                }
            }
            catch { }
        }

        private void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAdSoyad.Text)) { MessageBox.Show("Ad Soyad girmek zorunludur!"); return; }

            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    string sql = @"INSERT INTO Tbl_Personeller (ad_soyad, tc_no, telefon, gorev, maas, ise_giris_tarihi, dogum_tarihi, adres, ehliyet_varmi, aktif, aciklama) 
                                 VALUES (@ad, @tc, @tel, @gor, @maas, @ise, @dogum, @adr, @ehl, @akt, @ack)";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    ParametreleriEkle(cmd);
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    Temizle();
                    Listele();
                    IstatistikleriGuncelle();
                    MessageBox.Show("Personel başarıyla kaydedildi.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Kayıt Hatası: " + ex.Message); }
        }

        private void btnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (seciliPersonelID == 0) { MessageBox.Show("Lütfen listeden bir personel seçin!"); return; }

            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    string sql = @"UPDATE Tbl_Personeller SET ad_soyad=@ad, tc_no=@tc, telefon=@tel, gorev=@gor, maas=@maas, 
                                 ise_giris_tarihi=@ise, dogum_tarihi=@dogum, adres=@adr, ehliyet_varmi=@ehl, aktif=@akt, aciklama=@ack 
                                 WHERE personel_id=@id";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", seciliPersonelID);
                    ParametreleriEkle(cmd);
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    Listele();
                    IstatistikleriGuncelle();
                    MessageBox.Show("Personel bilgileri güncellendi.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Güncelleme Hatası: " + ex.Message); }
        }

        private void btnSil_click(object sender, RoutedEventArgs e)
        {
            if (seciliPersonelID == 0) return;

            var cevap = MessageBox.Show("Seçili personeli silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (cevap == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(App.connectionString))
                    {
          
                        string sql = "UPDATE Tbl_Personeller SET aktif = 0 WHERE personel_id = @id";
                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", seciliPersonelID);

                        conn.Open();
                        cmd.ExecuteNonQuery();

                        Temizle();
                        Listele();
                        IstatistikleriGuncelle();
                        MessageBox.Show("Personel silindi.");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Silme Hatası: " + ex.Message); }
            }
        }

      
        private void ParametreleriEkle(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@ad", txtAdSoyad.Text);
            cmd.Parameters.AddWithValue("@tc", txtTcNo.Text ?? "");
            cmd.Parameters.AddWithValue("@tel", txtTelefon.Text ?? "");
            cmd.Parameters.AddWithValue("@gor", cbGorev.Text ?? "");
            cmd.Parameters.AddWithValue("@maas", decimal.TryParse(txtMaas.Text, out decimal m) ? m : 0);
            cmd.Parameters.AddWithValue("@ise", dtIseGiris.SelectedDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@dogum", dtDogum.SelectedDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@adr", txtAdres.Text ?? "");
            cmd.Parameters.AddWithValue("@ehl", chkEhliyet.IsChecked == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@akt", chkAktif.IsChecked == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@ack", txtAciklama.Text ?? "");
        }

        private void dgPersoneller_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPersoneller.SelectedItem is DataRowView row)
            {
                seciliPersonelID = Convert.ToInt32(row["personel_id"]);
                txtAdSoyad.Text = row["ad_soyad"].ToString();
                txtTcNo.Text = row["tc_no"].ToString();
                txtTelefon.Text = row["telefon"].ToString();
                cbGorev.Text = row["gorev"].ToString();
                txtMaas.Text = row["maas"].ToString();
                dtIseGiris.SelectedDate = row["ise_giris_tarihi"] as DateTime?;
                dtDogum.SelectedDate = row["dogum_tarihi"] as DateTime?;
                txtAdres.Text = row["adres"].ToString();
                chkEhliyet.IsChecked = Convert.ToBoolean(row["ehliyet_varmi"]);
                chkAktif.IsChecked = Convert.ToBoolean(row["aktif"]);
                txtAciklama.Text = row["aciklama"].ToString();
            }
        }

        private void btnTemizle_Click(object sender, RoutedEventArgs e) => Temizle();

        private void Temizle()
        {
            seciliPersonelID = 0;
            txtAdSoyad.Clear(); txtTcNo.Clear(); txtTelefon.Clear();
            txtMaas.Clear(); txtAdres.Clear(); txtAciklama.Clear();
            cbGorev.SelectedIndex = -1;
            dtDogum.SelectedDate = null; dtIseGiris.SelectedDate = null;
            chkEhliyet.IsChecked = false; chkAktif.IsChecked = true;
            dgPersoneller.SelectedItem = null;
        }

        private void btnPersonelYenile_Click(object sender, RoutedEventArgs e)
        {
            Listele(); 
            IstatistikleriGuncelle(); 
            
        }
    }
}