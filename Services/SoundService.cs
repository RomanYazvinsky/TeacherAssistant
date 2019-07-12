using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Model;

namespace TeacherAssistant {
    public class SoundService {
        public const string SoundDirectoryPath = "sounds";
        private Dictionary<long, string> _soundPaths = new Dictionary<long, string>();

        public void InitLibrary(IEnumerable<AlarmModel> alarms) {
            foreach (var alarmModel in alarms) {
                var fileName = CreateFileName(alarmModel);
                var combine = Path.Combine(SoundDirectoryPath, fileName);
                if (!File.Exists(combine)) {
                    File.WriteAllBytes(combine, alarmModel.Sound);
                }

                _soundPaths.Add(alarmModel.Id, fileName);
            }
            CleanDirectory();
        }

        private void CleanDirectory() {
            var orphanFiles = Directory.GetFiles(SoundDirectoryPath)
                .Where(file => !_soundPaths.Values.Contains(file));
            foreach (var file in orphanFiles) {
                File.Delete(file);
            }
        }

        protected virtual string CreateFileName(AlarmModel model) {
            return model.Id + "_" + model.Timer + ".wav";
        }

        public Uri GetSoundPath(AlarmModel model) {
            return new Uri(Path.Combine(SoundDirectoryPath, _soundPaths[model.Id]));
        }
    }
}