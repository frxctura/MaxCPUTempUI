/* using System;
using OpenHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.Drawing;

namespace MaxCPUTempUI
{
    class AutoShutDown
    {
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }
        public static int enteredTemperature = int.MaxValue;
        static void EnterTemp()
        {
            Console.WriteLine("Enter the temperature on which you want the system to automatically shut down: ");
            enteredTemperature = int.Parse(Console.ReadLine());
        }
        public static int enteredTime = int.MaxValue;
        static void EnterTime()
        {
            Console.WriteLine("Enter the time in which your system will shut down: ");
            enteredTime = int.Parse(Console.ReadLine());
            Console.Clear();
            Console.CursorVisible = false;
        }
        public static string currentTemp;
        static void GrabInfo(ref Computer computer, ref UpdateVisitor update)
        {
            computer.Accept(update);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            if (computer.Hardware[i].Sensors[j].Value < enteredTemperature)
                            {
                                Console.Write(computer.Hardware[i].Sensors[j].Value.ToString() + "\r");
                                currentTemp = computer.Hardware[i].Sensors[j].Value.ToString();
                            }
                            if (computer.Hardware[i].Sensors[j].Value > enteredTemperature)
                            {
                                ShutDown();
                            }
                        }
                    }
                }
            }
        }
        [STAThread]
        public static void Main(string[] args)
        {
            new NotifyIcon();
            UpdateVisitor update = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            EnterTemp();
            EnterTime();
            Thread threadone = new Thread(() => ExecuteInForeGround(ref computer, ref update));
            threadone.Start();
        }
        static void ShutDown()
        {
            Process.Start("shutdown", $"/s /t {enteredTime}");
            Console.WriteLine("Too high of a temperature, shutting down");
            System.Environment.Exit(0);
        }
        public static void ExecuteInForeGround(ref Computer computer, ref UpdateVisitor update)
        {
            while (true)
            {
                GrabInfo(ref computer, ref update);
                Thread.Sleep(1000);
                Console.Clear();
            }
        }
    }
}
*/