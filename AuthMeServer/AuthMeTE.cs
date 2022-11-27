using System.Collections.Generic;
using System.Timers;

namespace AuthMeServer
{
    public class AuthMeTE
    {
        private Dictionary<string, object> _args;
        private readonly Timer _timer;

        public delegate void TimedEventFireDelegate(AuthMeTE evt);
        public event TimedEventFireDelegate OnFire;

        public AuthMeTE(double interval)
        {
            _timer = new Timer();
            _timer.Interval = interval;
            _timer.Elapsed += _timer_Elapsed;
        }

        public AuthMeTE(double interval, Dictionary<string, object> args)
            : this(interval)
        {
            Args = args;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (OnFire != null)
            {
                OnFire(this);
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Kill()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        public Dictionary<string, object> Args
        {
            get { return _args; }
            set { _args = value; }
        }

        public double Interval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }
    }
}