using IRCamera.Hardware;
using System.Diagnostics;
using System.IO;
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
using System.Drawing.Imaging;
using System.Drawing;

namespace IRCamera.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private IrCameraSdkWrapper _camera;

    public MainWindow()
    {
        InitializeComponent();
        _camera = new IrCameraSdkWrapper();

        // 订阅图片更新事件
        _camera.NewFrameReceived += OnNewFrameReceived;

        Closed += (s, e) =>
        {
            _camera?.Dispose();
            _camera.NewFrameReceived -= OnNewFrameReceived;
        };
    }

    // 处理图片更新
    private void OnNewFrameReceived(Bitmap bitmap)
    {
        // 确保在 UI 线程上更新
        Dispatcher.Invoke(() =>
        {
            try
            {
                // 将 Bitmap 转换为 BitmapSource
                BitmapSource bitmapSource = ConvertBitmapToBitmapSource(bitmap);
                // 将 System.Drawing.Bitmap 转换为 BitmapImage
                this.CameraImage.Source = bitmapSource;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新图片失败: {ex.Message}");
            }
            finally
            {
                // 释放 Bitmap 资源
                bitmap?.Dispose();
            }
        });
    }

    private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }

    // Bitmap 转换方法
    private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
    {
        if (bitmap == null) return null;

        using (var memory = new System.IO.MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // 冻结对象，使其可以跨线程访问

            return bitmapImage;
        }
    }


    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            TxtLog.ScrollToEnd();
        });
    }

    private void BtnInit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool success = _camera.InitializeSdk();
            Log(success ? "✅ SDK初始化成功" : "❌ SDK初始化失败");
            TxtStatus.Text = success ? "SDK已初始化" : "初始化失败";
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
        }
    }

    private void BtnQuery_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log("正在查询设备...");
            _camera.SetIpAddressArray();
            var ips = _camera.QueryDeviceIPs();

            if (ips.Count > 0)
            {
                Log($"✅ 发现 {ips.Count} 台设备:");
                foreach (var ip in ips)
                {
                    Log($"   - {ip}");
                    CmbDevices.Items.Add(ip);
                }
                CmbDevices.SelectedIndex = 0;
            }
            else
            {
                Log("❌ 未发现设备");
            }
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
        }
    }

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CmbDevices.SelectedItem == null)
            {
                Log("❌ 请先选择设备");
                return;
            }

            string ip = CmbDevices.SelectedItem.ToString();
            Log($"正在连接 {ip}...");

            bool success = _camera.ConnectToDevice(ip);
            Log(success ? $"✅ 连接成功: {ip}" : $"❌ 连接失败: {ip}");
            TxtStatus.Text = success ? $"已连接: {ip}" : "连接失败";
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
        }
    }

    private void BtnInfo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_camera.IsConnected())
            {
                Log("❌ 相机未连接");
                return;
            }

            var info = _camera.GetDeviceInfo();
            Log("✅ 设备信息:");
            Log($"   型号: {info.Model}");
            Log($"   序列号: {info.SerialNum}");
            Log($"   镜头: {info.Lens}");
            Log($"   IP: {info.IP}");
            Log($"   MAC: {info.Mac}");
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_camera.IsConnected())
            {
                Log("❌ 相机未连接");
                return;
            }

            string filename = $"test_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            bool success = _camera.SaveFrameAsJpeg(filename);
            Log(success ? $"✅ 图像已保存: {filename}" : "❌ 保存失败");
        }
        catch (Exception ex)
        {
            Log($"❌ 异常: {ex.Message}");
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _camera.Stop();
    }
}