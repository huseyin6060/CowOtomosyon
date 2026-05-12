using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class SuruBilgi : UserControl
    {
        public SuruBilgi()
        {
            InitializeComponent();
            _ = SuruDagiliminiYukleAsync();
            _ = SaglikVeUretimYukleAsync();
            _ = FinansVeSutAnaliziYukleAsync();
            _ = YemVeGorevBilgileriniYukleAsync();
        }

        private async Task SuruDagiliminiYukleAsync()
        {
            try
            {
              

                string sql = @"
            SELECT 
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE genclikdurumu='İnek' AND durum='Aktif') AS Inek,
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE genclikdurumu='Düve' AND durum='Aktif') AS Duve,
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE genclikdurumu='Tosun' AND durum='Aktif') AS Tosun,
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE genclikdurumu='Dana' AND durum='Aktif') AS Dana,
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE genclikdurumu='Buzağı' AND durum='Aktif') AS Buzagi,
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE durum IN ('Satıldı', 'Kesim')) AS Cikanlar,
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE durum='Öldü') AS Silinenler";

                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    await conn.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            
                            txtInekAdet.Text = dr["Inek"].ToString();
                            txtDuveAdet.Text = dr["Duve"].ToString();
                            txtTosunAdet.Text = dr["Tosun"].ToString();
                            txtDanaAdet.Text = dr["Dana"].ToString();
                            txtBuzagiAdet.Text = dr["Buzagi"].ToString();

                            txtCikanAdet.Text = dr["Cikanlar"].ToString();
                            txtSilinenAdet.Text = dr["Silinenler"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sürü dağılımı güncellenirken hata oluştu: {ex.Message}", "CowMaster Hatası");
            }
        }





        private async Task SaglikVeUretimYukleAsync()
        {
            try
            {
        
                string sql = @"
            SELECT 
                -- 1. Üst Üçlü Panel
                (SELECT COUNT(*) FROM Tbl_Hayvanlar WHERE durum = 'Hasta') AS HastaCount,
                
                (SELECT COUNT(DISTINCT hayvan_id) FROM Tbl_SutKayit 
                 WHERE CAST(sagim_tarihi AS DATE) = CAST(GETDATE() AS DATE)) AS SagmalCount,
                
                (SELECT COUNT(*) FROM Tbl_Hayvanlar 
                 WHERE durum = 'Aktif' AND hayvan_id NOT IN (SELECT DISTINCT hayvan_id FROM Tbl_SutKayit)) AS KurudaCount,

                -- 2. Alt Dörtlü Detay Paneli
                (SELECT COUNT(*) FROM Tbl_Hayvanlar 
                 WHERE durum = 'Aktif' AND giris_tarihi >= DATEADD(DAY, -30, GETDATE())) AS TazeCount,

                (SELECT COUNT(*) FROM Tbl_Gebelik WHERE durum = 'Pozitif') AS GebeCount,
                
                (SELECT COUNT(*) FROM Tbl_Gebelik WHERE durum = 'Beklemede') AS TohumluCount,
                
                (SELECT COUNT(*) FROM Tbl_Gebelik WHERE durum = 'Negatif') AS BosCount";

                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    await conn.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                          
                            txtHastaAdet.Text = dr["HastaCount"].ToString();
                            txtSagmalAdet.Text = dr["SagmalCount"].ToString();
                            txtKurudaAdet.Text = dr["KurudaCount"].ToString();

                       
                            txtTazeAdet.Text = dr["TazeCount"].ToString();
                            txtGebeAdet.Text = dr["GebeCount"].ToString();
                            txtTohumluAdet.Text = dr["TohumluCount"].ToString();
                            txtBosAdet.Text = dr["BosCount"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               
                MessageBox.Show($"Sağlık verileri yüklenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }





        private async Task YemVeGorevBilgileriniYukleAsync()
        {
            try
            {
                string sql = @"
            SELECT 
                (SELECT COUNT(*) FROM Tbl_Rasyonlar) AS RasyonSayisi,
                (SELECT COUNT(*) FROM Tbl_Stok) AS StokKalemSayisi,
                (SELECT COUNT(*) FROM Tbl_AsiTakip WHERE durum = 'Bekliyor') AS BekleyenAsi,
                (SELECT COUNT(*) FROM Gorevler WHERE durum = 'Beklemede') AS BekleyenGorev";

                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    await conn.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                           
                            txtRasyon.Text = $"{dr["RasyonSayisi"]} Rasyon";
                            txtStok.Text = $"{dr["StokKalemSayisi"]} Stok";

                            txtDurumBildirim.Text = $"{dr["BekleyenAsi"]} Bekleyen Aşı";

                           
                            txtGorevBildirim.Text = $"{dr["BekleyenGorev"]} Bekleyen";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yem ve Görev bilgileri yüklenirken hata oluştu:\n{ex.Message}",
                                "CowMaster Veri Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }




        private async Task FinansVeSutAnaliziYukleAsync()
        {
            try
            {
                
                string sql = @"
            SELECT 
                -- NAKİT AKIŞ ANALİZİ
                (SELECT ISNULL(SUM(tutar), 0) FROM Tbl_Finans WHERE islem_turu = 'Gelir') AS ToplamGelir,
                (SELECT ISNULL(SUM(tutar), 0) FROM Tbl_Finans WHERE islem_turu = 'Gider') AS ToplamGider,

                -- SÜT VERİMLİLİĞİ
                (SELECT ISNULL(SUM(litre), 0) FROM Tbl_SutKayit) AS ToplamSut,
                (SELECT ISNULL(AVG(litre), 0) FROM Tbl_SutKayit WHERE sagim_tarihi >= DATEADD(YEAR, -1, GETDATE())) AS YillikSutOrt";

                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    await conn.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                           
                            decimal gelir = Convert.ToDecimal(dr["ToplamGelir"]);
                            decimal gider = Convert.ToDecimal(dr["ToplamGider"]);

                            txtGelir.Text = string.Format("{0:N2} ₺", gelir);
                            txtGider.Text = string.Format("{0:N2} ₺", gider);

                           if (gelir > 0)
                            {
                                decimal denge = (1 - (gider / gelir)) * 100;
                           }

                           decimal yillikOrt = Convert.ToDecimal(dr["YillikSutOrt"]);
                            decimal toplamSut = Convert.ToDecimal(dr["ToplamSut"]);

                            txtYillikSut.Text = string.Format("{0:N1} L", yillikOrt);
                            txtToplamSut.Text = string.Format("{0:N0} L", toplamSut);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Finans ve Süt verileri yüklenirken hata: {ex.Message}", "Veri Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }









































    }
    }
