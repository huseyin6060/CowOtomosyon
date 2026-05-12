using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class Hayvanlarım : UserControl
    {
        private int secilenHayvanId = 0;

        public Hayvanlarım()
        {
            InitializeComponent();
            VerileriYukle();
        }

        private async void VerileriYukle()
        {
            await ListeleAsync();
            await IstatistikleriGuncelleAsync();
        }

        private async Task ListeleAsync(string aramaKelimesi = "")
        {
            try
            {
                using (SqlConnection con = new SqlConnection(App.connectionString))
                {
                    string query = "SELECT * FROM Tbl_Hayvanlar";
                    if (!string.IsNullOrEmpty(aramaKelimesi))
                    {
                        query += " WHERE kupe_no LIKE @arama OR hayvan_adi LIKE @arama";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(aramaKelimesi))
                        {
                            cmd.Parameters.AddWithValue("@arama", "%" + aramaKelimesi + "%");
                        }

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();

                        await Task.Run(() => da.Fill(dt));
                        dgHayvanlar.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veriler listelenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void txtHayvanAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            await ListeleAsync(txtHayvanAra.Text);
        }

        private async void btnyenile_Click(object sender, RoutedEventArgs e)
        {
            await ListeleAsync();
            await IstatistikleriGuncelleAsync();
            txtHayvanAra.Clear();
        }

        private async Task IstatistikleriGuncelleAsync()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(App.connectionString))
                {
                    string query = @"
                        SELECT 
                            COUNT(hayvan_id) AS ToplamHayvan,
                            SUM(CASE WHEN durum = 'Aktif' THEN 1 ELSE 0 END) AS AktifHayvan,
                            AVG(ISNULL(kilo, 0)) AS OrtalamaKilo,
                            SUM(CASE WHEN durum = 'Hasta' THEN 1 ELSE 0 END) AS HastaHayvan,
                            COUNT(DISTINCT ahir_no) AS AhirSayisi
                        FROM Tbl_Hayvanlar";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        await con.OpenAsync();
                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                txtToplamHayvan.Text = dr["ToplamHayvan"]?.ToString() ?? "0";
                                txtAktif.Text = dr["AktifHayvan"]?.ToString() ?? "0";

                                if (dr["OrtalamaKilo"] != DBNull.Value)
                                {
                                    decimal ortKilo = Convert.ToDecimal(dr["OrtalamaKilo"]);
                                    txtOrtalamaKilo.Text = ortKilo.ToString("0.##") + " KG";
                                }
                                else { txtOrtalamaKilo.Text = "0 KG"; }

                                txtHasta.Text = dr["HastaHayvan"]?.ToString() ?? "0";
                                txtAhir.Text = dr["AhirSayisi"]?.ToString() ?? "0";
                            }
                        }
                    }
                }
            }
            catch {}
        }

      

  

        private void ParametreleriEkle(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@p1", txtKupeNo.Text.Trim());
            cmd.Parameters.AddWithValue("@p2", txtHayvanAdi.Text.Trim());
            cmd.Parameters.AddWithValue("@p3", txtIrk.Text.Trim());
            cmd.Parameters.AddWithValue("@p4", cbCinsiyet.SelectedItem != null ? (cbCinsiyet.SelectedItem as ComboBoxItem)!.Content.ToString() : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p5", dtDogum.SelectedDate.HasValue ? dtDogum.SelectedDate.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p6", decimal.TryParse(txtKilo.Text, out decimal kilo) ? kilo : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p7", txtRenk.Text.Trim());
            cmd.Parameters.AddWithValue("@p8", txtAnneno.Text.Trim());
            cmd.Parameters.AddWithValue("@p9", txtBaba.Text.Trim());
            cmd.Parameters.AddWithValue("@p10", cbDurum.SelectedItem != null ? (cbDurum.SelectedItem as ComboBoxItem)!.Content.ToString() : "Aktif");
            cmd.Parameters.AddWithValue("@p11", txtAhirNo.Text.Trim());
            cmd.Parameters.AddWithValue("@p12", dtGiris.SelectedDate.HasValue ? dtGiris.SelectedDate.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p13", decimal.TryParse(txtSatisFiyati.Text, out decimal fiyat) ? fiyat : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p14", txtAciklama.Text.Trim());
            cmd.Parameters.AddWithValue("@p15", cbgenclikdurumu.SelectedItem is ComboBoxItem gdi ? gdi.Content.ToString() : (object)DBNull.Value);
        }

   
        private void Temizle()
        {
            secilenHayvanId = 0;
            txtKupeNo.Clear();
            txtHayvanAdi.Clear();
            txtIrk.Clear();
            cbCinsiyet.SelectedIndex = -1;
            txtKilo.Clear();
            txtRenk.Clear();
            txtAhirNo.Clear();
            dtDogum.SelectedDate = null;
            dtGiris.SelectedDate = null;
            cbDurum.SelectedIndex = -1;
            txtSatisFiyati.Clear();
            txtAciklama.Clear();
            dgHayvanlar.SelectedItem = null;
        }

        private void dgHayvanlar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgHayvanlar.SelectedItem is DataRowView row)
            {
                secilenHayvanId = Convert.ToInt32(row["hayvan_id"]);
                txtKupeNo.Text = row["kupe_no"].ToString();
                txtHayvanAdi.Text = row["hayvan_adi"].ToString();
                txtIrk.Text = row["irk"].ToString();
                txtBaba.Text = row["baba_Kno"].ToString();
                txtAnneno.Text = row["anne_Kno"].ToString();
                string cinsiyet = row["cinsiyet"].ToString()!;
                cbCinsiyet.SelectedIndex = (cinsiyet == "Erkek") ? 0 : (cinsiyet == "Dişi" ? 1 : -1);

                txtKilo.Text = row["kilo"].ToString();
                txtRenk.Text = row["renk"].ToString();
                txtAhirNo.Text = row["ahir_no"].ToString();

                if (row["dogum_tarihi"] != DBNull.Value) dtDogum.SelectedDate = Convert.ToDateTime(row["dogum_tarihi"]);
                if (row["giris_tarihi"] != DBNull.Value) dtGiris.SelectedDate = Convert.ToDateTime(row["giris_tarihi"]);

                string durum = row["durum"].ToString()!;
                foreach (ComboBoxItem item in cbDurum.Items)
                {
                    if (item.Content.ToString() == durum) { cbDurum.SelectedItem = item; break; }
                }

                txtSatisFiyati.Text = row["satis_fiyati"].ToString();
                txtAciklama.Text = row["aciklama"].ToString();

                string genclik = row["genclikdurumu"].ToString()!;
                foreach (ComboBoxItem item in cbgenclikdurumu.Items)
                {
                    if (item.Content.ToString() == genclik)
                    {
                        cbgenclikdurumu.SelectedItem = item;
                        break;
                    }
                }

            }
        }

    
        private async void btnKaydet_Click_1(object sender, RoutedEventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(txtKupeNo.Text))
            {
                MessageBox.Show("Küpe No boş bırakılamaz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(App.connectionString))
                {
                    string query = @"INSERT INTO Tbl_Hayvanlar 
                                    (kupe_no, hayvan_adi, irk, cinsiyet, dogum_tarihi, kilo, renk,anne_Kno,baba_Kno ,durum, ahir_no, giris_tarihi, satis_fiyati, aciklama,genclikdurumu) 
                                    VALUES 
                                    (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12,@p13,@p14,@p15)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        ParametreleriEkle(cmd); 
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();

                        MessageBox.Show("Hayvan başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);

                        Temizle(); 
                        await ListeleAsync(); 
                        await IstatistikleriGuncelleAsync(); 
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void guncelel_Click(object sender, RoutedEventArgs e)
        {
            if (secilenHayvanId == 0)
            {
                MessageBox.Show("Lütfen güncellenecek hayvanı listeden seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(App.connectionString))
                {
                    string query = @"UPDATE Tbl_Hayvanlar SET 
                                    kupe_no=@p1, hayvan_adi=@p2, irk=@p3, cinsiyet=@p4, dogum_tarihi=@p5, 
                                    kilo=@p6, renk=@p7, anne_Kno=@p8, baba_Kno=@p9, durum=@p10, 
                                    ahir_no=@p11, giris_tarihi=@p12 ,satis_fiyati=@p13,aciklama=@p14,genclikdurumu=@p15
                                    WHERE hayvan_id=@id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        ParametreleriEkle(cmd);
                        cmd.Parameters.AddWithValue("@id", secilenHayvanId);

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();

                        MessageBox.Show("Hayvan bilgileri güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);

                        Temizle();
                        await ListeleAsync();
                        await IstatistikleriGuncelleAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güncelleme hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Temizle_Click_1(object sender, RoutedEventArgs e)
        {
            Temizle(); 
        }



        private async void Sil_Click(object sender, RoutedEventArgs e)
        {
            
            if (secilenHayvanId == 0)
            {
                MessageBox.Show("Lütfen silmek istediğiniz hayvanı listeden seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult onay = MessageBox.Show($"{txtKupeNo.Text} küpe numaralı hayvanı silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (onay == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(App.connectionString))
                    {
                       
                        string query = "DELETE FROM Tbl_Hayvanlar WHERE hayvan_id = @id";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@id", secilenHayvanId);

                            await con.OpenAsync(); 
                            await cmd.ExecuteNonQueryAsync(); 

                            MessageBox.Show("Hayvan başarıyla silindi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);

                            
                            Temizle();
                            await ListeleAsync();
                            await IstatistikleriGuncelleAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Silme işlemi sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}