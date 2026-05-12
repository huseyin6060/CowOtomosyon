using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CowMaster.Sayfalar
{
    public partial class HaftalikGorevler : UserControl
    {
        // App.cs'deki bağlantı adresini kullanıyoruz
        private readonly string connStr = App.connectionString;

        public HaftalikGorevler()
        {
            InitializeComponent();
            ListeGuncelle();
        }

     
        private void ListeGuncelle(string filtre = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = "SELECT * FROM Gorevler";
                    if (!string.IsNullOrEmpty(filtre))
                        sql += " WHERE gorev_tanimi LIKE @ara";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    if (!string.IsNullOrEmpty(filtre))
                        da.SelectCommand.Parameters.AddWithValue("@ara", "%" + filtre + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgGorevler.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

     
        private void BtnEkle_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = "INSERT INTO Gorevler (gorev_tanimi, kategori, oncelik_durumu, son_tarih) VALUES (@p1, @p2, @p3, @p4)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p1", txtGorevAd.Text);
                cmd.Parameters.AddWithValue("@p2", (cbKategori.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "");
                cmd.Parameters.AddWithValue("@p3", (cbOncelik.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "");
                cmd.Parameters.AddWithValue("@p4", dpTarih.SelectedDate ?? DateTime.Now);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            ListeGuncelle();
        }

        private void BtnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (dgGorevler.SelectedItem is DataRowView satir)
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = "UPDATE Gorevler SET gorev_tanimi=@p1, kategori=@p2, oncelik_durumu=@p3, son_tarih=@p4 WHERE id=@id";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p1", txtGorevAd.Text);
                    cmd.Parameters.AddWithValue("@p2", (cbKategori.SelectedItem as ComboBoxItem)?.Content.ToString() ?? cbKategori.Text);
                    cmd.Parameters.AddWithValue("@p3", (cbOncelik.SelectedItem as ComboBoxItem)?.Content.ToString() ?? cbOncelik.Text);
                    cmd.Parameters.AddWithValue("@p4", dpTarih.SelectedDate ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@id", satir["id"]);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                ListeGuncelle();
            }
        }

    
        private void BtnSil_Click(object sender, RoutedEventArgs e)
        {
            if (dgGorevler.SelectedItem is DataRowView satir)
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    SqlCommand cmd = new SqlCommand("DELETE FROM Gorevler WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", satir["id"]);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                ListeGuncelle();
            }
        }

      
        private void dgGorevler_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (dgGorevler.SelectedItem is DataRowView satir)
            {
                txtGorevAd.Text = satir["gorev_tanimi"].ToString();
                cbKategori.Text = satir["kategori"].ToString();
                cbOncelik.Text = satir["oncelik_durumu"].ToString();
                dpTarih.SelectedDate = Convert.ToDateTime(satir["son_tarih"]);
            }
        }

      
        private void txtAktifgorevAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            ListeGuncelle(txtGorevAra.Text);
        }

  
        private void Btnyenile_Click(object sender, RoutedEventArgs e)
        {
            ListeGuncelle();
        }

        
        
    }
}