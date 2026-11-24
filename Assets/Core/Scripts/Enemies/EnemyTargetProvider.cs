using System.Collections.Generic;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    public abstract class EnemyTargetProvider : MonoBehaviour
    {
        /// <param name="results">
        /// Collection that receives candidate targets.
        /// </param>
        public abstract void GetTargets(
            List<Transform> results);
    }
}