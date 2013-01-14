/*
 * David Maxson
 * Relativity graphical example
 * 1/13/13
 * scnerd@gmail.com
 * 
 * TO USE: Compile and run, then click on the gray box to begin.
 * Use the left/right arrow keys to move the circle
 * When done, drag the slider around to re-create any point in
 * the animation
 * 
 * ABOUT: This example demonstrates how to create a program with
 * continuous animation that's tied to a relative timeline. Note
 * that the position of the circle is actually re-calculated at
 * every drawing cycle, but this isn't much more intensive than
 * any other animation method, and it's totally consistent. Also
 * note how variables are used to remember the past rather than
 * to store the status of the present.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TimeRelativity;

namespace TimeTestDodger
{
    public enum Direction
    {
        Left,
        Right
    }

    public enum State
    {
        Start,
        Stop
    }

    public struct Command
    {
        public Direction Dir;
        public State St;

        public Command(Direction D, State S)
        {
            Dir = D;
            St = S;
        }
    }

    public partial class Form1 : Form
    {
        bool HasBeenScrolled = false,
            IsRunning = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs loadargs)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs clickargs)
        {
            if (IsRunning)
                return;
            IsRunning = true;
            int Velocity = 200; //Pixels per second

            Point PreviousPosition, CurrentPosition = new Point(pictureBox1.Width / 2, pictureBox1.Height * 3 / 4);
            Direction PreviousDir = Direction.Right;
            State PreviousState = State.Stop;
            long PreviousCmdTime = 0;

            PreviousPosition = CurrentPosition;

            EventManager Manager = new EventManager(
                (c, t) =>
                {
                    if (PreviousState == State.Start && ((Command)c).St == State.Start)
                        return false;
                    if (((Command)c).St == State.Stop && ((Command)c).Dir != PreviousDir)
                        return false;
                    if (((Command)c).St == State.Stop)
                        PreviousPosition.X = CurrentPosition.X =
                            PreviousPosition.X + (int)(Velocity * (t - PreviousCmdTime) / 1000 * (PreviousDir == Direction.Left ? -1 : 1));

                    PreviousDir = ((Command)c).Dir;
                    PreviousState = ((Command)c).St;
                    PreviousCmdTime = t;

                    return true;
                },
                () =>
                    new object[] { PreviousPosition, PreviousDir, PreviousState, PreviousCmdTime },
                (o) =>
                {
                    PreviousPosition = (Point)((object[])o)[0];
                    PreviousDir = (Direction)((object[])o)[1];
                    PreviousState = (State)((object[])o)[2];
                    PreviousCmdTime = (long)((object[])o)[3];
                },
                2000);

            splitContainer1.KeyDown += new KeyEventHandler((o, e) =>
            {
                if (PreviousState == State.Stop)
                {
                    if (e.KeyCode == Keys.Left)
                        Manager.AddEvent(new Command(Direction.Left, State.Start));
                    else if (e.KeyCode == Keys.Right)
                        Manager.AddEvent(new Command(Direction.Right, State.Start));
                }
            });

            splitContainer1.KeyUp += new KeyEventHandler((o, e) =>
            {
                if (e.KeyCode == Keys.Left)
                    Manager.AddEvent(new Command(Direction.Left, State.Stop));
                else if (e.KeyCode == Keys.Right)
                    Manager.AddEvent(new Command(Direction.Right, State.Stop));
            });

            Manager.Start();
            Bitmap img = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(img);
            long MaxTime = 0;
            while (!this.IsDisposed)
            {
                Manager.ProcessEvents();

                if (PreviousState == State.Start)
                    CurrentPosition.X = PreviousPosition.X +
                        (int)(Velocity * (Manager.CurrentTime() - PreviousCmdTime) / 1000 * (PreviousDir == Direction.Left ? -1 : 1));

                g.Clear(Color.White);
                g.FillEllipse(new SolidBrush(Color.Red),
                    CurrentPosition.X - 20,
                    CurrentPosition.Y - 20,
                    40,
                    40);

                pictureBox1.Image = img;

                if (HasBeenScrolled)
                {
                    Manager.JumpToTime((int)(MaxTime * trackBar1.Value / trackBar1.Maximum));
                    HasBeenScrolled = false;
                }
                else
                {
                    long CurTime = Manager.CurrentTime();
                    MaxTime = Math.Max(MaxTime, CurTime);
                    trackBar1.Value = (int)(CurTime * trackBar1.Maximum / MaxTime);
                }

                this.Refresh();
                Application.DoEvents();
                splitContainer1.Focus();
                System.Threading.Thread.Sleep(30);
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            HasBeenScrolled = true;
        }
    }
}
