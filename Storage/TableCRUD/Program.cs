using System;

namespace TableCRUD
{
    class Program
    {
        static void Main(string[] args)
        {
            BasicSamples basicSamples = new BasicSamples();
            basicSamples.RunSamples().Wait();

            Console.WriteLine("Press any key to exit");
            Console.Read();
        }
    }
}
