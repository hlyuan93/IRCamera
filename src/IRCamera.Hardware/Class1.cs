using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Resources;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Security;
using Microsoft.Win32;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.IO;

namespace IRCamera.Hardware;


/// <summary>
/// 红外相机SDK包装器类
/// </summary>
public class IrCameraSdkWrapper : IDisposable
{
    #region 常亮定义
    public const int IMAGE_WIDTH = 640;
    public const int IMAGE_HEIGHT = 512;
    public const int PIXEL_COUNT = IMAGE_WIDTH * IMAGE_HEIGHT;
    public const int FRAME_SIZE_BYTES = PIXEL_COUNT * 2;
    public const int HEADER_SIZE = 32;
    #endregion

    #region 私有字段
    private readonly object _frameLock = new object();
    private bool _isConnected = false;
    private bool _disposed = false;

    // GCHandle引用（需手动释放）
    private GCHandle _garyHandle;
    private GCHandle _rgbHandle;
    private GCHandle _bmpHandle;
    private GCHandle _bmpLenHandle;
    private GCHandle _ipHandle;
    private GCHandle _threadHandle;

    private IntPtr garyDataPtr;
    private short[] garyData = new short[PIXEL_COUNT];

    private IntPtr rgbDataPtr;
    private byte[] rgbData = new byte[PIXEL_COUNT * 3];

    private IntPtr bmpDataPtr;
    private byte[] bmpData = new byte[FRAME_SIZE_BYTES * 2];

    private IntPtr bmpLenPtr;
    private int[] bmpLen = new int[1];

    private IntPtr pIpbuffer;
    private byte[] u8ipbuffer = new byte[32 * 72];

    private IRSDK.CBF_IR frameProc = null;

    public const int tempSize = 640 * 512 * 2;
    public const int headSize = 32;
    private byte[] head_data = new byte[headSize + tempSize];

    private IRSDK.Frame pFrame;
    private IRSDK.STAT_TEMPER full_Temper = new IRSDK.STAT_TEMPER();
    private IRSDK.STAT_RECT sRectStat;
    private IRSDK.STAT_POINT sPt;
    private IRSDK.STAT_POLYGON spoly;

    private IntPtr input_data_copy = Marshal.AllocHGlobal(headSize + tempSize);
    private IntPtr input_data;
    private int length = 32;
    private IRSDK.T_IPADDR[] _t_ipaddr;

    private IRSDK.STAT_OBJ sobj;

    private IntPtr sFrameRelayPtr = Marshal.AllocHGlobal(headSize + tempSize);
    private IRSDK.Frame sReplayFrame = new IRSDK.Frame();
    private uint replay_index = 0;

    private IRSDK.T_SAVE_HEAD sHead = new IRSDK.T_SAVE_HEAD();
    private IntPtr threadBufPtr;
    private Byte[] threadBuf = new Byte[1024];

    private byte paltype = 0;
    private byte palindex = 1;

    private byte u8Constrast = 50, u8Bright = 50;

    #endregion

    #region 构造函数和初始化
    public IrCameraSdkWrapper()
    {
        InitializeArrays();
        InitializeStructures();
    }

    private void InitializeArrays()
    {
        _garyHandle = GCHandle.Alloc(garyData, GCHandleType.Pinned);
        garyDataPtr = _garyHandle.AddrOfPinnedObject();

        _rgbHandle = GCHandle.Alloc(rgbData, GCHandleType.Pinned);
        rgbDataPtr = _rgbHandle.AddrOfPinnedObject();

        _bmpHandle = GCHandle.Alloc(bmpData, GCHandleType.Pinned);
        bmpDataPtr = _bmpHandle.AddrOfPinnedObject();

        _bmpLenHandle = GCHandle.Alloc(bmpLen, GCHandleType.Pinned);
        bmpLenPtr = _bmpLenHandle.AddrOfPinnedObject();

        _ipHandle = GCHandle.Alloc(u8ipbuffer, GCHandleType.Pinned);
        pIpbuffer = _ipHandle.AddrOfPinnedObject();

        _threadHandle = GCHandle.Alloc(threadBuf, GCHandleType.Pinned);
        threadBufPtr = _threadHandle.AddrOfPinnedObject();

        _t_ipaddr = new IRSDK.T_IPADDR[length];

        input_data_copy = Marshal.AllocHGlobal(HEADER_SIZE + FRAME_SIZE_BYTES);
        sFrameRelayPtr = Marshal.AllocHGlobal(HEADER_SIZE + FRAME_SIZE_BYTES);
    }

    private void InitializeStructures()
    {
        Marshal.StructureToPtr(sReplayFrame, sFrameRelayPtr, true);
        spoly.sPolygon.Pt = new IRSDK.T_POINT[16];
    }
    #endregion

