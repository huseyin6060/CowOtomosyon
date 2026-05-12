using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using LiveCharts;
using LiveCharts.Wpf;

namespace CowMaster.Sayfalar
{
    public partial class GelirGider : UserControl, INotifyPropertyChanged
    {
        private readonly string connStr = App.connectionString;
        private int secilenFinansId = 0;


        private SeriesCollection? _finansSerisi;
        public SeriesCollection FinansSerisi
        {
            get => _finansSerisi!;
            set { _finansSerisi = value; OnPropertyChanged("FinansSerisi"); }
        }

        private List<string> ?_gunler;
        public List<string> Gunler
        {
            get => _gunler!;
            set { _gunler = value; OnPropertyChanged("Gunler"); }
        }

        private decimal _toplamGelir;
        public decimal ToplamGelir
        {
            get => _toplamGelir;
            set { _toplamGelir = value; OnPropertyChanged("ToplamGelir"); }
        }

        private decimal _toplamGider;
        public decimal ToplamGider
        {
            get => _toplamGider;
            set { _toplamGider = value; OnPropertyChanged("ToplamGider"); }
        }

        private decimal _netKar;
      
        public decimal NetKar
        {
            get => _netKar;
            set { _netKar = value; OnPropertyChanged("NetKar"); }
        }

        public GelirGider()
        {
            InitializeComponent();
            this.DataContext = this;
            dpTarih.SelectedDate = DateTime.Now;
            VerileriTazele();
        }

        private void VerileriTazele()
        {
            MusteriListesiniGetir();
            PersonelListesiniGetir();
            FinansListesiniGetir();
            IstatistikleriVeGrafigiYukle();
        }

       

