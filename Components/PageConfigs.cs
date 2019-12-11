using TeacherAssistant.Components;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl {
    public static class PageConfigs {
        public static readonly PageProperties<TeacherAssistant.ComponentsImpl.SchedulePage.SchedulePage> SchedulePageConfig = new PageProperties<TeacherAssistant.ComponentsImpl.SchedulePage.SchedulePage> {
            Header = "Расписание", MinHeight = 700,
        };

        public static readonly PageProperties<TeacherAssistant.RegistrationPage.RegistrationPage> RegistrationPageConfig =
            new PageProperties<TeacherAssistant.RegistrationPage.RegistrationPage> {
                Header = "Регистрация", MinHeight = 700,
            };
    }
}