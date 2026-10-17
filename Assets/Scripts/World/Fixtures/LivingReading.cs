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
        };

        private static readonly string[] Magazines =
        {
            "a dog-eared Chrome & Gasket \u2014 a two-page spread on a cherry-red pickup. Somebody circled it in biro.",
            "last spring's Valley Homemaker. \u201CTen Thrifty Ways With Leftover Ham.\u201D Somebody tried number four.",
            "a Tackle Box Digest with a tide table for a coast a thousand miles from here.",
            "a Rainy Day Crosswords with the easy ones filled in, in two different hands.",
            "a Starlight Screen Gossip. Everyone in it is on a yacht, worried about nothing at all.",
            "the County Seed Catalogue. Pole beans, sweetcorn, a giant pumpkin promised by October.",
        };

        private static readonly string[] Tidying =
        {
            "You line the spines up neatly, tallest to shortest. It helps, a little.",
            "You put the books back in order \u2014 by author, this time.",
            "You square up the magazines and tuck in a loose dust jacket.",
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
    }
}
