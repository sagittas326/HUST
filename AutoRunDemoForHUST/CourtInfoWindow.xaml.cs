using System.Windows;

namespace AutoRunDemoForHUST
{
    /// <summary>
    /// CourtInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CourtInfoWindow : Window
    {
        public ViewModel ViewModel { get; set; } = new ViewModel();
        private MainWindow mainWindow;
        public CourtInfoWindow()
        {
            InitializeComponent();

            mainWindow = (MainWindow)Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w is MainWindow);
            ViewModel = mainWindow.ViewModel;
            DataContext = this;
        }

        private void BtnAddCampus_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddToListAndUpdateText(mainWindow.CampusList, ComboBoxCampus.Text, mainWindow.TxtCampus, "校区信息");
        }

        private void BtnAddCourt_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddToListAndUpdateText(mainWindow.CourtList, ComboBoxCourt.Text, mainWindow.TxtCourt, "场地信息");
        }

        private void BtnAddDate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddToListAndUpdateText(mainWindow.DateList, ComboBoxDate.Text, mainWindow.TxtDate, "日期信息");
        }

        private void BtnAddTime_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddToListAndUpdateText(mainWindow.TimeList, ComboBoxTime.Text, mainWindow.TxtTime, "时间信息");
        }

        private void BtnClearCampus_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearListAndUpdateText(mainWindow.CampusList, mainWindow.TxtCampus, "校区信息");
        }

        private void BtnClearCourt_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearListAndUpdateText(mainWindow.CourtList, mainWindow.TxtCourt, "场地信息");
        }

        private void BtnClearDate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearListAndUpdateText(mainWindow.DateList, mainWindow.TxtDate, "日期信息");
        }

        private void BtnClearTime_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearListAndUpdateText(mainWindow.TimeList, mainWindow.TxtTime, "时间信息");
        }



    }
}
