using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using Scream.Models;
using Scream.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Scream.Views
{
    /// <summary>
    /// Routes.xaml 的交互逻辑
    /// </summary>
    public partial class Routes : Page
    {
        public ObservableCollection<RouteSummary> RoutesList = new ObservableCollection<RouteSummary>();
        public Rule Json = new Rule();
        public MainWindow mainWindow;
        public Routes()
        {
            InitializeComponent();
            ListBoxRoutes.ItemsSource = RoutesList;
            TextBoxJson.DataContext = Json;
        }

        public void InitializeData()
        {
            foreach (Dictionary<string, object> set in mainWindow.routingRuleSets)
            {
                RoutesList.Add(new RouteSummary { Name = set["name"] as string, DomainStrategy = set["domainStrategy"] as string });
            }
            ListBoxRoutes.SelectionChanged += ListBoxRoutes_SelectionChanged;
        }

        private void ListBoxRoutes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxRoutes.SelectedIndex >= 0 && ListBoxRoutes.SelectedIndex < mainWindow.profiles.Count)
            {
                Json.RuleJson = JsonConvert.SerializeObject(mainWindow.routingRuleSets[ListBoxRoutes.SelectedIndex], Formatting.Indented);
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender == ButtonNew)
            {
                RoutesList.Add(new RouteSummary { Name = Strings.rulenamebypass, DomainStrategy = "IPIfNonMatch" });
                mainWindow.routingRuleSets.Add(Utilities.ROUTING_BYPASSCN_PRIVATE_APPLE);
            }
            if (ListBoxRoutes.SelectedItems.Count <= 0)
            {
                return;
            }
            if (sender == ButtonSave)
            {
                Dictionary<string, object> routingobject;
                try
                {
                    routingobject = Utilities.javaScriptSerializer.Deserialize<dynamic>(Json.RuleJson) as Dictionary<string, object>;
                }
                catch (System.Exception)
                {
                    mainWindow.notifyIcon.ShowBalloonTip("", Strings.messagenotvalidjson, BalloonIcon.None);
                    return;
                }
                if (Utilities.DOMAIN_STRATEGY_LIST.FindIndex(x => x == routingobject["domainStrategy"] as string) == -1)
                {
                    mainWindow.notifyIcon.ShowBalloonTip($" {routingobject["domainStrategy"]} ", Strings.messagenotvaliddomainstrategy, BalloonIcon.None);
                    return;
                }
                mainWindow.routingRuleSets[ListBoxRoutes.SelectedIndex] = routingobject;
                RoutesList[ListBoxRoutes.SelectedIndex] = new RouteSummary { Name = mainWindow.routingRuleSets[ListBoxRoutes.SelectedIndex]["name"].ToString(), DomainStrategy = mainWindow.routingRuleSets[ListBoxRoutes.SelectedIndex]["domainStrategy"].ToString() };
            }
            if (sender == ButtonDelete)
            {
                mainWindow.routingRuleSets.RemoveAt(ListBoxRoutes.SelectedIndex);
                RoutesList.RemoveAt(ListBoxRoutes.SelectedIndex);
            }

        }
    }
}
