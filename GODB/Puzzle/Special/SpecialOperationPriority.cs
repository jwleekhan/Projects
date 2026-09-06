using UnityEngine;

namespace Game.Puzzle
{
    public static class SpecialOperationPriority
    {
        public static SpecialType CollisionFuncPriority(SpecialType type1, SpecialType type2)
        {
            if (type1 == type2) return type1;
            if (type1 > type2) return CollisionFuncPriority(type2, type1);

            // 여기부터 type1보다 type2가 큰 int값

            // Special이 None보다 우선함
            if (type1 == SpecialType.None) return type2;
            // Fire가 Ash보다 우선함
            if (type1 == SpecialType.Fire && type2 == SpecialType.Ash) return SpecialType.Fire;
            // 기본적으로 낮은 int값이 우선함 (신규 기능에 보수적)
            return type1;
        }
    }
}
