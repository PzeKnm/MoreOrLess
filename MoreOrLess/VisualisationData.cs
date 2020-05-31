using GameLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MoreOrLess
{


  [DataContract]
  public class VisualisationData
  {
    [DataMember]
    public byte EnvironmentStatus;

    [DataMember]
    public string GameState;

    [DataMember]
    public int GameStateInt;

    [DataMember]
    public string InternalState;

    [DataMember]
    public int InternalStateInt;

    [DataMember]
    public int AccessCode;
    
    [DataMember]
    public Question currentQuestion;

    [DataMember]
    public int currentQuestionMin;

    [DataMember]
    public int currentQuestionMax;

    [DataMember]
    public int currentAnswerLo;

    [DataMember]
    public int currentAnswerHi;

    [DataMember]
    public int PotentialScore;

    [DataMember]
    public int Score;

    [DataMember]
    public int TotalGameSecs;

    // Could be remaining game time, pregame countdown or authentication countdown
    [DataMember]
    public int RemainingSecs;

    [DataMember]
    public int RemainingQuestionSecs;   


  }

}
