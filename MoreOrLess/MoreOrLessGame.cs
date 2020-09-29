using GameLib;
using Newtonsoft.Json;
using RaspberryGPIOManager;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace MoreOrLess
{

  public class MoreOrLess: Game
  {

    string _cVersion = "2.0";

    // Constants
    int _cGameLengthSecs = 60;
    int _cAuthenticationtimeoutSecs = 30;


    int _cShowAnswerSecs = 5;
    int _cShowAutoAcceptAnswerSecs = 20;

    // How often to update viz regardless of detected changes.
    int _cUpdateVisualisationMSecs = 1000;

    public enum InternalState
    {
      Ready = 0,
      FetchQuestion,
      DisplayQuestion,
      DisplayAnswer
    }

    InternalState _internalState;
    
    QuestionSource _questionSource;

    Question _currentQuestion;

    AnswerParameters _currentAnswer;


    Watchdog _dogShowAnswerTimeout;
    Watchdog _dogAcceptAnswerTimeout;
    Watchdog _dogUpdateVisualisation;
    Watchdog _dogAuthenticationCountdownUpdate;

    VisualisationGenerator _visGenerator;

    RaspPi _pi;

    DateTime _lastMotionDetected;

    DateTime _demoStarted;


    public MoreOrLess(GameManager mgr) : base(mgr)
    {
      Console.WriteLine("Program 'MoreOrLess' begins. Version: " + _cVersion);
      _visGenerator = new VisualisationGenerator();

      SetHubDeviceDetails("Station002", "rgvlStnhl3c7vPMeFI9OEK5TnQGL/0WNPBgZji/Hiro=");
      SetGameLength(_cGameLengthSecs);
      SetAuthenticationTimeoutSec(_cAuthenticationtimeoutSecs);
      _HeartbeatSec = 20;
      
      _dogShowAnswerTimeout = new Watchdog(1000 * _cShowAnswerSecs);// 
      _dogShowAnswerTimeout.WatchdogBites += _dogShowAnswerTimeout_WatchdogBites;
      _dogShowAnswerTimeout.Start();

      _dogAcceptAnswerTimeout = new Watchdog(1000 * _cShowAutoAcceptAnswerSecs);// 
      _dogAcceptAnswerTimeout.WatchdogBites += _dogAcceptAnswerTimeout_WatchdogBites;
      _dogAcceptAnswerTimeout.Start();

      _dogUpdateVisualisation = new Watchdog(_cUpdateVisualisationMSecs);// 
      _dogUpdateVisualisation.WatchdogBites += _dogUpdateVisualisation_WatchdogBites;
      _dogUpdateVisualisation.Start();

      _dogAuthenticationCountdownUpdate = new Watchdog(800);// 800ms
      _dogAuthenticationCountdownUpdate.WatchdogBites += _dogAuthenticationCountdown_WatchdogBites;
      _dogAuthenticationCountdownUpdate.Stop();

      _lastMotionDetected = DateTime.Now;

      SetInternalState(InternalState.Ready);
    }



    private void SetInternalState(InternalState newState)
    {
      _internalState = newState;
      UpdateVisualisation();
    }

    private InternalState GetInternalState()
    {
      return _internalState;
    }

         

    public override void ProcessGameCommand(GameCommand gc)
    {
      if(GetInternalState() != InternalState.DisplayQuestion)
        return;

      if (gc.Command == "DecLo")
      {
        _currentAnswer.DecrementLo();
        DisplayOutput();        
      }
        
      if (gc.Command == "IncLo")
      {
        _currentAnswer.IncrementLo();
        DisplayOutput();        
      }   

      if (gc.Command == "DecHi")
      {
        _currentAnswer.DecrementHi();
        DisplayOutput();        
      }

      if (gc.Command == "IncHi")
      {
        _currentAnswer.IncrementHi();
        DisplayOutput();        
      }

      if (gc.Command == "AcceptAnswer")
      {
        AcceptAnswer();
        DisplayOutput();        
      }
      /*
      if (gc.Command == "Left")
        BumpLeft();
      if (gc.Command == "Right")
        BumpRight();
        */
    }


    public override void Initialise()
    {
      if(_questionSource == null)
        _questionSource = new QuestionSource(GetRestApi());
      
      bool bSim = false;
      #if( SIMULATION )
            bSim = true;
      #endif 
      
      _pi = new RaspPi(bSim);

      List<RaspPin> lstPins = new List<RaspPin>();
      lstPins.Add(new RaspPin { name = "MotionSensor", pinNumber = GPIOPinDriver.Pin.GPIO22, direction = GPIOPinDriver.GPIODirection.In});
      lstPins.Add(new RaspPin { name = "LightSensor", pinNumber = GPIOPinDriver.Pin.GPIO10, direction = GPIOPinDriver.GPIODirection.In});
      lstPins.Add(new RaspPin { name = "CorrectAnswer", pinNumber = GPIOPinDriver.Pin.GPIO17, direction = GPIOPinDriver.GPIODirection.Out});
      lstPins.Add(new RaspPin { name = "IncorrectAnswer", pinNumber = GPIOPinDriver.Pin.GPIO27, direction = GPIOPinDriver.GPIODirection.Out});

      _pi.InitialiseFromPinArray(lstPins);

      GetRestApi().ShowPerformance(false);
    }
    
    public override void Deinitialise()
    {
      _pi.Deinitialise();
    }

    private bool IsMotionDetected()
    {
      if(_pi.ReadPin("MotionSensor") == 1)
      {
        _lastMotionDetected = DateTime.Now;
        return true;
      }
      
      return false;
    }

    private bool IsLightDetected()
    {
      return (_pi.ReadPin("LightSensor") == 1);
    }

    public override string GetGameCommands()
    {
      string s = "";
      s += "Q : DecLo" + Environment.NewLine;
      s += "W : IncLo" + Environment.NewLine;
      s += "E : DecHi" + Environment.NewLine;
      s += "R : IncHi" + Environment.NewLine;
      s += "T : AcceptAnswer" + Environment.NewLine;

      return s;
    }

    public override bool ProcessConsoleCommand(ConsoleKeyInfo k)
    {
      if(GetGameMangerState() != GameManagerState.GamePlaying)
        return false;

      if (k.Key == ConsoleKey.Q)
      {
        ProcessGameCommand(new GameCommand("DecLo", ""));        
        return true;
      }

      if (k.Key == ConsoleKey.W)
      {
        ProcessGameCommand(new GameCommand("IncLo", ""));        
        return true;
      }

      if (k.Key == ConsoleKey.E)
      {
        ProcessGameCommand(new GameCommand("DecHi", ""));        
        return true;
      }

      if (k.Key == ConsoleKey.R)
      {
        ProcessGameCommand(new GameCommand("IncHi", ""));       
        return true;
      }

      if (k.Key == ConsoleKey.T)
      {
        AcceptAnswer();
        return true;
      }

      if (k.Key == ConsoleKey.M)
      {
        ReBootPi();
        return true;
      }

      return false;
    }

    // Called when there hasn't been any user activity for a while. Current mode might be Ready, Demo, or Dormant
    public override GameManagerState GetGameManagerStateFromIdle() 
    {
      int nSecsGameInactive = GetTimeWithoutGameActivitySecs();
      bool bMotionDetected = IsMotionDetected();

      if(GetGameMangerState() == GameManagerState.Online_Ready)
      {
        TimeSpan ts = DateTime.Now - _lastMotionDetected;
        if(ts.TotalMinutes > 20.0)
        {
          return GameManagerState.Online_Dormant;
        }

        if(nSecsGameInactive > (60 * 10))
        {
          _demoStarted = DateTime.Now;
          return GameManagerState.Online_Demo;
        }
      }
      
      if (GetGameMangerState() == GameManagerState.Online_Dormant)
      {
        if(bMotionDetected)
        {
          return GameManagerState.Online_Ready;
        }
      }

      if (GetGameMangerState() == GameManagerState.Online_Demo)
      {
        TimeSpan ts = DateTime.Now - _demoStarted;

        if (ts.TotalMinutes > 5.0)
        {
          return GameManagerState.Online_Ready;
        }       
      }

      return GetGameMangerState(); 
    }


    public override bool RebootMachine() 
    {
      ReBootPi();
      return false; 
    }

    private void ReBootPi()
    {
      if (DeactivateGame())
      {
        Console.WriteLine("Game Deactivated");
        _pi.Reboot();
      }
    }


    public override void GameStateHasChanged() 
    { 
      UpdateVisualisation();

      if(GetGameMangerState() == GameManagerState.Authenticating)
        _dogAuthenticationCountdownUpdate.Start();
      else
        _dogAuthenticationCountdownUpdate.Stop();
    }


    public override void ChangeToPreGame()
    {
      base.ChangeToPreGame();
    }

    public override void ChangeToBeginGame() 
    {      
      base.ChangeToBeginGame();

      if(GetInternalState() == InternalState.Ready)
      {
        StartEndOfGameWatchdog();
        FetchQuestion();
      }
    }

    
    public override void ChangeToEndGame() 
    { 
      // overridden so that game doesn't end immediately; the user should have a chance to
      // answer current question.
    }


    private void FetchQuestion()
    {
      SetInternalState(InternalState.FetchQuestion);
      _currentQuestion = _questionSource.GetQuestion();      
      _currentAnswer = new AnswerParameters();
      _currentAnswer.SetOriginalRange(_currentQuestion.Answer, _currentQuestion.RangeLo, _currentQuestion.RangeHi);
      
      Console.WriteLine("");
      Console.WriteLine("");
      Console.WriteLine("");
      Console.WriteLine("------------------------------------------------------------------");
      Console.WriteLine("New Question      -    remaining time(s): " + GetRemainingGameTimeSec());
      Console.WriteLine("------------");
           
      _dogAcceptAnswerTimeout.Reset();
      DisplayOutput();
      SetInternalState(InternalState.DisplayQuestion);
    }

    private void _dogAcceptAnswerTimeout_WatchdogBites(object sender, ElapsedEventArgs e)
    {
      if(GetInternalState() != InternalState.DisplayQuestion)
        return;
      AcceptAnswer();
    }


    private void AcceptAnswer()
    {
      // _internalState = InternalState.DisplayAnswer;      

      Console.WriteLine("The question was:");
      Console.WriteLine(_currentQuestion.QuestionText);
      Console.WriteLine("Your answer was: " + _currentAnswer.GetRangeAsString());      
      Console.WriteLine("The correct answer is:" + _currentQuestion.Answer );
      if(_currentAnswer.IsInRange(_currentQuestion.Answer))
      {
        Console.WriteLine("Yay!");
        PerformPiAnswer(true);
        IncreaseScore(_currentAnswer.GetPotentialPoints());
      }
      else
      {
        Console.WriteLine("Doh!");
        PerformPiAnswer(false);
      }

      
      SetInternalState(InternalState.DisplayAnswer);
      _dogShowAnswerTimeout.Reset();

      Console.WriteLine("         Current Score: {0}", GetScore());  
    }

    private void _dogShowAnswerTimeout_WatchdogBites(object sender, ElapsedEventArgs e)
    {
      if(GetInternalState() != InternalState.DisplayAnswer)
        return;

      if(HasEndOfGameFlagged())
      {
        SetInternalState(InternalState.Ready);
        InformManagerGameFinished();
        _questionSource.Reset();
      }
      else
      {
        Console.WriteLine("Next question.....");
        FetchQuestion();
      }

    }


    private void _dogUpdateVisualisation_WatchdogBites(object sender, ElapsedEventArgs e)
    {
      UpdateVisualisation();
    }

    private void _dogAuthenticationCountdown_WatchdogBites(object sender, ElapsedEventArgs e)
    {
      UpdateVisualisation();
      _dogAuthenticationCountdownUpdate.Reset();
    }

    private void DisplayOutput()
    {
      Console.WriteLine(_currentQuestion.QuestionText);
      _currentAnswer.DisplayOnConsole();

      UpdateVisualisation();
    }

    private void UpdateVisualisation()
    {
      int nRemainingTime = GetRemainingGameTimeSec();
      int nTotalTime = _cGameLengthSecs;

      if(GetGameMangerState() == GameManagerState.PreGame)
      {
        nRemainingTime = GetRemainingPreGameTimeSec();
        nTotalTime = GetTotalPreGameTimeSec();
      }
      
      if(GetGameMangerState() == GameManagerState.Authenticating)
      {
        nRemainingTime = GetRemainingAthenticationSec();
        nTotalTime = GetTotalAthenticationSec();
      }

      if(GetGameMangerState() == GameManagerState.PostGame)
      {
        nRemainingTime = GetRemainingPostGameTimeSec();
        nTotalTime = GetTotalPostGameTimeSec();
      }
      
      int nRemainingQuestionTime = 0;
      if(/*_dogAcceptAnswerTimeout != null*/GetInternalState() == InternalState.DisplayQuestion)
        nRemainingQuestionTime = _dogAcceptAnswerTimeout.GetRemainingTimeSec();

      _visGenerator.UpdateVisualisation(GetEnvironmentStatus(), GetGameMangerState(), GetInternalState(), GetAccessCode(),
        _currentQuestion, _currentAnswer, GetScore(), 
        _cGameLengthSecs, nRemainingTime, nRemainingQuestionTime);

      NotifyClientTimeRemaining();
      _dogUpdateVisualisation.Reset();
    }

    private void PerformPiAnswer(bool bCorrect)
    {      
      int nPulseMs = 1000 * _cShowAnswerSecs;
      if(bCorrect)
        _pi.PulsePin("CorrectAnswer", nPulseMs);
      else
        _pi.PulsePin("IncorrectAnswer", nPulseMs);      
    }

    private byte GetEnvironmentStatus()
    {
      if(_pi == null)
        return 0;
           
      bool bDay = (_pi.ReadPin("LightSensor") == 1);
      bool bMovement = (_pi.ReadPin("MotionSensor") == 1);
      bool bDormant = (GetGameMangerState() == GameManagerState.Online_Dormant);
  
      byte btStatus = 0;
      btStatus += GetDemoStatus(); 

      if(bDay)
        btStatus += 0b00001000;

      if(bMovement)
        btStatus += 0b00010000;

      if(bDormant)
        btStatus += 0b10000000;

      return btStatus;
    }

    private byte GetDemoStatus()
    {
      byte btStatus = 0;
      if(GetGameMangerState() == GameManagerState.Online_Demo)
        btStatus += 0b00000001;

      // todo mkmkkm demo status could have 4 alternatives
      // btStatus += 0b00000001;
      // btStatus += 0b00000011;
      // btStatus += 0b00000101;
      // btStatus += 0b00000111;

      return btStatus;
    }


  }





}
