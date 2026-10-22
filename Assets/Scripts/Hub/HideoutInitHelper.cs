using CloneZ;

namespace CloneZ
{
    public static class HideoutInitHelper
    {
        private static CharacterProfile s_LastChar;
        public static void EnsureInitialized(CharacterProfile c)
        {
            if (c == null) return;
            if (s_LastChar != c)
            {
                Hideout.InitModules(c);
                s_LastChar = c;
            }
        }
    }
}
