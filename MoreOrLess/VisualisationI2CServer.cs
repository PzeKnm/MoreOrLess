using System;
using System.Collections.Generic;
using System.Text;
using System.Device.I2c;
using static MoreOrLess.I2CSlaveData;

namespace MoreOrLess
{
  
  public class I2CSlaveData
  {


  // Since I2C is byte-based, we need to decide data types carefully.
  // Total datagram length must be <= 32 bytes
  public struct SI2CVizData {
    public byte EnvironmentStatus;      //    1 byte
    public byte GameState;              //    1 byte
    public byte InternalState;          //    1 byte
    public ushort PotentialScore;       //    2 bytes
    public ushort Score;                //    2 bytes
    public ushort TotalGameSecs;        //    2 bytes
    public ushort RemainingSecs;        //    2 bytes
    public byte RemainingQuestionSecs;  //    1 byte
  };                                    // = 12 bytes

  public static int SIZE_OF_VIZ_DATA_IN_BYTES =     12;

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
      I2CSlaveData.SI2CVizData data;
      data.EnvironmentStatus = vd.EnvironmentStatus;
      data.GameState = (byte)vd.GameStateInt;
      data.InternalState = (byte)vd.InternalStateInt;
      data.PotentialScore = (ushort)vd.PotentialScore;
      data.Score = (ushort)vd.Score;
      data.TotalGameSecs = (ushort)vd.TotalGameSecs;
      data.RemainingSecs = (ushort)vd.RemainingSecs;
      data.RemainingQuestionSecs = (byte)vd.RemainingQuestionSecs;

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
      SI2CVizData data;
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
      Console.WriteLine("Status Address Device");
      Console.WriteLine("------ ------- ------");
      foreach (I2CSlaveData slave in m_PotentialSlaves)
      {
        var i2cDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: slave.m_Address));
        var sm = new I2CSlaveDevice(i2cDevice);
        try
        {
          sm.SendVisualisationData(data);
          slave.m_bOnline = true;
          Console.WriteLine("OK       " + slave.m_Address.ToString() + "     " + slave.m_Name );
        }
        catch
        {
          Console.WriteLine("NOT OK!  " + slave.m_Address.ToString() + "     " + slave.m_Name);
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


    public void SendVisualisationData(SI2CVizData data)
    {
      /*
      ushort number = Convert.ToUInt16("3510");
      byte upper = (byte) (number >> 8);
      byte lower = (byte) (number & 0xff);
       * */
      var arr = new byte[SIZE_OF_VIZ_DATA_IN_BYTES];
      
      // I2C is a byte-based protocol. We need to explicitely think whether we want to send
      // ints as ints, or truncate them to bytes
      arr[0] = (byte)data.EnvironmentStatus;      
      arr[1] = (byte)data.GameState;
      arr[2] = (byte)data.InternalState;
      arr[3] = (byte)(data.PotentialScore >> 8);
      arr[4] = (byte)(data.PotentialScore & 0xff);
      arr[5] = (byte)(data.Score >> 8);
      arr[6] = (byte)(data.Score & 0xff);
      arr[7] = (byte)(data.TotalGameSecs >> 8);
      arr[8] = (byte)(data.TotalGameSecs & 0xff); 
      arr[9] = (byte)(data.RemainingSecs >> 8);
      arr[10] = (byte)(data.RemainingSecs & 0xff);
      arr[11] = (byte)data.RemainingQuestionSecs;
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
