using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TeacherAssistant.ReaderPlugin
{
    public class StudentCard
    {
        public string CardUid { get; }
        public string CardId { get; }
        public string FirstName { get; }
        public string SecondName { get; }
        public string LastName { get; }
        public string FullName { get; }
        public DateTime ActiveFrom { get; }

        public StudentCard(string[] cardData)
        {
            if (cardData.Length > 1)
            {
                var uid = cardData[0];
                CardUid = uid.Replace("Card UID:", "");
                CardId = int.Parse(CardUid, NumberStyles.HexNumber).ToString();
                string studentData;

                if (cardData.Length > 2)
                {
                    var studentDataArray = new string[cardData.Length - 1];
                    Array.Copy(cardData, 1, studentDataArray, 0, studentDataArray.Length);
                    studentData = string.Join("", studentDataArray);
                }
                else
                {
                    studentData = cardData[1];
                }

                studentData = TryParseData(studentData)
                    .Replace("\0", "");
                const int dateLength = 4;
                var nameStart = 6;
                if (char.IsDigit(studentData, nameStart))
                {
                    nameStart++;
                }

                FullName = studentData.Substring(nameStart);
                var date = studentData.Substring(0, dateLength);
                var names = FullName.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var namesCount = names.Length;
                if (namesCount > 0)
                {
                    LastName = names[0];
                    if (namesCount > 1)
                    {
                        FirstName = names[1];
                        if (namesCount > 2)
                        {
                            SecondName = names[2];
                        }
                    }
                }
                ActiveFrom = DateTime.ParseExact(date, "yyyy", CultureInfo.InvariantCulture);
            }
        }

        private static string TryParseData(string data)
        {
            return HexToString(Regex.Replace(data, @"\s+", ""));
        }


        private static string HexToString(string hex)
        {
            var buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexdec = hex.Substring(i, 2);
                if (!byte.TryParse(hexdec, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out buffer[i / 2]))
                {
                    return hex;
                }
            }

            return Encoding.GetEncoding("Windows-1251").GetString(buffer);
        }
    }
}