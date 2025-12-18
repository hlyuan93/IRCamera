
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IRCamera.Hardware
{
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
            OverHigh = 0,				//高于高门限
            UnderLow = 1,			    //低于低门限
            BetweenHL = 2,				//区间
            DeBetweenHL = 3,				//反选区间
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
            public T_COLOR colorAlarm;			//对象颜色
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
            public float inputEmiss;		//输入辐射率
            [MarshalAs(UnmanagedType.R4)]
            public float inputReflect;		//输入反射温度
            [MarshalAs(UnmanagedType.R4)]
            public float inputDis;		    //输入距离
            [MarshalAs(UnmanagedType.R4)]
            public float inputOffset;		//输入温度偏正
            [MarshalAs(UnmanagedType.R4)]
            public float Area;		        //计算面积
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
            public float inputEmiss;		//输入辐射率
            [MarshalAs(UnmanagedType.R4)]
            public float inputReflect;		//输入反射温度
            [MarshalAs(UnmanagedType.R4)]
            public float inputDis;		    //输入距离
            [MarshalAs(UnmanagedType.R4)]
            public float inputOffset;		//输入温度偏正
            [MarshalAs(UnmanagedType.R4)]
            public float Area;		        //计算面积
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
            public float inputEmiss;		//输入辐射率
            [MarshalAs(UnmanagedType.R4)]
            public float inputReflect;		//输入反射温度
            [MarshalAs(UnmanagedType.R4)]
            public float inputDis;		    //输入距离
            [MarshalAs(UnmanagedType.R4)]
            public float inputOffset;		//输入温度偏正
            [MarshalAs(UnmanagedType.R4)]
            public float Area;		        //计算面积
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved1;
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved2;
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved3;
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved4;
            [MarshalAs(UnmanagedType.Struct)]
            public T_COLOR color;			//对象颜色
            [MarshalAs(UnmanagedType.Struct)]
            public T_ALARM sAlarm;			//对象颜色
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
            public float inputEmiss;		//输入辐射率
            [MarshalAs(UnmanagedType.R4)]
            public float inputReflect;		//输入反射温度
            [MarshalAs(UnmanagedType.R4)]
            public float inputDis;		    //输入距离
            [MarshalAs(UnmanagedType.R4)]
            public float inputOffset;		//输入温度偏正
            [MarshalAs(UnmanagedType.R4)]
            public float Area;		        //计算面积
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved1;
            [MarshalAs(UnmanagedType.R4)]
            public float offset;		    //温度偏移
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved2;
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved3;
            [MarshalAs(UnmanagedType.U1)]
            public byte reserved4;
            [MarshalAs(UnmanagedType.Struct)]
            public T_COLOR color;			//对象颜色
            [MarshalAs(UnmanagedType.Struct)]
            public T_ALARM sAlarm;			//对象颜色
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
            public float inputEmiss;		//输入辐射率
            [MarshalAs(UnmanagedType.R4)]
            public float inputReflect;		//输入反射温度
            [MarshalAs(UnmanagedType.R4)]
            public float inputDis;		    //输入距离
            [MarshalAs(UnmanagedType.R4)]
            public float inputOffset;		//输入温度偏正
            [MarshalAs(UnmanagedType.R4)]
            public float Area;		        //计算面积
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
#if true
        [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_Init();
        [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_Quit();
        [DllImport("IRSDKlib_Win32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)] //引入dll，并设置字符集
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
        public extern static int
            Temp(IntPtr pFrame, ref STAT_OBJ pObjStat);

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


#else
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_Init();
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static uint IRSDK_Quit();
        [DllImport("IRSDKlib_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)] //引入dll，并设置字符集
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
#endif

    }
}
