using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NatGeoImgs
{
    class Program
    {
        static Conf _conf;
        static async Task Main(string[] args)
        {
            _conf = Deserialize<Conf>(GetEnvValue("CONF"));

            await NGImg.InitDB();
            List<NGImg> imgs = await NGImg.Query(DateTime.UtcNow.AddDays(-1));

            using HttpClient client = new HttpClient();
            UnicodeEncoding enc = new UnicodeEncoding();
            Random rand = new Random();
            string data = await client.GetStringAsync(_conf.Url);
            XElement xe = XElement.Parse(data);
            foreach (XElement xeItem in xe.Descendants("item"))
            {
                XText cdataNode = xeItem.Element("description").FirstNode as XText;
                GroupCollection groups = Regex.Match(cdataNode.Value, @"(pod-[\s\S]+?)\.jpg[\s\S]+>([\s\S]+)").Groups;
                string key = groups[1].Value;
                NGImg img = imgs.FirstOrDefault(p => p.Key == key);
                if (img == null)
                {
                    string enDesc = groups[2].Value;
                    string salt = rand.Next(1000, 10000).ToString();
                    string sign = MD5Hash($"{_conf.BdAppId}{enDesc}{salt}{_conf.BdSecret}");
                    string rspData = await client.GetStringAsync($"http://api.fanyi.baidu.com/api/trans/vip/translate?from=en&to=zh&appid={_conf.BdAppId}&salt={salt}&sign={sign}&q={enDesc}");
                    BdTransRsp bdTransRsp = Deserialize<BdTransRsp>(rspData);
                    string zhDesc = enc.GetString(enc.GetBytes(bdTransRsp.trans_result[0].dst));

                    img = new NGImg
                    {
                        Key = key,
                        Title = xeItem.Element("title").Value,
                        Link = xeItem.Element("link").Value,
                        EnDesc = enDesc,
                        ZhDesc = zhDesc
                    };
                    await NGImg.Add(img);
                }

                cdataNode.Value += $"<br/>{img.ZhDesc}";
            }

            string xml = xe.ToString();
            await File.WriteAllTextAsync("rss.xml", xml);
            Console.WriteLine(xml);
        }

        static string MD5Hash(string str)
        {
            StringBuilder sbHash = new StringBuilder(32);
            byte[] s = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str));
            for (int i = 0; i < s.Length; i++)
            {
                sbHash.Append(s[i].ToString("x2"));
            }
            return sbHash.ToString();
        }

        static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

        static string GetEnvValue(string key) => Environment.GetEnvironmentVariable(key);
    }

    #region Conf

    public class Conf
    {
        public string Url { get; set; }
        public string BdAppId { get; set; }
        public string BdSecret { get; set; }
    }

    #endregion
}
