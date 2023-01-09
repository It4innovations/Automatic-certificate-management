using ConsoleManager.DataStructure;
using CoreCG.DataStructures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Security;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using ConsoleManager.Properties;

namespace ConsoleManager
{
    internal class Manager
    {
        private List<string> mainMenu;
        private List<string> JsonMenu;
        private List<string> NodeMenu;
        private string pathToJson = "list.json";
        private readonly DNSTree? tree;
        private string pathtoROOTfolder="Output";
        private string decodeFolder = "Decode";
        private string SSLFolder = "SSLFolder";
        private string serverpem = "serversigned.pem";
        private string serverkey = "serverkey.pem";
        private string serverpfx = "server.pfx";
        private DNSTree? Selected = null;

        private int DayToPast = -27;

        public Manager()
        {
            mainMenu = new List<string>();
            mainMenu.Add("Modify JSON file");
            mainMenu.Add("Save");
            mainMenu.Add("Upload json to git");
            mainMenu.Add("Decode");

            JsonMenu = new List<string>();
            JsonMenu.Add("Go one back");
            JsonMenu.Add("Select current object");

            NodeMenu = new List<string>();
            NodeMenu.Add("Add a child");
            NodeMenu.Add("Modify the node");
            NodeMenu.Add("Delete the node");
            NodeMenu.Add("Turn off autorenew");

            pathToJson = FindPathToJson(pathToJson);

            if (File.Exists(pathToJson) )
            {
                string jsonString = File.ReadAllText(pathToJson);

                tree = JsonConvert.DeserializeObject<DNSTree>(jsonString);

                Console.WriteLine("Status JSON file: OK");
            }
            else
            {
                Console.WriteLine("Status JSON file: Problem");
            }

            pathtoROOTfolder = FindPathToROOT(pathtoROOTfolder);

            if(Directory.Exists(decodeFolder))
            {
                Directory.Delete(decodeFolder, true);
            }
        }

        private X509Certificate2 IsHTTPSCertificateFine(string url)
        {
            string finalurl = "";
            if (url.StartsWith("https://"))
                finalurl = url;
            else
                finalurl="https://"+url;

            DateTime expiry = DateTime.UtcNow;
            X509Certificate2 result = null;
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, cert, chain, policyErrors) =>
                {
                    expiry = cert.NotAfter;
                    result = cert;
                    return true;
                }
            };

