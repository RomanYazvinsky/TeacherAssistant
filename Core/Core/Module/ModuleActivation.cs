using System;
using JetBrains.Annotations;

namespace TeacherAssistant.Core.Module
{
    public class ModuleActivation<T> : IModuleActivation where T : class, IModuleToken
    {
        public ModuleActivation(string id, [NotNull] T token)
        {
            this.Id = id;
            this.Token = token;
        }

        public event EventHandler Deactivated;

        public void Deactivate()
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        public string Id { get; }
        public T Token { get; }

        public IModuleToken GetToken()
        {
            return this.Token;
        }
    }

    public interface IModuleActivation
    {
        event EventHandler Deactivated;
        void Deactivate();
        string Id { get; }

        IModuleToken GetToken();
    }
}