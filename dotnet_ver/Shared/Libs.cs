using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shared
{
    public static class Libs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username">校园网用户名</param>
        /// <param name="password">密码</param>
        /// <param name="channel">运营商</param>
        /// <returns>登陆操作是否成功</returns>
        /// <exception cref="HttpRequestException"></exception>
        public static async Task<bool> AuthAsync(string username, string password, Channel channel)
        {
            CookieContainer cookies = new CookieContainer();
            HttpClient client = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = cookies
            });
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.1043.1 Safari/537.36 Edg/96.0.1043.1");

            var redirResp = await client.GetAsync("https://i.njtech.edu.cn");
            var body = await redirResp.Content.ReadAsStringAsync();

            // long ago there's someone saying C# Regex has low performance... Shall we avoid using it?
            Regex actionUrlPattern = new Regex(@"<form.*action=""(/cas/login.+?)\""");
            Regex ltInputPattern = new Regex(@"<input.*name\w?=\w?""lt"" value=""(.*?)""");
            Regex executionInputPattern = new Regex(@"<input[\w\s\S]*name\w?=\w?""execution"" value=""(.*?)""");

            UriBuilder formSubmitUrlBuilder = new UriBuilder("https://u.njtech.edu.cn");//only base
            formSubmitUrlBuilder.Path = actionUrlPattern.Match(body).Groups[1].Value;
            Uri formSubmitUrl = formSubmitUrlBuilder.Uri;
            string ltInputValue = ltInputPattern.Match(body).Groups[1].Value;
            string executionInputValue = executionInputPattern.Match(body).Groups[1].Value;

            Dictionary<string, string> @params = new();
            @params.Add("username", username);
            @params.Add("password", password);
            @params.Add("channelshow", channel.ToChannelShowParam());
            @params.Add("channel", channel.ToChannelParam());
            @params.Add("lt", ltInputValue);
            @params.Add("execution", executionInputValue);
            @params.Add("_eventId", "submit");
            @params.Add("login", "登录");
            FormUrlEncodedContent content = new FormUrlEncodedContent(@params);
            var loginResp = await client.PostAsync(formSubmitUrl, content);

            bool success = loginResp.StatusCode == HttpStatusCode.OK
                && loginResp.RequestMessage.RequestUri.ToString() == "https://i.njtech.edu.cn/index.html";
            return success;
        }
    }
}
