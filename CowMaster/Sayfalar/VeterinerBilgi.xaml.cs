using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using CowMaster; 
namespace CowMaster.Sayfalar
{
    public partial class VeterinerBilgi : UserControl
    {
        private readonly string connString = App.connectionString;

        public VeterinerBilgi()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                VerileriYukle();
                HastaliklariYukle();
                ComboDoldur();
            };
        }

        private void VerileriYukle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string queryHayvan = "SELECT hayvan_id, kupe_no, irk, cinsiyet, hayvan_adi, tur FROM Tbl_Hayvanlar WHERE durum = 'Aktif' OR durum = 'Hastalıklı'";
                    SqlDataAdapter daHayvan = new SqlDataAdapter(queryHayvan, conn);
                    DataTable dtHayvan = new DataTable();
                    daHayvan.Fill(dtHayvan);
                    dgHayvanlar.ItemsSource = dtHayvan.DefaultView;

                    string queryStats = @"
                        SELECT 
                            (SELECT COUNT(*) FROM Tbl_Hayvanlar) as Toplam,
                            (SELECT COUNT(DISTINCT hayvan_id) FROM Tbl_VeterinerIslem WHERE tarih = CAST(GETDATE() AS DATE)) as GunlukIslem,
                            (SELECT COUNT(*) FROM Tbl_AsiTakip WHERE yapilma_tarihi = CAST(GETDATE() AS DATE)) as GunlukAsi,
                            (SELECT SUM(maliyet) FROM Tbl_VeterinerIslem WHERE MONTH(tarih) = MONTH(GETDATE()) AND YEAR(tarih) = YEAR(GETDATE())) as AylikMaliyet";

                    using (SqlCommand cmd = new SqlCommand(queryStats, conn))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                txtToplamHayvan.Text = dr["Toplam"].ToString();
                                txtHasta.Text = dr["GunlukIslem"].ToString();
                                txtAsi.Text = dr["GunlukAsi"].ToString();
                                txtMaliyet.Text = string.Format("{0:N2} ₺", dr["AylikMaliyet"] == DBNull.Value ? 0 : dr["AylikMaliyet"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Veri Yükleme Hatası: " + ex.Message); }
        }

        private void ComboDoldur()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                
                    SqlDataAdapter daV = new SqlDataAdapter("SELECT v.veteriner_id, (v.ad + ' ' + v.soyad) as ad_soyad FROM Tbl_Veterinerler v", conn);
                    DataTable dtV = new DataTable();
                    daV.Fill(dtV);
                    cbVeteriner.ItemsSource = dtV.DefaultView;
                    cbVeteriner.DisplayMemberPath = "ad_soyad";
                    cbVeteriner.SelectedValuePath = "veteriner_id";

                    SqlDataAdapter daH = new SqlDataAdapter("SELECT hastalik_id, hastalik_adi FROM Tbl_Hastaliklar WHERE durum = 1", conn);
                    DataTable dtH = new DataTable();
                    daH.Fill(dtH);
                    cbHastalik.ItemsSource = dtH.DefaultView;
                    cbHastalik.DisplayMemberPath = "hastalik_adi";
                    cbHastalik.SelectedValuePath = "hastalik_id";
                }
            }
            catch (Exception ex) { MessageBox.Show("Combo Doldurma Hatası: " + ex.Message); }
        }

        private void HayvanDetayYukle(string hayvanId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string sqlIslem = @"   SELECT vi.islem_id, v.ad, v.soyad, h.hastalik_adi,  vi.islem_turu, vi.ilac_adi, vi.tarih, vi.sonraki_kontrol, vi.maliyet,
                                           vi.tedavi FROM Tbl_VeterinerIslem vi LEFT JOIN Tbl_Veterinerler v ON vi.veteriner_id = v.veteriner_id LEFT JOIN Tbl_Hastaliklar h 
                                           ON vi.hastalik_id = h.hastalik_id WHERE vi.hayvan_id = @id";

                    SqlCommand cmdIslem = new SqlCommand(sqlIslem, conn);
                    cmdIslem.Parameters.AddWithValue("@id", hayvanId);
                    SqlDataAdapter daV = new SqlDataAdapter(cmdIslem);
                    DataTable dtV = new DataTable(); daV.Fill(dtV);
                    dgVeteriner.ItemsSource = dtV.DefaultView;

                    SqlDataAdapter daA = new SqlDataAdapter($"SELECT * FROM Tbl_AsiTakip WHERE hayvan_id = {hayvanId}", conn);
                    DataTable dtA = new DataTable(); daA.Fill(dtA);
                    dgAsiTakip.ItemsSource = dtA.DefaultView;

                    SqlDataAdapter daG = new SqlDataAdapter($"SELECT * FROM Tbl_Gebelik WHERE hayvan_id = {hayvanId}", conn);
                    DataTable dtG = new DataTable(); daG.Fill(dtG);
                    dgGebelik.ItemsSource = dtG.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Detay Yükleme Hatası: " + ex.Message); }
        }

        private void dgHayvanlar_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (dgHayvanlar.SelectedItem is DataRowView row)
            {
                txtKupeNo.Text = row["kupe_no"].ToString();
                HayvanDetayYukle(row["hayvan_id"].ToString()!);
            }
        }

        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (dgHayvanlar.SelectedItem == null) { MessageBox.Show("Hayvan seçiniz!"); return; }
            ExecuteParametricQuery(@"INSERT INTO Tbl_VeterinerIslem (hayvan_id, hastalik_id, veteriner_id, islem_turu, ilac_adi, tarih, sonraki_kontrol, maliyet, tedavi) 
                                   VALUES (@hid, @hastid, @pid, @tur, @ilac, @tarih, @sonraki, @maliyet, @not)");
        }

        private void btnAsı_Click(object sender, RoutedEventArgs e)
        {
            if (dgHayvanlar.SelectedItem == null) { MessageBox.Show("Hayvan seçiniz!"); return; }
            ExecuteParametricQuery(@"INSERT INTO Tbl_AsiTakip (hayvan_id, asi_adi, yapilma_tarihi, tekrar_tarihi, durum) 
                                   VALUES (@hid, @ad, @t1, @t2, @durum)");
        }

        private void btnGebelik_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                if (dgHayvanlar.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen listeden bir hayvan seçiniz!");
                    return;
                }
                DataRowView row = (DataRowView)dgHayvanlar.SelectedItem;
                string hayvanId = row["hayvan_id"].ToString()!;
                DateTime tohumTarih = dtTohumlama.SelectedDate ?? DateTime.Now;
                DateTime dogumTarih = dtTahminiDogum.SelectedDate ?? tohumTarih.AddDays(280);
                string durum = (cbGebelikDurum.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Beklemede";
                string not = txtGebelikNotu.Text ?? "";

       
                string sql = @"INSERT INTO Tbl_Gebelik (hayvan_id, tohumlama_tarihi, tahmini_dogum, durum, veteriner_notu) 
                       VALUES (@hid, @t1, @t2, @durum, @not)";

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                      
                        cmd.Parameters.AddWithValue("@hid", hayvanId);
                        cmd.Parameters.AddWithValue("@t1", tohumTarih);
                        cmd.Parameters.AddWithValue("@t2", dogumTarih);
                        cmd.Parameters.AddWithValue("@durum", durum);
                        cmd.Parameters.AddWithValue("@not", not);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Gebelik kaydı başarıyla eklendi.");
                HayvanDetayYukle(hayvanId); 
            }
            catch (Exception ex)
            {
          
                MessageBox.Show("Gebelik Kaydedilemedi!\nHata: " + ex.Message);
            }
        }
        private void Guncelle1_Click(object sender, RoutedEventArgs e)
        {
            if (dgHastaliklar.SelectedItem is DataRowView row)
                ExecuteParametricQuery(@"UPDATE Tbl_Hastaliklar SET hastalik_adi=@adi, belirtiler=@belirti, bulasici=@bulasici, tedavi=@tedavi WHERE hastalik_id=@id", row["hastalik_id"]);
        }

        private void Sil1_Click(object sender, RoutedEventArgs e)
        {
            if (dgHastaliklar.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Bu hastalığı listeden kaldırmak istediğinize emin misiniz? (Geçmiş kayıtlar silinmez)", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    string id = row["hastalik_id"].ToString()!;
                    ExecuteSimpleQuery($"UPDATE Tbl_Hastaliklar SET durum = 0 WHERE hastalik_id={id}");
                    MessageBox.Show("Hastalık pasif duruma getirildi.");
                    HastaliklariYukle();
                    ComboDoldur();
                }
            }
        }

        private void HastaliklariYukle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                   
                    string sql = @"SELECT hastalik_id, hastalik_adi, belirtiler, 
                                 CASE WHEN bulasici = 1 THEN 'Evet' ELSE 'Hayır' END AS bulasici_metin, 
                                 bulasici, tedavi FROM Tbl_Hastaliklar WHERE durum = 1";
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgHastaliklar.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btnHastalikEkle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHastalıkAdi.Text)) { MessageBox.Show("Lütfen hastalık adını giriniz!"); return; }
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    
                    string sql = @"INSERT INTO Tbl_Hastaliklar (hastalik_adi, belirtiler, bulasici, tedavi, durum) 
                                   VALUES (@adi, @belirti, @bulasici, @tedavi, 1)";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@adi", txtHastalıkAdi.Text.Trim());
                        cmd.Parameters.AddWithValue("@belirti", txtbelirtiler.Text.Trim());
                        cmd.Parameters.AddWithValue("@bulasici", (rbhayır.IsChecked == true) ? 0 : 1);
                        cmd.Parameters.AddWithValue("@tedavi", txttedavi.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Hastalık eklendi.");
                    HastaliklariYukle();
                    ComboDoldur();
                    btnTemizle_Click(null!, null!);
                }
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void ExecuteParametricQuery(string sql, object updateId = null!)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    DataRowView rowHayvan = (DataRowView)dgHayvanlar.SelectedItem;

                    cmd.Parameters.AddWithValue("@id", updateId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@hid", rowHayvan?["hayvan_id"] ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@hastid", cbHastalik.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pid", cbVeteriner.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tur", txtIslemTuru.Text);
                    cmd.Parameters.AddWithValue("@ilac", txtIlacAdi.Text);
                    cmd.Parameters.AddWithValue("@tarih", dtIslemTarihi.SelectedDate ?? (object)DateTime.Now);
                    cmd.Parameters.AddWithValue("@sonraki", (object)dtSonrakiKontrol.SelectedDate! ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@maliyet", decimal.TryParse(txtMaliyetler.Text, out decimal m) ? m : 0);
                    cmd.Parameters.AddWithValue("@not", txtTedaviNotu.Text ?? txtGebelikNotu.Text);
                    cmd.Parameters.AddWithValue("@ad", txtAsiAdi.Text);
                    cmd.Parameters.AddWithValue("@adi", txtHastalıkAdi.Text);
                    cmd.Parameters.AddWithValue("@belirti", txtbelirtiler.Text);
                    cmd.Parameters.AddWithValue("@bulasici", rbhayır.IsChecked == true ? 0 : 1);
                    cmd.Parameters.AddWithValue("@tedavi", txttedavi.Text);
                    cmd.Parameters.AddWithValue("@t1", dtAsiYapilma.SelectedDate ?? dtTohumlama.SelectedDate ?? (object)DateTime.Now);
                    cmd.Parameters.AddWithValue("@t2", (object)dtAsiTekrar.SelectedDate! ?? dtTahminiDogum.SelectedDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@durum", (cbAsiDurum.SelectedItem as ComboBoxItem)?.Content.ToString() ?? (cbGebelikDurum.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Bekliyor");

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("İşlem başarıyla tamamlandı.");
                    HastaliklariYukle();
                    if (rowHayvan != null) HayvanDetayYukle(rowHayvan["hayvan_id"].ToString()!);
                }
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void ExecuteSimpleQuery(string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    new SqlCommand(query, conn).ExecuteNonQuery();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void dgHastaliklar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgHastaliklar.SelectedItem is DataRowView row)
            {
                txtHastalıkAdi.Text = row["hastalik_adi"].ToString();
                txtbelirtiler.Text = row["belirtiler"].ToString();
                txttedavi.Text = row["tedavi"].ToString();
                bool isBulasici = row["bulasici"] != DBNull.Value && Convert.ToBoolean(row["bulasici"]);
                rbhayır.IsChecked = !isBulasici;
            }
        }

        private void btnTemizle_Click(object sender, RoutedEventArgs e)
        {
            txtKupeNo.Clear(); txtIslemTuru.Clear(); txtIlacAdi.Clear(); txtMaliyetler.Clear();
            txtTedaviNotu.Clear(); txtAsiAdi.Clear(); txtGebelikNotu.Clear();
            txtHastalıkAdi.Clear(); txtbelirtiler.Clear(); txttedavi.Clear();
            cbHastalik.SelectedIndex = -1; cbVeteriner.SelectedIndex = -1;
            cbAsiDurum.SelectedIndex = -1; cbGebelikDurum.SelectedIndex = -1;
            dtIslemTarihi.SelectedDate = null; dtSonrakiKontrol.SelectedDate = null;
        }

        private void dgVeteriner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVeteriner.SelectedItem is DataRowView row)
            {
                txtIslemTuru.Text = row["islem_turu"].ToString();
                txtIlacAdi.Text = row["ilac_adi"].ToString();
                txtMaliyetler.Text = row["maliyet"].ToString();

                txtTedaviNotu.Text = row["tedavi"].ToString();

           
                string ad = row["ad"].ToString()!;
                string soyad = row["soyad"].ToString()!;

                foreach (DataRowView item in cbVeteriner.Items)
                {
                    if (item["ad_soyad"].ToString() == ad + " " + soyad)
                    {
                        cbVeteriner.SelectedItem = item;
                        break;
                    }
                }


                string hastalik = row["hastalik_adi"].ToString()!;

                foreach (DataRowView item in cbHastalik.Items)
                {
                    if (item["hastalik_adi"].ToString() == hastalik)
                    {
                        cbHastalik.SelectedItem = item;
                        break;
                    }
                }

            
                if (row["tarih"] != DBNull.Value)
                    dtIslemTarihi.SelectedDate = Convert.ToDateTime(row["tarih"]);

                if (row["sonraki_kontrol"] != DBNull.Value)
                    dtSonrakiKontrol.SelectedDate = Convert.ToDateTime(row["sonraki_kontrol"]);
            }
        }

        private void dgAsiTakip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAsiTakip.SelectedItem is DataRowView row)
            {
                txtAsiAdi.Text = row["asi_adi"].ToString();
                dtAsiYapilma.SelectedDate = Convert.ToDateTime(row["yapilma_tarihi"]);
            }
        }

        private void dgGebelik_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgGebelik.SelectedItem is DataRowView row)
            {
                dtTohumlama.SelectedDate = Convert.ToDateTime(row["tohumlama_tarihi"]);
                txtGebelikNotu.Text = row["veteriner_notu"].ToString();
            }
        }

        private void Guncelle2_Click(object sender, RoutedEventArgs e)
        {
            if (dgVeteriner.SelectedItem is DataRowView row)
            {
                ExecuteSimpleQuery($"UPDATE Tbl_VeterinerIslem SET islem_turu='{txtIslemTuru.Text}', ilac_adi='{txtIlacAdi.Text}', maliyet={txtMaliyetler.Text.Replace(",", ".")} WHERE islem_id={row["islem_id"]}");
                MessageBox.Show("Güncellendi.");
                if (dgHayvanlar.SelectedItem is DataRowView h) HayvanDetayYukle(h["hayvan_id"].ToString()!);
            }
        }

        private void Sil2_Click(object sender, RoutedEventArgs e)
        {
            if (dgVeteriner.SelectedItem is DataRowView row && MessageBox.Show("Silinsin mi?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ExecuteSimpleQuery($"DELETE FROM Tbl_VeterinerIslem WHERE islem_id={row["islem_id"]}");
                if (dgHayvanlar.SelectedItem is DataRowView h) HayvanDetayYukle(h["hayvan_id"].ToString()!);
            }
        }

        private void Guncelle3_Click(object sender, RoutedEventArgs e)
        {
            if (dgAsiTakip.SelectedItem is DataRowView row)
            {
                string durum = (cbAsiDurum.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Bekliyor";
                ExecuteSimpleQuery($"UPDATE Tbl_AsiTakip SET asi_adi='{txtAsiAdi.Text}', durum='{durum}' WHERE asi_id={row["asi_id"]}");
                MessageBox.Show("Güncellendi.");
                if (dgHayvanlar.SelectedItem is DataRowView h) HayvanDetayYukle(h["hayvan_id"].ToString()!);
            }
        }

        private void Sil3_Click(object sender, RoutedEventArgs e)
        {
            if (dgAsiTakip.SelectedItem is DataRowView row && MessageBox.Show("Silinsin mi?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ExecuteSimpleQuery($"DELETE FROM Tbl_AsiTakip WHERE asi_id={row["asi_id"]}");
                if (dgHayvanlar.SelectedItem is DataRowView h) HayvanDetayYukle(h["hayvan_id"].ToString()!);
            }
        }

        private void Guncelle4_Click(object sender, RoutedEventArgs e)
        {
            if (dgGebelik.SelectedItem is DataRowView row)
            {
                string durum = (cbGebelikDurum.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Devam Ediyor";
                ExecuteSimpleQuery($"UPDATE Tbl_Gebelik SET veteriner_notu='{txtGebelikNotu.Text}', durum='{durum}' WHERE gebelik_id={row["gebelik_id"]}");
                MessageBox.Show("Güncellendi.");
                if (dgHayvanlar.SelectedItem is DataRowView h) HayvanDetayYukle(h["hayvan_id"].ToString()!);
            }
        }

        private void Sil4_Click(object sender, RoutedEventArgs e)
        {
            if (dgGebelik.SelectedItem is DataRowView row && MessageBox.Show("Silinsin mi?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ExecuteSimpleQuery($"DELETE FROM Tbl_Gebelik WHERE gebelik_id={row["gebelik_id"]}");
                if (dgHayvanlar.SelectedItem is DataRowView h) HayvanDetayYukle(h["hayvan_id"].ToString()!);
            }
        }

        private void btnVeterinerEkle_Click(object sender, RoutedEventArgs e)
        {
            
            var anaPencere = System.Windows.Application.Current.MainWindow as CowMaster.MainWindow;

            if (anaPencere != null)
            {
         
                CowMaster.Sayfalar.VeterinerEkle vEkran = new CowMaster.Sayfalar.VeterinerEkle();
                anaPencere.SayfaIcerigi.Content = vEkran;
            }
        }



    }
}