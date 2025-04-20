using Godot;
using System;
using System.Collections.Generic;

public partial class TimeControl : Node
{
    public static Dictionary<char, PlayerTimer> playerTimerInfo = new()
    {
        { 'w', new(10, 0) },
        { 'b', new(10, 0) }
    };
    public class PlayerTimer
    {
        public float InitialTime, PlyIncrement;
        public Timer actualTimer;
        public PlayerTimer(float initialTime, float plyIncrement)
        {
            InitialTime = initialTime;
            PlyIncrement = plyIncrement;
            actualTimer = new() { Autostart = true, OneShot = true, WaitTime = initialTime };
            actualTimer.AddChild(LoadGraphics.I);
            GD.Print("!!!");
            actualTimer.Timeout += () =>
            {
                actualTimer.QueueFree();
            };
        }
    }
}
