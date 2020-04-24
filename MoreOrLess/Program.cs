using GameLib;
using System;
using System.Threading;

namespace MoreOrLess
{
  class Program
  {

    public static Game CreateGame(GameManager gm)
    {
      return new MoreOrLess(gm);
    }


    static void Main(string[] args)
    {
      try
      {
        System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Console.WriteLine("MoreOrLess starts. 28/7, 15:55");

        // Start worker threads
     //   var cancelSourceVisualisationSignalRServer = new CancellationTokenSource();
     //   VisualisationSignalRServer srv = new VisualisationSignalRServer();
    //    srv.RunServer(cancelSourceVisualisationSignalRServer.Token);

        GameManagerWorker wkrGame = new GameManagerWorker(CreateGame);

        ThreadStart tsGame = new ThreadStart(wkrGame.Run);
        Thread threadGame = new Thread(tsGame);
        threadGame.Start();



        Console.WriteLine("Press Esc to exit.");

        ConsoleKeyInfo k = Console.ReadKey();
        while (k.Key != ConsoleKey.Escape)
        {
          if(!wkrGame.ProcessStandardConsoleCommand(k))
          {
            wkrGame.ProcessConsoleCommand(k);
          }

          k = Console.ReadKey();
        }

        Console.ReadKey();
        Console.WriteLine("Shutting down...");

        // Stop worker threads
     //   cancelSourceVisualisationSignalRServer.Cancel();
        wkrGame.Stop();

        threadGame.Join();

        Console.WriteLine("MoreOrLess end.");
      }
      catch(Exception exc)
      {
        Console.WriteLine("Exception");
        Console.WriteLine(exc.Message);
        Console.WriteLine("Press Esc to exit.");

        ConsoleKeyInfo k = Console.ReadKey();
        while (k.Key != ConsoleKey.Escape)
        {      
          k = Console.ReadKey();
        }
      }

    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Console.WriteLine("Exception");
      Console.WriteLine(e.ExceptionObject.ToString());
      Console.WriteLine("Press Esc to exit.");

      ConsoleKeyInfo k = Console.ReadKey();
      
      while (k.Key != ConsoleKey.Escape)
      {      
        k = Console.ReadKey();
      }
      
    }
  }
}
