using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using mshtml;

namespace TorExitNodes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string listUrl = "http://proxy.org/tor.shtml";
        const string proxyRegex = @"<tr><td[^>]*>[^<]*</td><td[^>]*>[^<]*</td><td[^>]*>(?<name>[^<]*)</td><td[^>]*>(?<exitNode>[^<]*)</td><td[^>]*>(?<country>[^<]*)</td><td[^>]*>[^<]*</td></tr>";
        const string proxyCommentsRegex = @"<!--[^-]*-->";
        const string appDataFileName = "appdata.xml";

        string StatusLabel
        {
            set
            {
                Dispatcher.Invoke(new Action(() => statusBarLabel.Content = value));
            }
        }

        int WorkInProgress
        {
            get
            {
                return _workInProgress;
            }
            set
            {
                _workInProgress = value;
                Dispatcher.Invoke(new Action(
                    () =>
                    {
                        if (value > 0)
                            progressBar.Visibility = Visibility.Visible;
                        else
                            progressBar.Visibility = Visibility.Hidden;
                    }));
            }
        }
        int _workInProgress;

        List<ProxyItem> Proxies
        {
            get
            {
                return _proxies;
            }
            set
            {
                _proxies = value;
                proxyGridView.ItemsSource = _proxies;
                if (value != null && value.Count > 0)
                    proxyGridView.Visibility = System.Windows.Visibility.Visible;
                else
                    proxyGridView.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        List<ProxyItem> _proxies;

        string LastUsedCountry
        {
            get
            {
                if (appData == null)
                    return null;
                if (appData.LastUsedFields.Count == 0)
                    return null;
                return appData.LastUsedFields.Single().Country;
            }
            set
            {
                if (appData.LastUsedFields.Count == 0)
                    appData.LastUsedFields.AddLastUsedFieldsRow(value);
                else
                    appData.LastUsedFields.Single().Country = value;
            }
        }

        AppData appData;

        public MainWindow()
        {
            InitializeComponent();
            appData = new AppData();
            string fullPath = Environment.CurrentDirectory + "\\" + appDataFileName;
            if(File.Exists(fullPath))
                appData.ReadXml(fullPath);
            CountryBox.Text = LastUsedCountry;
            WorkInProgress = 0;
            Browser.LoadCompleted += LoadTorList;
        }

        private void LoadTorListButton_Click(object sender, RoutedEventArgs e)
        {
            StatusLabel = "Loading the Tor List...";
            WorkInProgress++;
            Browser.Navigate(new Uri(listUrl));
        }

        void LoadTorList(object sender, NavigationEventArgs e)
        {
            LastUsedCountry = CountryBox.Text;

            List<ProxyItem> proxies = new List<ProxyItem>();
            HTMLDocument document = (HTMLDocument)Browser.Document;

            var tbodies = document.getElementsByTagName("tbody");
            IHTMLElement table = (IHTMLElement)tbodies.item(3);
            StringBuilder sb = new StringBuilder(table.innerHTML);
            sb.Replace("\r\n", "");
            sb.Replace("\n", "");

            string tableHtml = sb.ToString();
            tableHtml = Regex.Replace(tableHtml, proxyCommentsRegex, "");

            Match proxyMatch = Regex.Match(tableHtml, proxyRegex, RegexOptions.IgnoreCase);
            while (proxyMatch.Success)
            {
                bool exitNode = (proxyMatch.Groups["exitNode"].Value == "YES");
                if (exitNode)
                {
                    string country = proxyMatch.Groups["country"].Value;
                    if(country == CountryBox.Text)
                        proxies.Add(new ProxyItem(proxyMatch.Groups["name"].Value, country));
                }
                proxyMatch = proxyMatch.NextMatch();
            }


            StatusLabel = "List loaded, found " + proxies.Count + " proxies";
            WorkInProgress--;
            Proxies = proxies;
        }

        class ProxyItem
        {
            public string Name
            {
                get;
                private set;
            }

            public string Country
            {
                get;
                private set;
            }

            public ProxyItem(string name, string country)
            {
                this.Name = name;
                this.Country = country;
            }
        }

        private void CountryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadTorListButton_Click(sender, e);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            appData.WriteXml(Environment.CurrentDirectory + "\\" + appDataFileName);
        }
    }
}
