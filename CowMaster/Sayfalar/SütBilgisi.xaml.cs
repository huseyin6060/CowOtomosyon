using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CowMaster.Sayfalar
{
    public partial class SütBilgisi : UserControl, INotifyPropertyChanged
    {
        private readonly string connStr = App.connectionString;

        private int secilenSutId = 0;

      
        private SeriesCollection _grafikSerileri = new SeriesCollection();
        public SeriesCollection GrafikSerileri
        {
            get { return _grafikSerileri; }
            set
            {
                _grafikSerileri = value;
                OnPropertyChanged(nameof(GrafikSerileri));
            }
        }

        private string[] _eksenEtiketleri = Array.Empty<string>();
        public string[] EksenEtiketleri
        {
            get { return _eksenEtiketleri; }
            set
            {
                _eksenEtiketleri = value;
                OnPropertyChanged(nameof(EksenEtiketleri));
            }
        }

        public SütBilgisi()
        {
            InitializeComponent();

            DataContext = this;

            dpSagimTarihi.SelectedDate = DateTime.Now;

            VerileriTazele();
        }

     
        private void VerileriTazele()
        {
            HayvanListesiniGetir();
            SutKayitlariniGetir();
            GrafigiGuncelle();
        }

  
        private void HayvanListesiniGetir()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"
                    SELECT hayvan_id, kupe_no
                    FROM Tbl_Hayvanlar
                    WHERE durum = 'Aktif'";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);

                    DataTable dt = new DataTable();

                    da.Fill(dt);

                    cbHayvanSec.ItemsSource = dt.DefaultView;
                    cbHayvanSec.DisplayMemberPath = "kupe_no";
                    cbHayvanSec.SelectedValuePath = "hayvan_id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

  
        private void SutKayitlariniGetir(string arama = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"
                    SELECT
                        s.sut_id,
                        s.hayvan_id,
                        h.kupe_no,
                        s.sagim_tarihi AS islem_tarihi,
                        s.litre AS miktar,
                        s.kalite,
                        s.aciklama
                    FROM Tbl_SutKayit s
                    INNER JOIN Tbl_Hayvanlar h
                        ON s.hayvan_id = h.hayvan_id
                    WHERE
                        s.aciklama LIKE @ara
                        OR s.kalite LIKE @ara
                    ORDER BY s.sagim_tarihi DESC";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);

                    da.SelectCommand.Parameters.AddWithValue("@ara", "%" + arama + "%");

                    DataTable dt = new DataTable();

                    da.Fill(dt);

                    dgSutKayitlari.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

  
        private void GrafigiGuncelle()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"
                    SELECT TOP 10
                        litre,
                        sagim_tarihi
                    FROM Tbl_SutKayit
                    ORDER BY sagim_tarihi ASC";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    conn.Open();

                    ChartValues<double> degerler = new ChartValues<double>();

                    List<string> tarihler = new List<string>();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            double litre = Convert.ToDouble(dr["litre"]);

                            DateTime tarih =
                                Convert.ToDateTime(dr["sagim_tarihi"]);

                            degerler.Add(litre);

                            tarihler.Add(tarih.ToString("dd.MM"));
                        }
                    }

                    GrafikSerileri = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Süt Üretimi",

                            Values = degerler,

                            Stroke = Brushes.MediumSeaGreen,

                            StrokeThickness = 5,

                            Fill = Brushes.Transparent,

                            LineSmoothness = 0.8,

                            PointGeometry = DefaultGeometries.Circle,

                            PointGeometrySize = 12,

                            PointForeground = Brushes.White,

                            DataLabels = true,

                            Foreground = Brushes.Black,

                            FontSize = 14
                        }
                    };

                    EksenEtiketleri = tarihler.ToArray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

      
        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (cbHayvanSec.SelectedValue == null ||
                string.IsNullOrWhiteSpace(txtLitre.Text))
            {
                MessageBox.Show("Lütfen hayvan ve litre bilgisi girin.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"
                    INSERT INTO Tbl_SutKayit
                    (
                        hayvan_id,
                        sagim_tarihi,
                        litre,
                        kalite,
                        aciklama
                    )
                    VALUES
                    (
                        @hId,
                        @tarih,
                        @litre,
                        @kalite,
                        @not
                    )";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    decimal litre = 0;

                    decimal.TryParse(
                        txtLitre.Text.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out litre);

                    cmd.Parameters.AddWithValue("@hId", cbHayvanSec.SelectedValue);

                    cmd.Parameters.AddWithValue(
                        "@tarih",
                        dpSagimTarihi.SelectedDate ?? DateTime.Now);

                    cmd.Parameters.AddWithValue("@litre", litre);

                    cmd.Parameters.AddWithValue(
                        "@kalite",
                        (cbKalite.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");

                    cmd.Parameters.AddWithValue(
                        "@not",
                        txtAciklama.Text ?? "");

                    conn.Open();

                    cmd.ExecuteNonQuery();

                    VerileriTazele();

                    Temizle();

                    MessageBox.Show("Kayıt eklendi.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (secilenSutId == 0)
            {
                MessageBox.Show("Kayıt seçin.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string sql = @"
                    UPDATE Tbl_SutKayit
                    SET
                        hayvan_id = @hId,
                        sagim_tarihi = @tarih,
                        litre = @litre,
                        kalite = @kalite,
                        aciklama = @not
                    WHERE sut_id = @id";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    decimal litre = 0;

                    decimal.TryParse(
                        txtLitre.Text.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out litre);

                    cmd.Parameters.AddWithValue("@id", secilenSutId);

                    cmd.Parameters.AddWithValue("@hId", cbHayvanSec.SelectedValue);

                    cmd.Parameters.AddWithValue(
                        "@tarih",
                        dpSagimTarihi.SelectedDate ?? DateTime.Now);

                    cmd.Parameters.AddWithValue("@litre", litre);

                    cmd.Parameters.AddWithValue(
                        "@kalite",
                        (cbKalite.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");

                    cmd.Parameters.AddWithValue(
                        "@not",
                        txtAciklama.Text ?? "");

                    conn.Open();

                    cmd.ExecuteNonQuery();

                    VerileriTazele();

                    MessageBox.Show("Kayıt güncellendi.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    
        private void btnSil_Click(object sender, RoutedEventArgs e)
        {
            if (secilenSutId == 0)
                return;

            if (MessageBox.Show(
                "Kayıt silinsin mi?",
                "Onay",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    SqlCommand cmd = new SqlCommand(
                        "DELETE FROM Tbl_SutKayit WHERE sut_id=@id",
                        conn);

                    cmd.Parameters.AddWithValue("@id", secilenSutId);

                    conn.Open();

                    cmd.ExecuteNonQuery();

                    VerileriTazele();

                    Temizle();
                }
            }
        }

     
        private void dgSutKayitlari_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (dgSutKayitlari.SelectedItem is DataRowView row)
            {
                secilenSutId =
                    Convert.ToInt32(row["sut_id"]);

                cbHayvanSec.SelectedValue =
                    row["hayvan_id"];

                dpSagimTarihi.SelectedDate =
                    Convert.ToDateTime(row["islem_tarihi"]);

                txtLitre.Text =
                    row["miktar"].ToString();

                cbKalite.Text =
                    row["kalite"].ToString();

                txtAciklama.Text =
                    row["aciklama"].ToString();
            }
        }

    
        private void Temizle()
        {
            secilenSutId = 0;

            cbHayvanSec.SelectedIndex = -1;

            txtLitre.Clear();

            cbKalite.SelectedIndex = -1;

            txtAciklama.Clear();

            dpSagimTarihi.SelectedDate = DateTime.Now;
        }

        
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        private void btnIsletmeSut_Click(
            object sender,
            RoutedEventArgs e)
        {
            VerileriTazele();
        }

        private void btnBireySut_Click(
            object sender,
            RoutedEventArgs e)
        {

        }

        private void btnSutYenile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}