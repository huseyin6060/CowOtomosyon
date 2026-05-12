using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class SonDurumlar : UserControl
    {
        private readonly string connStr = App.connectionString;

        public SonDurumlar()
        {
            InitializeComponent();
            VerileriYukle();
        }

        private void VerileriYukle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

               
                    string sqlKartlar = @"SELECT 
        
                                        (SELECT ISNULL(CAST(COUNT(*) AS NVARCHAR), '0') FROM Tbl_Hayvanlar WHERE durum = 'Aktif') as ToplamHayvan,
                                        (SELECT ISNULL(CAST(SUM(litre) AS NVARCHAR), '0') FROM Tbl_SutKayit WHERE CAST(sagim_tarihi as date) = CAST(GETDATE() as date)) as GunlukSut,
                                        (SELECT ISNULL(CAST(COUNT(*) AS NVARCHAR), '0')  FROM Tbl_AsiTakip WHERE durum = 'Bekliyor') as AktifTedaviSayisi,  75 as KapasiteOrani,  'Veriler Güncel' as SutTrend";

                    SqlDataAdapter daKart = new SqlDataAdapter(sqlKartlar, conn);
                    DataTable dtKart = new DataTable();
                    daKart.Fill(dtKart);

                    if (dtKart.Rows.Count > 0)
                        this.DataContext = dtKart.DefaultView[0];

              
                    SonHareketleriListele("");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Yükleme Hatası: " + ex.Message);
            }
        }

        private void SonHareketleriListele(string arama)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sqlGrid = @"
            SELECT * FROM (
               
                SELECT giris_tarihi as zaman, 'Hayvan Kaydı' as olay_baslik, 
                       hayvan_adi + ' (' + kupe_no + ') sisteme eklendi.' as detay, 'Sistem' as personel 
                FROM Tbl_Hayvanlar

                UNION ALL

             
                SELECT sagim_tarihi as zaman, 'Süt Verimi' as olay_baslik, 
                       CAST(litre AS NVARCHAR) + ' Litre süt sağımı yapıldı.' as detay, 'Sistem' as personel 
                FROM Tbl_SutKayit

                UNION ALL

             
                SELECT V.tarih as zaman, 'Veteriner İşlemi' as olay_baslik, 
                       ISNULL(H.hayvan_adi, 'Bilinmeyen') + ': ' + V.islem_turu as detay, 'Veteriner' as personel 
                FROM Tbl_VeterinerIslem V
                LEFT JOIN Tbl_Hayvanlar H ON V.hayvan_id = H.hayvan_id

                UNION ALL

              
                SELECT A.yapilma_tarihi as zaman, 'Aşılama' as olay_baslik, 
                       ISNULL(H.hayvan_adi, 'Bilinmeyen') + ': ' + A.asi_adi + ' (' + A.durum + ')' as detay, 'Sistem' as personel 
                FROM Tbl_AsiTakip A
                LEFT JOIN Tbl_Hayvanlar H ON A.hayvan_id = H.hayvan_id

                UNION ALL

              
                SELECT islem_tarihi as zaman, 'Finans' as olay_baslik, 
                       kategori + ' ' + islem_turu + ': ' + CAST(tutar AS NVARCHAR) + ' TL' as detay, 'Muhasebe' as personel 
                FROM Tbl_Finans

                UNION ALL

              
                SELECT ISNULL(KayitTarihi, GETDATE()) as zaman, 'Tarla İşlemi' as olay_baslik, 
                       TarlaAdi + ' ' + MahsulAdi as detay, 'Saha' as personel 
                FROM Tarlalar

                UNION ALL

            
                SELECT ISNULL(SatinAlmaTarihi, GETDATE()) as zaman, 'Ekipman' as olay_baslik, 
                       MarkaModel + ' (' + EkipmanTuru + ')' as detay, 'Teknik' as personel 
                FROM EkipmanYonetimi

                UNION ALL

               
                SELECT SH.tarih as zaman, 'Stok' as olay_baslik, 
                       S.urun_adi + ' ' + SH.islem_turu + ': ' + CAST(SH.miktar AS NVARCHAR) as detay, 'Depo' as personel 
                FROM Tbl_StokHareket SH
                JOIN Tbl_Stok S ON SH.stok_id = S.stok_id

                UNION ALL

            
                SELECT satis_tarihi as zaman, 'Satış' as olay_baslik, 
                       'Hayvan Satış Tutarı: ' + CAST(toplam_tutar AS NVARCHAR) + ' TL' as detay, 'Satış' as personel 
                FROM Tbl_HayvanSatis

                UNION ALL

            
                SELECT olusturulma_tarihi as zaman, 'Görev' as olay_baslik, 
                       gorev_tanimi + ' (' + durum + ')' as detay, 'Yönetici' as personel 
                FROM Gorevler
            ) AS Havuz";

                    if (!string.IsNullOrEmpty(arama))
                        sqlGrid += " WHERE olay_baslik LIKE @p1 OR detay LIKE @p1 OR personel LIKE @p1";

                    sqlGrid += " ORDER BY zaman DESC";

                    SqlDataAdapter daGrid = new SqlDataAdapter(sqlGrid, conn);
                    daGrid.SelectCommand.Parameters.AddWithValue("@p1", "%" + arama + "%");

                    DataTable dtGrid = new DataTable();
                    daGrid.Fill(dtGrid);
                    dgSonHareketler.ItemsSource = dtGrid.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri Getirme Hatası: " + ex.Message);
            }
        }

        private void txtLogAra_TextChanged(object sender, TextChangedEventArgs e) => SonHareketleriListele(txtLogAra.Text);
        private void btnLogYenile_Click(object sender, RoutedEventArgs e) => VerileriYukle();
        private void Raporindir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
           
                string masaustuYolu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string dosyaAdi = "Ciftlik_Son_Durum_Raporu_" + DateTime.Now.ToString("ddMMyyyy_HHmm") + ".html";
                string tamYol = System.IO.Path.Combine(masaustuYolu, dosyaAdi);
                System.Text.StringBuilder html = new System.Text.StringBuilder();
                html.Append("<html><head><meta charset='utf-8'><title>Çiftlik Raporu</title>");
                html.Append("<style>body{font-family:Arial;} table{width:100%; border-collapse:collapse;} th,td{border:1px solid #ddd; padding:8px; text-align:left;} th{background-color:#2F80ED; color:white;}</style>");
                html.Append("</head><body>");
                html.Append($"<h1>CowMaster İşletme Raporu</h1>");
                html.Append($"<p>Rapor Tarihi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}</p>");
                html.Append($"<p>Sorumlu: Hüseyin Yüce</p>");
                html.Append("<table><tr><th>Tarih</th><th>İşlem Türü</th><th>Detay</th><th>Sorumlu</th></tr>");

            
                DataView dv = (DataView)dgSonHareketler.ItemsSource;
                foreach (DataRowView row in dv)
                {
                    html.Append("<tr>");
                    html.Append($"<td>{Convert.ToDateTime(row["zaman"]):dd.MM.yyyy HH:mm}</td>");
                    html.Append($"<td>{row["olay_baslik"]}</td>");
                    html.Append($"<td>{row["detay"]}</td>");
                    html.Append($"<td>{row["personel"]}</td>");
                    html.Append("</tr>");
                }

                html.Append("</table></body></html>");

               
                System.IO.File.WriteAllText(tamYol, html.ToString());

                MessageBox.Show($"Rapor başarıyla oluşturuldu ve masaüstüne kaydedildi:\n{dosyaAdi}", "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tamYol) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rapor oluşturulurken hata oluştu: " + ex.Message);
            }
        }
    }
}