    #region SDK生命周期管理
    /// <summary>
    /// 初始化SDK
    /// </summary>
    /// <returns>成功返回true，否则返回false</returns>
    public bool InitializeSdk()
    {
        try
        {
            uint result = IRSDK.IRSDK_Init();
            return result != 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"初始化SDK失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 释放SDK资源
    /// </summary>
    public void DisposeSdk()
    {
        try
        {
            if (_isConnected)
                Disconnect();

            IRSDK.IRSDK_Quit();

            // 释放分配的内存
            if (input_data_copy != IntPtr.Zero)
                Marshal.FreeHGlobal(input_data_copy);
            if (sFrameRelayPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(sFrameRelayPtr);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"释放SDK资源失败: {ex.Message}");
        }
    }
    #endregion

    #region 设备管理
    /// <summary>
    /// 设置IP地址数组
    /// </summary>
    public void SetIpAddressArray()
    {
        IRSDK.IRSDK_SetIPAddrArray(pIpbuffer);
    }

    /// <summary>
    /// 查询网络中的设备IP地址
    /// </summary>
    /// <returns>IP地址列表</returns>
    public List<string> QueryDeviceIPs()
    {
        var ips = new List<string>();
        try
        {
            uint result = IRSDK.IRSDK_InqureIP(pIpbuffer, 1000);
            Debug.WriteLine($"查询IP结果: {result}");

            for (int i = 0; i < length; i++)
            {
                IntPtr pPonitor = new IntPtr(pIpbuffer.ToInt64() + Marshal.SizeOf(typeof(IRSDK.T_IPADDR)) * i);
                _t_ipaddr[i] = (IRSDK.T_IPADDR)Marshal.PtrToStructure(pPonitor, typeof(IRSDK.T_IPADDR));

                if ((_t_ipaddr[i].IPAddr != "0.0.0.0") && (!string.IsNullOrEmpty(_t_ipaddr[i].IPAddr) && _t_ipaddr[i].isValid == 1))
                {
                    if (_t_ipaddr[i].isValid == 1)
                    {
                        ips.Add(_t_ipaddr[i].IPAddr);
                        Debug.WriteLine($"发现设备: {_t_ipaddr[i].IPAddr}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"查询设备IP失败: {ex.Message}");
        }

        return ips;
    }

    /// <summary>
    /// 连接到指定IP的设备
    /// </summary>
    /// <param name="ipAddress">设备IP地址</param>
    /// <returns>成功返回true，否则返回false</returns>
    public bool ConnectToDevice(string ipAddress)
    {
        try
        {
            // 查找匹配的IP地址
            IRSDK.T_IPADDR targetIp = new IRSDK.T_IPADDR();
            bool found = false;

            for (int i = 0; i < length; i++)
            {
                IntPtr pPonitor = new IntPtr(pIpbuffer.ToInt64() + Marshal.SizeOf(typeof(IRSDK.T_IPADDR)) * i);
                var currentIp = (IRSDK.T_IPADDR)Marshal.PtrToStructure(pPonitor, typeof(IRSDK.T_IPADDR));

                if (currentIp.IPAddr == ipAddress && currentIp.isValid == 1)
                {
                    targetIp = currentIp;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.WriteLine($"未找到有效的IP地址: {ipAddress}");
                return false;
            }

            frameProc = new IRSDK.CBF_IR(FrameProc);

            int createResult = IRSDK.IRSDK_Create(0, targetIp, frameProc, null, null, 0);
            if (createResult == 0)
            {
                Debug.WriteLine("创建连接失败");
                return false;
            }

            int connectResult = IRSDK.IRSDK_Connect(0);
            return connectResult != 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"连接设备失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 断开设备连接
    /// </summary>
    public void Disconnect()
    {
        try
        {
            IRSDK.IRSDK_Stop(0);
            IRSDK.IRSDK_Destroy(0);
            _isConnected = false;
            Debug.WriteLine("设备已断开");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"断开连接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查设备是否已连接
    /// </summary>
    /// <returns>已连接返回true，否则返回false</returns>
    public bool IsConnected()
    {
        try
        {
            byte connected = IRSDK.IRSDK_IsConnected(0);
            return connected == 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查连接状态失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取设备信息
    /// </summary>
    /// <returns>设备信息结构体</returns>
    public IRSDK.T_DEVICE_INFO GetDeviceInfo()
    {
        if (!_isConnected)
            throw new InvalidOperationException("相机未连接");

        try
        {            
            IRSDK.T_DEVICE_INFO deviceInfo = new IRSDK.T_DEVICE_INFO();
            uint result = IRSDK.IRSDK_InqureDeviceInfo(0, ref deviceInfo);

            if (result == 0)
            {
                Debug.WriteLine("获取设备信息失败");
                Debug.WriteLine($"  型号: {deviceInfo.Model}");
                Debug.WriteLine($"  序列号: {deviceInfo.SerialNum}");
            }

            return deviceInfo;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取设备信息失败: {ex.Message}");
            return new IRSDK.T_DEVICE_INFO();
        }
    }
    #endregion

    #region 图像处理
    /// <summary>
    /// 获取当前帧的灰度数据（不安全，直接返回内部数组）
    /// </summary>
    public short[] GetGrayDataUnsafe()
    {
        lock (_frameLock)
        {
            return garyData;
        }
    }

    /// <summary>
    /// 获取RGB数据（不安全，直接返回内部数组）
    /// </summary>
    public byte[] GetRgbDataUnsafe()
    {
        lock (_frameLock)
        {
            return rgbData;
        }
    }

    /// <summary>
    /// 将RGB数据转换为位图
    /// </summary>
    public Bitmap GetCurrentBitmap()
    {
        if (!_isConnected)
            throw new InvalidOperationException("相机未连接");

        lock (_frameLock)
        {
            try
            {
                return BytesToBitmap(bmpData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取位图失败: {ex.Message}");
                return null;
            }
        }
    }

    ///// <summary>
    ///// 获取当前帧的灰度数据
    ///// </summary>
    ///// <param name="framePtr">帧指针</param>
    ///// <returns>灰度数据</returns>
    //public short[] GetGrayData(IntPtr framePtr)
    //{
    //    try
    //    {
    //        Marshal.Copy(framePtr, head_data, 0, headSize + tempSize);
    //        pFrame = BytesToStruct<IRSDK.Frame>(head_data);
    //        Marshal.StructureToPtr(pFrame, input_data_copy, false);

    //        IRSDK.IRSDK_Frame2Gray(input_data_copy, garyDataPtr, (float)u8Constrast, (float)u8Bright, 0);

    //        // 复制灰度数据到新的数组
    //        short[] result = new short[garyData.Length];
    //        Array.Copy(garyData, result, garyData.Length);

    //        return result;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"获取灰度数据失败: {ex.Message}");
    //        return new short[0];
    //    }
    //}

    ///// <summary>
    ///// 将灰度数据转换为RGB数据
    ///// </summary>
    ///// <param name="grayData">灰度数据指针</param>
    ///// <returns>RGB数据</returns>
    //public byte[] GrayToRgb(IntPtr grayData)
    //{
    //    try
    //    {
    //        IRSDK.IRSDK_Gray2Rgb(grayData, rgbDataPtr, pFrame.width, pFrame.height, paltype, palindex);

    //        // 复制RGB数据到新数组
    //        byte[] result = new byte[rgbData.Length];
    //        Array.Copy(rgbData, result, rgbData.Length);

    //        return result;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"灰度转RGB失败: {ex.Message}");
    //        return new byte[0];
    //    }
    //}

    ///// <summary>
    ///// 将RGB数据转换为位图
    ///// </summary>
    ///// <param name="rgbData">RGB数据指针</param>
    ///// <returns>位图对象</returns>
    //public Bitmap RgbToBitmap(IntPtr rgbData)
    //{
    //    try
    //    {
    //        IRSDK.IRSDK_Rgb2Bmp(bmpDataPtr, bmpLenPtr, rgbData, pFrame.width, pFrame.height);
    //        return BytesToBitmap(bmpData);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"RGB转位图失败: {ex.Message}");
    //        return null;
    //    }
    //}

    /// <summary>
    /// 保存当前帧为JPEG
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>成功返回true，否则返回false</returns>
    public bool SaveFrameAsJpeg(string filename)
    {
        if (!_isConnected)
            throw new InvalidOperationException("相机未连接");

        lock (_frameLock)
        {
            try
            {
                int result = IRSDK.IRSDK_SaveFrame2Jpeg(
                    filename, input_data_copy, rgbDataPtr, 0, ref sobj);

                bool success = (result == 0);
                Debug.WriteLine($"保存JPEG: {(success ? "成功" : "失败")} - {filename}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存JPEG失败: {ex.Message}");
                return false;
            }
        }
    }
    #endregion

    #region 温度测量
    /// <summary>
    /// 获取点温度
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>温度值</returns>
    public float GetPointTemperature(int x, int y)
    {
        try
        {
            sPt.sPoint.x = (ushort)x;
            sPt.sPoint.y = (ushort)y;
            sPt.inputEmiss = 0.98f;
            sPt.inputDis = 2;
            sPt.inputReflect = 20;
            sPt.inputOffset = 0;

            int ret = IRSDK.IRSDK_GetPointTemp(input_data_copy, ref sPt, 0);
            if (ret == 0)
            {
                return sPt.sTemp.avgTemper;
            }

            return 0.0f;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取点温度失败: {ex.Message}");
            return 0.0f;
        }
    }

    /// <summary>
    /// 获取矩形区域温度
    /// </summary>
    /// <param name="x1">矩形左上角X坐标</param>
    /// <param name="y1">矩形左上角Y坐标</param>
    /// <param name="x2">矩形右下角X坐标</param>
    /// <param name="y2">矩形右下角Y坐标</param>
    /// <returns>温度统计信息</returns>
    public IRSDK.STAT_TEMPER GetRectTemperature(int x1, int y1, int x2, int y2)
    {
        try
        {
            sRectStat.sRect.P1.x = (ushort)x1;
            sRectStat.sRect.P1.y = (ushort)y1;
            sRectStat.sRect.P2.x = (ushort)x2;
            sRectStat.sRect.P2.y = (ushort)y2;
            sRectStat.inputEmiss = 0.98f;
            sRectStat.inputDis = 2;
            sRectStat.inputReflect = 20;
            sRectStat.inputOffset = 0;

            int ret = IRSDK.IRSDK_GetRectTemp(input_data_copy, ref sRectStat, 0);
            if (ret == 0)
            {
                return sRectStat.sTemp;
            }

            return new IRSDK.STAT_TEMPER();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取矩形温度失败: {ex.Message}");
            return new IRSDK.STAT_TEMPER();
        }
    }

    /// <summary>
    /// 获取多边形区域温度
    /// </summary>
    /// <param name="points">多边形顶点数组</param>
    /// <returns>温度统计信息</returns>
    public IRSDK.STAT_TEMPER GetPolygonTemperature(Point[] points)
    {
        try
        {
            if (points.Length > 16) points = points.Take(16).ToArray(); // 限制最多16个点

            spoly.sPolygon.Pt_num = (uint)points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                spoly.sPolygon.Pt[i].x = (ushort)points[i].X;
                spoly.sPolygon.Pt[i].y = (ushort)points[i].Y;
            }

            spoly.inputEmiss = 0.98f;
            spoly.inputDis = 2;
            spoly.inputReflect = 20;
            spoly.inputOffset = 0;

            int ret = IRSDK.IRSDK_GetPolygonTemp(input_data_copy, ref spoly, 0);
            if (ret == 0)
            {
                return spoly.sTemp;
            }

            return new IRSDK.STAT_TEMPER();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取多边形温度失败: {ex.Message}");
            return new IRSDK.STAT_TEMPER();
        }
    }
    #endregion

    #region 辅助方法
    public static Bitmap BytesToBitmap(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("字节数组为空", nameof(bytes));

        using (var stream = new MemoryStream(bytes))
        {
            return new Bitmap(stream);  // 只创建一次
        }
    }

    public static T BytesToStruct<T>(byte[] bytes) where T : struct
    {      
        int size = Marshal.SizeOf(typeof(T));

        // 如果结构体对象的字节数大于所给byte数组的长度，则返回空
        if (size > bytes.Length)
        {
            return (default(T));
        }

        IntPtr structPtr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, 0, structPtr, size);
            return (T)Marshal.PtrToStructure(structPtr, typeof(T));
        }
        catch (Exception)
        {
            Marshal.FreeHGlobal(structPtr);
            throw;
        }
        finally
        {
            Marshal.FreeHGlobal(structPtr);
        }             
    }

    #region 回调处理
    private int FrameProc(IntPtr hFrame, IntPtr lParam)
    {
        try
        {
            lock (_frameLock)  // 添加锁保护
            {
                input_data = hFrame;
                Marshal.Copy(input_data, head_data, 0, headSize + tempSize);

                pFrame = BytesToStruct<IRSDK.Frame>(head_data);
                Marshal.StructureToPtr(pFrame, input_data_copy, false);

                IRSDK.IRSDK_Frame2Gray(input_data_copy, garyDataPtr,
                    (float)u8Constrast, (float)u8Bright, 0);

                IRSDK.IRSDK_Gray2Rgb(garyDataPtr, rgbDataPtr,
                    pFrame.width, pFrame.height, paltype, palindex);

                IRSDK.IRSDK_Rgb2Bmp(bmpDataPtr, bmpLenPtr, rgbDataPtr,
                    pFrame.width, pFrame.height);
            }

            return 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"帧处理失败: {ex.Message}");
            return 0;
        }
    }
    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // 释放托管资源
            DisposeSdk();
        }

        // 释放 GCHandle
        if (_garyHandle.IsAllocated) _garyHandle.Free();
        if (_rgbHandle.IsAllocated) _rgbHandle.Free();
        if (_bmpHandle.IsAllocated) _bmpHandle.Free();
        if (_bmpLenHandle.IsAllocated) _bmpLenHandle.Free();
        if (_ipHandle.IsAllocated) _ipHandle.Free();
        if (_threadHandle.IsAllocated) _threadHandle.Free();

        // 释放非托管内存
        if (input_data_copy != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(input_data_copy);
            input_data_copy = IntPtr.Zero;
        }

        if (sFrameRelayPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(sFrameRelayPtr);
            sFrameRelayPtr = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~IrCameraSdkWrapper()
    {
        Dispose(false);
    }
    #endregion
}

/// <summary>
/// 红外相机SDK API类
/// </summary>
public class IRSDK
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Frame
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort width;
        [MarshalAs(UnmanagedType.U2)]
        public ushort height;
        [MarshalAs(UnmanagedType.U2)]
        public ushort u16FpaTemp;
        [MarshalAs(UnmanagedType.U2)]
        public ushort u16EnvTemp;
        [MarshalAs(UnmanagedType.U1)]
        public byte u8TempDiv;
        [MarshalAs(UnmanagedType.U1)]
        public byte u8DeviceType;
        [MarshalAs(UnmanagedType.U1)]
        public byte u8SensorType;
        [MarshalAs(UnmanagedType.U1)]
        public byte u8MeasureSel;
        [MarshalAs(UnmanagedType.U1)]
        public byte u8Lens;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] Reversed;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 640 * 512)] //最大640*512
        public short[] buffer;
    };

    public struct T_IPADDR
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string IPAddr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved;
        [MarshalAs(UnmanagedType.U4)]
        public int DataPort;
        [MarshalAs(UnmanagedType.U1)]
        public byte isValid;
        [MarshalAs(UnmanagedType.U1)]
        public byte totalOnline;
        [MarshalAs(UnmanagedType.U1)]
        public byte Index;
    };

    public struct T_POINT
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort x;
        [MarshalAs(UnmanagedType.U2)]
        public ushort y;
    };

    public struct T_LINE
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT P1;
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT P2;
    };

    public struct T_CIRCLE
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT Pc;
        [MarshalAs(UnmanagedType.U2)]
        public ushort a;
        [MarshalAs(UnmanagedType.U2)]
        public ushort b;
    };

    public struct T_RECT
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT P1;
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT P2;
    };

