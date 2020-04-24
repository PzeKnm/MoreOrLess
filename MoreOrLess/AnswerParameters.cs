using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MoreOrLess
{
  public class AnswerParameters
  {
    // The original range from the question
    private int OriginalAnswer;
    private int OriginalMin;
    private int OriginalMax;

    // The maximum allowable range, by aplying a random factor to the original inputs
    private int Min;
    private int Max;
    private int Step;

    // The number of steps, represents granularity of range
    private int cStepCount = 20;

    // Currently selected answer
    private int Begin;
    private int End;
       

    public AnswerParameters()
    {
      Min = 0;
      Max = 100;
      Step = 5;
      Begin = 20;
      End = 80;
    }

    public void SetOriginalRange(int dAnswer, int min, int max)
    {
      OriginalAnswer = dAnswer;
      OriginalMin = min;
      OriginalMax = max;

      Random rand = new Random();

      // Calculate the allowed range for this turn
      int nLeftDiff = OriginalAnswer - OriginalMin;
      int nRightDiff = OriginalMax - OriginalAnswer;

      Min = rand.Next(OriginalMin - nLeftDiff, OriginalMin + nLeftDiff);     
      Max = rand.Next(OriginalMax - nRightDiff, OriginalMax + nRightDiff); 

      Step = (Max - Min) / cStepCount;
      if(Step == 0)
      {
        Max += 10;
        Min -= 10;
        Step = (Max - Min) / cStepCount;
      }

      Begin = Min;
      End = Max;

      string s = String.Format("Calculated range: {0} - {1}, Step: {2}", Min, Max, Step);
      Console.WriteLine(s);
    }

    public void IncrementLo()    {      Move(true, true);    }

    public void IncrementHi()    {      Move(false, true);    }

    public void DecrementLo()    {      Move(true, false);    }

    public void DecrementHi()    {      Move(false, false);    }

    void Move(bool bLeft, bool bIncrease)
    {
      if(bLeft)
      {
        if(bIncrease && Begin >= (Max - Step))
          return;

        if(!bIncrease && Begin <= (Min))
          return;       

        if(bIncrease)
          Begin += Step;
        else
          Begin -= Step;

        if(Begin >= End)
          End += Step;
      }
      else
      {
        if(!bIncrease && End <= (Min + Step))
          return;

        if(bIncrease && End >= (Max))
          return;       

        if(bIncrease)
          End += Step;
        else
          End -= Step;

        if(End <= Begin)
          Begin -= Step;
      }
      Debug.Assert(Begin < End);
    }

    public bool IsInRange(int n)
    {
      return (Begin <= n && End >= n);
    }

    public string GetRangeAsString()
    {
      return string.Format("{0} - {1}", Begin, End);
    }
    
    public int GetMin()
    {
      return Min;
    }

    public int GetMax()
    {
      return Max;
    }
    
    public int GetLo()
    {
      return Begin;
    }

    public int GetHi()
    {
      return End;
    }

    public void DisplayOnConsole()
    {
      int nWidth = 50;
      string sLine1 = string.Format("[{0}]", Min) + 
                      new String('-', nWidth) +
                      string.Format("[{0}]", Max);

      int nFirstDash = sLine1.IndexOf('-');
      int nLastDash = sLine1.LastIndexOf('-');

      int nBegin = nFirstDash + map(Begin, Min, Max, 1, nWidth) - 1;
      int nEnd = nFirstDash + map(End, Min, Max, 1, nWidth) - 1;
      string sLine2 = new String(' ', nBegin) +
                      "I" + 
                      new String('=', nEnd - nBegin) +
                      "I";

      string sLine3 = string.Format("({0} - {1}, Potential Point: {2})", Begin, End, GetPotentialPoints());

      Console.WriteLine(sLine1);
      Console.WriteLine(sLine2);
      Console.WriteLine(sLine3);
 //     string s = String.Format("Answer: {0} - {1}, ({2})", Begin, End, GetPotentialPoints());
 //     Console.WriteLine(s);
    }

    private int map(int x, int in_min, int in_max, int out_min, int out_max)
    {
      return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    public int GetPotentialPoints()
    {
      // Just for now let's take the chosen range over total range
      int nPcChosen = ((End - Begin) * 100) / (Max - Min);

      // 1 - 100% => 0 - 10 points
      int nPoints = 10 - (nPcChosen / 10); 
      if(nPoints < 0)
        nPoints = 0;
      return nPoints;
    }


  }
}
