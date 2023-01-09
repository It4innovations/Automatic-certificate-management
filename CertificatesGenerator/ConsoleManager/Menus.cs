using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleManager
{
    internal static class Menus
    {
        public static void ShowMenu(List<string> options, string question)
        {
            Console.WriteLine();
            Console.WriteLine(question);

            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine("{0}. {1}",i+1,options[i]);
            }            
            Console.WriteLine();
        }

        public static int SelectOption(int maxNumber)
        {
            int result = 0;
            while (true)
            {
                Console.Write("Please select option (0- exit):");

                string? input = Console.ReadLine();

                if (input == null)
                    return 0;

                if (int.TryParse(input, out result))
                {
                    if (result <= maxNumber)
                        return result;
                }
            }
        }
    }
}
