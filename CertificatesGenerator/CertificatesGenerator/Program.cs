using CertificatesGenerator.DataStructures;
using System;

namespace CertificatesGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            //0 - path to json file
            //1 - path to root of certificates
            //2 - path to template for openssl
            //3 - path to robot pem
            //4 - path to robot key
            //5 - path to chain

            Console.WriteLine("Start");
            if (args.Length == 6)
            {
                CertificatesWord certificatesWord = new CertificatesWord(args[0], args[1], args[2], args[3], args[4], args[5]);
                certificatesWord.Run();
            }
            //Test only
            //0 - path to robot pem
            //1 - path to robot key
            //2 - question

            if (args.Length == 3)
            {
                CertificatesWord certificatesWord = new CertificatesWord(args[0], args[1]);
                certificatesWord.TestRequest(Convert.ToInt32(args[2]));
            }



            if (args.Length == 1)
            {
                CertificatesWord certificatesWord = new CertificatesWord();
                certificatesWord.GenerateDefaultJson(args[0]);
            }


            Console.WriteLine("End");
        }
    }
}
