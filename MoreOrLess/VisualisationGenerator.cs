using GameLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MoreOrLess
{
  public class VisualisationGenerator
  {
    private string _path;

    private string _cVisData = "VisData.json";
    private string _cReadyToReceive = "ReadyToReceive.dat";
    private string _cFinishedSending = "FinishedSending.dat";
    private string _cTestFile = "TestFile.dat";

    // It can be that there are 2 updates in quick succession. Since the first one may not have been processed, the
    // second one would be ignored because the client isn't ready. Therefore, the second (or last) will get stored as a pending
    // update. When a pending update is active, a timer will keep checking to see when the client is ready, then 
    // it will apply the pending change.
    Watchdog _dogPendingUpdate;
    VisualisationData _vdPending;

    VisualisationSignalRServer srvSignalRViz;
    CancellationTokenSource cancelSourceVisualisationSignalRServer;

    VisualisationI2CServer srvI2CServer;

    bool m_bUseFileSystem = false;

    public VisualisationGenerator()
    {
      #if SIMULATION
        _path = @"D:\Temp\ram\tmp\"; 
      #else
        _path = @"/ram/tmp/";
      #endif

      if(m_bUseFileSystem)
      {
        WriteTestFile();
        _dogPendingUpdate = new Watchdog(100);// 
        _dogPendingUpdate.WatchdogBites += _dogPendingUpdate_WatchdogBites; 
      }
            
      srvSignalRViz = new VisualisationSignalRServer();
      cancelSourceVisualisationSignalRServer = new CancellationTokenSource();
      srvSignalRViz.RunServer(cancelSourceVisualisationSignalRServer.Token);

      srvI2CServer = new VisualisationI2CServer();
    }


    public void StopVizServer()
    {
      cancelSourceVisualisationSignalRServer.Cancel();
    }


    private void WriteTestFile()
    {
      try
      {
        Console.WriteLine("output path is: " + _path);
        string sTestFile = _path + _cTestFile;
        if(File.Exists(sTestFile))
          File.Delete(sTestFile);
        File.WriteAllText(sTestFile, "Test file");  
      }
      catch(Exception exc)
      {
        Console.WriteLine(exc.Message);
      }

    }



    public void UpdateVisualisation(GameManagerState GameState, 
      MoreOrLess.InternalState internalState, int accessCode, Question _currentQuestion, 
      AnswerParameters _currentAnswer, int nScore, 
      int nTotalGameSecs, int nRemainingSecs, int nRemainingQuestionSecs)
    {
      VisualisationData vd = new VisualisationData();
      vd.GameState = GameState.ToString();
      vd.GameStateInt = (int)GameState;
      vd.InternalState = internalState.ToString();
      vd.InternalStateInt = (int)internalState;
      vd.AccessCode = accessCode;

      if(_currentQuestion != null)
        vd.currentQuestion = _currentQuestion;

      if(_currentAnswer != null)
      {
        vd.currentQuestionMin = _currentAnswer.GetMin();
        vd.currentQuestionMax = _currentAnswer.GetMax();
        vd.currentAnswerLo = _currentAnswer.GetLo();
        vd.currentAnswerHi = _currentAnswer.GetHi();
        vd.PotentialScore = _currentAnswer.GetPotentialPoints();
      }

      vd.Score = nScore;
      vd.TotalGameSecs = nTotalGameSecs;
      vd.RemainingSecs = nRemainingSecs;
      vd.RemainingQuestionSecs = nRemainingQuestionSecs;

      WriteToVizServer(vd);
      
      if(m_bUseFileSystem)
      {
        if(!IsClientReceiving() || IsVisFileAlreadyPresent())
        {
          _vdPending = vd;
          _dogPendingUpdate.Reset();
          return;
        }

        WriteToVizFile(vd);
      }


    }

    private void WriteToVizFile(VisualisationData vd)
    {
      string json = JsonConvert.SerializeObject(vd, Formatting.Indented);
      System.IO.File.WriteAllText(_path + _cVisData, json);  
      System.IO.File.WriteAllText(_path + _cFinishedSending, json);  
    }


    private void WriteToVizServer(VisualisationData vd)
    {
      srvSignalRViz.PublishVisualisationData(vd);
      srvI2CServer.PublishVisualisationData(vd);
    }


    public bool IsClientReceiving()
    {
      string sFullPath = _path + _cReadyToReceive;
      return System.IO.File.Exists(sFullPath);      
    }

    public bool IsVisFileAlreadyPresent()
    {
      string sFullPath = _path + _cVisData;
      return System.IO.File.Exists(sFullPath);      
    }

    private void _dogPendingUpdate_WatchdogBites(object sender, System.Timers.ElapsedEventArgs e)
    {
      _dogPendingUpdate.Stop();

      if(!IsClientReceiving() || IsVisFileAlreadyPresent())
      {
         _dogPendingUpdate.Reset();
        return;
      }

      WriteToVizFile(_vdPending);
    }

  }
}
