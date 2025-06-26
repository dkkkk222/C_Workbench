using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Workbench.Models;

namespace Workbench.Utils.Common
{
    public static class UpdateHelper
    {
        public static T GetLocalInfo<T>(string jsonFileName)
        {
            T jsonFileDes = default(T);
            if (!File.Exists(jsonFileName))
                return jsonFileDes;
            try
            {
                var json = File.ReadAllText(jsonFileName);
                jsonFileDes = JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {

            }
            return jsonFileDes;
        }
        public static async Task<Upgrade> GetUpdaterRemoteInfo(string _host, string upgradeFileName)
        {
            try
            {
                Upgrade updater = null;
                using (var client = new HttpHelper())
                {
                    var url = $"{_host}/{upgradeFileName}";
                    var content = await client.GetAsync(url);
                    if (!string.IsNullOrEmpty(content))
                    {
                        updater = JsonConvert.DeserializeObject<Upgrade>(content);
                    }
                }
                return updater;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
        /// 版本号比较
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns>0：版本相同，1：版本1高于版本2，-1：版本1低于版本2</returns>
        public static int Compare(string version1, string version2)
        {
            var res = 0;
            Version v1 = new Version(version1);
            Version v2 = new Version(version2);

            int comparisonResult = v1.CompareTo(v2);

            if (comparisonResult > 0)
            {
                //Console.WriteLine($"{version1} is greater than {version2}.");
                res = 1;
            }
            else if (comparisonResult < 0)
            {
                //Console.WriteLine($"{version1} is less than {version2}.");
                res = -1;
            }
            else
            {
                //Console.WriteLine($"{version1} is equal to {version2}.");
            }
            return res;
        }
    }
}
