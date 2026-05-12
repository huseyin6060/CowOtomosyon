using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class HayvanSatis : UserControl
    {
        private readonly string connStr = App.connectionString;

        public HayvanSatis()
        {
            InitializeComponent();

            txtCanliKg.TextChanged += HesaplaTutar;
            txtKgFiyati.TextChanged += HesaplaTutar;

            VerileriTazele();
        }

        private void VerileriTazele()
        {
            SatisaUygunHayvanlariGetir();
            SatisGecmisiniGetir();
            MusteriListesiniGetir();
            IstatistikleriGuncelle();
        }

        

        private void MusteriListesiniGetir()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"SELECT musteri_id, 
                                 LTRIM(RTRIM(ISNULL(firma_adi, '') + ' ' + ISNULL(ad, '') + ' ' + ISNULL(soyad, ''))) as MusteriGorunumu 
                                 FROM Tbl_Musteriler WHERE aktif = 1";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cbMusteri.ItemsSource = dt.DefaultView;
                    cbMusteri.DisplayMemberPath = "MusteriGorunumu";
                    cbMusteri.SelectedValuePath = "musteri_id";
                }
            }
            catch (Exception ex) { MessageBox.Show("Müşteri listesi yüklenemedi: " + ex.Message); }
        }

        private void SatisaUygunHayvanlariGetir(string arama = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
          
                    string query = "SELECT hayvan_id, kupe_no, hayvan_adi, irk, kilo FROM Tbl_Hayvanlar WHERE durum = 'Aktif'";

                    if (!string.IsNullOrEmpty(arama))
                        query += " AND (kupe_no LIKE @ara OR hayvan_adi LIKE @ara)";

                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    if (!string.IsNullOrEmpty(arama)) da.SelectCommand.Parameters.AddWithValue("@ara", "%" + arama + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgSatisUygunHayvanlar.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Uygun hayvanlar listelenirken hata: " + ex.Message); }
        }

        private void SatisGecmisiniGetir(string arama = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string query = @"SELECT s.*, 
                                   LTRIM(RTRIM(ISNULL(m.firma_adi, '') + ' ' + ISNULL(m.ad, '') + ' ' + ISNULL(m.soyad, ''))) as MusteriAdSoyad, 
                                   h.kupe_no as HayvanKupeNo 
                                   FROM Tbl_HayvanSatis s
                                   INNER JOIN Tbl_Musteriler m ON s.musteri_id = m.musteri_id
                                   INNER JOIN Tbl_Hayvanlar h ON s.hayvan_id = h.hayvan_id";

                    if (!string.IsNullOrEmpty(arama))
                        query += " WHERE m.ad LIKE @ara OR m.firma_adi LIKE @ara OR h.kupe_no LIKE @ara";

                    query += " ORDER BY s.satis_tarihi DESC";

                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    if (!string.IsNullOrEmpty(arama)) da.SelectCommand.Parameters.AddWithValue("@ara", "%" + arama + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgSatisGecmisi.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Satış geçmişi yükleme hatası: " + ex.Message); }
        }

 

        private void HesaplaTutar(object sender, TextChangedEventArgs e)
        {
            decimal kg = 0, fiyat = 0;
            decimal.TryParse(txtCanliKg.Text.Replace(".", ","), out kg);
            decimal.TryParse(txtKgFiyati.Text.Replace(".", ","), out fiyat);
            txtToplamTutar.Text = (kg * fiyat).ToString("C2");
        }

        private void dgSatisUygunHayvanlar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSatisUygunHayvanlar.SelectedItem is DataRowView row)
            {
                txtHayvanID.Text = row["hayvan_id"].ToString();
                txtSecilenHayvan.Text = row["kupe_no"].ToString();
                txtCanliKg.Text = row["kilo"].ToString();
            }
        }

        private void dgSatisGecmisi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSatisGecmisi.SelectedItem is DataRowView row)
            {
                txtSatisID.Text = row["satis_id"].ToString();
                txtHayvanID.Text = row["hayvan_id"].ToString();
                cbMusteri.SelectedValue = row["musteri_id"];
                dtSatisTarihi.SelectedDate = Convert.ToDateTime(row["satis_tarihi"]);
                txtCanliKg.Text = row["canli_kg"].ToString();
                txtKarkasKg.Text = row["karkas_kg"].ToString();
                txtKgFiyati.Text = row["kg_fiyat"].ToString();
                txtAciklama.Text = row["aciklama"].ToString();
            }
        }

     

        private void btnSatiskadet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtHayvanID.Text) || cbMusteri.SelectedValue == null)
            {
                MessageBox.Show("Hayvan ve Müşteri seçimi zorunludur!"); return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string sql = @"INSERT INTO Tbl_HayvanSatis (hayvan_id, musteri_id, satis_tarihi, canli_kg, karkas_kg, kg_fiyat, aciklama) 
                                          VALUES (@hId, @mId, @tarih, @ckg, @kkg, @fiyat, @not)";

                            SqlCommand cmd = new SqlCommand(sql, conn, trans);
                            cmd.Parameters.AddWithValue("@hId", txtHayvanID.Text);
                            cmd.Parameters.AddWithValue("@mId", cbMusteri.SelectedValue);
                            cmd.Parameters.AddWithValue("@tarih", dtSatisTarihi.SelectedDate ?? DateTime.Now);
                            cmd.Parameters.AddWithValue("@ckg", decimal.TryParse(txtCanliKg.Text.Replace(".", ","), out decimal ckg) ? ckg : 0);
                            cmd.Parameters.AddWithValue("@kkg", decimal.TryParse(txtKarkasKg.Text.Replace(".", ","), out decimal kkg) ? kkg : 0);
                            cmd.Parameters.AddWithValue("@fiyat", decimal.TryParse(txtKgFiyati.Text.Replace(".", ","), out decimal fiyat) ? fiyat : 0);
                            cmd.Parameters.AddWithValue("@not", txtAciklama.Text ?? "");
                            cmd.ExecuteNonQuery();

                            SqlCommand cmdUpd = new SqlCommand("UPDATE Tbl_Hayvanlar SET durum = 'Satıldı' WHERE hayvan_id = @hId", conn, trans);
                            cmdUpd.Parameters.AddWithValue("@hId", txtHayvanID.Text);
                            cmdUpd.ExecuteNonQuery();

                            trans.Commit();
                            MessageBox.Show("Satış başarıyla kaydedildi.");
                            VerileriTazele();
                            FormuTemizle();
                        }
                        catch { trans.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Kayıt hatası: " + ex.Message); }
        }

        private void btnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSatisID.Text)) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"UPDATE Tbl_HayvanSatis SET musteri_id=@mId, satis_tarihi=@tarih, canli_kg=@ckg, 
                                  karkas_kg=@kkg, kg_fiyat=@fiyat, aciklama=@not WHERE satis_id=@sId";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@sId", txtSatisID.Text);
                    cmd.Parameters.AddWithValue("@mId", cbMusteri.SelectedValue);
                    cmd.Parameters.AddWithValue("@tarih", dtSatisTarihi.SelectedDate ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@ckg", decimal.TryParse(txtCanliKg.Text.Replace(".", ","), out decimal ckg) ? ckg : 0);
                    cmd.Parameters.AddWithValue("@kkg", decimal.TryParse(txtKarkasKg.Text.Replace(".", ","), out decimal kkg) ? kkg : 0);
                    cmd.Parameters.AddWithValue("@fiyat", decimal.TryParse(txtKgFiyati.Text.Replace(".", ","), out decimal fiyat) ? fiyat : 0);
                    cmd.Parameters.AddWithValue("@not", txtAciklama.Text ?? "");

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Kayıt güncellendi.");
                    VerileriTazele();
                }
            }
            catch (Exception ex) { MessageBox.Show("Güncelleme hatası: " + ex.Message); }
        }

        private void txtKupeAra_TextChanged(object sender, TextChangedEventArgs e) => SatisaUygunHayvanlariGetir((sender as TextBox)!.Text);
        private void txtmusterisatısıAra_TextChanged(object sender, TextChangedEventArgs e) => SatisGecmisiniGetir((sender as TextBox)!.Text);
        private void btnSatisauygun_Click(object sender, RoutedEventArgs e) => SatisaUygunHayvanlariGetir();
        private void btnYenile2_Click(object sender, RoutedEventArgs e) => SatisGecmisiniGetir();
        private void btnTemizle_Click(object sender, RoutedEventArgs e) => FormuTemizle();

        private void IstatistikleriGuncelle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) as Adet, SUM(toplam_tutar) as Ciro FROM Tbl_HayvanSatis";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        txtToplamSatisSayisi.Text = dr["Adet"].ToString();
                        txtToplamCiro.Text = string.Format("{0:C2}", dr["Ciro"] == DBNull.Value ? 0 : dr["Ciro"]);
                    }
                }
            }
            catch { }
        }

        private void FormuTemizle()
        {
            txtSatisID.Clear(); txtHayvanID.Clear(); txtSecilenHayvan.Clear();
            cbMusteri.SelectedIndex = -1; txtCanliKg.Clear(); txtKarkasKg.Clear();
            txtKgFiyati.Clear(); txtToplamTutar.Text = "0,00 ₺"; txtAciklama.Clear();
            dtSatisTarihi.SelectedDate = DateTime.Now;
        }

        private void Sil_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}