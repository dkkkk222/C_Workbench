using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Utils.Common
{
    public class HttpHelper : IDisposable
    {
        private readonly HttpClient _httpClient;
        public HttpHelper()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetAsync(string uri)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode(); // 确认响应成功，否则将抛出异常
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                // 处理HTTP请求异常
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        public async Task<Stream> GetStreamAsync(string uri)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode(); // 确认响应成功，否则将抛出异常
                var responseBody = await response.Content.ReadAsStreamAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                // 处理HTTP请求异常
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        public async Task<HttpResponseMessage> GetStreamAsync2(string uri)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode(); // 确认响应成功，否则将抛出异常                

                return response;
            }
            catch (HttpRequestException e)
            {
                // 处理HTTP请求异常
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        public async Task<string> PostAsync(string uri, object data)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                // 处理HTTP请求异常
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        // 释放HttpClient实例
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
