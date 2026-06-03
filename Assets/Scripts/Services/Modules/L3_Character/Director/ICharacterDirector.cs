using UnityEngine;

namespace RedDust.Character.Director
{
    public interface ICharacterDirector
    {
        SCharacterIntent Evaluate();
    }
}
