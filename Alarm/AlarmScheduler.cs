using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using Dao;
using TeacherAssistant.State;
using Model;

namespace TeacherAssistant.Alarm
{
    public class AlarmScheduler
    {
        private class AlarmTask
        {
            public AlarmModel AlarmModel { get; }
            public Timer Timer { get; set; }

            public AlarmTask(AlarmModel alarmModel)
            {
                AlarmModel = alarmModel;
            }

            public void Activate()
            {
                if (AlarmModel.time == null) return;
                if (Timer != null)
                {
                    throw new Exception($"Alarm [{AlarmModel.time}] have been activated one more than once");
                }
                var millisecondsLeft = AlarmModel.time.Value - ToJavaLong(DateTime.Now);
                if (millisecondsLeft <= 0)
                {
                    throw new Exception($"Alarm [{AlarmModel.time}] have been activated after time exceed");
                }
                Timer = new Timer(millisecondsLeft);
                Timer.Elapsed += async (sender, args) =>
                {
                    Timer.Stop();
                    await Play();
                };
                Timer.Start();
            }

            public void Deactivate()
            {
                Timer?.Stop();
                Timer = null;
            }
        }
        private static AlarmScheduler _instance;
        private static MediaPlayer _player;
        private AlarmScheduler()
        {
            _player = new MediaPlayer();
        }

        private static async Task Play()
        {
            var mediaFile = Publisher.Get<string>("Alarm.MediaFile");
            if (mediaFile == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                if (_player.HasAudio)
                {
                    _player.Stop();
                }
                _player.Open(new Uri(mediaFile));
                _player.Play();
            });
        }

        // TODO: DI singleton
        public static AlarmScheduler GetInstance()
        {
            return _instance ?? (_instance = new AlarmScheduler());
        }
        private void Save(AlarmModel model)
        {
            GeneralDbContext.GetInstance().AlarmModels.Add(model);
        }

        public static long ToJavaLong(DateTime time)
        {
            return (long)(time - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static DateTime FromJavaLong(long javaDate)
        {
            return new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(javaDate);
        }

        public AlarmModel Schedule(DateTime time, bool active)
        {
            var alarmModel = new AlarmModel { active = active ? 1 : 0, time = ToJavaLong(time) };
            Save(alarmModel);
            if (active)
            {
                Activate(alarmModel);
            }
            return alarmModel;
        }

        public AlarmModel Activate(AlarmModel model)
        {
            if (model.active == 0)
            {
                model.active = 1;
                Save(model);
            }

            if (model.time != null && (FromJavaLong(model.time.Value) - DateTime.Now).Hours < 24)
            {

            }
            return model;
        }

        


    }
}
