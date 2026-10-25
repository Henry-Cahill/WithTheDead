namespace CloneZ
{
    /// <summary>
    /// Names for a survivor nobody named (the wave-survival survivor, a runner made from an old extracted kit): a first
    /// and a last name of the sort you'd hear round Bardstown and the hollers east of it in the winter of 1993.
    /// </summary>
    public static class SurvivorNames
    {
        private static readonly string[] s_First =
        {
            "Dale", "Earl", "Wayne", "Randy", "Ricky", "Darrell", "Tommy", "Larry", "Gary", "Jimmy", "Bobby", "Danny",
            "Terry", "Kenny", "Roger", "Dwayne", "Curtis", "Travis", "Shane", "Ronnie", "Lonnie", "Glenn", "Clint", "Rusty",
            "Tammy", "Rhonda", "Brenda", "Debbie", "Sherry", "Connie", "Donna", "Teresa", "Angie", "Crystal", "Misty", "Peggy",
            "Darlene", "Wanda", "Lori", "Pam", "Vicky", "Carla", "Becky", "Jolene", "Ruby", "June", "Shelby", "Dana",
        };

        private static readonly string[] s_Last =
        {
            "Mattingly", "Hayden", "Ballard", "Greenwell", "Lanham", "Cecil", "Hagan", "Mudd", "Clark", "Hardin", "Thompson",
            "Simpson", "Howard", "Caudill", "Combs", "Hensley", "Sizemore", "Napier", "Slone", "Mullins", "Collins", "Begley",
            "Bowling", "Fugate", "Noble", "Ritchie", "Couch", "Gentry", "Riddle", "Pennington", "Blevins", "Whitaker", "Akers",
            "Tackett", "Bledsoe", "Gibson", "Hall", "Adams", "Turner", "Fields", "Mays",
        };

        // Its own generator: the seeded worlds and loot draw on UnityEngine.Random, and naming someone mustn't shift them.
        private static readonly System.Random s_Rng = new System.Random(System.Guid.NewGuid().GetHashCode());

        /// <summary>A fresh "First Last" name.</summary>
        public static string Roll() => s_First[s_Rng.Next(s_First.Length)] + " " + s_Last[s_Rng.Next(s_Last.Length)];
    }
}
