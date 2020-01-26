using System;
using System.Linq;

namespace TeacherAssistant.State {
    public static class IdGenerator {
        private const string IdValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly Random Random = new Random();

        public static string GenerateId(int length = 10) {
            return new string
            (
                Enumerable.Repeat(IdValues, length)
                    .Select(s => s[Random.Next(s.Length)])
                    .ToArray()
            );
        }
    }
}