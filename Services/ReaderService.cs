using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Model.Models;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;

namespace TeacherAssistant.Components
{
    public class ReaderService
    {
        private CancellationTokenSource _cancellationTokenSource;
        private static ReaderService _instance;
        public bool Busy { get; set; }

        public async Task Start()
        {
            var serialUtil = new SerialUtil();
            _cancellationTokenSource = new CancellationTokenSource();
  

            Busy = true;
            string readData;
            try
            {
                readData = (await serialUtil.ReadCardAsync(_cancellationTokenSource.Token)).Replace('\r', ' ')
                    .Replace('\0', ' ').Trim();
            }
            catch (OperationCanceledException e)
            {
                Busy = false;
                return;
            }
            catch (Exception)
            {
                return;
            }

            var model = new StudentModel {card_uid = readData.Substring(9, 8)};
            model.card_id = int.Parse(model.card_uid, System.Globalization.NumberStyles.HexNumber).ToString();
            int studyInfoLength = 7;
            var dateAndName = readData.Split('\n')[1];
            if (!char.IsDigit(dateAndName[studyInfoLength]))
            {
                studyInfoLength--;
            }

            var studyBeginning = dateAndName.Substring(0, studyInfoLength);
            var fullName = dateAndName.Substring(studyInfoLength, dateAndName.Length - studyInfoLength).Split(' ');
            model.last_name = fullName[0];
            model.first_name = fullName[1];
            model.patronymic = fullName[2];
            DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish()
                {Data = model, Id = "LastReadStudentCard"});
        }
    }

}