    public struct T_POLYGON
    {
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 Pt_num;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 16)]
        public T_POINT[] Pt;
    };

    public struct STAT_TEMPER
    {
        [MarshalAs(UnmanagedType.R4)]
        public float maxTemper;
        [MarshalAs(UnmanagedType.R4)]
        public float minTemper;
        [MarshalAs(UnmanagedType.R4)]
        public float avgTemper;
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT maxTemperPT;
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT minTemperPT;
    };

    public struct T_COLOR
    {
        [MarshalAs(UnmanagedType.U1)]
        public char r;
        [MarshalAs(UnmanagedType.U1)]
        public char g;
        [MarshalAs(UnmanagedType.U1)]
        public char b;
        [MarshalAs(UnmanagedType.U1)]
        public char a;
    };

    public enum T_ALARMTYPE
    {
        OverHigh = 0,               //高于高门限
        UnderLow = 1,               //低于低门限
        BetweenHL = 2,              //区间
        DeBetweenHL = 3,                //反选区间
    }

    public struct T_ALARM
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte alarmType;
        [MarshalAs(UnmanagedType.U1)]
        public byte isDraw;
        [MarshalAs(UnmanagedType.U1)]
        public byte isVioce;
        [MarshalAs(UnmanagedType.U1)]
        public byte isVideo;
        [MarshalAs(UnmanagedType.R4)]
        public float HighThresh;
        [MarshalAs(UnmanagedType.R4)]
        public float LowThresh;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR colorAlarm;          //对象颜色
    };

    public struct STAT_GLOBAL
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_LINE sLine;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_TEMPER sTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] LableEx;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lable;
        [MarshalAs(UnmanagedType.R4)]
        public float inputEmiss;        //输入辐射率
        [MarshalAs(UnmanagedType.R4)]
        public float inputReflect;      //输入反射温度
        [MarshalAs(UnmanagedType.R4)]
        public float inputDis;          //输入距离
        [MarshalAs(UnmanagedType.R4)]
        public float inputOffset;       //输入温度偏正
        [MarshalAs(UnmanagedType.R4)]
        public float Area;              //计算面积
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved1;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved3;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved4;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR unused1;
        [MarshalAs(UnmanagedType.Struct)]
        public T_ALARM unused2;
    };

    public struct STAT_POINT
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_POINT sPoint;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_TEMPER sTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] LableEx;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lable;
        [MarshalAs(UnmanagedType.R4)]
        public float inputEmiss;        //输入辐射率
        [MarshalAs(UnmanagedType.R4)]
        public float inputReflect;      //输入反射温度
        [MarshalAs(UnmanagedType.R4)]
        public float inputDis;          //输入距离
        [MarshalAs(UnmanagedType.R4)]
        public float inputOffset;       //输入温度偏正
        [MarshalAs(UnmanagedType.R4)]
        public float Area;              //计算面积
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved1;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved3;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved4;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR unused1;
        [MarshalAs(UnmanagedType.Struct)]
        public T_ALARM unused2;
    };

    public struct STAT_LINE
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_LINE sLine;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_TEMPER sTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] LableEx;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lable;
        [MarshalAs(UnmanagedType.R4)]
        public float inputEmiss;        //输入辐射率
        [MarshalAs(UnmanagedType.R4)]
        public float inputReflect;      //输入反射温度
        [MarshalAs(UnmanagedType.R4)]
        public float inputDis;          //输入距离
        [MarshalAs(UnmanagedType.R4)]
        public float inputOffset;       //输入温度偏正
        [MarshalAs(UnmanagedType.R4)]
        public float Area;              //计算面积
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved1;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved3;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved4;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR unused1;
        [MarshalAs(UnmanagedType.Struct)]
        public T_ALARM unused2;
    };

    public struct STAT_CIRCLE
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_CIRCLE sCircle;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_TEMPER sTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] LableEx;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lable;
        [MarshalAs(UnmanagedType.R4)]
        public float inputEmiss;        //输入辐射率
        [MarshalAs(UnmanagedType.R4)]
        public float inputReflect;      //输入反射温度
        [MarshalAs(UnmanagedType.R4)]
        public float inputDis;          //输入距离
        [MarshalAs(UnmanagedType.R4)]
        public float inputOffset;       //输入温度偏正
        [MarshalAs(UnmanagedType.R4)]
        public float Area;              //计算面积
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved1;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved3;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved4;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR color;           //对象颜色
        [MarshalAs(UnmanagedType.Struct)]
        public T_ALARM sAlarm;          //对象颜色
    };

    public struct STAT_RECT
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_RECT sRect;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_TEMPER sTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] LableEx;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lable;
        [MarshalAs(UnmanagedType.R4)]
        public float inputEmiss;        //输入辐射率
        [MarshalAs(UnmanagedType.R4)]
        public float inputReflect;      //输入反射温度
        [MarshalAs(UnmanagedType.R4)]
        public float inputDis;          //输入距离
        [MarshalAs(UnmanagedType.R4)]
        public float inputOffset;       //输入温度偏正
        [MarshalAs(UnmanagedType.R4)]
        public float Area;              //计算面积
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved1;
        [MarshalAs(UnmanagedType.R4)]
        public float offset;            //温度偏移
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved3;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved4;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR color;           //对象颜色
        [MarshalAs(UnmanagedType.Struct)]
        public T_ALARM sAlarm;          //对象颜色
    };

    public struct STAT_POLYGON
    {
        [MarshalAs(UnmanagedType.Struct)]
        public T_POLYGON sPolygon;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_TEMPER sTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] LableEx;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lable;
        [MarshalAs(UnmanagedType.R4)]
        public float inputEmiss;        //输入辐射率
        [MarshalAs(UnmanagedType.R4)]
        public float inputReflect;      //输入反射温度
        [MarshalAs(UnmanagedType.R4)]
        public float inputDis;          //输入距离
        [MarshalAs(UnmanagedType.R4)]
        public float inputOffset;       //输入温度偏正
        [MarshalAs(UnmanagedType.R4)]
        public float Area;              //计算面积
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved1;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved3;
        [MarshalAs(UnmanagedType.U1)]
        public byte reserved4;
        [MarshalAs(UnmanagedType.Struct)]
        public T_COLOR unused1;
        [MarshalAs(UnmanagedType.Struct)]
        public T_ALARM unused2;
    };

    public struct STAT_OBJ
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte numPt;
        [MarshalAs(UnmanagedType.U1)]
        public byte numLine;
        [MarshalAs(UnmanagedType.U1)]
        public byte numCircle;
        [MarshalAs(UnmanagedType.U1)]
        public byte numRect;
        [MarshalAs(UnmanagedType.U1)]
        public byte numPolygon;
        [MarshalAs(UnmanagedType.U1)]
        public byte Reserved1;
        [MarshalAs(UnmanagedType.U1)]
        public byte Reserved2;
        [MarshalAs(UnmanagedType.U1)]
        public byte Reserved3;
        [MarshalAs(UnmanagedType.Struct)]
        public STAT_GLOBAL sGlobal;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 32)]
        public STAT_POINT[] sPt;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 32)]
        public STAT_LINE[] sLine;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 32)]
        public STAT_CIRCLE[] sCircle;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 32)]
        public STAT_RECT[] sRect;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 32)]
        public STAT_POLYGON[] sPolygon;
    };

    public struct T_SAVE_HEAD
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Head;
        [MarshalAs(UnmanagedType.U2)]
        public ushort width;
        [MarshalAs(UnmanagedType.U2)]
        public ushort height;
        [MarshalAs(UnmanagedType.U4)]
        public int totalFrames;
        [MarshalAs(UnmanagedType.U2)]
        public ushort Freq;
        [MarshalAs(UnmanagedType.U1)]
        public byte isSaveObj;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 85)]
        public string Reserved;
    };

    public struct T_DEVICE_INFO
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Model;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string SerialNum;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Lens;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FactoryTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string WorkTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Mac;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string IP;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved2;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved3;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved4;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved5;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved6;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved7;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Reserved8;
    };

    public enum T_PARAMTYPE
    {
        paramDevice = 0,            //设备类型
        paramDownSample = 1,        //降采样
        paramDecCoef = 2,           //校正系数
        paramReserved1 = 3,     //保留
        paramReserved2 = 4,     //保留
        paramSpaceFilter = 5,       //空域滤波
        paramReserved4 = 6,     //保留
        paramTempSegSel = 7,        //温度段选择
        paramReserved6 = 8,     //保留
        paramReserved7 = 9,     //保留
        paramReserved8 = 10,        //保留
        paramCaliSwitch = 11,       //快门校正开关
        paramTempCorrect = 12,    //校正温度
        paramReserved11 = 13,     //保留
        paramReserved12 = 14,     //保留
        paramReserved13 = 15,     //保留
        paramAutoSelTempSelSw = 16,     //自动选择温度段开关
        paramObjTempFilterSw = 17,      //测温对象温度滤波开关
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int CBF_IR(IntPtr p, IntPtr lParam);

#if X64
        // 64位版本
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_Init();
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_Quit();
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_SetIPAddrArray(IntPtr pIpInfo);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Create(int handle, T_IPADDR tagIPaddr, CBF_IR cbf_stm, CBF_IR cbf_cmd, CBF_IR cbf_comm, int param = 0);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Destroy(int handle);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Connect(int handle);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Play(int handle);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Stop(int handle);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_SetIP(int handle, string param);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Command(int handle, int command, int param);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Calibration(int handle);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_CommSend(int handle, string pBuf, int len);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static byte IRSDK_IsConnected(int handle);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_InqureIP(IntPtr pIpInfo, uint TimerInterval);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_InqureDeviceInfo(int handle, ref T_DEVICE_INFO pDevInfo);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_ParamCfg(int handle, T_PARAMTYPE mParamType, float f32Param);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_NearFarFocus(int handle, uint param);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Frame2Gray(IntPtr pFrame, IntPtr yn8, float f32Constrast, float f32Bright, ushort u16TFilterCoef);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetPaletteJpeg(IntPtr pPaletteJpeg, IntPtr pJpegLen, byte Method, int PalType, int Pal);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetPaletteBmp(IntPtr pPaletteBmp, IntPtr pBmpLen, byte Method, int PalType, int Pal);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetGlobalTemp(IntPtr pFrame, ref STAT_GLOBAL pGlobalStat);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetPointTemp(IntPtr pFrame, ref STAT_POINT pPointStat, byte index);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetLineTemp(IntPtr pFrame, ref STAT_LINE pLineStat, byte index);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetCircleTemp(IntPtr pFrame, ref STAT_CIRCLE pCircleStat, byte index);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetRectTemp(IntPtr pFrame, ref STAT_RECT pRectStat, byte index);
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetPolygonTemp(IntPtr pFrame, ref STAT_POLYGON pPolygonStat, byte index);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_GetObjTemp(IntPtr pFrame, ref STAT_OBJ pObjStat);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Gray2Rgb(IntPtr pGray, IntPtr pRgb, ushort Width, ushort Height, int PalType, int Pal);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Rgb2Bmp(IntPtr pBmpData, IntPtr pLen, IntPtr pRgb, ushort Width, ushort Height);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_Rgb2Jpeg(IntPtr pJpegout, IntPtr pLen, int quality, IntPtr pRgb, ushort Width, ushort Height);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_SaveFrame2Jpeg(string pFile, IntPtr pFrame, IntPtr pRgb, byte isSaveObj, ref STAT_OBJ pObj);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_ReadJpeg2Frame(string pFile, IntPtr pFrame, byte isLoadObj, ref STAT_OBJ pObj);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_SaveFrame2CSV(string pFile, IntPtr pFrame, ref T_RECT sRect);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_SaveFrame2Video(string pFile, IntPtr pFrame, byte Op, byte isSaveObj, ref STAT_OBJ pObj, IntPtr pThreadBuf);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_ReadVideo2Frame(string pFile, IntPtr pFrame, uint Index, byte Op, ref T_SAVE_HEAD pVideoHead, ref STAT_OBJ pObj, IntPtr pThreadBuf);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_SaveRgb2AVI(string pFile, IntPtr pRgb, ushort Width, ushort Height, byte Op, int quality, IntPtr pThreadBuf);

        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int IRSDK_MoveCtrl(int handle, int mProtocol, int mType, int u32Param);
#else
    // 32位版本
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static uint IRSDK_Init();
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static uint IRSDK_Quit();
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static uint IRSDK_SetIPAddrArray(IntPtr pIpInfo);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Create(int handle, T_IPADDR tagIPaddr, CBF_IR cbf_stm, CBF_IR cbf_cmd, CBF_IR cbf_comm, int param = 0);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Destroy(int handle);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Connect(int handle);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Play(int handle);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Stop(int handle);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_SetIP(int handle, string param);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Command(int handle, int command, int param);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Calibration(int handle);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_CommSend(int handle, string pBuf, int len);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static byte IRSDK_IsConnected(int handle);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static uint IRSDK_InqureIP(IntPtr pIpInfo, uint TimerInterval);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static uint IRSDK_InqureDeviceInfo(int handle, ref T_DEVICE_INFO pDevInfo);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_ParamCfg(int handle, T_PARAMTYPE mParamType, float f32Param);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_NearFarFocus(int handle, uint param);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Frame2Gray(IntPtr pFrame, IntPtr yn8, float f32Constrast, float f32Bright, ushort u16TFilterCoef);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetPaletteJpeg(IntPtr pPaletteJpeg, IntPtr pJpegLen, byte Method, int PalType, int Pal);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetPaletteBmp(IntPtr pPaletteBmp, IntPtr pBmpLen, byte Method, int PalType, int Pal);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetGlobalTemp(IntPtr pFrame, ref STAT_GLOBAL pGlobalStat);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetPointTemp(IntPtr pFrame, ref STAT_POINT pPointStat, byte index);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetLineTemp(IntPtr pFrame, ref STAT_LINE pLineStat, byte index);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetCircleTemp(IntPtr pFrame, ref STAT_CIRCLE pCircleStat, byte index);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetRectTemp(IntPtr pFrame, ref STAT_RECT pRectStat, byte index);
    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetPolygonTemp(IntPtr pFrame, ref STAT_POLYGON pPolygonStat, byte index);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_GetObjTemp(IntPtr pFrame, ref STAT_OBJ pObjStat);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Gray2Rgb(IntPtr pGray, IntPtr pRgb, ushort Width, ushort Height, int PalType, int Pal);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Rgb2Bmp(IntPtr pBmpData, IntPtr pLen, IntPtr pRgb, ushort Width, ushort Height);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_Rgb2Jpeg(IntPtr pJpegout, IntPtr pLen, int quality, IntPtr pRgb, ushort Width, ushort Height);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_SaveFrame2Jpeg(string pFile, IntPtr pFrame, IntPtr pRgb, byte isSaveObj, ref STAT_OBJ pObj);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_ReadJpeg2Frame(string pFile, IntPtr pFrame, byte isLoadObj, ref STAT_OBJ pObj);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_SaveFrame2CSV(string pFile, IntPtr pFrame, ref T_RECT sRect);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_SaveFrame2Video(string pFile, IntPtr pFrame, byte Op, byte isSaveObj, ref STAT_OBJ pObj, IntPtr pThreadBuf);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_ReadVideo2Frame(string pFile, IntPtr pFrame, uint Index, byte Op, ref T_SAVE_HEAD pVideoHead, ref STAT_OBJ pObj, IntPtr pThreadBuf);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_SaveRgb2AVI(string pFile, IntPtr pRgb, ushort Width, ushort Height, byte Op, int quality, IntPtr pThreadBuf);

    [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public extern static int IRSDK_MoveCtrl(int handle, int mProtocol, int mType, int u32Param);
#endif
}