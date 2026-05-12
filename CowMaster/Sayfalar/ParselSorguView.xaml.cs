#nullable disable
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace CowMaster.Sayfalar
{
    public partial class ParselSorguView : UserControl
    {
        private readonly string _folder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "CowMaster");

        private readonly string _file;
        private string currentCoordinates = "";

        public ParselSorguView()
        {
            InitializeComponent();

        
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            _file = Path.Combine(_folder, "son_arazi.txt");

            Loaded += async (s, e) =>
            {
                
                if (webViewHarita == null) return;

                try
                {
                    await webViewHarita.EnsureCoreWebView2Async();
                    InitWebView();
                    LoadSaved();
                }
                catch (Exception)
                {
                    
                }
            };
        }

        private void InitWebView()
        {
            
            var core = webViewHarita.CoreWebView2;
            if (core == null) return;

            core.Settings.IsScriptEnabled = true;
            core.Settings.IsWebMessageEnabled = true;
            core.WebMessageReceived += OnMessage;

            core.NavigationCompleted += async (s, e) =>
            {
                await InjectScript();
            };
        }

        private async Task InjectScript()
        {
           
            if (webViewHarita?.CoreWebView2 == null) return;

            string script = @"
(function () {
    let lastSentArea = '';
    setInterval(() => {
        let foundArea = null;
        const rows = document.querySelectorAll('tr');
        for (let row of rows) {
            let cells = row.querySelectorAll('td');
            if (cells.length >= 2) {
                let label = cells[0].innerText.trim();
                if (label === 'Tapu Alanı') {
                    foundArea = cells[1].innerText.trim();
                    break;
                }
            }
        }
        if (!foundArea) {
            const allElems = document.querySelectorAll('td, span, b');
            for (let el of allElems) {
                let txt = el.innerText.trim();
                if (txt.includes('m2') || txt.includes('m²')) {
                    let match = txt.match(/(\d[\d\.,]*)/);
                    if (match) {
                        foundArea = match[1];
                        break;
                    }
                }
            }
        }
        let currentArea = foundArea || '0';
        if (currentArea !== lastSentArea && currentArea !== '0') {
            window.chrome.webview.postMessage(currentArea);
            lastSentArea = currentArea;
        }
    }, 1500);
})();";

            try
            {
                await webViewHarita.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch { }
        }

        private void OnMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string msg = e.TryGetWebMessageAsString();

            Dispatcher.Invoke(() =>
            {
                
                if (Application.Current.MainWindow == null) return;

                var lbl = FindChild<TextBlock>(Application.Current.MainWindow, "lblAlan");

                if (lbl == null) return;
                if (string.IsNullOrWhiteSpace(msg) || msg == "0") return;

                string temiz = Regex.Replace(msg, @"[^\d\.,]", "").Trim();

                if (!string.IsNullOrEmpty(temiz))
                {
                    lbl.Text = temiz;
                    File.WriteAllText(_file, temiz);
                    currentCoordinates = temiz;
                }
            });
        }

        private void LoadSaved()
        {
            try
            {
                if (File.Exists(_file))
                {
                    string val = File.ReadAllText(_file);

                    
                    if (Application.Current.MainWindow != null)
                    {
                        var lbl = FindChild<TextBlock>(Application.Current.MainWindow, "lblAlan");
                        if (lbl != null)
                            lbl.Text = val;
                    }

                    currentCoordinates = val;
                }
            }
            catch { }
        }

        public string GetCurrentCoordinates() => currentCoordinates;

        public void HaritayaGit(string koordinat)
        {
            currentCoordinates = koordinat;

            if (Application.Current.MainWindow != null)
            {
                var lbl = FindChild<TextBlock>(Application.Current.MainWindow, "lblAlan");
                if (lbl != null) lbl.Text = koordinat;
            }
        }

        private T FindChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe && fe.Name == name)
                    return (T)child;

                var res = FindChild<T>(child, name);
                if (res != null) return res;
            }
            return null;
        }
    }
}