using UnityEngine;

namespace Game.Puzzle
{
    public interface ISpecialBehavior
    {
        public bool isSelectable { get; }

        public bool OnCollide(PlaceableObject a, PlaceableObject b, out PlaceableObject result)
        {
            result = b;
            return false;
        }

        public bool OnFinishOperation(PlaceableObject obj, out PlaceableObject result)
        {
            result = obj;
            return true;
        }
    }
}
