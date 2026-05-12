using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CowMaster.Sayfalar
{
    public partial class Rasyon : UserControl
    {
        public Rasyon()
        {
            InitializeComponent();
            TumTablolariYukle();
        }

  

        public DataTable VeriGetir(string sql)
        {
            using (SqlConnection conn = new SqlConnection(App.connectionString))
            {
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                da.Fill(dt);
                return dt;
            }
        }

        private void StokMiktariGuncelle(int stokId, decimal miktar, bool artir)
        {
            string islem = artir ? "+" : "-";
            string sql = $"UPDATE Tbl_Stok SET mevcut_stok = mevcut_stok {islem} @miktar WHERE stok_id = @id";
            SQLCalistir(sql, new SqlParameter("@miktar", miktar), new SqlParameter("@id", stokId));
        }

        private void SQLCalistir(string sql, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem Hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal ToDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string cleaned = text.Replace(".", ",");
            if (decimal.TryParse(cleaned, out decimal result)) return result;
            return 0;
        }
        private void TumTablolariYukle()
        {
      
            dgStok.ItemsSource = VeriGetir("SELECT * FROM Tbl_Stok WHERE durum = 1").DefaultView;

            dgStokHareket.ItemsSource = VeriGetir(@"SELECT h.*, s.urun_adi FROM Tbl_StokHareket h 
                                            LEFT JOIN Tbl_Stok s ON h.stok_id = s.stok_id").DefaultView;

            dgRasyonlar.ItemsSource = VeriGetir("SELECT * FROM Tbl_Rasyonlar WHERE durum = 1").DefaultView;
            dgRasyonDetay.ItemsSource = VeriGetir(@"SELECT rd.*, r.rasyon_adi, s.urun_adi 
                                           FROM Tbl_RasyonDetay rd 
                                           LEFT JOIN Tbl_Rasyonlar r ON rd.rasyon_id = r.rasyon_id 
                                           LEFT JOIN Tbl_Stok s ON rd.stok_id = s.stok_id").DefaultView;

         
            DataTable aktifStoklar = VeriGetir("SELECT stok_id, urun_adi FROM Tbl_Stok WHERE durum = 1");
            cbStokSec.ItemsSource = aktifStoklar.DefaultView;
            cbStokSec.DisplayMemberPath = "urun_adi";
            cbStokSec.SelectedValuePath = "stok_id";


            cbRasyonSec.ItemsSource = VeriGetir("SELECT rasyon_id, rasyon_adi FROM Tbl_Rasyonlar WHERE durum = 1").DefaultView;
            cbRasyonSec.DisplayMemberPath = "rasyon_adi";
            cbRasyonSec.SelectedValuePath = "rasyon_id";

            cbRasyonStok.ItemsSource = aktifStoklar.DefaultView;
            cbRasyonStok.DisplayMemberPath = "urun_adi";
            cbRasyonStok.SelectedValuePath = "stok_id";

            KartlariGuncelle();
        }
        private void KartlariGuncelle()
        {
          
            DataTable dt = VeriGetir("SELECT urun_adi, mevcut_stok, birim FROM Tbl_Stok WHERE durum = 1");

            
            txtSilajStok.Text = "0";
            txtkuspeStok.Text = "0";
            txtYoncaStok.Text = "0";
            txtSamanStok.Text = "0";
            txtFıgStok.Text = "0";
            txtYulafStok.Text = "0";
            txtArpaStok.Text = "0";
            txtSutYemiStok.Text = "0";

            foreach (DataRow row in dt.Rows)
            {
                string ad = row["urun_adi"].ToString()!.ToLower(new CultureInfo("tr-TR"));

         
                decimal miktarRaw = row["mevcut_stok"] != DBNull.Value ? Convert.ToDecimal(row["mevcut_stok"]) : 0;
                string miktar = miktarRaw.ToString("G29");

                string birim = row["birim"].ToString()!;
                string tamMetin = $"{miktar} {birim}";

              
                if (ad.Contains("mısır") || ad.Contains("silaj")) txtSilajStok.Text = tamMetin;
                else if (ad.Contains("pancar") || ad.Contains("küspe")) txtkuspeStok.Text = tamMetin;
                else if (ad.Contains("yonca")) txtYoncaStok.Text = tamMetin;
                else if (ad.Contains("saman")) txtSamanStok.Text = tamMetin;
                else if (ad.Contains("fığ") || ad.Contains("fig")) txtFıgStok.Text = tamMetin;
                else if (ad.Contains("yulaf")) txtYulafStok.Text = tamMetin;
                else if (ad.Contains("arpa")) txtArpaStok.Text = tamMetin;
                else if (ad.Contains("süt yemi")) txtSutYemiStok.Text = tamMetin;
            }
        }

      

        private void dgStok_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStok.SelectedItem is DataRowView row)
            {
                cbUrunAdi.Text = row["urun_adi"].ToString();
                cbKategori.Text = row["kategori"].ToString();
                cbBirim.Text = row["birim"].ToString();
                txtMevcutStok.Text = ToDecimal(row["mevcut_stok"].ToString()!).ToString("G29");
                txtKritikStok.Text = ToDecimal(row["kritik_stok"].ToString()!).ToString("G29");
              
                cbDepoYeri.Text = row["depo_yeri"].ToString();
            }
        }
        private void dgStokhareketi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStokHareket.SelectedItem is DataRowView row)
            {
                cbStokSec.SelectedValue = row["stok_id"];
                cbIslemTuru.Text = row["islem_turu"]?.ToString();
                dpTarih.SelectedDate = Convert.ToDateTime(row["tarih"]);
                txtMiktar.Text = row["miktar"]?.ToString();
                txtHareketBirimFiyat.Text = row["birim_fiyat"]?.ToString();
                txtHareketAciklama.Text = row["aciklama"]?.ToString();
            }
        }

    
        private void dgRasyonlar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRasyonlar.SelectedItem is DataRowView row)
            {
                txtRasyonAdi.Text = row["rasyon_adi"].ToString();
                txtHedefHayvan.Text = row["hedef_hayvan"].ToString();
                txtRasyonAciklama.Text = row["aciklama"].ToString();
            }
        }

        private void dgRasyonDetay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRasyonDetay.SelectedItem is DataRowView row)
            {
                cbRasyonSec.SelectedValue = row["rasyon_id"];
                cbRasyonStok.SelectedValue = row["stok_id"];
                txtRasyonMiktar.Text = row["miktar"].ToString();
            }
        }



        private void Guncelle1_Click(object sender, RoutedEventArgs e)
        {
            if (dgStok.SelectedItem is DataRowView row)
            {
                string sql = "UPDATE Tbl_Stok SET urun_adi=@p1, kategori=@p2, birim=@p3, mevcut_stok=@p4, kritik_stok=@p5,  depo_yeri=@p6 WHERE stok_id=@id";
                SQLCalistir(sql,
                    new SqlParameter("@p1", cbUrunAdi.Text), new SqlParameter("@p2", cbKategori.Text), new SqlParameter("@p3", cbBirim.Text),
                    new SqlParameter("@p4", ToDecimal(txtMevcutStok.Text)), new SqlParameter("@p5", ToDecimal(txtKritikStok.Text)),
                     new SqlParameter("@p6", cbDepoYeri.Text), new SqlParameter("@id", row["stok_id"]));
                TumTablolariYukle();
                MessageBox.Show("Stok kartı güncellendi.");
            }
        }

        private void Guncelle2_Click(object sender, RoutedEventArgs e)
        {
            if (dgStokHareket.SelectedItem is DataRowView row)
            {
               
                int eskiStokId = Convert.ToInt32(row["stok_id"]);
                decimal eskiMiktar = Convert.ToDecimal(row["miktar"]);
                string eskiIslemTuru = row["islem_turu"].ToString()!;

              
                int yeniStokId = Convert.ToInt32(cbStokSec.SelectedValue);
                decimal yeniMiktar = ToDecimal(txtMiktar.Text);
                string yeniIslemTuru = cbIslemTuru.Text;

             StokMiktariGuncelle(eskiStokId, eskiMiktar, eskiIslemTuru != "Giriş");

         
                StokMiktariGuncelle(yeniStokId, yeniMiktar, yeniIslemTuru == "Giriş");

                string sql = "UPDATE Tbl_StokHareket SET stok_id=@p1, islem_turu=@p2, miktar=@p3, birim_fiyat=@p4, tarih=@p5, aciklama=@p6 WHERE hareket_id=@id";
                SQLCalistir(sql,
                    new SqlParameter("@p1", yeniStokId),
                    new SqlParameter("@p2", yeniIslemTuru),
                    new SqlParameter("@p3", yeniMiktar),
                    new SqlParameter("@p4", ToDecimal(txtHareketBirimFiyat.Text)),
                    new SqlParameter("@p5", dpTarih.SelectedDate ?? DateTime.Now),
                    new SqlParameter("@p6", txtHareketAciklama.Text),
                    new SqlParameter("@id", row["hareket_id"]));

                TumTablolariYukle();
                MessageBox.Show("Stok hareketi güncellendi ve stok miktarları düzeltildi.");
            }
        }

        private void Guncelle3_Click(object sender, RoutedEventArgs e)
        {
            if (dgRasyonlar.SelectedItem is DataRowView row)
            {
                string sql = "UPDATE Tbl_Rasyonlar SET rasyon_adi=@p1, hedef_hayvan=@p2, aciklama=@p3 WHERE rasyon_id=@id";
                SQLCalistir(sql, new SqlParameter("@p1", txtRasyonAdi.Text), new SqlParameter("@p2", txtHedefHayvan.Text), new SqlParameter("@p3", txtRasyonAciklama.Text), new SqlParameter("@id", row["rasyon_id"]));
                TumTablolariYukle();
                MessageBox.Show("Rasyon tanımı güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Guncelle4_Click(object sender, RoutedEventArgs e)
        {
            if (dgRasyonDetay.SelectedItem is DataRowView row)
            {
           
                int detayId = Convert.ToInt32(row["detay_id"]);
                int eskiStokId = Convert.ToInt32(row["stok_id"]);
                decimal eskiMiktar = Convert.ToDecimal(row["miktar"]);

                
                int yeniStokId = Convert.ToInt32(cbRasyonStok.SelectedValue);
                decimal yeniMiktar = ToDecimal(txtRasyonMiktar.Text);

              

                if (eskiStokId == yeniStokId)
                {
                    
                    decimal fark = yeniMiktar - eskiMiktar;
                    if (fark != 0)
                    {
                       
                        StokMiktariGuncelle(yeniStokId, fark, false);
                    }
                }
                else
                {
                
                  
                    StokMiktariGuncelle(eskiStokId, eskiMiktar, true);
             
                    StokMiktariGuncelle(yeniStokId, yeniMiktar, false);
                }

                string sql = "UPDATE Tbl_RasyonDetay SET rasyon_id=@p1, stok_id=@p2, miktar=@p3 WHERE detay_id=@id";
                SQLCalistir(sql,
                    new SqlParameter("@p1", cbRasyonSec.SelectedValue),
                    new SqlParameter("@p2", yeniStokId),
                    new SqlParameter("@p3", yeniMiktar),
                    new SqlParameter("@id", detayId));

                TumTablolariYukle();
                MessageBox.Show("Rasyon içeriği ve bağlı stoklar güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void Sil1_Click(object sender, RoutedEventArgs e)
        {
            if (dgStok.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["stok_id"]);
                string urunAdi = row["urun_adi"].ToString()!;

                if (MessageBox.Show($"{urunAdi} ürününü listeden kaldırmak istiyor musunuz?\n(Geçmiş hareket kayıtları bozulmayacaktır.)",
                                    "Stok Arşivleme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    
                    string sql = "UPDATE Tbl_Stok SET durum = 0 WHERE stok_id = @id";
                    SQLCalistir(sql, new SqlParameter("@id", id));

                    TumTablolariYukle();
                    MessageBox.Show("Ürün başarıyla arşivlendi.");
                }
            }
        }

        private void Sil2_Click(object sender, RoutedEventArgs e)
        {
            if (dgStokHareket.SelectedItem is DataRowView row && MessageBox.Show("Bu hareketi silmek stoğu eski haline döndürür. Onaylıyor musunuz?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                int stokId = Convert.ToInt32(row["stok_id"]);
                decimal miktar = Convert.ToDecimal(row["miktar"]);
                string islemTuru = row["islem_turu"].ToString()!;

                StokMiktariGuncelle(stokId, miktar, islemTuru != "Giriş");

                SQLCalistir("DELETE FROM Tbl_StokHareket WHERE hareket_id=@id", new SqlParameter("@id", row["hareket_id"]));
                TumTablolariYukle();
            }
        }

        private void Sil3_Click(object sender, RoutedEventArgs e)
        {
            if (dgRasyonlar.SelectedItem is DataRowView row)
            {
                int rasyonId = Convert.ToInt32(row["rasyon_id"]);
                string rasyonAdi = row["rasyon_adi"].ToString()!;

                if (MessageBox.Show($"{rasyonAdi} rasyonunu arşivlemek istiyor musunuz?",
                                    "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                
                    string sql = "UPDATE Tbl_Rasyonlar SET durum = 0 WHERE rasyon_id = @id";
                    SQLCalistir(sql, new SqlParameter("@id", rasyonId));

                    TumTablolariYukle();
                    MessageBox.Show("Rasyon başarıyla arşivlendi.");
                }
            }
        }

        private void Sil4_Click(object sender, RoutedEventArgs e)
        {
            if (dgRasyonDetay.SelectedItem is DataRowView row && MessageBox.Show("Bu malzemeyi rasyondan çıkarmak istiyor musunuz?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SQLCalistir("DELETE FROM Tbl_RasyonDetay WHERE detay_id=@id", new SqlParameter("@id", row["detay_id"]));
                TumTablolariYukle();
            }
        }

  

        private void btnStokKaydet_Click(object sender, RoutedEventArgs e)
        {
            string sql = "INSERT INTO Tbl_Stok (urun_adi, kategori, birim, mevcut_stok, kritik_stok, depo_yeri) VALUES (@p1,@p2,@p3,@p4,@p5,@p6)";
            SQLCalistir(sql, new SqlParameter("@p1", cbUrunAdi.Text), new SqlParameter("@p2", cbKategori.Text), new SqlParameter("@p3", cbBirim.Text),
                        new SqlParameter("@p4", ToDecimal(txtMevcutStok.Text)), new SqlParameter("@p5", ToDecimal(txtKritikStok.Text)),
                       new SqlParameter("@p6", cbDepoYeri.Text));
            TumTablolariYukle();
            MessageBox.Show("Yeni stok kaydedildi.");
        }

        private void btnHareketKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (cbStokSec.SelectedValue == null) return;

            int stokId = Convert.ToInt32(cbStokSec.SelectedValue);
            decimal miktar = ToDecimal(txtMiktar.Text);
            string islemTuru = cbIslemTuru.Text;

           
            string sql = "INSERT INTO Tbl_StokHareket (stok_id, islem_turu, miktar, birim_fiyat, tarih, aciklama) VALUES (@p1,@p2,@p3,@p4,@p5,@p6)";
            SQLCalistir(sql, new SqlParameter("@p1", stokId), new SqlParameter("@p2", islemTuru),
                        new SqlParameter("@p3", miktar), new SqlParameter("@p4", ToDecimal(txtHareketBirimFiyat.Text)),
                        new SqlParameter("@p5", dpTarih.SelectedDate ?? DateTime.Now), new SqlParameter("@p6", txtHareketAciklama.Text));

        
            StokMiktariGuncelle(stokId, miktar, islemTuru == "Giriş");

            TumTablolariYukle();
            MessageBox.Show("Hareket kaydedildi ve stok güncellendi.");
        }

        private void btnRasyonKaydet_Click(object sender, RoutedEventArgs e)
        {
            string sql = "INSERT INTO Tbl_Rasyonlar (rasyon_adi, hedef_hayvan, aciklama) VALUES (@p1,@p2,@p3)";
            SQLCalistir(sql, new SqlParameter("@p1", txtRasyonAdi.Text), new SqlParameter("@p2", txtHedefHayvan.Text), new SqlParameter("@p3", txtRasyonAciklama.Text));
            TumTablolariYukle();
            MessageBox.Show("Yeni rasyon oluşturuldu.");
        }

        private void btnRasyonDetayKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (cbRasyonSec.SelectedValue == null || cbRasyonStok.SelectedValue == null) return;

            int rId = Convert.ToInt32(cbRasyonSec.SelectedValue);
            int sId = Convert.ToInt32(cbRasyonStok.SelectedValue);
            decimal miktar = ToDecimal(txtRasyonMiktar.Text);

           
            string sql = "INSERT INTO Tbl_RasyonDetay (rasyon_id, stok_id, miktar) VALUES (@p1,@p2,@p3)";
            SQLCalistir(sql, new SqlParameter("@p1", rId), new SqlParameter("@p2", sId), new SqlParameter("@p3", miktar));

          
            StokMiktariGuncelle(sId, miktar, false);

            TumTablolariYukle();
            MessageBox.Show("Malzeme rasyona eklendi ve stoktan düşüldü.");
        }

        

        private void StockScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - (e.Delta * 2));
                e.Handled = true;
            }
        }

       
    }
}

