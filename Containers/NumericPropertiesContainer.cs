using System.Collections;
using System.Collections.Generic;

namespace TeacherAssistant.ComponentsImpl
{
    public class NumericPropertiesContainer : SafeDictionary<int>
    {
        public override int GetDefault(string key)
        {
            return 0;
        }
    }
}