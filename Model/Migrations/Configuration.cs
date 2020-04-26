
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace TeacherAssistant.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<TeacherAssistant.Database.LocalDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }
    } 
}