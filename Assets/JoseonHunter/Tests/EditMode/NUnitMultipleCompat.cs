using System;

namespace JoseonHunter.Tests.EditMode
{
    internal static class NUnitMultipleCompat
    {
        public static void Run(Action assertions) => assertions();
    }
}
