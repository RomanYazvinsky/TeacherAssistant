using Dao;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
// using TeacherAssistant.ReaderPlugin;

namespace TestLib
{
    class Program
    {
        public static void Main(string[] args)
        {
            /*SerialUtil.GetInstance().ContinueWith(async util =>
            {
               /*var a = (await util).Subscribe(text =>
                {
                    Console.WriteLine("Sent====>" + text);
                });#1#
                for (;;)
                {

                    Console.WriteLine("Sent====>" + await (await util).ReadCardAsync());
                }
            });
            Console.ReadKey();*/
            /*var a = new SimpleDao(new MyDbContext());
            foreach (var dep in a.LessonGroupModels)
            {
                Console.WriteLine((dep.Group != null) ? dep.Group.name : "Hello");
            }*/

            Console.ReadKey();
        }

        /*public static async void Write(SerialUtil serialUtil)
        {
            Console.WriteLine(await serialUtil.ReadCardAsync());
        }*/
    }
}