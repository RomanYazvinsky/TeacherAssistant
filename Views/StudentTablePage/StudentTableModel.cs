using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dao;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Footer.TaskExpandList;
using TeacherAssistant.State;

namespace TeacherAssistant.StudentTable
{
    public class StudentTableModel : AbstractModel
    {
        public class StudentVisualModel : StudentModel
        {
            private string _groupsText = null;

            public string GroupsText
            {
                get { return _groupsText ?? (_groupsText = string.Join("\t ", Groups.Select(model => model.name))); }
            }
        }

        private ListCollectionView _students = new ListCollectionView(new List<StudentVisualModel>());
        private ObservableCollection<StudentVisualModel> _studentModels;
        private StudentModel _selectedStudentModel;
        private BitmapImage _studentPhoto;
        private string _studentFilterText;
        private CommandHandler _loadPhotos;
        private IPhotoService PhotoService { get; }

        public StudentTableModel(IPhotoService photoService)
        {
            PhotoService = photoService;
        }

        public override async Task Init(string id)
        {
            _studentModels = new ObservableCollection<StudentVisualModel>(
                GeneralDbContext.Instance.StudentModels.Include(model => model.Groups).Select(model =>
                    new StudentVisualModel
                    {
                        card_uid = model.card_uid,
                        id = model.id,
                        card_id = model.card_id,
                        Groups = model.Groups,
                        first_name = model.first_name,
                        last_name = model.last_name,
                        patronymic = model.patronymic,
                        email = model.email,
                        phone = model.phone
                    }).OrderBy(model => model.last_name));
            _students = new ListCollectionView(_studentModels);
            _loadPhotos = new CommandHandler(async () =>
            {
                var taskHandler = new TaskHandler("Загрузка фото", _studentModels.Count, async actions =>
                {
                    foreach (var studentModel in _studentModels)
                    {
                        if (actions.IsCancelled())
                        {
                            actions.ConfirmCancel();
                            break;
                        }
                        await PhotoService.DownloadPhoto(studentModel.card_id).ConfigureAwait(false);
                        actions.Next();
                    }
                    actions.Complete();
                });
                Publisher.Add("TaskList", taskHandler);
                taskHandler.Start();
            });
        }

        public ListCollectionView Students
        {
            get => _students;
            set
            {
                _students = value;
                OnPropertyChanged(nameof(Students));
            }
        }


        private void UpdatePhoto(StudentModel model)
        {
            if (model == null) return;
            StudentPhoto = null;
            Task.Run(async () =>
            {
                var photoPath = await PhotoService.DownloadPhoto(model.card_id).ConfigureAwait(false);
                if (photoPath == null)
                {
                    return;
                }

                var image = await PhotoService.GetImage(photoPath).ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() => { StudentPhoto = image; },
                    DispatcherPriority.DataBind);
            });
        }

        public StudentModel SelectedStudentModel
        {
            get => _selectedStudentModel;
            set
            {
                _selectedStudentModel = value;
                OnPropertyChanged(nameof(SelectedStudentModel));
                UpdatePhoto(_selectedStudentModel);
            }
        }

        public CommandHandler LoadPhotos
        {
            get => _loadPhotos;
            set
            {
                _loadPhotos = value;
                OnPropertyChanged(nameof(LoadPhotos));
            }
        }

        public BitmapImage StudentPhoto
        {
            get => _studentPhoto;
            set
            {
                _studentPhoto = value;
                OnPropertyChanged(nameof(StudentPhoto));
            }
        }

        public string StudentFilterText
        {
            get => _studentFilterText;
            set
            {
                _studentFilterText = value;
                _studentFilterText = _studentFilterText.ToLowerInvariant();
                OnPropertyChanged(nameof(StudentFilterText));
                if (_studentFilterText != "")
                {
                    Students.Filter = o =>
                    {
                        var student = ((StudentVisualModel) o);
                        var groupsText = student.GroupsText;
                        return (student.first_name != null &&
                                student.first_name.ToLowerInvariant().Contains(_studentFilterText))
                               || (student.last_name != null &&
                                   student.last_name.ToLowerInvariant().Contains(_studentFilterText))
                               || (student.patronymic != null &&
                                   student.patronymic.ToLowerInvariant().Contains(_studentFilterText))
                               || (groupsText != null && groupsText.ToLowerInvariant().Contains(_studentFilterText));
                    };
                }
                else
                {
                    Students.Filter = null;
                }
            }
        }
    }
}