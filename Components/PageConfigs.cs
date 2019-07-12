using TeacherAssistant.Components;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl {
    public static class PageConfigs {
        public static readonly PageProperties SchedulePageConfig = new PageProperties {
            Header = "Расписание", MinHeight = 700,
            PageType = typeof(TeacherAssistant.ComponentsImpl.SchedulePage.SchedulePage)
        };

        public static readonly PageProperties RegistrationPageConfig =
            new PageProperties {
                Header = "Регистрация", MinHeight = 700,
                PageType = typeof(TeacherAssistant.RegistrationPage.RegistrationPage)
            };
    }
}