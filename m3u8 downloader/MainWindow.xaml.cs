using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using m3u8_Linker;


namespace m3u8_downloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr WindowHandle);
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        private void FocusProcess(string procName)
        {
            Process[] objProcesses = System.Diagnostics.Process.GetProcessesByName(procName);
            if (objProcesses.Length > 0)
            {
                IntPtr hWnd = IntPtr.Zero;
                hWnd = objProcesses[0].MainWindowHandle;
                ShowWindowAsync(new HandleRef(null, hWnd), SW_RESTORE);
                SetForegroundWindow(objProcesses[0].MainWindowHandle);
            }
        }



        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainContext mainContext)
            {
                //   mainContext.Linkers.Add(new LinkerNoEdit());
                //  mainContext.Linkers.Add(new LinkerNoEdit());
            }


        }

        private async void Button_Click(object sender, RoutedEventArgs env)
        {
            var mainContext = (MainContext)DataContext;
            //mainContext.TwitchParesetLoader.Url

            var links = await u3m8_twitch.TwitchLinker.Get(mainContext.TwitchParesetLoader.Url);

            if (links.Count == 0)
            {
                // MessageBox.Show($"Ничего не найдено", "Результат поиска ссылок");
            }

            // MessageBox.Show($"Найдено ссылок {links.Count}", "Результат поиска ссылок");

            mainContext.Linkers.Clear();

            foreach (var link in links)
            {
                mainContext.Linkers.Add(link);
            }


            //    if (sender is FrameworkElement element && element.DataContext is MainContext mainContext)
            //{

            //}
        }

        private T? GetDataContext<T>(object sender) where T : class
        {
            if (sender is FrameworkElement element && element.DataContext is T value)
            {
                return value;
            }

            return null;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (GetDataContext<ILinker>(sender) is ILinker linker)
            {
                // старт загрузчика

                StartLoad(linker);
            }
        }

        private void StartLoad(ILinker linker)
        {
            //    Process process = new Process( );

            Directory.CreateDirectory("output");

            Regex replace = new Regex("[^A-zА-я0-9 _-]");

            var name = replace.Replace(linker.Name, "_") + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");




            ProcessStartInfo startInfo = new("ffmpeg.exe")
            {
                Arguments = $"-i \"{linker.Url}\" -y -c copy -bsf:a aac_adtstoasc \"output/{name}.mkv\"",
                //  WorkingDirectory = "/output"


            };
            Process? process = Process.Start(startInfo);

            if (process == null)
                return;


            DownloadFFMpeg downloadFFMpeg = new DownloadFFMpeg
            {
                Process = process,
                Linker = new LinkerNoEdit(linker.Name, linker.Url, linker.Quality)
            };

            MainContext mainContext = (MainContext)DataContext;
            mainContext.Downloads.Add(downloadFFMpeg);



            SpinWait.SpinUntil(() => process.MainWindowHandle != IntPtr.Zero, 2000);

            Thread.Sleep(100);

            SetWindowText(process.MainWindowHandle, name);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (GetDataContext<DownloadFFMpeg>(sender) is DownloadFFMpeg downloadFFMpeg)
            {
              //  MainContext mainContext = (MainContext)DataContext;

                SetForegroundWindow(downloadFFMpeg.Process.MainWindowHandle);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (GetDataContext<DownloadFFMpeg>(sender) is DownloadFFMpeg downloadFFMpeg)
            {
                MainContext mainContext = (MainContext)DataContext;

                mainContext.Downloads.Remove(downloadFFMpeg);
            }
        }
    }

    public class MainContext
    {
        public TwitchParesetLoader TwitchParesetLoader { get; set; } = new TwitchParesetLoader();

        public ObservableCollection<ILinker> Linkers { get; set; } = new ObservableCollection<ILinker>();

        public ObservableCollection<DownloadFFMpeg> Downloads { get; set; } = new ObservableCollection<DownloadFFMpeg>();

    }

    public class TwitchParesetLoader
    {
        public string Url { get; set; } = "https://www.twitch.tv/xqc";
    }

    public struct LinkerNoEdit : ILinker
    {
        public LinkerNoEdit(string name, string url, string quality)
        {
            Name = name;
            Url = url;
            Quality = quality;
        }

        public string Name { get; set; } = "название";
        public string Url { get; set; } = "ссылка";
        public string Quality { get; set; } = "качество";


    }


    public class DownloadFFMpeg
    {
        public Process Process { get; set; } = null!;

        public ILinker Linker { get; set; } = null!;
    }


}
