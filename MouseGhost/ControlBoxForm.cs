using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
// ReSharper disable LocalizableElement

namespace MouseGhost
{
    using Helpers;

    public partial class ControlBoxForm : Form
    {
        public ControlBoxForm()
        {
            InitializeComponent();

            _gHook = new GlobalKeyboardHook(); // Create a new GlobalKeyboardHook
            // Declare a KeyDown Event
            _gHook.KeyDown += gHook_KeyDown;

            // Adding hooks to monitor key status
            _gHook.HookedKeys.Add(Keys.F9);
            _gHook.HookedKeys.Add(Keys.F11);
            _gHook.HookedKeys.Add(Keys.F12);

            _gHook.HookedKeys.Add(Keys.F8);
            _gHook.HookedKeys.Add(Keys.F7);
            _gHook.HookedKeys.Add(Keys.F6);

            _gHook.hook();

            TopMost = true;
        }

        #region Fields And Externs
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const uint MouseeventfLeftdown = 0x02;
        private const uint MouseeventfLeftup = 0x04;
        private const uint MouseeventfRightdown = 0x08;
        private const uint MouseeventfRightup = 0x10;

        private readonly List<int[]> _mouseLocations = new List<int[]>();

        private readonly GlobalKeyboardHook _gHook;

        #endregion

        #region Recording Methods

        private bool _tmStat;
        private void btnRecord_Click(object sender, EventArgs e)
        {
            DoRecord();
        }

        private void DoRecord()
        {
            if (_tmStat)
            {
                timer1.Stop();
                btnRecord.Text = "Recorded!";
                _tmStat = false;

            }
            else
            {
                timer1.Start();
                _mouseLocations.Clear();
                btnRecord.Text = "Recording...";
                _tmStat = true;


            }
        }

        private bool _forceStop;
        private readonly bool _listen = true;

        private void gHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_listen) return;

            switch (e.KeyCode)
            {
                case Keys.F9: // Adds single left click
                    _mouseLocations.Add(new[] {-10, 0});
                    break;
                case Keys.F6: // Adds single right click
                    _mouseLocations.Add(new[] {-10, 1});
                    break;
                case Keys.F11: // Start recording
                    DoRecord();
                    break;
                case Keys.F8: // Starts playback
                case Keys.F12: // Playback alternative
                    PlayBack();
                    break;
                case Keys.F7: // Adds a pause
                    _mouseLocations.Add(new[] { -20, 1 });
                    break;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _mouseLocations.Add(new[] { Cursor.Position.X, Cursor.Position.Y });
        }

        #endregion

        public void DoMouseClick(int x, int y)
        {
            //Call the imported function with the cursor's current position
            Cursor.Position = new Point(x, y);
            mouse_event(MouseeventfLeftdown, (uint)x, (uint)y, 0, 0);
            Thread.Sleep(30);
            mouse_event(MouseeventfLeftup, (uint)x, (uint)y, 0, 0);
        }

        public void DoMouseRightClick(int x, int y)
        {
            //Call the imported function with the cursor's current position
            Cursor.Position = new Point(x, y);
            mouse_event(MouseeventfRightdown, (uint)x, (uint)y, 0, 0);
            Thread.Sleep(30);
            mouse_event(MouseeventfRightup, (uint)x, (uint)y, 0, 0);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            PlayBack();
        }

        int _patternCursor;
        private void PlayBack()
        {
            Rehook();

            btnPlay.Text = "Playing...";
            for (var i = _patternCursor; i < _mouseLocations.Count; i++)
            {
                if (_forceStop)
                {
                    _forceStop = false;
                    _patternCursor = 0;
                    btnPlay.Text = "Play >";
                    break;
                }
                if (_mouseLocations[i][0] == -10)
                {
                    if (_mouseLocations[i][1] == 0)
                        DoMouseClick(_mouseLocations[i - 1][0], _mouseLocations[i - 1][1]);
                    else if (_mouseLocations[i][1] == 1)
                        DoMouseRightClick(_mouseLocations[i - 1][0], _mouseLocations[i - 1][1]);
                }
                if (_mouseLocations[i][0] == -20)
                {
                    _patternCursor = i + 1;
                    btnPlay.Text = "Play >";
                    return;
                }
                else
                {
                    Cursor.Position = new Point(_mouseLocations[i][0], _mouseLocations[i][1]);
                    Thread.Sleep(30);
                }
            }
            _patternCursor = 0;

        }

        /// <summary>
        /// Sometimes our hook on keys will be ignored by OS
        /// by rehooking in every playback we fix the problem
        /// </summary>
        private void Rehook()
        {
            _gHook.unhook();
            _gHook.hook();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.Filter = "Mazyar Ghost|*.mghost";
            saveFileDialog1.DefaultExt = "mghost";
            saveFileDialog1.ShowDialog();

            if (string.IsNullOrEmpty(saveFileDialog1.FileName))
                return;

            var sw = new StreamWriter(saveFileDialog1.FileName);

            foreach (var t in _mouseLocations)
            {
                sw.WriteLine(t[0] + "," + t[1]);
            }
            sw.Flush();
            sw.Close();

            MessageBox.Show("Pattern Saved!");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Mazyar Ghost|*.mghost";
            openFileDialog1.Multiselect = false;
            openFileDialog1.ShowDialog();

            if (string.IsNullOrEmpty(openFileDialog1.FileName) || openFileDialog1.FileName == "openFileDialog1")
                return;

            var reader = new StreamReader(openFileDialog1.FileName);

            _mouseLocations.Clear();

            while (true)
            {
                var s = reader.ReadLine();
                if (string.IsNullOrEmpty(s))
                    break;

                var xy = s.Split(',');
                var x = int.Parse(xy[0]);
                var y = int.Parse(xy[1]);

                _mouseLocations.Add(new[] { x, y });
            }

            reader.Close();

            MessageBox.Show("Pattern Loaded!");
        }
    }
}
