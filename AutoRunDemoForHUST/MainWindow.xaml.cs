using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace AutoRunDemoForHUST
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string HUSTURL = "http://pecg.hust.edu.cn";//HUST体育学院场馆中心
        private string HUSTLoginURL = "http://pecg.hust.edu.cn/wescms/";//HUST体育学院场馆中心-登录
        private string HUSTReservationURL = "http://pecg.hust.edu.cn/cggl/front/yuyuexz";//HUST体育学院场馆中心-场地预约
        private string BaiduURL = "https://www.baidu.com";//百度首页
        public ViewModel ViewModel { get; set; } = new ViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            BtnResetSpeed_Click(null, null);
            ViewModel.CampusMap = new Dictionary<string, string>
            {
                { "场地预约-主校区", "/cggl/front/yuyuexz?campus=0" },
                { "场地预约-同济校区", "/cggl/front/yuyuexz?campus=60622" }
            };
            ViewModel.CourtMap = new Dictionary<string, (string, int[])>
            {
                { "西边体育馆-羽毛球场", ("/cggl/front/syqk?cdbh=69", new int[] { 5, 6, 4, 7, 2, 1, 9, 3, 8 }) },
                { "游泳馆-二楼羽毛球场", ("/cggl/front/syqk?cdbh=117", new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }) },
                { "光谷体育馆-主馆羽毛球场", ("/cggl/front/syqk?cdbh=45", new int[] { 20, 19, 18, 5, 4, 3, 15, 14, 13, 10, 9, 8, 17, 2, 12, 7, 16, 11, 6, 1, 21, 22 }) }
            };
            ViewModel.DateMap = new Dictionary<string, int>
            {
                {"第一天",0 },
                {"第二天",1 },
                {"第三天",2 }
            };
            ViewModel.TimeMap = new Dictionary<string, int>
            {
                { "08:00-10:00", 0 },
                { "10:00-12:00", 1 },
                { "12:00-14:00", 2 },
                { "14:00-16:00", 3 },
                { "16:00-18:00", 4 },
                { "18:00-20:00", 5 },
                { "20:00-22:00", 6 }
            };

            SetBrowserFeatureControl();

            BtnHome_Click(null, null);
            string defaultCampus = ViewModel.CampusMap.Keys.First();
            ViewModel.AddToListAndUpdateText(CampusList, defaultCampus, TxtCampus, "校区信息");
        }

        Dictionary<string, string> DayMap = new Dictionary<string, string>
        {
            {"上一天","pre_day"},
            {"下一天", "next_day"}
        };

        private static void SetBrowserFeatureControl()
        {
            // 获取当前应用程序的名称
            string appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            // 设置WebBrowser控件的默认渲染模式为IE11
            string featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
            Microsoft.Win32.Registry.SetValue(featureControlRegKey, appName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
        }

        private Uri previousUrl;

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            UpdateNavigatorBtnStatus();

            string currentUrl = Browser.Source.ToString();
            bool isHUSTUrl = currentUrl.StartsWith(HUSTURL);
            bool wasHUSTUrl = previousUrl?.ToString().StartsWith(HUSTURL) ?? false;

            if (!isHUSTUrl)
            {
                TxtTips.Text = "请先打开HUST体育学院场馆中心的网页：";
                TxtTips.Foreground = Brushes.Red;
                TxtTips.FontWeight = FontWeights.Bold;
                BtnStart.IsEnabled = false;
            }
            else if (!wasHUSTUrl || currentUrl == HUSTLoginURL || currentUrl == HUSTReservationURL)
            {
                dynamic document = Browser.Document;
                // 获取网页的HTML
                string html = document.Body.InnerHTML;
                // 检查HTML是否包含特定的字符串
                bool containsLogoutLink = html.Contains("<li>您好,") && html.Contains("<li><a class=\"logo_link\" href=\"/cggl/logout\">退出</a></li>");
                if (containsLogoutLink)
                {
                    TxtTips.Text = "已登录，可以开始抢场地啦！";
                    TxtTips.Foreground = Brushes.Green;
                    TxtTips.FontWeight = FontWeights.Normal;
                    BtnStart.IsEnabled = true;
                }
                else
                {
                    TxtTips.Text = "请先登录再开抢：";
                    TxtTips.Foreground = Brushes.Red;
                    TxtTips.FontWeight = FontWeights.Bold;
                    BtnStart.IsEnabled = false;
                }
            }
            previousUrl = e.Uri;
            if (tcs?.Task != null && !tcs.Task.IsCompleted)
            {
                tcs.SetResult(true); // 通知等待网页加载完成的任务已经完成
            }
        }

        private async void Browser_Navigated(object sender, NavigationEventArgs e)
        {
            if (tcs?.Task != null)
            {
                await tcs.Task; // 等待网页加载完成
            }
            tcs = new TaskCompletionSource<bool>();// 创建一个新的等待网页加载完成的任务
            // 获取WebBrowser的ActiveX实例
            WebBrowser browser = (WebBrowser)sender;
            SHDocVw.WebBrowser? activeXInstance = (SHDocVw.WebBrowser)browser.GetType().InvokeMember("ActiveXInstance",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, browser, new object[] { });

            // 禁止显示JavaScript错误
            activeXInstance.Silent = true;
            // 注册NewWindow3事件
            activeXInstance.NewWindow3 += ActiveXInstance_NewWindow3;

            TxtUrl.Text = Browser.Source.ToString();
        }

        private void ActiveXInstance_NewWindow3(ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
        {
            // 取消新窗口的创建
            Cancel = true;

            // 在当前窗口中导航到新的URL
            Browser.Navigate(bstrUrl);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Browser.GoBack();//浏览器控件后退
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            Browser.GoForward();//浏览器控件前进
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //if (tcs?.Task != null && !tcs.Task.IsCompleted)
            //{
            //    tcs.TrySetCanceled(); // 取消等待网页加载完成的任务
            //}
            //tcs = new TaskCompletionSource<bool>();// 创建一个新的等待网页加载完成的任务
            Browser.Refresh(); // 刷新浏览器控件
            //if (Browser.IsLoaded)// 如果浏览器控件已经加载完成
            //    tcs.SetResult(true);// 通知等待网页加载完成的任务已经完成
        }

        private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            BtnNavigator_Click(null, null);
        }

        private void BtnNavigator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Browser.Navigate(new Uri(TxtUrl.Text));//浏览器控件加载指定的URL
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show("输入的URL格式不正确: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载URL时出现错误: " + ex.Message);
            }
        }

        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            TxtUrl.Text = BaiduURL;
            BtnNavigator_Click(null, null);
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            TxtUrl.Text = HUSTURL;
            BtnNavigator_Click(null, null);
        }

        private void BtnAddCourtInfo_Click(object sender, RoutedEventArgs e)
        {
            CourtInfoWindow courtInfoWindow = new CourtInfoWindow();
            courtInfoWindow.ShowDialog();
        }

        private void TxtPartnerId_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TxtPartnerId.Text, out int parsedValue) && parsedValue > 0)
            {
                partnerIndex = parsedValue - 1;
            }
            else
            {
                TxtPartnerId.Text = "1";
            }
        }

        private void TxtPartnerId_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //ViewModel.Speed = SpeedSlider.Value / 10;
            //ViewModel.Speed = Math.Round(0.095162 * Math.Exp(0.046505 * SpeedSlider.Value), 1);//0.1-10.0
            ViewModel.Speed = Math.Round(10.2167188 * Math.Exp(0.00172011323 * SpeedSlider.Value) - 10.1343078, 1);//0.1-2.0
            if (TxtSpeedValue != null) TxtSpeedValue.Text = ViewModel.Speed.ToString("F1") + "X";
        }

        private void BtnResetSpeed_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Speed = defaultSpeed;
            //SpeedSlider.Value = ViewModel.Speed * 10;
            //SpeedSlider.Value = Math.Log(ViewModel.Speed / 0.095162) / 0.046505;//0.1-10.0
            SpeedSlider.Value = Math.Log((ViewModel.Speed + 10.1343078) / 10.2167188) / 0.00172011323;//0.1-2.0
        }

        private int defaultSpeed = 1;
        public List<string> CampusList = new List<string>();
        public List<string> CourtList = new List<string>();
        public List<string> DateList = new List<string>();
        public List<string> TimeList = new List<string>();
        private string campus = null;
        private string court = null;
        private string date = null;
        private string time = null;
        private bool isSuccess = false;//是否抢到场地

        private void BtnStart_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //BtnCampus.IsEnabled = BtnStart.IsEnabled;
            //if (!BtnCampus.IsEnabled)
            //{
            //    UpdateBtns(false);
            //}
        }

        private void UpdateBtns(bool isEnabled)
        {
            //BtnCourt.IsEnabled = isEnabled;
            //BtnPreviousDay.IsEnabled = isEnabled;
            //BtnNextDay.IsEnabled = isEnabled;
            //BtnTime.IsEnabled = isEnabled;
            //BtnPartner.IsEnabled = isEnabled;
            //BtnCourtNo.IsEnabled = isEnabled;
            //BtnCloseDialog.IsEnabled = isEnabled;
        }

        private TaskCompletionSource<bool> tcs;// 用于等待网页加载完成
        private CancellationTokenSource cts;// 用于取消操作
        private Task task;// 用于执行抢场地的任务

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (CampusList.Count == 0 || CourtList.Count == 0 || DateList.Count == 0 || TimeList.Count == 0)
            {
                MessageBox.Show("请检查设置抢场地的条件！");
                return;
            }

            if (task != null && !task.IsCompleted)
            {
                return;
            }

            cts = new CancellationTokenSource();
            int waitTime = (int)Math.Round(1000 / ViewModel.Speed);// 等待时间
            isSuccess = false;

            Dispatcher.Invoke(() =>
            {
                TxtUrl.Text = HUSTReservationURL;
                BtnNavigator_Click(null, null);
            });
            await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
            UpdateBtnStatus(true);

            task = Task.Run(async () =>
            {
                try
                {
                    // 每日8:00-22:00才能预约
                    Dispatcher.Invoke(() => TxtTips.Text = "等待8:00开始抢场地……");
                    DateTime now = DateTime.Now;
                    DateTime oStartClock = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                    DateTime oEndClock = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
                    TimeSpan sleepTime = now < oStartClock ? oStartClock - now : now > oEndClock ? oStartClock.AddDays(1) - now : TimeSpan.Zero;
                    await Task.Delay(sleepTime, cts.Token);

                    foreach (string campus1 in CampusList)
                    {
                        campus = campus1;
                        foreach (string date1 in DateList)
                        {
                            date = date1;
                            foreach (string time1 in TimeList)
                            {
                                time = time1;
                                foreach (string court1 in CourtList)
                                {
                                    court = court1;
                                    Dispatcher.Invoke(() => BtnRefresh_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    Dispatcher.Invoke(() => BtnCampus_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    Dispatcher.Invoke(() => BtnCourt_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    Dispatcher.Invoke(() => BtnCloseDialog_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    // 日期
                                    // if (DateList.Count == 0) { MessageBox.Show("请检查设置日期的条件！"); return; }
                                    //date ??= DateList[0];
                                    for (int n = 0; n < ViewModel.DateMap[date]; n++)
                                    {
                                        Dispatcher.Invoke(() => BtnNextDay_Click(null, null));
                                        await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                        Dispatcher.Invoke(() => BtnCloseDialog_Click(null, null));
                                        await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    }
                                    Dispatcher.Invoke(() => BtnTime_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    Dispatcher.Invoke(() => BtnCloseDialog_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    Dispatcher.Invoke(() => BtnPartner_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    Dispatcher.Invoke(() => BtnCourtNo_Click(null, null));
                                    await Task.Delay(waitTime, cts.Token); // 等待网页加载完成
                                    if (isSuccess)
                                    {
                                        return;
                                    }
                                    cts.Token.ThrowIfCancellationRequested();// 检查是否请求了取消
                                }
                            }
                        }
                    }
                    Dispatcher.Invoke(() => TxtTips.Text = "未抢到场地，请重新设置条件！");
                }
                catch (OperationCanceledException)
                {
                    Dispatcher.Invoke(() => TxtTips.Text = "已取消抢场地！");
                }
                finally
                {
                    Dispatcher.Invoke(() => UpdateBtnStatus(false));
                }
            });
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();// 请求取消操作
        }

        private void BtnCampus_Click(object sender, RoutedEventArgs e)
        {
            //TxtTips.Text = $"正在抢场地：{campus}-{court}-{date}-{time} 中……";
            TxtTips.Inlines.Clear();
            List<Run> runs = new List<Run>
            {
                new Run("正在抢场地："),
                new Run(campus) { Foreground = Brushes.Orange, FontWeight = FontWeights.Bold },
                new Run("-"),
                new Run(court) { Foreground = Brushes.Blue, FontWeight = FontWeights.Bold },
                new Run("-"),
                new Run(date) { Foreground = Brushes.Green, FontWeight = FontWeights.Bold },
                new Run("-"),
                new Run(time) { Foreground = Brushes.Red, FontWeight = FontWeights.Bold },
                new Run(" 中……")
            };
            foreach (Run run in runs)
            {
                TxtTips.Inlines.Add(run);
            }
            // 校区
            if (CampusList.Count == 0) { MessageBox.Show("请检查设置校区的条件！"); return; }
            campus ??= CampusList[0];
            Browser.InvokeScript("eval", $"document.querySelector('a[href=\"{ViewModel.CampusMap[campus]}\"]').click();");

            UpdateBtns(true);
        }

        private void BtnCourt_Click(object sender, RoutedEventArgs e)
        {
            // 场地
            if (CourtList.Count == 0) { MessageBox.Show("请检查设置场地的条件！"); return; }
            court ??= CourtList[0];
            Browser.InvokeScript("eval", $"document.querySelector('a[href=\"{ViewModel.CourtMap[court].Item1}\"]').click();");
            order = ViewModel.CourtMap[court].Item2;

            //BtnCloseDialog_Click(null, null);
        }

        private void BtnPreviousDay_Click(object sender, RoutedEventArgs e)
        {
            Browser.InvokeScript("eval", $"document.querySelector('.{DayMap["上一天"]}').click();");

            //BtnCloseDialog_Click(null, null);
        }

        private void BtnNextDay_Click(object sender, RoutedEventArgs e)
        {
            Browser.InvokeScript("eval", $"document.querySelector('.{DayMap["下一天"]}').click();");

            //BtnCloseDialog_Click(null, null);
        }

        private void BtnTime_Click(object sender, RoutedEventArgs e)
        {
            // 时间
            if (TimeList.Count == 0) { MessageBox.Show("请检查设置时间的条件！"); return; }
            time ??= TimeList[0];
            // 第一步：点击展开下拉框
            Browser.InvokeScript("eval", "document.getElementById('starttime').focus();");
            // 第二步：选择下拉框的元素
            Browser.InvokeScript("eval", $"document.getElementById('starttime').selectedIndex = {ViewModel.TimeMap[time]};");
            // 第三步：触发下拉框的onchange事件
            Browser.InvokeScript("eval", "document.getElementById('starttime').onchange();");

            //BtnCloseDialog_Click(null, null);
        }

        private int partnerIndex = 0;

        private void BtnPartner_Click(object sender, RoutedEventArgs e)
        {
            // 选择同伴
            Browser.InvokeScript("eval", "document.querySelector('input[type=\"button\"][value=\"选择同伴\"]').click();");

            dynamic document = Browser.Document;
            string html = document.Body.InnerHTML;
            Regex regex = new Regex(@"putPartner\('(.+?)','(.+?)','(.+?)','(.+?)'\)");
            MatchCollection matches = regex.Matches(html);
            List<(string, string, string, string)> partnersInfo = new List<(string, string, string, string)>();
            foreach (Match match in matches)
            {
                string param1 = match.Groups[1].Value;
                string param2 = match.Groups[2].Value;
                string param3 = match.Groups[3].Value;
                string param4 = match.Groups[4].Value;
                partnersInfo.Add((param1, param2, param3, param4));
            }
            if (partnerIndex >= partnersInfo.Count)
            {
                TxtPartnerId.Text = "1";
            }
            var partner = partnersInfo[partnerIndex];
            Browser.InvokeScript("eval", $"putPartner('{partner.Item1}','{partner.Item2}','{partner.Item3}','{partner.Item4}')");
            #region //无用代码
            //List<string> values = new List<string>();
            //Regex regex = new Regex(@"<input disabled=""disabled"" type=""checkbox"" value=""(\d+)"">");
            //MatchCollection matches = regex.Matches(html);
            //foreach (Match match in matches)
            //{
            //    values.Add(match.Groups[1].Value);
            //}
            //string checkboxValue = values[0];
            ////Browser.InvokeScript("eval", $"document.querySelector('input[value=\"{checkboxValue}\"]').click();");
            //Browser.InvokeScript("eval", $"document.querySelector('.stepFourMain').querySelector('input[value=\"{checkboxValue}\"]').click();");

            //Browser.InvokeScript("eval", $"document.querySelector('tr[onclick^=\"putPartner\"]:has(input[type=\"checkbox\"][value=\"{checkboxValue}\"])').click();");


            //int elementsCount = (int)Browser.InvokeScript("eval", "document.querySelectorAll('.datagrid.stepFourMain input[type=\"checkbox\"]').length");
            //dynamic checkboxes = Browser.InvokeScript("eval", $"document.querySelectorAll('.datagrid.stepFourMain input[type=\"checkbox\"]')");
            //for (int i = 0; i < elementsCount; i++)
            //{
            //    string value = "1";
            //    values.Add(value);
            //}

            //            string script = @"
            //    var checkboxes = document.querySelectorAll('.datagrid.stepFourMain input[type=""checkbox""]');
            //    var values = [];
            //    for (var i = 0; i < checkboxes.length; i++) {
            //        values.push(checkboxes[i].value);
            //    }
            //    return values.join(','); // 返回所有的value值，用逗号分隔
            //";
            //            string result = (string)Browser.InvokeScript("eval", script);
            //            string[] values = result.Split(',');
            //string checkboxValue = values[0];
            //Browser.InvokeScript("eval", $"document.querySelector('tr[onclick^=\"putPartner\"]:has(input[type=\"checkbox\"][value=\"{checkboxValue}\"])').click();");

            //Browser.InvokeScript("eval", $"document.querySelector('input[type=\"checkbox\"][value=\"{values[0]}\"]').click();");

            //有bug

            //Browser.InvokeScript("eval", "document.querySelector('input[type=\"checkbox\"][value=\"14249\"]').click();");//同伴1
            //Browser.InvokeScript("eval", "document.querySelector('input[type=\"checkbox\"][value=\"19198\"]').click();");//同伴2

            //        string script = @"
            //var checkExist = setInterval(function() {
            //    var checkbox = document.querySelector('input[type=\""checkbox\""][value=\""14249\""]');
            //    if (checkbox)
            //        {
            //            checkbox.click();
            //            clearInterval(checkExist);
            //        }
            //    }, 500);
            //        ";
            //        Browser.InvokeScript("eval", script);
            #endregion
        }

        private void BtnCourtNo_Click(object sender, RoutedEventArgs e)
        {
            // 选择场地编号
            List<(string, string)> courtStatus = GetCourtInfo();
            if (courtStatus.Count == 0)
            {
                RefreshCourtStatus();
                courtStatus = GetCourtInfo();
            }

            if (courtStatus.Count > 0)
            {
                SelectCourt(courtStatus);
            }
            else
            {
                TxtTips.Text = "无场地可预约";
                //MessageBox.Show(TxtTips.Text);
            }
        }

        private void BtnCloseDialog_Click(object sender, RoutedEventArgs e)
        {
            // 关闭弹窗
            Browser.InvokeScript("eval", "dialog.close();");
            #region //无用代码
            //Browser.InvokeScript("eval", "$.fn.cgDialog.close();");
            //Browser.InvokeScript("eval", "var iframe = document.querySelector('iframe'); var innerDoc = iframe.contentDocument || iframe.contentWindow.document; var element = innerDoc.querySelector('.window-close'); if(element != null) { element.click(); }");
            //Browser.InvokeScript("eval", "var element = document.querySelector('.window-close'); if(element != null) { element.click(); }");//关闭弹窗
            //Browser.InvokeScript("eval", "document.querySelector('.window-close').click();");//关闭弹窗
            #endregion
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Browser != null)
            {
                try
                {
                    Browser.InvokeScript("eval", "window.onresize();");
                }
                catch (Exception)
                {
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void UpdateNavigatorBtnStatus()
        {
            BtnBack.IsEnabled = Browser.CanGoBack;
            BtnForward.IsEnabled = Browser.CanGoForward;
        }

        private void UpdateBtnStatus(bool working)
        {
            if (working)
            {
                BtnBack.IsEnabled = !working;
                BtnForward.IsEnabled = !working;
            }
            else
            {
                UpdateNavigatorBtnStatus();
            }
            BtnRefresh.IsEnabled = !working;
            TxtUrl.IsEnabled = !working;
            BtnNavigator.IsEnabled = !working;
            BtnDefault.IsEnabled = !working;
            BtnHome.IsEnabled = !working;

            TxtPartnerId.IsEnabled = !working;
            SpeedSlider.IsEnabled = !working;
            BtnResetSpeed.IsEnabled = !working;
            BtnAddCourtInfo.IsEnabled = !working;
            BtnStart.IsEnabled = !working;
            BtnStop.IsEnabled = working;

            Browser.IsEnabled = !working;
        }

        private int[]? order = null;// 设置场地优先级

        private List<(string, string)> GetCourtInfo()
        {
            // 创建一个List，用于存储场地名称和预约状态
            List<(string, string)> courtStatus = new List<(string, string)>();
            // 获取所有的预约信息元素的数量
            int elementsCount = (int)Browser.InvokeScript("eval", "document.querySelectorAll('.getajax').length");
            // 遍历所有的预约信息元素
            for (int i = 0; i < elementsCount; i++)
            {
                // 获取场地名称
                string courtName = (string)Browser.InvokeScript("eval", $"document.querySelectorAll('.getajax')[{i}].querySelector('.imageTitle p').innerText");
                // 获取预约状态
                string status = (string)Browser.InvokeScript("eval", $"document.querySelectorAll('.getajax')[{i}].querySelector('.spacezt').innerText");
                // 将场地名称和预约状态添加到List中
                courtStatus.Add((courtName, status));
            }
            // 按照优先级排序
            List<(string, string)> orderedCourtStatus = order == null ? courtStatus : order.Select(o => courtStatus.First(c => int.Parse(Regex.Match(c.Item1, @"\d+").Value) == o)).ToList();
            return orderedCourtStatus.Where(c => c.Item2 == "可预约").ToList();//返回可预约的场地
        }

        private void SelectCourt(List<(string, string)> courtStatus)
        {
            foreach (var court in courtStatus)
            {
                // 如果预约状态为"可预约"
                if (court.Item2 == "可预约")
                {
                    // 模拟点击这个场地的按钮
                    //Browser.InvokeScript("eval", $"Array.from(document.querySelectorAll('.getajax')).find(element => element.querySelector('.imageTitle p').innerText.trim() == '{court.Item1}').querySelector('.choosetime').click();");
                    // 获取所有的预约信息元素的数量
                    int elementsCount = (int)Browser.InvokeScript("eval", "document.querySelectorAll('.getajax').length");
                    // 遍历所有的预约信息元素
                    for (int i = 0; i < elementsCount; i++)
                    {
                        // 获取场地名称
                        string courtName = (string)Browser.InvokeScript("eval", $"document.querySelectorAll('.getajax')[{i}].querySelector('.imageTitle p').innerText");
                        // 如果场地名称和当前场地名称相同
                        if (courtName == court.Item1)
                        {
                            Browser.InvokeScript("eval", $"document.querySelectorAll('.getajax')[{i}].querySelector('.choosetime').checked = true; checkbox();");
                            Browser.InvokeScript("eval", $"document.querySelectorAll('.getajax')[{i}].querySelector('.choosetime').click(); ");
                            break;
                        }
                    }

                    // 提交预约
                    Browser.InvokeScript("eval", "document.querySelector('input[type=\"submit\"][value=\"预约\"]').click();");

                    TxtTips.Text = "已抢到场地，请尽快确认并支付！";
                    //MessageBox.Show(TxtTips.Text);
                    CustomMessageBoxWindow customMessageBoxWindow = new CustomMessageBoxWindow(TxtTips.Text);
                    customMessageBoxWindow.ShowDialog();

                    isSuccess = true;
                    // 跳出循环
                    return;
                }
            }
        }

        private void RefreshCourtStatus()
        {
            //刷新场地
            Browser.InvokeScript("eval", "document.querySelector('input[type=\"button\"][value=\"刷新\"]').click();");
        }



    }
}