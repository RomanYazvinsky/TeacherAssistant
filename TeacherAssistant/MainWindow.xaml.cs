using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dao;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.Components.Table;
using TeacherAssistant.State;

namespace TeacherAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LayoutStateManagement _layoutStateManagement;

        public MainWindow()
        {
            InitializeComponent();
            string firstTableId = "RegistrationStreamList";
            string lessonTableId = "LessonsList";
            string secondTableId = "Registered";
            string studentName = "StudentName";
            string isPresent = "IsPresent";
            _layoutStateManagement = LayoutStateManagement.GetInstance();
            SideEffectManager.Init();
            var readerService = ReaderService.GetInstance();

            Publisher.Publish("RegisterButton.Text", "->");

            Publisher.Publish("AutoregisterButton.Text", "Ручная регистрация");
            Publisher.Publish("UnregisterButton.Text", "<-");
            Publisher.Publish("ManualAdd.Text", "+");
            var updateStudentView = new Action<StudentModel>(async student =>
            {
                if (student == null) return;
                string path = await PhotoService.GetInstance().DownloadPhoto(student.card_id);

                Publisher.Publish("PhotoPath", path);
                Publisher.Publish(studentName + ".Text", student.last_name + " " + student.first_name + " " + student.patronymic);
            });
            var register = new Action<LessonModel, List<StudentModel>>(async (lesson, models) =>
            {
                if (models.Count == 0)
                {
                    return;
                }
                var studentLessonModels = models.Select(student =>
                {
                    var studentLessonModel = new StudentLessonModel
                    {
                        lesson_id = lesson.id,
                        student_id = student.id,
                        registration_time = DateTime.Now.ToString(CultureInfo.InvariantCulture)
                    };
                    return studentLessonModel;
                });
                GeneralDbContext.GetInstance().StudentLessonModels.AddRange(studentLessonModels);
                await GeneralDbContext.GetInstance().SaveChangesAsync();
            });
            var unregister = new Action<LessonModel, List<StudentModel>>(async (lesson, models) =>
            {
                if (models.Count == 0)
                {
                    return;
                }

                var studentLessonModels = new List<StudentLessonModel>(GeneralDbContext.GetInstance()
                    .StudentLessonModels.Where(model => model.lesson_id == lesson.id));
                studentLessonModels = new List<StudentLessonModel>(from x in studentLessonModels
                                                                   where models.Any(model => x.student_id == model.id)
                                                                   select x);
                GeneralDbContext.GetInstance().StudentLessonModels.RemoveRange(studentLessonModels);
                await GeneralDbContext.GetInstance().SaveChangesAsync();
            });
            var store = DataExchangeManagement.GetInstance().PublishedDataStore;
            SideEffectManager.AddSideEffect("OnSelectedStudentChangeUpdatePhoto", firstTableId + ".SelectedItem", updateStudentView);
            SideEffectManager.AddSideEffect("OnSelectedStudentRegisterTableChangeUpdatePhoto", secondTableId + ".SelectedItem", updateStudentView);
            bool autoregisterEnabled = false;
            SideEffectManager.AddSideEffect<StudentModel>("OnCardReadCheckInDb", "LastReadStudentCard", student =>
            {
                var state = DataExchangeManagement.GetInstance().PublishedDataStore.GetState();
                var lesson = Publisher.Get<LessonModel>(state, lessonTableId + ".SelectedItem");
                if (lesson == null)
                {
                    return;
                }
                Publisher.Publish(isPresent + ".Text", "");
                if (student == null)
                {
                    return;
                }
                var existentStudent = GeneralDbContext.GetInstance().StudentModels.FirstOrDefault(model => model.card_id == student.card_id);
                if (existentStudent == null)
                {
                    Publisher.Publish(isPresent + ".Text", "Не найден(а) в базе");
                    existentStudent = student;
                    _layoutStateManagement.AttachedPluginStore.Dispatch(
                        new LayoutStateManagement.Hide { Id = "ManualAdd" });
                }
                else
                {
                    var models = Publisher.Get<ObservableCollection<StudentModel>>(state, secondTableId + ".Items") ??
                                 new ObservableCollection<StudentModel>();
                    if (!models.Contains(existentStudent))
                    {
                        if (autoregisterEnabled)
                        {

                            Publisher.Publish(secondTableId + ".Items",
                                new ObservableCollection<StudentModel>(models) { existentStudent }
                            );

                        }
                        else
                        {
                            _layoutStateManagement.AttachedPluginStore.Dispatch(
                                new LayoutStateManagement.AttachView("ManualAdd", "Button", FirstTab, new ViewConfig
                                {
                                    ColumnSize = 2,
                                    RowSize = 2,
                                    Column = 33,
                                    Row = 7
                                }));
                        }
                    }

                }
                updateStudentView(existentStudent);
            });
            SideEffectManager.AddSideEffect<bool>("OnChangeReaderStatus", "IsReaderEnabled", async isReaderEnabled =>
           {
               if (isReaderEnabled)
               {
                   if (readerService.Busy) return;
                   await readerService.Start();
                   if (!readerService.Busy)
                   {
                       Publisher.Publish("IsReaderEnabled", false);
                   }
               }
               else
               {
                   readerService.Stop();
               }
           });
            _layoutStateManagement.AttachedPluginStore.Dispatch(
                new LayoutStateManagement.InitLayout(36, 64, FirstTab));
            var studentTableConfig = new TableConfig<StudentModel>
            {
                DefaultSortColumn = new ListViewComparer<StudentModel>(new List<SortDescription>
                {
                    new SortDescription("last_name", ListSortDirection.Ascending),
                    new SortDescription("first_name", ListSortDirection.Ascending)
                }),
                SelectOnDoubleClick = true,
                SelectOnEnter = true,
                EnableMultiSelect = true,
                ColumnConfigs = new Dictionary<string, ColumnConfig>
                {
                    {
                        "Имя",
                        new ColumnConfig
                        {
                            PropertyPath = "first_name",
                            DefaultWidth = 100
                        }
                    },
                    {
                        "Фамилия",
                        new ColumnConfig
                        {
                            PropertyPath = "last_name",
                            DefaultWidth = 100
                        }
                    },
                    {
                        "Отчество", new ColumnConfig
                        {
                            PropertyPath = "patronymic",
                            DefaultWidth = 100
                        }
                    },
                }
            };
            store.Dispatch(new DataExchangeManagement.Publish
            {
                Id = lessonTableId + ".TableConfig",
                Data = new TableConfig<LessonModel>
                {
                    DefaultSortColumn = new ListViewComparer<LessonModel>(new List<SortDescription>()
                    {
                        new SortDescription("Date", ListSortDirection.Descending)
                    }),
                    SelectOnDoubleClick = true,
                    SelectOnEnter = true,
                    ColumnConfigs = new Dictionary<string, ColumnConfig>
                    {
                        {
                            "Поток",
                            new ColumnConfig
                            {
                                PropertyPath = "Stream.name",
                                DefaultWidth = 300
                            }
                        },
                        {
                            "Начало", new ColumnConfig
                            {
                                PropertyPath = "Schedule.Begin",
                                StringFormat = "HH:mm",
                                DefaultWidth = 100,
                                SortEnabled = false
                            }
                        },
                        {
                            "Окончание", new ColumnConfig
                            {
                                PropertyPath = "Schedule.End",
                                StringFormat = "HH:mm",
                                DefaultWidth = 100,
                                SortEnabled = false
                            }
                        },
                        {
                            "Дата", new ColumnConfig
                            {
                                PropertyPath = "Date",
                                StringFormat = "dd/MM/yyyy",
                                DefaultWidth = 100
                            }
                        }
                    }
                }
            });
            _layoutStateManagement.AttachedPluginStore.Dispatch(
                new LayoutStateManagement.AttachGenericView(lessonTableId, "Table", FirstTab,
                    new ViewConfig { ColumnSize = 62, RowSize = 32, Row = 3, Column = 1 }, typeof(LessonModel)));
            _layoutStateManagement.AttachedPluginStore.Dispatch(
                new LayoutStateManagement.AttachView("Toolbar", "Toolbar", FirstTab,
                    new ViewConfig { ColumnSize = 64, RowSize = 2 }));
            SideEffectManager.AddSideEffect<LessonModel>("OnSelectLessonOpenRegistration", lessonTableId + ".SelectedItem", lesson =>
            {
                if (lesson == null)
                {
                    return;
                }
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.Hide { Id = lessonTableId });
                var dao = GeneralDbContext.GetInstance();
                var registeredStudents = new ObservableCollection<StudentModel>(dao
                    .StudentLessonModels.Where(model => model.lesson_id == lesson.id)
                    .Select(model => model.Student));
                store.Dispatch(new DataExchangeManagement.Publish
                {
                    Id = firstTableId + ".Items",
                    Data = new ObservableCollection<StudentModel>(dao
                        .StudentGroupModels.Where(studentGroupModel =>
                            dao.StreamGroupModels.Any(streamGroupModel =>
                                streamGroupModel.stream_id == lesson.stream_id && studentGroupModel.group_id == streamGroupModel.group_id))
                        .Select(model => model.Student))
                });
                store.Dispatch(new DataExchangeManagement.Publish
                {
                    Id = secondTableId + ".Items",
                    Data = registeredStudents
                });
                SideEffectManager.AddSideEffect<ObservableCollection<StudentModel>>("UpdateDatabaseOnStudentRegistration", secondTableId + ".Items", students =>
                        {
                            if (students == null)
                            {
                                return;
                            }
                            var added = new List<StudentModel>(students);
                            var removed = new List<StudentModel>(registeredStudents);
                            added.RemoveAll(registeredStudents.Contains);
                            removed.RemoveAll(students.Contains);
                            register(lesson, added);
                            unregister(lesson, removed);
                            registeredStudents = new ObservableCollection<StudentModel>(students);
                        });
                store.Dispatch(new DataExchangeManagement.Publish
                {
                    Id = firstTableId + ".TableConfig",
                    Data = studentTableConfig

                });
                store.Dispatch(new DataExchangeManagement.Publish
                {
                    Id = secondTableId + ".TableConfig",
                    Data = studentTableConfig
                });
                Publisher.Publish("RegisterButton.Action", new Action(() =>
                {
                    var selectedStudentModels = Publisher.Get<List<StudentModel>>(store.GetState(), firstTableId + ".SelectedItems");
                    if (selectedStudentModels == null)
                    {
                        return;
                    }
                    var state = store.GetState();
                    var models = Publisher.Get<ObservableCollection<StudentModel>>(state, secondTableId + ".Items") ??
                                 new ObservableCollection<StudentModel>();
                    foreach (var selectedStudentModel in selectedStudentModels)
                    {
                        if (!models.Contains(selectedStudentModel))
                        {
                            models.Add(selectedStudentModel);
                        }
                    }
                    store.Dispatch(new DataExchangeManagement.Publish
                    {
                        Id = secondTableId + ".Items",
                        Data = new ObservableCollection<StudentModel>(models)
                    });
                }));
                Publisher.Publish("UnregisterButton.Action", new Action(() =>
                {
                    var selectedStudentModels = Publisher.Get<List<StudentModel>>(store.GetState(), secondTableId + ".SelectedItems");
                    if (selectedStudentModels == null)
                    {
                        return;
                    }
                    var state = store.GetState();
                    var models = Publisher.Get<ObservableCollection<StudentModel>>(state, secondTableId + ".Items") ??
                                 new ObservableCollection<StudentModel>();
                    foreach (var selectedStudentModel in selectedStudentModels)
                    {
                        models.Remove(selectedStudentModel);
                    }
                    Publisher.Publish(secondTableId + ".Items",
                        new ObservableCollection<StudentModel>(models));
                }));
                Publisher.Publish("AutoregisterButton.Action", new Action(() =>
                {
                    autoregisterEnabled = !autoregisterEnabled;
                    Publisher.Publish("AutoregisterButton.Text",
                        autoregisterEnabled ? "Авторегистрация" : "Ручная регистрация");
                    if (autoregisterEnabled)
                    {
                        _layoutStateManagement.AttachedPluginStore.Dispatch(
                            new LayoutStateManagement.Hide { Id = "ManualAdd" });
                    }
                }));
                Publisher.Publish("ManualAdd.Action", new Action(() =>
                {
                    _layoutStateManagement.AttachedPluginStore.Dispatch(
                        new LayoutStateManagement.Hide { Id = "ManualAdd" });
                    var state = DataExchangeManagement.GetInstance().PublishedDataStore.GetState();
                    var models = Publisher.Get<ObservableCollection<StudentModel>>(state, secondTableId + ".Items") ??
                                 new ObservableCollection<StudentModel>();
                    var readStudentModel = Publisher.Get<StudentModel>(state, "LastReadStudentCard");
                    if (readStudentModel == null)
                    {
                        return;
                    }

                    var existent = GeneralDbContext.GetInstance().StudentModels.FirstOrDefault(model => model.card_id == readStudentModel.card_id);

                    if (models.Contains(existent) || models.Contains(readStudentModel))
                    {
                        return;
                    }
                    Publisher.Publish(secondTableId + ".Items", new ObservableCollection<StudentModel>(models) { existent });
                }));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachGenericView(firstTableId, "Table", FirstTab,
                        new ViewConfig { ColumnSize = 30, RowSize = 17, Row = 18, Column = 1 }, typeof(StudentModel)));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachGenericView(secondTableId, "Table", FirstTab,
                        new ViewConfig { ColumnSize = 30, RowSize = 17, Row = 18, Column = 33 }, typeof(StudentModel)));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachView("Photo", "Photo", FirstTab,
                        new ViewConfig { ColumnSize = 10, RowSize = 10, Column = 1, Row = 3 }));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachView(studentName, "Text", FirstTab,
                        new ViewConfig { ColumnSize = 40, RowSize = 2, Column = 11, Row = 3 }));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachView(isPresent, "Text", FirstTab,
                        new ViewConfig { ColumnSize = 40, RowSize = 2, Column = 11, Row = 5 }));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachView("RegisterButton", "Button", FirstTab,
                        new ViewConfig { ColumnSize = 2, RowSize = 2, Column = 29, Row = 15 }));
                _layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachView("UnregisterButton", "Button", FirstTab,
                        new ViewConfig { ColumnSize = 2, RowSize = 2, Column = 33, Row = 15 }));

                SideEffectManager.AddSideEffect<bool>("OnChangeReaderStatusAllowAutoregister", "IsReaderEnabled", isReaderEnabled =>
                {
                    if (isReaderEnabled)
                    {
                        _layoutStateManagement.AttachedPluginStore.Dispatch(
                            new LayoutStateManagement.AttachView("AutoregisterButton", "Button", FirstTab,
                                new ViewConfig { ColumnSize = 10, RowSize = 2, Column = 33, Row = 5 }));

                    }
                    else
                    {
                        _layoutStateManagement.AttachedPluginStore.Dispatch(
                            new LayoutStateManagement.Hide { Id = "AutoregisterButton" });
                    }
                });
            });
            InitSecondTab();
        }

        private void InitSecondTab()
        {
            string commonTableId = "cm";

            Publisher.Publish(commonTableId + ".TableConfig", new TableConfig<StudentGroupModel>
            {
                DefaultSortColumn = new ListViewComparer<StudentGroupModel>(new List<SortDescription>
                {
                    new SortDescription("Group.name", ListSortDirection.Descending)
                }),
                SelectOnDoubleClick = true,
                SelectOnEnter = true,
                ColumnConfigs = new Dictionary<string, ColumnConfig>
                    {
                        {
                            "Фамилия", new ColumnConfig
                            {
                                PropertyPath = "Student.last_name",
                                DefaultWidth = 100
                            }
                        },
                        {
                            "Имя",
                            new ColumnConfig
                            {
                                PropertyPath = "Student.first_name",
                                DefaultWidth = 100
                            }
                        },
                        {
                            "Отчество", new ColumnConfig
                            {
                                PropertyPath = "Student.patronymic",
                                DefaultWidth = 100
                            }
                        },
                        {
                            "Группа", new ColumnConfig
                            {
                                PropertyPath = "Group.name",
                                DefaultWidth = 100
                            }
                        }
                    }
            });
            LayoutStateManagement.GetInstance().AttachedPluginStore.Dispatch(
                new LayoutStateManagement.AttachGenericView(
                    commonTableId, "Table", SecondTab, new ViewConfig
                    {
                        Column = 1,
                        Row = 1,
                        RowSize = 34,
                        ColumnSize = 62
                    }, typeof(StudentGroupModel))
                );
        }

        private void SelectDatabase_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish()
                {
                    Id = "DataBase",
                    Data = dlg.FileName
                });
                LayoutStateManagement.GetInstance().AttachedPluginStore
                    .Dispatch(new LayoutStateManagement.RefreshAll());
            }
        }

        private void ConnectReader_Click(object sender, RoutedEventArgs e)
        {
            DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish()
            {
                Id = "IsReaderEnabled",
                Data = true
            });
        }

        private void DiconnectReader_Click(object sender, RoutedEventArgs e)
        {
            DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish()
            {
                Id = "IsReaderEnabled",
                Data = false
            });
        }

        private void TabHeader_OnMouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock block = new TextBlock
            {
                Text = "x",
                Width = 20,
                TextAlignment = TextAlignment.Right
            };
            block.MouseDown += (o, args) => { Tabs.Items.Remove(TestItem); };
            Header2.Children.Add(block);
            Grid.SetColumn(block, 1);
        }

        private void TabHeader_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Header2.Children.RemoveAt(1);
        }
    }
}