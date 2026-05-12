using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class MusterilerPage : UserControl
    {
        public MusterilerPage()
        {
            InitializeComponent();
            Listele();
        }

       
        private void Listele(string arama = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                  
                    string sql = @"SELECT *, (ad + ' ' + soyad) as ad_soyad FROM Tbl_Musteriler WHERE aktif = 1";

                    if (!string.IsNullOrEmpty(arama))
                        sql += " AND (firma_adi LIKE @ara OR ad LIKE @ara OR soyad LIKE @ara OR telefon LIKE @ara)";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.Parameters.AddWithValue("@ara", "%" + arama + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgMusteriler.ItemsSource = dt.DefaultView;
                    txtToplamcari.Text = dt.Rows.Count.ToString();

                  
                    txtFirmasayısı.Text = dt.Compute("Count(musteri_id)", "musteri_tipi = 'Firma'").ToString();
                    txtKasaplar.Text = dt.Compute("Count(musteri_id)", "musteri_tipi = 'Kasap'").ToString();

                    object bakiyeSum = dt.Compute("Sum(bakiye)", "");
                    txtToplambakiye.Text = bakiyeSum != DBNull.Value ? string.Format("₺{0:N2}", bakiyeSum) : "₺0,00";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata oluştu: " + ex.Message, "Sistem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    string sql = @"INSERT INTO Tbl_Musteriler (firma_adi, ad, soyad, musteri_tipi, telefon, telefon2, email, bakiye, adres, aciklama) 
                                   VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p1", txtFirma.Text);
                    cmd.Parameters.AddWithValue("@p2", txtYetkili.Text);
                    cmd.Parameters.AddWithValue("@p3", txtYetkiliS.Text);
                    cmd.Parameters.AddWithValue("@p4", cbCariTip.Text);
                    cmd.Parameters.AddWithValue("@p5", txtTelefon.Text);
                    cmd.Parameters.AddWithValue("@p6", txtTelefon2.Text);
                    cmd.Parameters.AddWithValue("@p7", txtMail.Text);
                    cmd.Parameters.AddWithValue("@p8", decimal.TryParse(txtBakiye.Text, out decimal b) ? b : 0);
                    cmd.Parameters.AddWithValue("@p9", txtAdres.Text);
                    cmd.Parameters.AddWithValue("@p10", txtNot.Text);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    Listele();
                    FormuTemizle();
                }
            }
            catch (Exception ex) { MessageBox.Show("Kaydetme hatası: " + ex.Message); }
        }

        private void btnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (dgMusteriler.SelectedItem == null) return;
            DataRowView row = (DataRowView)dgMusteriler.SelectedItem;

            try
            {
                using (SqlConnection conn = new SqlConnection(App.connectionString))
                {
                    string sql = @"UPDATE Tbl_Musteriler SET firma_adi=@p1, ad=@p2, soyad=@p3, musteri_tipi=@p4, 
                                   telefon=@p5, email=@p6, bakiye=@p7, adres=@p8, aciklama=@p9 WHERE musteri_id=@id";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p1", txtFirma.Text);
                    cmd.Parameters.AddWithValue("@p2", txtYetkili.Text);
                    cmd.Parameters.AddWithValue("@p3", txtYetkiliS.Text);
                    cmd.Parameters.AddWithValue("@p4", cbCariTip.Text);
                    cmd.Parameters.AddWithValue("@p5", txtTelefon.Text);
                    cmd.Parameters.AddWithValue("@p6", txtMail.Text);
                    cmd.Parameters.AddWithValue("@p7", decimal.TryParse(txtBakiye.Text, out decimal b) ? b : 0);
                    cmd.Parameters.AddWithValue("@p8", txtAdres.Text);
                    cmd.Parameters.AddWithValue("@p9", txtNot.Text);
                    cmd.Parameters.AddWithValue("@id", row["musteri_id"]);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    Listele();
                    MessageBox.Show("Bilgiler güncellendi.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Güncelleme hatası: " + ex.Message); }
        }

       
        private void dgMusteriler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgMusteriler.SelectedItem is DataRowView row)
            {
                txtFirma.Text = row["firma_adi"].ToString();
                txtYetkili.Text = row["ad"].ToString();
                txtYetkiliS.Text = row["soyad"].ToString();
                txtTelefon.Text = row["telefon"].ToString();
                txtTelefon2.Text = row["telefon2"].ToString();
                txtMail.Text = row["email"].ToString();
                txtBakiye.Text = row["bakiye"].ToString();
                txtAdres.Text = row["adres"].ToString();
                txtNot.Text = row["aciklama"].ToString();
                cbCariTip.Text = row["musteri_tipi"].ToString();
            }
        }

        private void txtCariAra_TextChanged(object sender, TextChangedEventArgs e) => Listele(txtCariAra.Text);
        private void btnCariYenile_Click(object sender, RoutedEventArgs e) => Listele();
        private void btnTemizle_Click_1(object sender, RoutedEventArgs e) => FormuTemizle();

        private void FormuTemizle()
        {
            txtFirma.Clear(); txtYetkili.Clear(); txtYetkiliS.Clear();
            txtTelefon.Clear(); txtTelefon2.Clear(); txtMail.Clear();
            txtBakiye.Clear(); txtAdres.Clear(); txtNot.Clear();
            cbCariTip.SelectedIndex = -1;
        }
    }
}