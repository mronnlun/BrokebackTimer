using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Timers;
using Android.Speech.Tts;
using Java.Util;
using System.Linq;

namespace BrokebackTimer
{
    [Activity(Label = "BrokebackTimer", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, TextToSpeech.IOnInitListener
    {
        private Button startbutton;
        private Button stopbutton;
        private Button pausebutton;
        private Button resumebutton;

        TextView status;

        int setLength = 45;
        int setPause = 15;
        int iterations = 2;
        int stationCount = 4;
        int moveTime = 45;

        const int WarmUpDuration = 10;

        //1000 ms
        System.Timers.Timer timer;

        int timeLeftInPhase = int.MinValue;

        TimerState currentTimerState;

        private TextToSpeech engine;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            startbutton = FindViewById<Button>(Resource.Id.start_button);
            startbutton.Click += Start;
            stopbutton = FindViewById<Button>(Resource.Id.stop_button);
            stopbutton.Click += Stop;
            pausebutton = FindViewById<Button>(Resource.Id.pause_button);
            pausebutton.Click += Pause;
            resumebutton = FindViewById<Button>(Resource.Id.resume_button);
            resumebutton.Click += Resume;

            status = FindViewById<TextView>(Resource.Id.status);

            timer = new System.Timers.Timer(1000);
            timer.AutoReset = true;
            timer.Elapsed += SecondElapsed;

            currentTimerState = TimerState.NotStarted;

            engine = new TextToSpeech(this, this);
        }

        private int GetIntFromControl(int resourceId)
        {
            var edittext = FindViewById<EditText>(resourceId);
            if (int.TryParse(edittext.Text, out int number))
                return number;
            else
                return 0;
        }

        bool readyset = false;
        int iterationsInSamePlace = 0;
        int stationsRotated = 0;

        Phase currentPhase = Phase.None;

        private void SecondElapsed(object sender, ElapsedEventArgs e)
        {
            timeLeftInPhase--;

            string statusText = "";

            string utterance = "";

            switch (currentPhase)
            {
                case Phase.Warmup:
                    statusText = timeLeftInPhase + "s kvar till start";

                    if (timeLeftInPhase == 0)
                    {
                        utterance = "GO!";

                        currentPhase = Phase.Training;
                        timeLeftInPhase = setLength;
                    }
                    else
                    {
                        if (!readyset)
                        {
                            readyset = true;

                            utterance = timeLeftInPhase + " seconds to start";
                        }
                    }
                    break;
                case Phase.MoveBetweenStations:
                    statusText = timeLeftInPhase + "s att flytta till nästa station";

                    if (timeLeftInPhase == 0)
                    {
                        utterance = "GO!";

                        currentPhase = Phase.Training;
                        timeLeftInPhase = setLength;
                    }
                    else
                    {
                        if (timeLeftInPhase == 5)
                        {
                            utterance = timeLeftInPhase + " seconds to start";
                        }
                    }
                    break;
                case Phase.IterationPause:
                    statusText = timeLeftInPhase + "s kvar av paus";

                    if (timeLeftInPhase == 0)
                    {
                        utterance = "GO!";

                        currentPhase = Phase.Training;
                        timeLeftInPhase = setLength;
                    }
                    else
                    {
                        if (timeLeftInPhase == 5)
                        {
                            utterance = timeLeftInPhase + " seconds to start";
                        }
                    }
                    break;
                case Phase.Training:
                    statusText = timeLeftInPhase + "s kvar av träningen";

                    if (timeLeftInPhase == 0)
                    {
                        utterance = "Well done!";

                        iterationsInSamePlace++;
                        if (iterationsInSamePlace == iterations)
                        {
                            if (stationsRotated == stationCount)
                            {
                                utterance += " All stations finished!";
                                Stop(null, null);
                            }
                            else
                            {
                                utterance += " Move to the next station";

                                currentPhase = Phase.MoveBetweenStations;
                                timeLeftInPhase = moveTime;
                                iterationsInSamePlace = 0;
                                stationsRotated++;
                            }
                        }
                        else
                        {
                            utterance += " Now take a " + setPause + " second break.";

                            currentPhase = Phase.IterationPause;
                            timeLeftInPhase = setPause;
                        }
                    }
                    else
                    {
                        if (timeLeftInPhase % 15 == 0)
                        {
                            utterance = timeLeftInPhase + " seconds left";
                        }
                        else if (timeLeftInPhase <= 10)
                        {
                            utterance = timeLeftInPhase.ToString();
                        }
                    }
                    break;
            }

            if (!string.IsNullOrWhiteSpace(utterance))
                engine.Speak(utterance, QueueMode.Flush, null, null);

            status.Text = statusText;
        }

        enum Phase
        {
            None = 0,
            Warmup,
            Training,
            IterationPause,
            MoveBetweenStations
        }

        enum TimerState
        {
            NotStarted = 0,
            Started,
            Paused,
            Stopped
        }

        void ReadSettings()
        {
            setLength = GetIntFromControl(Resource.Id.set_seconds);
            setPause = GetIntFromControl(Resource.Id.set_paus);
            iterations = GetIntFromControl(Resource.Id.set_iterationer);
            stationCount = GetIntFromControl(Resource.Id.set_count);
            moveTime = GetIntFromControl(Resource.Id.move_time);
        }

        void SetButtonVisibility()
        {
            switch (currentTimerState)
            {
                case TimerState.NotStarted:
                    startbutton.Visibility = ViewStates.Visible;
                    stopbutton.Visibility = ViewStates.Gone;
                    pausebutton.Visibility = ViewStates.Gone;
                    resumebutton.Visibility = ViewStates.Gone;
                    break;
                case TimerState.Started:
                    startbutton.Visibility = ViewStates.Gone;
                    stopbutton.Visibility = ViewStates.Visible;
                    pausebutton.Visibility = ViewStates.Visible;
                    resumebutton.Visibility = ViewStates.Gone;
                    break;
                case TimerState.Paused:
                    startbutton.Visibility = ViewStates.Gone;
                    stopbutton.Visibility = ViewStates.Visible;
                    pausebutton.Visibility = ViewStates.Gone;
                    resumebutton.Visibility = ViewStates.Visible;
                    break;
                case TimerState.Stopped:
                    startbutton.Visibility = ViewStates.Visible;
                    stopbutton.Visibility = ViewStates.Gone;
                    pausebutton.Visibility = ViewStates.Gone;
                    resumebutton.Visibility = ViewStates.Gone;
                    break;
            }
        }
        void Start(object sender, EventArgs e)
        {
            ReadSettings();

            timeLeftInPhase = WarmUpDuration + 1;
            currentPhase = Phase.Warmup;

            currentTimerState = TimerState.Started;
            SetButtonVisibility();

            //Same as setting enabled to true
            timer.Start();

            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn);

        }
        void Stop(object sender, EventArgs e)
        {
            currentTimerState = TimerState.Stopped;
            SetButtonVisibility();

            readyset = false;
            iterationsInSamePlace = 0;
            stationsRotated = 0;

            timer.Stop();

            status.Text = "";

            this.Window.ClearFlags(WindowManagerFlags.KeepScreenOn);

        }

        void Pause(object sender, EventArgs e)
        {
            currentTimerState = TimerState.Paused;
            SetButtonVisibility();

            timer.Stop();
        }

        private void Resume(object sender, EventArgs e)
        {
            ReadSettings();

            currentTimerState = TimerState.Started;
            SetButtonVisibility();

            timer.Start();
        }

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            if (status == OperationResult.Success)
            {
                engine.SetLanguage(Locale.Us);
            }
        }

    }
}

