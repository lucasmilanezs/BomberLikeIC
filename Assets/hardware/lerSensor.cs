using UnityEngine;
using System;
using System.IO.Ports;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

public class LerSensor : MonoBehaviour
{

    public string PortJog = "COM14";

    public int BaudJog = 115200;

    public SerialPort ArdJ;

    public int PJogCon = 0;

    public string[] LJog = { "0", "0" };

    public int XJog = 0;
    public int YJog = 0;

    public int XRaw = 0;
    public int YRaw = 0;

    public int xDeadZone = 50;
    public int yDeadZone = 50;

    public int xZero = 0;
    public int yZero = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (PJogCon == 1)
        {
            try
            {
                if (ArdJ.BytesToRead > 0)
                {
                    ReadJog();
                }
            }
            catch
            {
                print("Erro na leitura");
            }
        }

    }

    public void ConectarPlataforma()
    {

        if (PJogCon == 1)
        {
            print("Porta Conectada");
        }
        else
        {

            try
            {
                ArdJ = new SerialPort(PortJog, BaudJog);

                //ArdJ = new SerialPort("COM14", 9600);

                ArdJ.Open();

                PJogCon = 1;
            }
            catch (Exception ex)
            {
                Debug.LogError("Erro na conexão");
                print(ex.Message);
            }

        }

    }

    public void ClosePlat()
    {
        ArdJ.Close();
        PJogCon = 0;
    }


    public void ReadJog()
    {
        LJog = ArdJ.ReadLine().Split("\t");
        XJog = int.Parse(LJog[0]) - xZero;
        YJog = int.Parse(LJog[1]) - yZero;

        XRaw = XJog;
        YRaw = YJog;

        if (XJog > xDeadZone)
        {
            XJog = 1;
        }
        else if (XJog < (xDeadZone * -1))
        {
            XJog = -1;
        }
        else
        {
            XJog = 0;
        }


        if (YJog > yDeadZone)
        {
            YJog = 1;
        }
        else if (YJog < (yDeadZone * -1))
        {
            YJog = -1;
        }
        else
        {
            YJog = 0;
        }
    }

    public void CalibrarSensores()
    {
        xZero = int.Parse(LJog[0]);
        yZero = int.Parse(LJog[1]);
    }

    public void ReadTxt()
    {
        string path = "D:/PC lab/Faculdade/Kerygma/JogosPlataforma/K-strike/K-strike/K-strike/Bowling/Assets/Scripts/COMTXT.txt";
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        //Debug.Log(reader.ReadToEnd());
        string input = reader.ReadToEnd();

        reader.Close();

        string[] Ports = input.Split(',');

        print(input);

        print(Ports[0]);

        PortJog = Ports[0];
        BaudJog = int.Parse(Ports[1]);

        /*string[] Ports = input.Split(',');

        PortMot = Ports[0].Replace("[", "").Replace("'", "").Replace("]", "");
        PortSen = Ports[1].Replace("[", "").Replace("'", "").Replace("]", "");
        PortEMG = Ports[2].Replace("[", "").Replace("'", "").Replace("]", "");

        print(PortMot);
        print(PortSen);
        print(PortEMG);*/

    }


}
