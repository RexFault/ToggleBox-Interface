using vJoyInterfaceWrap;
using System.IO.Ports;

namespace ToggleBox_Interface
{
    internal class Program
    {
        // Declaring one joystick (Device id 1) and a position structure. 
        static public vJoy joystick;
        static public vJoy.JoystickState iReport;
        static public uint id = 1;


        static void Main(string[] args)
        {
            showBanner();

            //Serial Port Setup
            string[] serialPortsAvailable = SerialPort.GetPortNames();
            int portSelected = 0;
            Console.WriteLine("Please Select a Serial Port: ");
            for (int i = 0; i < serialPortsAvailable.Length;  i++)
            {
                Console.WriteLine($"{i+1}: {serialPortsAvailable[i]}");
            }
            portSelected = int.Parse(Console.ReadLine());
            if ((portSelected <= 0 ) || (portSelected > serialPortsAvailable.Length)) {
                    Console.WriteLine("Invalid Serial Port Selected! Quitting...");
                    Console.ReadKey(true);
                    return;
            }
            SerialPort toggleBoxSerialPort = new SerialPort(serialPortsAvailable[portSelected - 1], 115200, Parity.None, 8, StopBits.One);
            toggleBoxSerialPort.NewLine = "\r\n";
            toggleBoxSerialPort.ReadTimeout = 2000; // Set a read timeout to prevent blocking indefinitely
            toggleBoxSerialPort.WriteTimeout = 2000; // Set a write timeout to prevent blocking indefinitely

            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();


            // Device ID can only be in the range 1-16
            if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
                id = Convert.ToUInt32(args[0]);
            if (id <= 0 || id > 16)
            {
                Console.WriteLine("Illegal device ID {0}\nExit!", id);
                return;
            }

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
                switch (status)
                {
                    case VjdStat.VJD_STAT_OWN:
                        Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                        break;
                    case VjdStat.VJD_STAT_FREE:
                        Console.WriteLine("vJoy Device {0} is free\n", id);
                        break;
                    case VjdStat.VJD_STAT_BUSY:
                        Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                        return;
                    case VjdStat.VJD_STAT_MISS:
                        Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                        return;
                    default:
                        Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                        return;
                }

            int nButtons = joystick.GetVJDButtonNumber(id);


            // Print results
            Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);

            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
                return;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.\n", id);

            Console.WriteLine("\nPress Enter to begin feed");
            Console.ReadKey(true);

            joystick.ResetVJD(id);

            try
            {

                toggleBoxSerialPort.Open();
                Thread.Sleep(100);
                Console.WriteLine("Serial port opened successfully.");
                Console.WriteLine("Beginning data feed...");
                //Console.ReadKey();
                toggleBoxSerialPort.ReadExisting(); // Clear any existing data in the buffer
                Console.WriteLine("Press 'q' to quit.");

                while (true)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Quitting...");
                        break;
                    }
                    try
                    {

                        if (toggleBoxSerialPort.BytesToRead > 0)
                        {
                            string data = toggleBoxSerialPort.ReadLine();
                            switch (data)
                            {

                                case "Device Started!\r\n":
                                    Console.WriteLine("Device Started");
                                    break;
                                default:
                                    handleJoyStickInput(data, id);
                                    break;

                            }
                        }


                        toggleBoxSerialPort.WriteLine("POL");
                        //Console.WriteLine("Sent POL command to device.");
                        Thread.Sleep(100); // Sleep for a short time to avoid overwhelming the device
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Warning: Serial port read/write operation timed out. Retrying...");
                        toggleBoxSerialPort.DiscardInBuffer(); // Clear the input buffer to avoid stale data
                    }

                }
            }
            catch (TimeoutException e)
            {
                Console.WriteLine("Error: Serial port read/write operation timed out.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }

        } // Main

        private static void handleJoyStickInput(string data, uint joyID)
        {
            //Console.WriteLine($"Received data: {data}");
            //Console.WriteLine("Recieved Data Length: " + data.Length);
            for(int i = 0; i < 18; i++)
            {
                if (data[i] == '0')
                {
                    joystick.SetBtn(true, joyID, (uint)(i + 1));
                    //Console.WriteLine($"Button {i + 1} Pressed");
                }
                else
                {
                    joystick.SetBtn(false, joyID, (uint)(i + 1));
                    //Console.WriteLine($"Button {i + 1} Released");
                }
            }
        }

        private static void showBanner()
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("ToggleBox vJoy Interface");
            Console.WriteLine("Created by: Shane McIntosh");
            Console.WriteLine("GitHub: https://github.com/RexFault");
            Console.WriteLine("===============================================");
            Console.WriteLine();
        }
    }
}
