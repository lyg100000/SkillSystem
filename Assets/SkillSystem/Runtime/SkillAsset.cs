using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem.Runtime
{
    public class SkillAsset : ScriptableObject
    {
        [field:SerializeField]
        public float duration { get; private set; }
    }
}
