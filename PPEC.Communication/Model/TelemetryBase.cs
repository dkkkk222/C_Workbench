using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Mozilla;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    /// <summary>
    /// 字节序（拼接 StartByte..StartByte+ByteCount-1 的顺序）
    /// </summary>
    public enum ByteOrder { BE, LE }

    /// <summary>
    /// 目标类型（位段切片后的数据类型）
    /// </summary>
    public enum TargetType { U8, I8, U16, I16, U32, I32, Float32, UInt, Int }

    /// <summary>
    /// 遥测位切片字段定义：支持任意字节/位区间，支持BE/LE，支持目标类型和线性缩放
    /// </summary>
    public sealed class TelemetrySliceField:BindableBase
    {
        public string Name { get; set; } = "";
        /// <summary>从有效负载payload的第几个字节开始（0-based）</summary>
        public int StartByte { get; set; }
        /// <summary>参与拼接的字节数（>=1）</summary>
        public int ByteCount { get; set; }
        /// <summary>在拼接出的无符号整数中，从LSB=0开始的起始位</summary>
        public int BitStart { get; set; }
        /// <summary>位长度（1..32；Float32需=32）</summary>
        public int BitLength { get; set; }
        public int EndLength => BitLength + BitStart-1;
        /// <summary>拼接字节序（协议多字节普遍BE）</summary>
        public ByteOrder Order { get; set; } = ByteOrder.BE;
        /// <summary>目标类型（U/I16、U/I32、Float32等）</summary>
        public TargetType As { get; set; } = TargetType.U32;
        /// <summary>线性缩放：value*Scale + Offset</summary>
        public double Scale { get; set; } = 1.0;
        public double Offset { get; set; } = 0.0;

        public string Unit { get; set; }
        public string ShowStr { get; set; }
        public string ParseStr { get; set; }

        private string _ShowHexStr;
        public string ShowHexStr 
        { 
            get=> _ShowHexStr;
            set=>SetProperty(ref _ShowHexStr,value); 
        }
        public string ParamA { get; set; }
        public string ParamB { get; set; }
        public string ParamC { get; set; }
        public string ParamSign { get; set; }

        private string _ShowResult;
        public string ShowResult
        {
            get => _ShowResult;
            set => SetProperty(ref _ShowResult, value);
        }

        public double _SourceData;
        public double SourceData
        {
            get => _SourceData;
            set => SetProperty(ref _SourceData, value);
        }
        public string StringResult { get; set; }

        private bool _IsChecked=false;
        public bool IsChecked
        {
            get => _IsChecked;
            set => SetProperty(ref _IsChecked, value);
        }
    }
    public sealed class TelemetryRecord
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>原始有效数据域</summary>
        public byte[] RawPayload { get; set; } = Array.Empty<byte>();
        /// <summary>解析后的键值（位切片或顺序解析）</summary>
        public Dictionary<string, double> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
    public readonly struct ControlAck
    {
        public bool Success { get; }
        public ushort RawCode { get; }   // 0xAAAA / 0xFFFF / 其他
        public byte[] RawPayload { get; }
        public ControlAck(bool ok, ushort code, byte[] payload)
        {
            Success = ok; RawCode = code; RawPayload = payload;
        }
    }
    public sealed class TelemetryEventArgs : EventArgs
    {
        public byte[] Payload { get; }
        public TelemetryEventArgs(byte[] payload) => Payload = payload;
    }
}