            try
            {
                var client = new HttpClient(httpClientHandler);

                var webRequest = new HttpRequestMessage(HttpMethod.Head, finalurl)
                {
                    Content = new StringContent("{ 'some': 'value' }", Encoding.UTF8, "application/json")
                };

                var response = client.Send(webRequest);

                using var reader = new StreamReader(response.Content.ReadAsStream());


            }catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("problem with url:"+ finalurl);
                Console.ForegroundColor = ConsoleColor.White;
            }
            
            return result;
        }

        internal static byte[] ReadFile(string fileName)
        {
            FileStream f = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            int size = (int)f.Length;
            byte[] data = new byte[size];
            size = f.Read(data, 0, size);
            f.Close();
            return data;
        }

        private X509Certificate2 GetCert(string file, string key, string pass)
        {
            try
            {
                X509Certificate2 x509 = new X509Certificate2();
                //Create X509Certificate2 object from .cer file.          
                x509 = X509Certificate2.CreateFromEncryptedPem(Encoding.UTF8.GetChars(File.ReadAllBytes(file)).AsSpan(),
                    Encoding.UTF8.GetChars(File.ReadAllBytes(key)).AsSpan(),
                    pass
                    );

                return x509;
            }catch(Exception ex)
            {
                Console.WriteLine("Problem with password");
                return null;
            }

            //AsymmetricKeyParameter asymmetricKeyParameter= ReadAsymmetricKeyParameter(file);

            //asymmetricKeyParameter

        }

        private string FindPathToROOT(string lookingfor)
        {
            string fullpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            while (true)
            {
                string[] arr = Directory.GetDirectories(fullpath, lookingfor);
                if (arr.Length > 0)
                {
                    return arr[0];
                }
                fullpath = GoOneUP(fullpath);
                if (fullpath.ToLower() == "c:\\")
                    break;
            }
            return "";
        }

        private string FindPathToJson(string lookingfor)
        {
            string fullpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            while (true)
            {                
                string[] arr = Directory.GetFiles(fullpath, lookingfor);
                if(arr.Length > 0)
                {
                    return arr[0];
                }
                fullpath= GoOneUP(fullpath);
                if (fullpath == "c:\\") 
                    break;
            }
            return "";
        }
        private string GoOneUP(string path)
        {
             return Path.GetFullPath(Path.Combine(path, @"..\"));
        }

        private void Save()
        {
            
            if (File.Exists(pathToJson+".txt")==false)
            {
                string test= JsonConvert.SerializeObject(tree ?? new DNSTree());

                File.WriteAllText(pathToJson    ,test);
            }
        }

        private void ModifyJson()
        {
            SelectDNSNode(tree?? new DNSTree(),"");

            if(Selected!=null)
            {
                Menus.ShowMenu(NodeMenu, "Choose operation for node:"+ GetFullPathToNode(Selected));

                int result = Menus.SelectOption(NodeMenu.Count);

                switch (result)
                {
                    case 1:
                        DNSTree dNSTree = CreateNode(GetFullPathToNode(Selected));
                        if (dNSTree != null)
                        {
                            Selected.Childs.Add(dNSTree);
                        }                        
                        break;
                    case 2:
                        ChangeNode(Selected);
                        break;
                    case 3:
                        DeleteDNSNode(tree ?? new DNSTree(), Selected);
                        break;
                    case 4:
                        TurnOffAutoRenew(Selected);
                        break;
                    case 0:
                        return;
                }
            }
        }
        private void TurnOffAutoRenew(DNSTree dNSTree)
        {
            dNSTree.AutomaticRenew = false;
        }

        private void ChangeNode(DNSTree dNSTree)
        {
            Console.WriteLine("Current DNS name:"+ dNSTree.MainDNS);
            if (QuestionYN("Do you want change it?"))
            {
                dNSTree.MainDNS = Console.ReadLine();
            }

            Console.WriteLine("Current Automatic renew:" + dNSTree.AutomaticRenew);
            if (QuestionYN("Do you want change it?"))
            {
                dNSTree.AutomaticRenew = QuestionYN("Automatic renew");
            }


            if(dNSTree.AlternativeDNS.Count==0)
            {
                Console.WriteLine("Current Alternative DNS:");
                foreach (string item in dNSTree.AlternativeDNS)
                {
                    Console.WriteLine(item);
                }
            }

            if (QuestionYN("Do you want an altenative DNS"))
            {

                dNSTree.AlternativeDNS = MultipleInpus();
            }

            if (ValidateDNSofNode(dNSTree) == false)
            {
                Console.WriteLine("Problem with validation");
            }
        }

        private bool QuestionYN(string text)
        {
            bool result = false;

            string? temp = "";
            while (true)
            {
                Console.WriteLine(text+" (y/n):");
                temp = Console.ReadLine();
                if (temp != null)
                {
                    temp = temp.ToLower();
                    if (temp == "y")
                    {
                        result = true;
                        break;
                    }else if (temp == "n")
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        private List<string> MultipleInpus()
        {
            List<string> result = new List<string>();
            Console.WriteLine("Cancel is empty line");

            for(int i=1; i<1000;i++)
            {
                Console.Write("Input number " + i + " :");
                string? text = Console.ReadLine();
                if (string.IsNullOrEmpty(text))
                    break;
                result.Add(text);
            }
            return result;

        }

        public bool CheckDNSRecord (string dns)
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(dns);
                return true;
            }catch(Exception e)
            {
                return false;
            }
          

        }

        private bool ValidateDNSofNEWNode(DNSTree node, string basicPath)
        {
            bool final = true;
            string path = node.MainDNS+"."+basicPath;
            Console.WriteLine("Start node validation :" + path);

            bool temp = CheckDNSRecord(path);
            Console.WriteLine("Main DNS status:" + temp);
            final = temp;

            if (node.AlternativeDNS != null && node.AlternativeDNS.Count != 0)
            {
                foreach (string item in node.AlternativeDNS)
                {
                    temp = CheckDNSRecord(item);
                    Console.WriteLine(String.Format("Alternative DNS ({0}) has status: {1}", item, temp));
                    if (final)
                        final = temp;
                }
            }
            Console.WriteLine("Finish node validation:" + final);
            return final;
        }

        private bool ValidateDNSofNode(DNSTree node)
        {
            bool final = true;
            string path = GetFullPathToNode(node);
            Console.WriteLine("Start node validation :"+ path);

            bool temp = CheckDNSRecord(path);
            Console.WriteLine("Main DNS status:"+temp);
            final = temp;

            if (node.AlternativeDNS != null && node.AlternativeDNS.Count != 0)
            {
                foreach (string item in node.AlternativeDNS)
                {
                    temp= CheckDNSRecord(item);
                    Console.WriteLine(String.Format("Alternative DNS ({0}) has status: {1}",item,temp));
                    if (final)
                        final = temp;
                }
            }
            Console.WriteLine("Finish node validation:"+final);

            return final;
        }


        private DNSTree CreateNode(string pathForCheck)
        {
            DNSTree dNSTree = new DNSTree();

            Console.WriteLine("DNS name:");
            dNSTree.MainDNS= Console.ReadLine();

            dNSTree.AutomaticRenew = QuestionYN("Automatic renew");

            if (QuestionYN("Do you want an altenative DNS"))
            {

                dNSTree.AlternativeDNS = MultipleInpus();
            }

            while (true)
            {
                Console.WriteLine("OS (linux or windows):");
                dNSTree.OS = Console.ReadLine();

                if (dNSTree.OS == "linux" || dNSTree.OS == "windows")
                    break;
            }

            if (QuestionYN("HTTPS is standard, do you want other services? Specify ports"))
            {
                while (true)
                {
                    List<string> inputs = MultipleInpus();
                    dNSTree.SSLPorts = new List<string>();

                    bool done = true;
                    foreach (var item in inputs)
                    {
                        if (ValidetSslRecord(item))
                        {
                            dNSTree.SSLPorts.Add(item);
                        }
                        else
                        {
                            Console.WriteLine("Wrong input '" + item + "'. You have to start again.");
                            done = false;
                            break;
                        }

                    }
                    if (done)
                        break;
                }
            }


            if (ValidateDNSofNEWNode(dNSTree, pathForCheck) ==false)
            {
                if (QuestionYN("There are some errors. Do you still want save it?") == false)
                    return null;
            }
            return dNSTree;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="current">false =end; true= previous</param>
        /// <returns></returns>
        public bool SelectDNSNode( DNSTree current, string actualpath, DNSTree lookingForDelete=null)
        {                        
 again:           
                List<string> list = GetChildrens(current);
                list.AddRange(JsonMenu);

                Menus.ShowMenu(list, "Select node ("+current.MainDNS+"."+actualpath+"):");

                int result = Menus.SelectOption(list.Count);

            if (result == 0)
                return false;
            else
                if (list.Count == result)
            {
                Selected = current;
                return false;
            }
            else
                if (list.Count - 1 == result)
                return true;
            else
            {
                if (current.Childs != null)
                {
                    bool temp = SelectDNSNode(current.Childs[result - 1], current.MainDNS + "." + actualpath);
                    if (temp == false)                          
                        return false;

                    goto again;
                }
                return false;
            }          
        }

        public void DeleteDNSNode(DNSTree current, DNSTree lookingForDelete)
        {
            if (current.Childs != null)
            {
                for (int i = 0; i < current.Childs.Count; i++)
                {
                    if (current.Childs[i]==lookingForDelete)
                    {
                        current.Childs.RemoveAt(i);
                        return;
                    }else
                    {
                        DeleteDNSNode(current.Childs[i], lookingForDelete);
                    }
                }
            }          
        }

        public string GetFullPathToNode (DNSTree node)
        {
            string result = "";

            if(tree!=null)
            {
                result = DNSWalk(tree, node);

                result = result.Replace(".Root", "");
            
            }
            return result;
        }

        public DNSTree GetNodeBasedOnDNS(DNSTree dNSTree, string[] dnslookingfor, int currentprogress)
        {
            if (dNSTree.Childs != null)
            {
                foreach (DNSTree child in dNSTree.Childs)
                {
                    if (child.MainDNS.ToLower() == dnslookingfor[currentprogress].ToLower())
                    {
                        currentprogress++;
                        if (currentprogress == dnslookingfor.Length)
                        {
                            return child;
                        }
                        else
                        {
                            return GetNodeBasedOnDNS(child, dnslookingfor, currentprogress );
                        }
                    }
                }
            }
            return null;
        }

        private string DNSWalk(DNSTree current, DNSTree looking)
        {
            if(current!= looking)
            {
                if (current.Childs != null)
                {
                    foreach (DNSTree child in current.Childs)
                    {
                      string temp=   DNSWalk(child, looking);
                        if(temp != "")
                        {
                            return temp+"."+ current.MainDNS;
                        }
                    }
                }
              return "";

            }else
            {
                return current.MainDNS;
            }
        }

        private List<string> GetChildrens(DNSTree tree)
        {
            List<string> childrens = new List<string>();

            if (tree.Childs != null)
            {
                foreach (DNSTree item in tree.Childs)
                {
                    childrens.Add(item.MainDNS);
                }
            }
            return childrens;
        }



        public void Start()
        {
            if (tree == null)
                return;

            while (true)
            {
                Menus.ShowMenu(mainMenu, "Choose operation");

                int result = Menus.SelectOption(mainMenu.Count);
                switch (result)
                {
                    case 1:
                        ModifyJson();
                        break;
                    case 2:
                        Save();
                        break;
                    case 3:
                        UploadToGIT();
                        break;
                    case 4:
                        Decode();
                        break;
                    case 0:
                        DeleteFolder(true);
                        return;
                }
            }
        }

        private SecureString SetPassword()
        {
            SecureString securePwd = new SecureString();
            ConsoleKeyInfo key;

            Console.Write("Enter password: ");
            do
            {
                key = Console.ReadKey(true);

                // Ignore any key out of range.
                if (((int)key.Key) >= 32 && ((int)key.Key <= 200))
                {
                    // Append the character to the password.
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                // Exit if Enter key is pressed.
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return securePwd;

        }

        private void Decode()
        {
            Console.WriteLine("Set password for private key:");
            SecureString secureString = SetPassword();

            Directory.CreateDirectory(decodeFolder);

            string[] files = Directory.GetFiles(pathtoROOTfolder, serverpem, SearchOption.AllDirectories);

            Console.WriteLine("How many days:");
            string tmp = Console.ReadLine();

            int days = 0;
            if (int.TryParse(tmp, out days) == false)
                days = DayToPast;


            DateTime today= DateTime.Now.AddDays(-days);

            foreach (string file in files)
            {
                string directory = Path.GetDirectoryName(file);
                string pathtokey = Path.Combine(directory, serverkey);
                X509Certificate2 temp = GetCert(file, pathtokey, new System.Net.NetworkCredential(string.Empty, secureString).Password);
                if (temp == null)
                    return;

                if (today< temp.NotBefore)
                {
                    string[] arr= temp.Subject.Split(',');
                    string fin = arr[0].Replace("CN=", "").Trim();

                    Console.WriteLine("Status of certificate:" + fin);
                    Dictionary<int, int> result = validateCertificateAsService(fin);
                    bool cancel = true;
                    

                    foreach (var item in result)
                    {
                        string finalText = "";
                        switch(item.Value)
                        {
                            case 0:
                                Console.ForegroundColor = ConsoleColor.Green;
                                finalText = "Fine";
                                Console.WriteLine("\t\t Port:" + item.Key + ":" + finalText);
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                            case 1:
                                Console.ForegroundColor = ConsoleColor.Red;
                                finalText = "Soon expire";
                                Console.WriteLine("\t\t Port:" + item.Key + ":" + finalText);
                                Console.ForegroundColor = ConsoleColor.White;
                                cancel = false;
                                break;
                            case 2:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                finalText = "I dont know :)";
                                Console.WriteLine("\t\t Port:" + item.Key + ":" + finalText);
                                Console.ForegroundColor = ConsoleColor.White;
                                cancel = false;
                                break;
                        }

                      
                    }

                    if (cancel)
                        continue;

                    string tempPath = Path.Combine(decodeFolder, fin);
                    Directory.CreateDirectory(tempPath);

                    File.Copy(file, Path.Combine(tempPath,serverpem));
                    //          File.Copy(pathtokey, Path.Combine(tempPath, serverkey));

                    ExportX509(temp, tempPath);
                }
            }
            
            Thread thread1 = new Thread(DeleteFolder);
            thread1.Start(false);

        }

        private void DeleteFolder(object now)
        {
            if((bool)now==false)
                Thread.Sleep(600000);
            if (Directory.Exists(decodeFolder))
                Directory.Delete(decodeFolder, true);
        }

        private int IsCertificateFine(string host,string type, int port)
        {
            X509Certificate2 result = null;

            switch(type)
            {
                case "ssl": result = GetCertificateStandardSSL(host,port);
                    break;

                case "mssql":result = GetCertificateSQL(host, port);
                    break;

                default:
                    result = GetCertificateStandardSSL(host, port);
                    break;

            }

            if (result == null)
                return 2;

            DateTime time = result.NotAfter;

            if (DateTime.Now.AddDays(-DayToPast) > time)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public X509Certificate2 GetCertificateStandardSSL(string host, int port)
        {            
            X509Certificate2 cert = null;

            if (CheckDNSRecord(host) == false)
                return cert;
           
            var certValidation = new RemoteCertificateValidationCallback(delegate (object snd, X509Certificate certificate, X509Chain chainLocal, SslPolicyErrors sslPolicyErrors)
            {
                //Accept every certificate, even if it's invalid
                return true;
            });

            try
            {
                var client = new TcpClient(host, port);
                // Create an SSL stream and takeover client's stream
                using (var sslStream = new SslStream(client.GetStream(), true, certValidation))
                {
                    sslStream.AuthenticateAsClient(host);

                    var serverCertificate = sslStream.RemoteCertificate;
                    cert = new X509Certificate2(serverCertificate);

                    //Convert Raw Data to Base64String
                    var certBytes = cert.Export(X509ContentType.Cert);

                    var certAsString = Convert.ToBase64String(certBytes, Base64FormattingOptions.None);                    
                }
            }catch
            {
              
            }

            return cert;
        }
        private void run_cmd(string cmd, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "python3";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError= false;
            start.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), SSLFolder);
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                  //  Console.Write(result);
                }
            }
        }

        public X509Certificate2 GetCertificateSQL(string host, int port)
        {
            X509Certificate2 result = null;

            Directory.CreateDirectory(SSLFolder);

            File.WriteAllBytes(SSLFolder+"/sslCert.py", Resources.get_tds_cert);
            run_cmd("sslCert.py", host + " " + port);

            if(File.Exists(SSLFolder+"/server.crt"))
            {
                result = new X509Certificate2(X509Certificate2.CreateFromCertFile(SSLFolder + "/server.crt"));
            }

            Directory.Delete(SSLFolder,true);

            return result;
        }

        private bool ValidetSslRecord(string text)
        {
            bool status = true;
            string[] arr2 = text.Split(',');
            if (arr2.Length != 2)
            {
                status = false;
                Console.WriteLine("Problem with parse ssl");
                return status;
            }

            switch(arr2[0])
            {
                case "ssl": status = true;
                    break;
                case "mssql":
                    status = true;
                    break;
                default: status = false;
                    Console.WriteLine("Unknow type of ssl");
                    break;
            }

            int port = 0;
            if (int.TryParse(arr2[1], out port))
            {
                if(port<0|| port>65535)
                {
                    status = false;
                    Console.WriteLine("Problem with port: Value has to be in range 0-65535");
                }    
            }else
            {
                Console.WriteLine("Problem with SSL port");
                status = false;
            }
            return status;
        }

        /// <summary>
        /// validate Certificate As Service
        /// </summary>
        /// <param name="DNS">0-ok,1-expire,2-unknow</param>
        /// <returns></returns>
        private Dictionary<int,int> validateCertificateAsService(string DNS)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            if (File.Exists(pathToJson))
            {
                string[] arr = DNS.Split('.');
                Array.Reverse(arr);
                DNSTree node = GetNodeBasedOnDNS(tree, arr, 0);

                if(node != null)
                {
                    if(node.SSLPorts!=null)
                    {
                        foreach (string item in node.SSLPorts)
                        {
                            string[] arr2 = item.Split(',');
                            if(arr2.Length!=2)
                            {
                                Console.WriteLine("Problem with parse ssl");
                                continue;
                            }
                            int port = 0;

                            if (int.TryParse(arr2[1], out port))
                            {
                                result.Add(port, IsCertificateFine(DNS, arr2[0], port));
                            }
                            else
                            {
                                Console.WriteLine("Problem with parse ssl port");
                            }
                        }
                    }else
                    {
                        result.Add(443, IsCertificateFine(DNS,"ssl", 443));
                    }
                }               
            }
            return result;
        }

        private void UploadToGIT()
        {
            if(File.Exists(pathToJson))
            {
                Process.Start("git", "commit -m 'update json file' " + pathToJson);
            }
        }

        private void ExportX509(X509Certificate2 x509Certificate2, string directory)
        {
            byte[] certificateBytes = x509Certificate2.RawData;
            char[] certificatePem = PemEncoding.Write("CERTIFICATE", certificateBytes);

            AsymmetricAlgorithm key = x509Certificate2.GetRSAPrivateKey();
            byte[] pubKeyBytes = key.ExportSubjectPublicKeyInfo();
            byte[] privKeyBytes = key.ExportPkcs8PrivateKey();
            char[] pubKeyPem = PemEncoding.Write("PUBLIC KEY", pubKeyBytes);
            char[] privKeyPem = PemEncoding.Write("PRIVATE KEY", privKeyBytes);
            byte[] certData = x509Certificate2.Export(X509ContentType.Pfx);
            
            File.WriteAllBytes(Path.Combine(directory, serverpfx), certData);           
            File.WriteAllText(Path.Combine(directory, serverkey), new string(privKeyPem));
        }
    }
}
