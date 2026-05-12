using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CowMaster.Sayfalar
{
    public partial class VeterinerEkle : UserControl
    {
        
        private string strBgl = CowMaster.App.connectionString;

        public VeterinerEkle()
        {
            InitializeComponent();
            VeterinerListele();
            OzetIstatistikleriGetir();
        }

       
        private SqlConnection BaglantiGetir()
        {
            SqlConnection baglanti = new SqlConnection(strBgl);
            if (baglanti.State == ConnectionState.Closed) baglanti.Open();
            return baglanti;
        }


        private void VeterinerListele()
        {
            try
            {
                using (SqlConnection bgl = BaglantiGetir())
                {
                    DataTable dt = new DataTable();
                    SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Tbl_Veterinerler ORDER BY veteriner_id DESC", bgl);
                    da.Fill(dt);
                    dgVeterinerKayitlari.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Liste yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

      
        private void OzetIstatistikleriGetir()
        {
            try
            {
                using (SqlConnection bgl = BaglantiGetir())
                {
                    
                    SqlCommand komut = new SqlCommand("SELECT COUNT(*), SUM(CASE WHEN durum = 1 THEN 1 ELSE 0 END), SUM(ISNULL(alinan_odenek, 0)) FROM Tbl_Veterinerler", bgl);
                    SqlDataReader dr = komut.ExecuteReader();
                    if (dr.Read())
                    {
                        lblToplamVeteriner.Text = dr[0].ToString();
                        lblAktifVeteriner.Text = dr[1].ToString() ?? "0";
                        lblToplamOdenek.Text = string.Format("₺{0:N2}", dr[2] != DBNull.Value ? dr[2] : 0);
                    }
                }
            }
            catch {  }
        }

        
        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtAd.Text))
                {
                    MessageBox.Show("Lütfen veteriner adını giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection bgl = BaglantiGetir())
                {
                    SqlCommand komut = new SqlCommand("INSERT INTO Tbl_Veterinerler (ad, soyad, telefon, kayit_tarihi, cikis_tarihi, alinan_odenek, durum) VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7)", bgl);
                    komut.Parameters.AddWithValue("@p1", txtAd.Text.Trim().ToUpper());
                    komut.Parameters.AddWithValue("@p2", txtSoyad.Text.Trim().ToUpper());
                    komut.Parameters.AddWithValue("@p3", (object)txtTelefon.Text ?? DBNull.Value);
                    komut.Parameters.AddWithValue("@p4", dpKayitTarihi.SelectedDate ?? DateTime.Now);
                    komut.Parameters.AddWithValue("@p5", (object)dpCikisTarihi.SelectedDate! ?? DBNull.Value);

                    string odenekStr = txtAlinanOdenek.Text.Replace(".", ",");
                    komut.Parameters.AddWithValue("@p6", decimal.TryParse(odenekStr, out decimal odenek) ? odenek : 0);

                    komut.Parameters.AddWithValue("@p7", btnDurum.IsChecked == true);
                    komut.ExecuteNonQuery();
                }

                Temizle();
                VeterinerListele();
                OzetIstatistikleriGetir();
                MessageBox.Show("Yeni veteriner kaydı başarıyla eklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Kayıt hatası: " + ex.Message); }
        }

        
        private void Guncelle1_Click(object sender, RoutedEventArgs e)
        {
            if (dgVeterinerKayitlari.SelectedItem == null) return;

            try
            {
                DataRowView row = (DataRowView)dgVeterinerKayitlari.SelectedItem;
                int id = Convert.ToInt32(row["veteriner_id"]);

                using (SqlConnection bgl = BaglantiGetir())
                {
                    SqlCommand komut = new SqlCommand("UPDATE Tbl_Veterinerler SET ad=@p1, soyad=@p2, telefon=@p3, kayit_tarihi=@p4, cikis_tarihi=@p5, alinan_odenek=@p6, durum=@p7 WHERE veteriner_id=@p8", bgl);
                    komut.Parameters.AddWithValue("@p1", txtAd.Text.Trim().ToUpper());
                    komut.Parameters.AddWithValue("@p2", txtSoyad.Text.Trim().ToUpper());
                    komut.Parameters.AddWithValue("@p3", (object)txtTelefon.Text ?? DBNull.Value);
                    komut.Parameters.AddWithValue("@p5", (object)dpCikisTarihi.SelectedDate! ?? DBNull.Value);
                    komut.Parameters.AddWithValue("@p5", (object)dpCikisTarihi.SelectedDate! ?? DBNull.Value);

                    string odenekStr = txtAlinanOdenek.Text.Replace(".", ",");
                    komut.Parameters.AddWithValue("@p6", decimal.TryParse(odenekStr, out decimal odenek) ? odenek : 0);

                    komut.Parameters.AddWithValue("@p7", btnDurum.IsChecked == true);
                    komut.Parameters.AddWithValue("@p8", id);
                    komut.ExecuteNonQuery();
                }

                VeterinerListele();
                OzetIstatistikleriGetir();
                MessageBox.Show("Kayıt başarıyla güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Güncelleme hatası: " + ex.Message); }
        }

  
        private void Sil1_Click(object sender, RoutedEventArgs e)
        {
            if (dgVeterinerKayitlari.SelectedItem == null) return;

            if (MessageBox.Show("Seçili veteriner kaydını silmek istediğinize emin misiniz?", "Kayıt Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    DataRowView row = (DataRowView)dgVeterinerKayitlari.SelectedItem;
                    using (SqlConnection bgl = BaglantiGetir())
                    {
                        SqlCommand komut = new SqlCommand("DELETE FROM Tbl_Veterinerler WHERE veteriner_id=@p1", bgl);
                        komut.Parameters.AddWithValue("@p1", row["veteriner_id"]);
                        komut.ExecuteNonQuery();
                    }

                    Temizle();
                    VeterinerListele();
                    OzetIstatistikleriGetir();
                }
                catch (Exception ex) { MessageBox.Show("Silme işlemi başarısız: " + ex.Message); }
            }
        }

        
        private void dgVeterinerKayitlari_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVeterinerKayitlari.SelectedItem is DataRowView row)
            {
                txtAd.Text = row["ad"].ToString();
                txtSoyad.Text = row["soyad"].ToString();
                txtTelefon.Text = row["telefon"].ToString();
                dpKayitTarihi.SelectedDate = row["kayit_tarihi"] as DateTime?;
                dpCikisTarihi.SelectedDate = row["cikis_tarihi"] as DateTime?;
                txtAlinanOdenek.Text = row["alinan_odenek"].ToString();
                btnDurum.IsChecked = row["durum"] != DBNull.Value && (bool)row["durum"];
            }
        }

        
        private void btnYenile_Click(object sender, RoutedEventArgs e)
        {
            Temizle();
            VeterinerListele();
            OzetIstatistikleriGetir();
        }

       
        private void Temizle()
        {
            txtAd.Clear();
            txtSoyad.Clear();
            txtTelefon.Clear();
            txtAlinanOdenek.Clear();
            dpKayitTarihi.SelectedDate = null;
            dpCikisTarihi.SelectedDate = null;
            btnDurum.IsChecked = true;
            dgVeterinerKayitlari.SelectedItem = null;
        }

        
     
    }
}