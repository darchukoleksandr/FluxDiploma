namespace Host.Console
{
    using Host.Base;

    class Program
    {
        public static void Main()
        {
            var renameSomehow = new RequestManager();
            
            System.Console.WriteLine("Listening for TCP messages on 127.0.0.1:42512");
            System.Console.ReadLine();
            System.Console.WriteLine("Good bye World!");

            renameSomehow.StopListening();
        }
    }
}
