using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TruckRemoteServer
{
    internal class ControlMappingRW
    {
        MainForm Main_Form = Application.OpenForms.OfType<MainForm>().Single(); //Acessing Main Form

        internal void SaveControlMapping()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Directory.GetCurrentDirectory() + @"\ControlMapping.cfg", false))
                {
                    bool fl = true;
                    foreach (KeyValuePair<string, short[]> currentCM in Main_Form.server.pcController.ControlMapping)
                    {
                        if (!fl)                        
                            writer.WriteLine();

                        if (fl)
                            fl = !fl;
                        
                        writer.Write(currentCM.Key + "=" + currentCM.Value[0] + ";" + currentCM.Value[1]);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error during Saving Control map");
            }
        }

        internal void LoadControlMapping()
        {
            string FilePath = Directory.GetCurrentDirectory() + @"\ControlMapping.cfg";
            if (File.Exists(FilePath))
                try
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string[] ControlMap = line.Split(new char[] { '=' }, 2);
                        string[] ScanCode = ControlMap[1].Split(new char[] { ';' }, 2);
                        short.TryParse(ScanCode[0], out short ScanCode1);
                        short.TryParse(ScanCode[1], out short ScanCode2);

                        if (Main_Form.server.pcController.ControlMapping.ContainsKey(ControlMap[0]))
                            Main_Form.server.pcController.ControlMapping[ControlMap[0]] = new short[] { ScanCode1, ScanCode2 };
                        else
                            Main_Form.server.pcController.ControlMapping.Add(ControlMap[0], new short[] { ScanCode1, ScanCode2 });
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error during Loading Control map");
                }
        }

    }
}
