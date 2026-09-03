using System;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Attribute used to render a clean dropdown selector for [SerializeReference] polymorphic types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubclassSelectorAttribute : PropertyAttribute
    {
        public bool AllowNull { get; set; } = true;

        public SubclassSelectorAttribute(bool allowNull = true)
        {
            AllowNull = allowNull;
        }
    }
}
