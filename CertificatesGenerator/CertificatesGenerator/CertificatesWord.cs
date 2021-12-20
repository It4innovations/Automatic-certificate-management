using Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace CertificatesGenerator.DataStructures
{
    public class CertificatesWord
    {
        private const string nameofstatusfile = "status.txt";
        private const string nameofcerconffile = "server-req.cfg";
        private const string serverkey = "serverkey.pem";
        private const string serverreq = "serverreq.pem";
        private const string serversigned = "serversigned.pem";        
        private const string serverpk12 = "serverall.p12";
        private const string URLRequest = "https://tcs.cesnet.cz/api/v2/certificate/request";
        private const string URLStatus = "https://tcs.cesnet.cz/api/v2/certificate/status/";
        private readonly string pathtoroot;
        private readonly string pathtotemplate;
        private readonly string cetrificatechain;
        private readonly string pathtocertificate;
        private readonly string certificatePassword;

        //Email address for notify from cesnet
        private readonly string[] emails = { "test@vsb.cz" };
        private readonly string requesterPhone ="+420123456789";

        private readonly DNSTree tree;

        public CertificatesWord()
        {

        }


        public CertificatesWord(string pathtojsonfile, string pathtoroot, string pathtotemplate, string certificate, string password, string pathchain)
        {
            if (File.Exists(pathtojsonfile) && File.Exists(pathtotemplate))
            {
                string jsonString = File.ReadAllText(pathtojsonfile);
                //  tree = JsonSerializer.Deserialize<DNSTree>(jsonString);
                tree = JsonConvert.DeserializeObject<DNSTree>(jsonString);
                this.pathtoroot = pathtoroot;
                this.pathtotemplate = pathtotemplate;
                pathtocertificate = certificate;
                certificatePassword = password;
                cetrificatechain = pathchain;
            }
            else
            {
                tree = null;
                Console.WriteLine("Missing json or template file");
            }
        }

        public CertificatesWord(string certificate, string password)
        {
            pathtocertificate = certificate;
            certificatePassword = password;
        }

        public bool IsInicializationDone()
        {
            if (tree == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Run()
        {
            if (tree != null)
            {
                treeway(pathtoroot, tree);
            }

        }

        private void treeway(string path, DNSTree dNSTree)
        {
            string temppath = System.IO.Path.Combine(path, dNSTree.MainDNS);
            if (dNSTree.Childs == null || dNSTree.Childs.Count == 0)
            {

                Directory.CreateDirectory(temppath);
                DirectoryWork(temppath, dNSTree);
            }
            else
            {
                foreach (DNSTree item in dNSTree.Childs)
                {
                    treeway(temppath, item);
                }
            }
        }

        private void DirectoryWork(string path, DNSTree dNSTree)
        {
            string tempstatusfile = System.IO.Path.Combine(path, nameofstatusfile);
            string tempMainDNS = GetMainDNS(path);

            bool findpreviousversion = false;

            if(dNSTree.AutomaticRenew)
            {
                if (CheckLessThenTwoWeeksPemFile(System.IO.Path.Combine(path, serversigned)))
                {
                    File.Delete(tempstatusfile);
                    findpreviousversion = true;
                }
            }

            if (File.Exists(tempstatusfile))
            {
                string[] lines = File.ReadAllLines(tempstatusfile);

                if (lines.Length != 0)
                {
                    if (lines[lines.Length - 1].Contains("Waiting"))
                    {
                        string[] arr = lines[lines.Length - 1].Split(':');
                        if (arr.Length == 2)
                        {
                            bool result = GetStatus(path, Convert.ToInt32(arr[1]));

                            if (result)
                            {
                                CreateDifferentFormats(path);
                                Notify(tempMainDNS, "It is ready");
                            }
                        }
                        else
                        {
                            Notify(tempMainDNS, "Error", lines);
                        }
                    }
                }
                return;
            }
            

            BackupOldFiles(path);

            InitConfigurationFiles(path, tempMainDNS, dNSTree.AlternativeDNS);

            GenerateRequest(path);

            SendRequest(path);

        }

        private bool CheckLessThenTwoWeeksStatusFile(string tempstatusfile)
        {
            try
            {
                if (File.Exists(tempstatusfile))
                {
                    string[] lines = File.ReadAllLines(tempstatusfile);

                    if (lines.Length == 2)
                    {
                        if (lines[lines.Length - 1].Contains("Done:"))
                        {
                            string[] arr = lines[lines.Length - 1].Split(':');
                            if (arr.Length == 2)
                            {
                                DateTime origin = DateTime.ParseExact(arr[1], "dd/MM/yyyy", null);

                                if ((DateTime.Now - origin).TotalDays > 350)
                                    return true;
                            }
                        }
                    }

                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }


        public bool CheckLessThenTwoWeeksPemFile(string v)
        {
            try
            {
            //    Console.WriteLine(Path.GetDirectoryName(v));
                if (File.Exists(v))
                {

                    X509Certificate cert = X509Certificate.CreateFromCertFile(v);

                    var cert2 = new X509Certificate2(cert);
                    if (cert2.NotAfter.AddDays(-30) <= DateTime.Now)
                    {
                        Console.WriteLine("Renew: "+Path.GetDirectoryName(v));
                        return true;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        private string GetMainDNS(string path)
        {
            string reducedpath = path.Replace(pathtoroot, "");

            string[] arr = reducedpath.Split(Path.DirectorySeparatorChar);

            StringBuilder result = new StringBuilder();

            //skip first one
            for (int i = arr.Length - 1; i > 0; i--)
            {
                result.Append(arr[i]);
                if (i != 1)
                {
                    result.Append(".");
                }
            }

            return result.ToString();
        }

        private void CreateDifferentFormats(string path)
        {
            Process process = new Process();
            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = "openssl";
            process.StartInfo.Arguments = string.Format("pkcs12 -export  -inkey {0} -out {1} -in {2} -passout env:PRIVATEPASS -passin env:PRIVATEPASS", serverkey, serverpk12, serversigned);
            process.StartInfo.WorkingDirectory = path;
            process.Start();
        }

        private void SendRequest(string path)
        {
            string tempfile = System.IO.Path.Combine(path, serverreq);


            DateTime timeout = DateTime.Now.Add(TimeSpan.FromMinutes(1));

            while (!File.Exists(tempfile))
            {
                if (DateTime.Now > timeout)
                {
                    Console.WriteLine("Application timeout; the request file is not here; try again");
                    Environment.Exit(0);
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }


            Certificate cert = new Certificate
            {
                certificateRequest = File.ReadAllText(tempfile),
                certificateType = "ov",
                certificateValidity = null,
                subjectLanguage = "cs",
                notificationMail = new List<string>()
            };

            foreach (string item in emails)
            {
                cert.notificationMail.Add(item);
            }

            cert.requesterPhone = requesterPhone;

            string json = JsonConvert.SerializeObject(cert);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClient client = new HttpClient(GetSecurityCert());
            try
            {
                HttpResponseMessage httpResponse = client.PostAsync(URLRequest, data).Result;

                if (httpResponse.Content != null)
                {
                    string responseContent = httpResponse.Content.ReadAsStringAsync().Result;

                    RequestAnswer answer = JsonConvert.DeserializeObject<RequestAnswer>(responseContent);
                    string tempfilestatus = System.IO.Path.Combine(path, nameofstatusfile);
                    if (answer.message == null || answer.message == "")
                    {
                        System.IO.File.WriteAllText(tempfilestatus, string.Format("Waiting:{0}", answer.id));
                    }
                    else
                    {
                        System.IO.File.WriteAllText(tempfilestatus, string.Format("Error:{0}, Detail:{1}", answer.message, answer.detail));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }

        private HttpClientHandler GetSecurityCert()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            handler.ClientCertificates.Add(new X509Certificate2(pathtocertificate, certificatePassword));


            return handler;
        }


        private void InitConfigurationFiles(string path, string MainDNS, List<string> AlternativeDNS)
        {
            if (File.Exists(pathtotemplate))
            {
                string[] lines = File.ReadAllLines(pathtotemplate);

                //Replace default setting
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].Replace("<REPLACEME>", MainDNS);
                }

                //add new 
                List<string> itemsList = lines.ToList<string>();

                if (AlternativeDNS != null)
                {
                    for (int i = 0; i < AlternativeDNS.Count; i++)
                    {
                        itemsList.Add(string.Format("DNS.{0}\t= {1}", i + 1, AlternativeDNS[i]));
                    }
                }

                string file = System.IO.Path.Combine(path, nameofcerconffile);
                using (TextWriter tw = new StreamWriter(file))
                {
                    foreach (string s in itemsList)
                    {
                        tw.WriteLine(s);
                    }
                }
            }
        }

        private void BackupOldFiles(string path)
        {
            string[] files = Directory.GetFiles(path);
            if (files.Length != 0)
            {
                string backupfolder = System.IO.Path.Combine(path, "Backup");
                Directory.CreateDirectory(backupfolder);
                string newfolder = System.IO.Path.Combine(backupfolder, DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"));
                DirectoryInfo directoryInfo = Directory.CreateDirectory(newfolder);

                string fileName = "";
                string destFile = "";
                foreach (string item in files)
                {
                    fileName = System.IO.Path.GetFileName(item);
                    destFile = System.IO.Path.Combine(directoryInfo.FullName, fileName);
                    System.IO.File.Move(item, destFile, true);
                }

            }
        }

        private void Notify(string mainDNS, string v, string[] lines = null)
        {
            string text = "Dear Users\nThe certificate " + mainDNS + " change status. Actual status is " + v + "\n";
            if (lines != null)
            {
                text += string.Join("", lines);
            }

            text += "\n Best Regards\n\t\t Administrators";

            Email.SendEmail(emails, null, "support@vsb.cz", "Status of certificate", text);
        }

        public void TestRequest(int v)
        {
            HttpClient client = new HttpClient(GetSecurityCert());
            string responseBody = client.GetStringAsync(URLStatus + v.ToString()).Result;

            StatusAnswer answer = JsonConvert.DeserializeObject<StatusAnswer>(responseBody);

            Console.WriteLine(answer.certificate);
            Console.WriteLine(answer.status);
            Console.WriteLine(answer.message);
            Console.WriteLine(answer.detail);
        }

        private bool GetStatus(string path, int v)
        {
            string tempfilestatus = System.IO.Path.Combine(path, nameofstatusfile);
            string tempfilessigned = System.IO.Path.Combine(path, serversigned);

            HttpClient client = new HttpClient(GetSecurityCert());
            try
            {

                string responseBody = client.GetStringAsync(URLStatus + v.ToString()).Result;

                StatusAnswer answer = JsonConvert.DeserializeObject<StatusAnswer>(responseBody);

                if (answer.certificate != null && answer.certificate != "")
                {
                    System.IO.File.AppendAllText(tempfilestatus, "\nDone:"+DateTime.Now.ToString("dd/MM/yyyy"));
                    System.IO.File.WriteAllText(tempfilessigned, answer.certificate);

                    if (File.Exists(cetrificatechain))
                    {
                        string myString = File.ReadAllText(cetrificatechain);
                        File.AppendAllText(tempfilessigned, myString);
                    }

                    return true;
                }
                else
                {
                    string logfolder = System.IO.Path.Combine(path, "Logs");
                    Directory.CreateDirectory(logfolder);
                    string logfile = System.IO.Path.Combine(logfolder, DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt");
                    System.IO.File.AppendAllText(logfile, string.Format("Status:{0}. Error:{1}. Detail:{2}", answer.status, answer.message, answer.detail));
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return false;
        }

        private void GenerateRequest(string path)
        {
            string tempconfigfile = System.IO.Path.Combine(path, nameofcerconffile);
            if (File.Exists(tempconfigfile))
            {
                Process process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = "openssl";
                process.StartInfo.Arguments = string.Format("req -new -keyout {0} -out {1} -config {2} -passout env:PRIVATEPASS", serverkey, serverreq, nameofcerconffile);
                process.StartInfo.WorkingDirectory = path;
                process.Start();
            }
        }

        public void GenerateDefaultJson(string pathtojsonfile)
        {
            DNSTree root = new DNSTree
            {
                MainDNS = "Root"
            };

            DNSTree cz = new DNSTree
            {
                MainDNS = "cz"
            };

            DNSTree vsb = new DNSTree
            {
                MainDNS = "vsb"
            };

            DNSTree test = new DNSTree
            {
                MainDNS = "test"
            };

            DNSTree test2 = new DNSTree
            {
                MainDNS = "test2",
                AlternativeDNS = new List<string>()
            };
            test2.AlternativeDNS.Add("test3.it4i.cz");


            vsb.Childs = new List<DNSTree>
            {
                test,
                test2
            };

            cz.Childs = new List<DNSTree>
            {
                vsb
            };

            DNSTree tech = new DNSTree
            {
                MainDNS = "tech"
            };

            DNSTree lexis = new DNSTree
            {
                MainDNS = "lexis"
            };

            DNSTree doc = new DNSTree
            {
                MainDNS = "doc"
            };
           
            lexis.Childs = new List<DNSTree>
            {
                doc
            };

            tech.Childs = new List<DNSTree>
            {
                lexis
            };

            root.Childs = new List<DNSTree>
            {
                cz,
                tech
            };

            string jsonString = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(pathtojsonfile, jsonString);
        }

    }
}
