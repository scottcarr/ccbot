using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Diagnostics;
namespace Github
{
    class Program
    {
        static readonly string gitCmd = @"C:\Users\Scott\AppData\Local\GitHub\PortableGit_69703d1db91577f4c666e767a6ca5ec50a48d243\bin\git.exe";
        static void Main(string[] args)
        {
            const int N = 1; // how many hundreds of results you want
            var repos = new List<Tuple<string, string>>(); // for now I am just keeping the name and clone url (both strings)

            for (int i = 0; i < N; i++)
            {
                var urlWithHole = "https://api.github.com/search/repositories?q=language:csharp&sort=stars&order=desc&page={0}&per_page=100/";
                var url = String.Format(urlWithHole, i);
            	var request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            	request.UserAgent = "ReviewBot"; // YOU MUST HAVE THIS SET TO SOMETHING !!!!
            	var resp = request.GetResponse() as HttpWebResponse;
            	var reader = new StreamReader(resp.GetResponseStream());
            	var results = JsonConvert.DeserializeObject<SearchResponse>(reader.ReadToEnd());
                foreach (var result in results.items) 
                {
                    repos.Add(new Tuple<string,string>(result.name, result.clone_url));
                    cloneRepo(result.clone_url);
                }
            }
            repos.ForEach(Console.WriteLine);
            Console.ReadKey();
        }
        static void cloneRepo(string url)
        {
            Process p = new Process();
            p.StartInfo.FileName = gitCmd;
            p.StartInfo.Arguments = String.Format("clone {0}", url);
            p.StartInfo.UseShellExecute = true;
            p.Start();
            p.WaitForExit();
        }
    }
} 