        private void MusteriListesiniGetir()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                   
                    string sql = "SELECT musteri_id, (ISNULL(ad,'') + ' ' + ISNULL(soyad,'') + ' ' + ISNULL(firma_adi,'')) as Musteri FROM Tbl_Musteriler WHERE aktif=1";
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cbMusteri.SelectedValuePath = "musteri_id"; 
                    cbMusteri.DisplayMemberPath = "Musteri";
                    cbMusteri.ItemsSource = dt.DefaultView;
                }
            }
            catch { }
        }

        private void PersonelListesiniGetir()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                   
                    string sql = "SELECT personel_id, ad_soyad FROM Tbl_Personeller WHERE aktif = 1";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cbPersonel.SelectedValuePath = "personel_id";
                    cbPersonel.DisplayMemberPath = "ad_soyad"; 
                    cbPersonel.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
               
                MessageBox.Show("Personel listesi yüklenirken bir hata oluştu: " + ex.Message);
            }
        }

        private void FinansListesiniGetir(string arama = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"SELECT f.finans_id, f.islem_tarihi as Tarih, f.kategori as Kategori, 
                                 f.aciklama as Aciklama, f.tutar as Miktar, f.islem_turu,
                                 f.musteri_id, f.personel_id, f.odeme_turu,
                                 CASE WHEN f.islem_turu = 'Gelir' THEN '#16A34A' ELSE '#DC2626' END as Renk
                                 FROM Tbl_Finans f 
                                 WHERE f.aciklama LIKE @ara OR f.kategori LIKE @ara
                                 ORDER BY f.islem_tarihi DESC";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.Parameters.AddWithValue("@ara", "%" + arama + "%");
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgFinans.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void IstatistikleriVeGrafigiYukle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                   
                    string sqlOzeti = @"SELECT 
                               SUM(CASE WHEN islem_turu='Gelir' THEN tutar ELSE 0 END) as Gelir,
                               SUM(CASE WHEN islem_turu='Gider' THEN tutar ELSE 0 END) as Gider 
                               FROM Tbl_Finans";

                    using (SqlCommand cmd = new SqlCommand(sqlOzeti, conn))
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ToplamGelir = dr["Gelir"] != DBNull.Value ? Convert.ToDecimal(dr["Gelir"]) : 0;
                            ToplamGider = dr["Gider"] != DBNull.Value ? Convert.ToDecimal(dr["Gider"]) : 0;
                            NetKar = ToplamGelir - ToplamGider;
                        }
                    }

                    var tempGelirSerisi = new ChartValues<decimal>();
                    var tempGiderSerisi = new ChartValues<decimal>();
                    var tempGunler = new List<string>();

                    string sqlGrafik = @"SELECT TOP 7 CAST(islem_tarihi AS DATE) as Tarih, 
                               SUM(CASE WHEN islem_turu='Gelir' THEN tutar ELSE 0 END) as G,
                               SUM(CASE WHEN islem_turu='Gider' THEN tutar ELSE 0 END) as D
                               FROM Tbl_Finans 
                               GROUP BY CAST(islem_tarihi AS DATE) 
                               ORDER BY Tarih ASC";

                    using (SqlCommand cmdG = new SqlCommand(sqlGrafik, conn))
                    using (SqlDataReader drG = cmdG.ExecuteReader())
                    {
                        while (drG.Read())
                        {
                            tempGelirSerisi.Add(Convert.ToDecimal(drG["G"]));
                            tempGiderSerisi.Add(Convert.ToDecimal(drG["D"]));
                            tempGunler.Add(Convert.ToDateTime(drG["Tarih"]).ToString("dd/MM"));
                        }
                    }

                    
                    FinansSerisi = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Gelir",
                    Values = tempGelirSerisi,
                    Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)),       
                    MaxColumnWidth = 120, 
                    ColumnPadding = 85,

                    DataLabels = false
                },
                new ColumnSeries
                {
                    Title = "Gider",
                    Values = tempGiderSerisi,
                    Fill = new SolidColorBrush(Color.FromRgb(234, 179, 8)), 
                    
                    MaxColumnWidth = 120,
                    ColumnPadding = 85,

                    DataLabels = false
                }
            };
                    Gunler = tempGunler;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Grafik Hatası: " + ex.Message);
            }
        }



        private void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtTutar.Text)) { MessageBox.Show("Tutar giriniz!"); return; }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"INSERT INTO Tbl_Finans (islem_tarihi, islem_turu, kategori, musteri_id, personel_id, tutar, odeme_turu, aciklama) 
                                 VALUES (@tarih, @turu, @kat, @mId, @pId, @tutar, @odeme, @not)";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@tarih", dpTarih.SelectedDate ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@turu", rbGelir.IsChecked == true ? "Gelir" : "Gider");
                    cmd.Parameters.AddWithValue("@kat", cbKategori.Text);
                    cmd.Parameters.AddWithValue("@mId", cbMusteri.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pId", cbPersonel.SelectedValue ?? DBNull.Value);
                    string temizTutar = txtTutar.Text.Replace("₺", "").Replace(" ", "").Replace(".", ",").Trim();
                    cmd.Parameters.AddWithValue("@tutar", decimal.Parse(temizTutar));

                    cmd.Parameters.AddWithValue("@odeme", cbOdemeTuru.Text);
                    cmd.Parameters.AddWithValue("@not", cbKategori.Text + " İşlemi");

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    VerileriTazele();
                    Temizle();
                    MessageBox.Show("İşlem başarıyla kaydedildi.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void BtnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (secilenFinansId == 0) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"UPDATE Tbl_Finans SET islem_tarihi=@tarih, islem_turu=@turu, kategori=@kat, 
                                 musteri_id=@mId, personel_id=@pId, tutar=@tutar, odeme_turu=@odeme WHERE finans_id=@id";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", secilenFinansId);
                    cmd.Parameters.AddWithValue("@tarih", dpTarih.SelectedDate ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@turu", rbGelir.IsChecked == true ? "Gelir" : "Gider");
                    cmd.Parameters.AddWithValue("@kat", cbKategori.Text);
                    cmd.Parameters.AddWithValue("@mId", cbMusteri.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pId", cbPersonel.SelectedValue ?? DBNull.Value);

                    string temizTutar = txtTutar.Text.Replace("₺", "").Replace(" ", "").Replace(".", ",").Trim();
                    cmd.Parameters.AddWithValue("@tutar", decimal.Parse(temizTutar));

                    cmd.Parameters.AddWithValue("@odeme", cbOdemeTuru.Text);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    VerileriTazele();
                    MessageBox.Show("Kayıt güncellendi.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Güncelleme Hatası: " + ex.Message); }
        }

        private void BtnSil_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)!.DataContext as DataRowView;
            if (row == null) return;

            if (MessageBox.Show("Bu kaydı silmek istiyor musunuz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        SqlCommand cmd = new SqlCommand("DELETE FROM Tbl_Finans WHERE finans_id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", row["finans_id"]);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        VerileriTazele();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
            }
        }

        private void dgFinans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFinans.SelectedItem is DataRowView row)
            {
                secilenFinansId = Convert.ToInt32(row["finans_id"]);
                dpTarih.SelectedDate = Convert.ToDateTime(row["Tarih"]);
                txtTutar.Text = row["Miktar"].ToString();
                cbKategori.Text = row["Kategori"].ToString();
                cbOdemeTuru.Text = row["odeme_turu"].ToString();
                cbMusteri.SelectedValue = row["musteri_id"];
                cbPersonel.SelectedValue = row["personel_id"];

                if (row["islem_turu"].ToString() == "Gelir") rbGelir.IsChecked = true;
                else rbGider.IsChecked = true;

                lblPanelBaslik.Text = "Kayıt Düzenle (ID: " + secilenFinansId + ")";
            }
        }
        private void BtnTemizle_Click(object sender, RoutedEventArgs e)
        {
            Temizle();
        }
        private void Temizle()
        {
            secilenFinansId = 0;
            txtTutar.Clear();
            cbKategori.SelectedIndex = -1;
            cbOdemeTuru.SelectedIndex = -1;
            cbMusteri.SelectedIndex = -1;
            cbPersonel.SelectedIndex = -1;
            dpTarih.SelectedDate = DateTime.Now;
            lblPanelBaslik.Text = "Yeni Finansal İşlem Kaydı";
        }

        private void txtgelirgiderAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            FinansListesiniGetir(txtIslemAra.Text);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnFinansYenile(object sender, RoutedEventArgs e)
        {

        }
    }
}