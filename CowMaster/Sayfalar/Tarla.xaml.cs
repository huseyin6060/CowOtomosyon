#nullable disable
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CowMaster.Sayfalar
{
    public partial class Tarla : UserControl
    {
        private int? seciliTarlaID = null;

        public Tarla()
        {
            InitializeComponent();
            LoadTarlalar();
        }

       
        private void LoadTarlalar()
        {
            try
            {
                using SqlConnection con = new SqlConnection(App.connectionString);
                con.Open();

                string query = @"
SELECT 
    ID,
    TarlaAdi,
    MahsulAdi as Mahsul,
    MetreKare as Alan,
    SuGideri,
    BoruAdet,
    SatisTutar,
    Koordinatlar
FROM Tarlalar
WHERE Durum = 1
ORDER BY ID DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgTarlalar.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri yükleme hatası: " + ex.Message);
            }
        }

    
        private void BtnEkle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTarlaAdi.Text))
            {
                MessageBox.Show("Lütfen bir tarla adı giriniz.");
                return;
            }

            try
            {
                

                decimal kaydedilecekAlan = ConvertToDecimal(txtCizilenAlan.Text);
                string sistemdenGelenAlan = parselSorguEkranı.GetCurrentCoordinates();

              
                if (kaydedilecekAlan <= 0)
                {
                    if (!string.IsNullOrWhiteSpace(sistemdenGelenAlan) && sistemdenGelenAlan != "0" && sistemdenGelenAlan != "0.00")
                    {
                        kaydedilecekAlan = ConvertToDecimal(sistemdenGelenAlan);
                    }
                }

                using SqlConnection con = new SqlConnection(App.connectionString);
                con.Open();

                string query = @"
INSERT INTO Tarlalar (TarlaAdi, MetreKare, MahsulAdi, SuGideri, BoruAdet, SatisTutar, Koordinatlar, Durum, KayitTarihi)
VALUES (@ad, @m2, @mah, @su, @boru, @satis, @koord, 1, GETDATE())";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ad", txtTarlaAdi.Text);
                cmd.Parameters.AddWithValue("@m2", kaydedilecekAlan);
                cmd.Parameters.AddWithValue("@mah", (cbMahsul.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@su", ConvertToDecimal(txtSuGideri.Text));
                cmd.Parameters.AddWithValue("@boru", string.IsNullOrWhiteSpace(txtBoru.Text) ? 0 : Convert.ToInt32(txtBoru.Text));
                cmd.Parameters.AddWithValue("@satis", ConvertToDecimal(txtSatis.Text));
                cmd.Parameters.AddWithValue("@koord", sistemdenGelenAlan ?? "");

                cmd.ExecuteNonQuery();
                MessageBox.Show("Yeni tarla başarıyla eklendi.");
                FormuTemizle();
                LoadTarlalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ekleme hatası: " + ex.Message);
            }
        }

       
        private void dgTarlalar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgTarlalar.SelectedItem is not DataRowView row)
                return;

            try
            {
                seciliTarlaID = Convert.ToInt32(row["ID"]);
                txtTarlaAdi.Text = row["TarlaAdi"].ToString();

                string alan = row["Alan"].ToString();
                txtCizilenAlan.Text = alan;
                lblAlan.Text = alan;

                txtSuGideri.Text = row["SuGideri"].ToString();
                txtBoru.Text = row["BoruAdet"].ToString();
                txtSatis.Text = row["SatisTutar"].ToString();

                string mahsul = row["Mahsul"].ToString();
                foreach (ComboBoxItem item in cbMahsul.Items)
                {
                    if (item.Content.ToString() == mahsul)
                    {
                        cbMahsul.SelectedItem = item;
                        break;
                    }
                }

                string koordinat = row["Koordinatlar"].ToString();
                if (!string.IsNullOrWhiteSpace(koordinat))
                {
                    parselSorguEkranı.HaritayaGit(koordinat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Seçim hatası: " + ex.Message);
            }
        }

        private void BtnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (seciliTarlaID == null)
            {
                MessageBox.Show("Lütfen listeden bir tarla seçiniz.");
                return;
            }

            try
            {
                
                decimal kaydedilecekAlan = ConvertToDecimal(txtCizilenAlan.Text);
                string sistemdenGelenAlan = parselSorguEkranı.GetCurrentCoordinates();

                if (kaydedilecekAlan <= 0)
                {
                    if (!string.IsNullOrWhiteSpace(sistemdenGelenAlan) && sistemdenGelenAlan != "0" && sistemdenGelenAlan != "0.00")
                    {
                        kaydedilecekAlan = ConvertToDecimal(sistemdenGelenAlan);
                    }
                }

                using SqlConnection con = new SqlConnection(App.connectionString);
                con.Open();

                string query = @"
UPDATE Tarlalar SET
    TarlaAdi=@ad,
    MetreKare=@m2,
    MahsulAdi=@mah,
    SuGideri=@su,
    BoruAdet=@boru,
    SatisTutar=@sat,
    Koordinatlar=@koord
WHERE ID=@id";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", seciliTarlaID);
                cmd.Parameters.AddWithValue("@ad", txtTarlaAdi.Text);
                cmd.Parameters.AddWithValue("@m2", kaydedilecekAlan);
                cmd.Parameters.AddWithValue("@mah", (cbMahsul.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@su", ConvertToDecimal(txtSuGideri.Text));
                cmd.Parameters.AddWithValue("@boru", string.IsNullOrWhiteSpace(txtBoru.Text) ? 0 : Convert.ToInt32(txtBoru.Text));
                cmd.Parameters.AddWithValue("@sat", ConvertToDecimal(txtSatis.Text));
                cmd.Parameters.AddWithValue("@koord", sistemdenGelenAlan ?? "");

                cmd.ExecuteNonQuery();
                MessageBox.Show("Tarla bilgileri güncellendi.");
                LoadTarlalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güncelleme hatası: " + ex.Message);
            }
        }

  
        private void BtnSil_Click(object sender, RoutedEventArgs e)
        {
            if (seciliTarlaID == null) return;

            if (MessageBox.Show("Bu tarlayı silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using SqlConnection con = new SqlConnection(App.connectionString);
                con.Open();
                string query = "UPDATE Tarlalar SET Durum=0 WHERE ID=@id";
                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", seciliTarlaID);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Tarla silindi.");
                FormuTemizle();
                LoadTarlalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Silme hatası: " + ex.Message);
            }
        }

      
        private decimal ConvertToDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            string cleanValue = Regex.Replace(value, @"[^\d\.,]", "").Trim();

            if (cleanValue.Contains(".") && cleanValue.Contains(","))
            {
                cleanValue = cleanValue.Replace(".", "").Replace(",", ".");
            }
            else
            {
                cleanValue = cleanValue.Replace(",", ".");
            }

            if (decimal.TryParse(cleanValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0;
        }

        private void FormuTemizle()
        {
            seciliTarlaID = null;
            txtTarlaAdi.Clear();
            txtCizilenAlan.Clear();
            txtSuGideri.Clear();
            txtBoru.Clear();
            txtSatis.Clear();
            cbMahsul.SelectedIndex = -1;
            lblAlan.Text = "0.00";
            dgTarlalar.SelectedItem = null;
        }

        private void BtnTemizle_Click(object sender, RoutedEventArgs e)
        {
            FormuTemizle();
        }
    }
}