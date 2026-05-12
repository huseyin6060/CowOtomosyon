using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CowMaster.kısıtlamalar
{
    public static class InputKisitlayici
    {

        public static readonly DependencyProperty SadeceSayiProperty =
            DependencyProperty.RegisterAttached("SadeceSayi", typeof(bool), typeof(InputKisitlayici), new PropertyMetadata(false, OnSadeceSayiChanged));

        public static bool GetSadeceSayi(DependencyObject obj) => (bool)obj.GetValue(SadeceSayiProperty);
        public static void SetSadeceSayi(DependencyObject obj, bool value) => obj.SetValue(SadeceSayiProperty, value);

        private static void OnSadeceSayiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                tb.PreviewTextInput -= SadeceSayi_Preview;
                tb.PreviewKeyDown -= BoslukEngelle_PreviewKeyDown;
                DataObject.RemovePastingHandler(tb, SayiYapistirmaEngelle);

                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += SadeceSayi_Preview;
                    tb.PreviewKeyDown += BoslukEngelle_PreviewKeyDown;
                    DataObject.AddPastingHandler(tb, SayiYapistirmaEngelle);
                }
            }
        }

        public static readonly DependencyProperty SadeceMetinProperty =
            DependencyProperty.RegisterAttached("SadeceMetin", typeof(bool), typeof(InputKisitlayici), new PropertyMetadata(false, OnSadeceMetinChanged));

        public static bool GetSadeceMetin(DependencyObject obj) => (bool)obj.GetValue(SadeceMetinProperty);
        public static void SetSadeceMetin(DependencyObject obj, bool value) => obj.SetValue(SadeceMetinProperty, value);

        private static void OnSadeceMetinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                tb.PreviewTextInput -= SadeceMetin_Preview;
                DataObject.RemovePastingHandler(tb, MetinYapistirmaEngelle);

                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += SadeceMetin_Preview;
                    DataObject.AddPastingHandler(tb, MetinYapistirmaEngelle);
                }
            }
        }


        private static void BoslukEngelle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) e.Handled = true;
        }

        private static void SadeceSayi_Preview(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private static void SadeceMetin_Preview(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(char.IsDigit);
        }

        private static void SayiYapistirmaEngelle(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit) || text.Contains(" ")) e.CancelCommand();
            }
        }

        private static void MetinYapistirmaEngelle(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (text.Any(char.IsDigit)) e.CancelCommand();
            }
        }
    }
}