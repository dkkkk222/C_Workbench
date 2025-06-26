using PPEC.Communication.Parameter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.CAN
{
    public class SmlsCanFrame
    {
        private string _mailBoxAddress;
        public string MailBoxAddress
        {
            get => _mailBoxAddress;
            set
            {
                MailBoxNum = Convert.ToUInt32(value, 16);
                _mailBoxAddress = value;
            }
        }

        public uint MailBoxNum { get; private set; }

        private List<SmlsCanData> _datas;
        public List<SmlsCanData> Datas
        {
            get => _datas;
            set
            {
                DataDetails = value.ToDictionary(s => s.CanDataName, s => s);
                _datas = value;
            }
        }

        private Dictionary<string, SmlsCanData> _dataDetails;
        public Dictionary<string, SmlsCanData> DataDetails
        {
            get => _dataDetails;
            private set
            {
                _dataDetails = value;
            }
        }

        /// <summary>
        /// 获取原始帧数据(均为默认值)
        /// </summary>
        /// <returns></returns>
        public List<byte> GetOriginalFrame()//IDictionary<string, CANModelParam> DicCache
        {
            var data = new List<byte>();
            foreach (var scd in this.Datas)
            {
                if (scd.NumOfData == 1)
                {
                    if (scd.DefaultValue == null)
                    {
                        data.AddRange(new byte[1]);
                        continue;
                    }

                    data.Add((byte)scd.DefaultValue);
                }
                else if (scd.NumOfData == 2)
                {
                    if (scd.DefaultValue == null)
                    {
                        data.AddRange(new byte[2]);
                        continue;
                    }
                    data.AddRange(BitConverter.GetBytes((ushort)scd.DefaultValue));
                }
                else if (scd.NumOfData == 4)
                {
                    if (scd.DefaultValue == null)
                    {
                        data.AddRange(new byte[4]);
                        continue;
                    }
                    data.AddRange(BitConverter.GetBytes((int)scd.DefaultValue));
                }
            }
            return data;
        }
        public List<byte> GetOriginalFrame(IDictionary<string, CANModelParam> DicCache)
        {
            var data = new List<byte>();
            foreach (var scd in this.Datas)
            {
                if (DicCache.ContainsKey(scd.CanDataName))
                {
                    if (DicCache[scd.CanDataName] != null)
                    {
                        if (DicCache[scd.CanDataName].ValueData != null)
                        {
                            data.AddRange(DicCache[scd.CanDataName].ValueData);
                            continue;
                        }
                    }
                }
                if (scd.NumOfData == 1)
                {
                    if (scd.DefaultValue == null)
                    {
                        data.AddRange(new byte[1]);
                        continue;
                    }

                    data.Add((byte)scd.DefaultValue);
                }
                else if (scd.NumOfData == 2)
                {
                    if (scd.DefaultValue == null)
                    {
                        data.AddRange(new byte[2]);
                        continue;
                    }
                    data.AddRange(BitConverter.GetBytes((ushort)scd.DefaultValue));
                }
                else if (scd.NumOfData == 4)
                {
                    if (scd.DefaultValue == null)
                    {
                        data.AddRange(new byte[4]);
                        continue;
                    }
                    data.AddRange(BitConverter.GetBytes((int)scd.DefaultValue));
                }

            }
            return data;
        }
    }
}
