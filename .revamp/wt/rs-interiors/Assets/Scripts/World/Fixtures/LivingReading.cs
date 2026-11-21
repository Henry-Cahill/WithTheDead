using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The bookcase's pastimes, offered after browsing its shelves: reading a few chapters of one of the books on it
    /// (a long settle of the nerves), flicking through one of the magazines stacked at the end of a shelf (a short one),
    /// and straightening the books (a small comfort, now and then). Reading needs light to read by — daylight, a lamp
    /// that's on, the TV's glow or a torch. Every title on the shelves is somebody's well-thumbed copy, and they come
    /// round in turn.
    /// </summary>
    internal static class LivingReading
    {
        private readonly struct Book
        {
            public readonly string Title, Author, Thought;
            public Book(string title, string author, string thought) { Title = title; Author = author; Thought = thought; }
        }

        private static readonly Book[] Books =
        {
            new Book("The Lighthouse at Gull Point", "Marian Featherstone", "The vicar did it. You're almost sure."),
            new Book("Summer of the Paper Kites", "Ruth Abernathy", "Three sisters, one long summer and a great deal of lemonade. Nothing much happens, beautifully."),
            new Book("Lawnmowers, Pumps and Other Small Engines", "D. W. Kettering", "Dry as toast, and oddly soothing. You now understand carburettors."),
            new Book("A Pocket Guide to Hedge and Garden Birds", "Irene Coldwater", "You'd forgotten there were so many kinds of sparrow."),
            new Book("Murder at the Marigold Motel", "Vance Delacroix", "It was the night clerk, obviously. It still kept you guessing."),
            new Book("A Year at Hollow Creek Farm", "Thomas Ahern", "A farmer's year, one season at a time. The harvest came in, whatever else happened."),
            new Book("Grandma Ottilie's Kitchen Notebook", "Ottilie Brandt", "Forty ways with a cabbage. You read the one with the bacon twice."),
            new Book("Star Ranger and the Moons of Calyx", "Buck Halloran", "The ray guns never run out of charge. Lucky them."),
            new Book("Letters from the Slow Barge", "Agnes Merriweather", "Somebody else's slow, ordinary summer, a hundred years ago. It's lovely."),
            new Book("Ice Over Pike Lake", "Carl Rostova", "A thriller about a town cut off by the winter. Everyone makes it home in the end."),
            new Book("Captain Pryor's Book of Useful Knots", "H. L. Pryor", "You tie a bowline in your bootlace, then a sheet bend. Could come in handy."),
            new Book("Dr. Ashcombe's Promise", "Lorraine St. Clair", "A country nurse, a brooding surgeon and a wedding at the end, as there should be."),
            new Book("Steam West: How the Railroads Crossed the Plains", "Howard Penn", "Pages of timetables. The world used to run on schedules."),
            new Book("Patience, Rummy and Ninety Other Card Games", "Edna Pike", "You learn a patience game called the Clock. You'll lay it out later."),
            new Book("The Mapmaker of Saltmarsh", "Sofia Marlow", "She charts a coast that keeps changing under her. You lose an hour and don't mind."),
            new Book("The Whispering Pines", "Eleanor Gray", "A haunting tale of a forest that never sleeps."),
            new Book("Mechanics of the Mind", "Dr. Felix Hsu", "Thoughts about cognition ripple like gears."),
            new Book("Forgotten Recipes of the Old World", "Marta Delgado", "You wonder if the stew could actually work."),
            new Book("Chronicles of the Riverfolk", "Liam O'Connor", "A saga of river merchants and hidden coves."),
            new Book("The Last Lightkeeper", "Samuel Reed", "A solitary watchman's story makes you appreciate the night sky."),
        };

        private static readonly string[] Magazines =
        {
            "a dog-eared Chrome & Gasket — a two-page spread on a cherry-red pickup. Somebody circled it in biro.",
            "last spring's Valley Homemaker. “Ten Thrifty Ways With Leftover Ham.” Somebody tried number four.",
            "a Tackle Box Digest with a tide table for a coast a thousand miles from here.",
            "a Rainy Day Crosswords with the easy ones filled in, in two different hands.",
            "a Starlight Screen Gossip. Everyone in it is on a yacht, worried about nothing at all.",
            "the County Seed Catalogue. Pole beans, sweetcorn, a giant pumpkin promised by October.",
            "a July 1993 Homebrew Gazette — a spread on making your own cider in the backyard.",
            "a July 1993 Survivalist's Almanac. Tips on building a rain collector and warding off pests.",
            "a July 1993 Retro Tech Review — a two‑page look at busted radios and tin‑plate bikes.",
            "a July 1993 Wildlife Watch, issue 7. A photo essay of deer tracks in early summer snows.",
            "a December 1993 Fire‑Making Handbook. Winter tips on gathering pine boughs and coaxing a spark in the snow.",
            "a folklore pamphlet titled “Gray‑Mouth”. The legend of the hollow‑voiced specter haunting the nearby hills.",
            "a farm‑yard recipe booklet: “Grandma’s Turnip Soup”. Hearty broth with root vegetables and a pinch of thyme.",
            "a February 1993 town hall bulletin. Announcing the community meeting to discuss road repairs and winter supplies.",
            "a hunting log: “First‑light Duck Hunt, 1993”. Notes on sunrise drives, mist over the pond, and a perfect spread.",
            "a March 1993 Local Sports Digest. Highlights of the town’s winter baseball league and the upcoming snowshoe race.",
        };

        private static readonly string[] Tidying =
        {
            "You line the spines up neatly, tallest to shortest. It helps, a little.",
            "You put the books back in order — by author, this time.",
            "You square up the magazines and tuck in a loose dust jacket.",
            "You dust the shelves, wiping away a thin layer of time.",
            "You tighten a loose hinge on a battered wooden panel.",
            "You flip through the pages, checking for any missing leaves.",
            "You smooth a warped board, pressing it back flat with gentle pressure.",
            "You adjust the lamp angle to cast a warm glow over the titles.",
            "You polish the brass knob on a shelf, restoring its warm gleam.",
            "You clear a thin film of snow‑dust from the top shelf, revealing the spines beneath.",
            "You straighten a tilted family photo, aligning it with the rows of books.",
            "You tighten a loose shelf bracket, preventing a future wobble.",
            "You sweep away a faint cobweb in a corner, the dust scattering in the light.",
            "You clear the soot‑stained ashtray, a thin layer of charcoal drifting away.",
            "You straighten a crooked candle holder, setting the wick straight for the next fire.",
            "You brush the snow‑dust off an old wooden coat rack, exposing the brass hooks.",
            "You wipe the frost from the windowsill, letting a sliver of daylight slip in.",
            "You tamp down loose pine needles on the floor, keeping the walk clear for boots.",
            "You untangle a knot in the kitchen rope, giving the pantry door a smoother swing.",
        };

        private static int s_Book = -1, s_Magazine = -1, s_Tidy;
        private static float s_TidyCalmAt;

        /// <summary>The bookcase's extra actions (after "Browse the shelves").</summary>
        public static void AddActions(InteractContext ctx, List<InteractAction> into)
        {
            string dark = TooDark(ctx) ? "It's too dark to read \u2014 put a light on" : null;
            into.Add(new InteractAction("Read a book", ReadBook)
                .Timed(CloneZConfig.ReadBookSeconds, "Reading", HumanoidActivity.Read)
                .DisabledIf(dark != null, dark));
            into.Add(new InteractAction("Flick through a magazine", ReadMagazine)
                .Timed(CloneZConfig.ReadMagazineSeconds, "Flicking through a magazine", HumanoidActivity.Read)
                .DisabledIf(dark != null, dark));
            into.Add(new InteractAction("Straighten the books", Straighten));
        }

        // Daylight, a lamp (or the TV) lighting the survivor, or a torch.
        private static bool TooDark(InteractContext ctx)
        {
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod == null || tod.LightLevel >= CloneZConfig.ReadingLightLevel) return false;
            if (PlayerGear.Instance != null && PlayerGear.Instance.FlashlightOn) return false;
            return !RoomLight.IsPointLit(ctx.Chest);
        }

        private static void ReadBook(InteractContext ctx)
        {
            if (s_Book < 0) s_Book = Random.Range(0, Books.Length);
            Book b = Books[s_Book % Books.Length];
            s_Book = (s_Book + 1) % Books.Length;
            LivingFixtures.Calm(ctx, CloneZConfig.ReadBookPanicRelief);
            ctx.Notify("You read a few chapters of \u201C" + b.Title + "\u201D by " + b.Author + ". " + b.Thought);
        }

        private static void ReadMagazine(InteractContext ctx)
        {
            if (s_Magazine < 0) s_Magazine = Random.Range(0, Magazines.Length);
            string m = Magazines[s_Magazine % Magazines.Length];
            s_Magazine = (s_Magazine + 1) % Magazines.Length;
            LivingFixtures.Calm(ctx, CloneZConfig.ReadMagazinePanicRelief);
            ctx.Notify("You flick through " + m);
        }

        private static void Straighten(InteractContext ctx)
        {
            if (Time.time >= s_TidyCalmAt)
            {
                s_TidyCalmAt = Time.time + CloneZConfig.TidyCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief);
                ctx.Notify(Tidying[s_Tidy++ % Tidying.Length]);
            }
            else ctx.Notify("You straighten the books again. They were already straight.");
        }

        // Return the next tidy line without advancing the cooldown (used by other fixtures).
        internal static string NextTidyingLine()
        {
            // Cycle through Tidying safely using the shared s_Tidy counter.
            string line = Tidying[s_Tidy % Tidying.Length];
            s_Tidy = (s_Tidy + 1) % Tidying.Length;
            return line;
        }
    }
}
