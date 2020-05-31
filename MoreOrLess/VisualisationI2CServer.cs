using System;
using System.Collections.Generic;
using System.Text;
using System.Device.I2c;

namespace MoreOrLess
{

  public struct SVizData {
    public int EnvironmentStatus;
    public int GameState;
    public int InternalState;
    public int PotentialScore;
    public int Score;
    public int TotalGameSecs;
    public int RemainingSecs;
    public int RemainingQuestionSecs;
  };

  public class I2CSlaveData
  {
    public I2CSlaveData(byte addr, string name)
    {
      m_Address = addr;
      m_Name = name;
      m_bOnline = false;
    }

    public byte m_Address;
    public string m_Name;
    public bool m_bOnline;
  }

  class VisualisationI2CServer
  {
    private int m_cBroadcastAddr = 0x0;


    private List<I2CSlaveData> m_PotentialSlaves;

    public VisualisationI2CServer()
    {
      m_PotentialSlaves = new List<I2CSlaveData>();

      // Even though we're going to broadcast the data to anyone who's listening, it would still
      // be nice to be able to test the bus for expected slaves.

      // todo, maybe read these from a config file...
      m_PotentialSlaves.Add(new I2CSlaveData(0x2b, "Potential Score Marker"));
      m_PotentialSlaves.Add(new I2CSlaveData(0x2d, "RobotPig"));
      m_PotentialSlaves.Add(new I2CSlaveData(0x2e, "Engaged Sign"));

      CheckBus();
    }
         
    public void PublishVisualisationData(VisualisationData vd)
    {      
      SVizData data;
      data.EnvironmentStatus = vd.EnvironmentStatus;
      data.GameState = vd.GameStateInt;
      data.InternalState = vd.InternalStateInt;
      data.PotentialScore = vd.PotentialScore;
      data.Score = vd.Score;
      data.TotalGameSecs = vd.TotalGameSecs;
      data.RemainingSecs = vd.RemainingSecs;
      data.RemainingQuestionSecs = vd.RemainingQuestionSecs;

      // Broadcast to all the devices on the bus and send them all the good news.
      var i2cDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: m_cBroadcastAddr));
      var sm = new I2CSlaveDevice(i2cDevice);
      try
      {
        sm.SendVisualisationData(data);
      }
      catch
      {
      }

      sm.Dispose();
    }


    private void CheckBus()
    {
      SVizData data;
      data.EnvironmentStatus = 0;
      data.GameState = 0;
      data.InternalState = 0;
      data.PotentialScore = 0;
      data.Score = 0;
      data.TotalGameSecs = 0;
      data.RemainingSecs = 0;
      data.RemainingQuestionSecs = 0;
      Console.WriteLine("======================================================================");
      Console.WriteLine("I2C Bus report");
      Console.WriteLine("--------------");
      foreach(I2CSlaveData slave in m_PotentialSlaves)
      {
        var i2cDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: slave.m_Address));
        var sm = new I2CSlaveDevice(i2cDevice);
        try
        {
          sm.SendVisualisationData(data);
          slave.m_bOnline = true;
          Console.WriteLine("Device: " + slave.m_Name + " OK at address: " + slave.m_Address.ToString());
        }
        catch
        {
          Console.WriteLine("Device: " + slave.m_Name + " not contactable at address: " + slave.m_Address.ToString());
        }

        sm.Dispose();
      }		
      
      Console.WriteLine("======================================================================");
    }
  }

  /*
    private void CheckBusOld()
    {
      SVizData data;
      data.GameState = 0;
      data.InternalState = 0;
      data.PotentialScore = 0;
      data.Score = 0;
      data.TotalGameSecs = 0;
      data.RemainingSecs = 0;
      data.RemainingQuestionSecs = 0;

      foreach(I2CSlaveData slave in m_Slaves)
      {
        var i2cDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: slave.m_Address));
        var sm = new I2CSlaveDevice(i2cDevice);
        try
        {
          sm.SendVisualisationData(data);
          slave.m_bOnline = true;
          Console.WriteLine("Device: " + slave.m_Name + " OK at address: " + slave.m_Address.ToString());
        }
        catch
        {
          Console.WriteLine("Device: " + slave.m_Name + " not contactable at address: " + slave.m_Address.ToString());
        }

        sm.Dispose();
      }			
    }
  */

  class I2CSlaveDevice: IDisposable
  {
      public const byte I2cAddressBase = 0x2b;

    private I2cDevice _device;

    public I2CSlaveDevice(I2cDevice i2cDevice)
    {
      _device = i2cDevice;
    }


    public void SendVisualisationData(SVizData data)
    {
      int size = 8;
      var arr = new byte[size];
      
      arr[0] = (byte)data.EnvironmentStatus;      
      arr[1] = (byte)data.GameState;
      arr[2] = (byte)data.InternalState;
      arr[3] = (byte)data.PotentialScore;
      arr[4] = (byte)data.Score;
      arr[5] = (byte)data.TotalGameSecs;
      arr[6] = (byte)data.RemainingSecs;
      arr[7] = (byte)data.RemainingQuestionSecs;
           
      ReadOnlySpan<byte> bytes = arr; // Implicit cast from T[] to Span<T>
      _device.Write(bytes);
    }



    public void Dispose()
    {
      _device?.Dispose();
      _device = null;
    }
  }




}
