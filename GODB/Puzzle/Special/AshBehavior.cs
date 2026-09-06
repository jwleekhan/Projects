using UnityEngine;

namespace Game.Puzzle
{
    public sealed class AshBehavior : ISpecialBehavior
    {
        public bool isSelectable => false;

        public bool OnCollide(PlaceableObject a, PlaceableObject b, out PlaceableObject result)
        {
            if (a.specialType == SpecialType.Ash && b.specialType == SpecialType.Ash)
            {
                result = b;
                return true;
            }
            if (a.specialType == SpecialType.Ash && b.specialType == SpecialType.None)
            {
                result = b;
                result.state = StateType.Ashed;
                return true;
            }
            if (a.specialType == SpecialType.None && b.specialType == SpecialType.Ash)
            {
                result = a;
                result.state = StateType.Ashed;
                return true;
            }
            result = b;
            return false;
        }
    }
}
