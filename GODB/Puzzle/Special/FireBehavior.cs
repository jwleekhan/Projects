using UnityEngine;

namespace Game.Puzzle
{
    public sealed class FireBehavior : ISpecialBehavior
    {
        public bool isSelectable => true;

        public bool OnCollide(PlaceableObject a, PlaceableObject b, out PlaceableObject result)
        {
            if (b.specialType == SpecialType.Fire)
            {
                result = b;
                return true;
            }
            if (a.specialType == SpecialType.Fire)
            {
                result = a;
                return true;
            }
            result = b;
            return false;
        }

        public bool OnFinishOperation(PlaceableObject obj, out PlaceableObject result)
        {
            if (obj.specialType == SpecialType.Fire)
            {
                obj.runtimeValue++;
                if (obj.runtimeValue > 1)
                {
                    result = new PlaceableObject();
                    result.specialType = SpecialType.Ash;
                    return true;
                }
            }
            result = obj;
            return true;
        }
    }
}
