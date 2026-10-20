using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The workings of a workplace fridge-freezer — the National Guard post's kitchenette fridge, the clinic's medical
    /// fridge — whose doors and compartments are <see cref="StorageFixture"/>s built by the kit
    /// (<see cref="RaidBuilding.WorkFridge"/>): the cool-white lamp that comes on while the fridge door is open and there's
    /// power, the cold to feel (or, at the clinic, the thermometer to read), and what's stuck to the door. The town's power
    /// holds for the whole of a raid, so it doesn't poll the grid: the lamp follows the door.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicFridge : MonoBehaviour
    {
        private static readonly string[] GuardNotes =
        {
            "A duty roster in marker pen. Somebody has struck out tonight's names and written EVERYONE across the lot.",
            "\u201CWHOEVER KEEPS TAKING MY PUDDING CUPS: I KNOW WHO YOU ARE. \u2014 SGT. OKAFOR\u201D",
            "A pizza menu from the place on Main, ringed in red pen: OPEN LATE!",
            "A photocopied sheet, COLD-WEATHER INJURIES: KNOW THE SIGNS. Somebody has drawn a moustache on the frostbitten foot.",
            "A child's drawing of a soldier under a big yellow sun. MY DAD, it says, in crayon.",
            "A torn memo dated Dec 1993: \u201CSupply run overdue – send a squad.\u201D",
            "A doodle of a snowflake with the words \u201CWinter patrol, 12/93\u201D in marker.",
            "A grocery list scribbled on a napkin, dated Dec 1993, includes beans, jam, and extra rations.",
            "A faded newspaper clipping from Dec 1993: \u201CLocal base reports morale dip.\u201D",
        };

        private static readonly string[] ClinicNotes =
        {
            "A temperature log on a clipboard, ticked off every four hours. The last tick is at eight o'clock tonight.",
            "A printed sign: VACCINES AND INSULIN ONLY \u2014 NO FOOD. Under it, in biro: \u201CThis means your yogurt, Gary.\u201D",
            "A pharmacy order form, half filled in. MORE GLOVES, somebody's written in the margin, three times over.",
            "A sticky note: \u201CDoor seal's going. Maintenance called Tues. Nobody came.\u201D",
            "A cracked emergency chart, dated Dec 1993: \u201CCold exposure risk – monitor vitals.\u201D",
            "A handwritten note: \u201CStock low on bandages – order ASAP.\u201D",
            "A marginal doodle: \u201CDecember 1993 – patient intake up 12%\u201D",
            "A torn prescription pad entry, Dec 1993: \u201CIV antibiotics – keep refrigerated.\u201D",
        };

        private static readonly Color LampColour = new Color(0.86f, 0.93f, 1f);

        private StorageFixture _fridge, _freezer;
        private RoomLight _lamp;
        private Renderer _lens;
        private bool _medical, _lampOn;
        private int _note;
        private Action<InteractContext> _feelFridge, _feelFreezer, _readNote;

        /// <summary>The lamp's colour (for the kit's light).</summary>
        internal static Color Lamp => LampColour;

        private void Awake()
        {
            _feelFridge = c => c.Notify(ColdReport(false));
            _feelFreezer = c => c.Notify(ColdReport(true));
            _readNote = ReadNote;
        }

        /// <summary>Hands over the lamp's lens and light (both off), and whether it's the clinic's medical fridge.</summary>
        internal void Init(Renderer lens, RoomLight lamp, bool medical)
        {
            _lens = lens;
            _lamp = lamp;
            _medical = medical;
        }

        /// <summary>Hands over the two compartments, once they're built.</summary>
        internal void Bind(StorageFixture fridge, StorageFixture freezer)
        {
            _fridge = fridge;
            _freezer = freezer;
        }

        /// <summary>The fridge compartment's extra actions.</summary>
        internal void FridgeActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction(_medical ? "Read the thermometer" : "Feel how cold it is", _feelFridge));
            into.Add(new InteractAction(_medical ? "Read what's taped to the door" : "Read the notes on the door", _readNote));
        }

        /// <summary>The freezer's extra action.</summary>
        internal void FreezerActions(InteractContext ctx, List<InteractAction> into)
            => into.Add(new InteractAction("Feel how cold it is", _feelFreezer));

        /// <summary>The fridge door opened or closed: the lamp follows it, if there's power to light it.</summary>
        internal void DoorMoved(bool open)
        {
            bool on = open && PowerGrid.HousePowered;
            if (on == _lampOn) return;
            _lampOn = on;
            if (_lamp != null) _lamp.On = on;
            if (_lens != null) _lens.enabled = on;
        }

        private string ColdReport(bool freezer)
        {
            string reason = PowerGrid.NoPowerReason;
            if (reason != null) return "It's no colder than the room in here \u2014 " + LivingFixtures.Lower(reason) + ".";
            if (freezer) return "Frost nips at your fingertips. Anything in here will keep for a good long while.";
            return _medical
                ? "The dial thermometer clipped to the shelf reads four degrees. Whatever's in here is still good."
                : "It's cold inside, and it hums to itself. Whatever's in here will keep.";
        }

        private void ReadNote(InteractContext ctx)
        {
            string[] notes = _medical ? ClinicNotes : GuardNotes;
            ctx.Notify(notes[_note % notes.Length]);
            _note = (_note + 1) % notes.Length;
        }
    }

    /// <summary>
    /// What's on the bookcases of the town's workplaces, offered after browsing their shelves — the Guard post commander's
    /// manuals, regimental history and dog-eared paperbacks; the clinic doctor's reference books and journals. Reading a few
    /// chapters settles the nerves for a long while, leafing through something for a short one, and squaring the books up is
    /// a small comfort now and then, as at the home's bookcase (<see cref="LivingReading"/>); like it, reading needs light
    /// to read by — a lamp that's on, a lit room or a torch.
    /// </summary>
    internal static class CivicReading
    {
        /// <summary>Whose shelves they are.</summary>
        internal enum Shelf { Army, Clinic }

        private readonly struct Book
        {
            public readonly string Title, Author, Thought;
            public Book(string title, string author, string thought) { Title = title; Author = author; Thought = thought; }
        }

        private static readonly Book[] ArmyBooks =
        {
            new Book("Winter Bivouac: Small Units in Cold Weather", "the Department of the Army", "Dig in, keep dry, change your socks. You check your socks."),
            new Book("The Bluegrass Rifles: A Regimental History", "Col. (Ret.) Amos T. Beckwith", "Three wars and a great many floods. They always came home, or nearly all of them."),
            new Book("Six Riders to Sundown", "Clay Merriman", "A western with a cracked spine. The sheriff saves the town. Of course he does."),
            new Book("Map Reading and Land Navigation", "the Training Command", "Declination, back bearings, pace counts. You could walk out of this county with this."),
            new Book("The Cold Front", "Mitchell Garr", "A submarine thriller somebody left half read. You finish it for them. The captain gets everyone home."),
            new Book("The Sergeant's Book of Drill", "the Training Command", "Dress right, dress. There's comfort in a world where everyone knows where to stand."),
        };

        private static readonly Book[] ClinicBooks =
        {
            new Book("The Country Practitioner's Handbook", "Dr. H. Galloway", "Croup, colic, chilblains, fevers. You read the chapter on fevers twice and wish you hadn't."),
            new Book("Wounds, Burns and Their Dressing", "Margaret O. Pell, R.N.", "Clean it, close it, cover it, watch it. You'll remember that."),
            new Book("Pocket Formulary, 1992 Edition", "the County Medical Society", "Doses, interactions, warnings. You commit the painkillers to memory."),
            new Book("Anatomy for Nurses, Illustrated", "Edith and Paul Stansbury", "Careful, calm drawings. Everything in its place, the way a body is meant to be."),
            new Book("A Practitioner's Garden", "Dr. Lena Vos", "Somebody's hobby: herbs, borders, a chapter on overwintering roses. A different world."),
        };

        private static readonly string[] ArmyLeaflets =
        {
            "the post's newsletter: a bake sale, a lost beagle, a list of birthdays for March.",
            "a field manual on vehicle maintenance, every page thumbed grey at the corners.",
            "a pocket diary from last year, the paydays circled and every weekend crossed off in red.",
            "a safety notice: wear helmets in the workshop, and keep exits clear.",
            "a volunteer flyer: join the weekend patrol, extra rations promised.",
            "a lost‑pet poster: missing terrier, answer with a photo and a bag of kibble.",
            "a weather alert: blizzard expected Thursday, stock warm blankets and fuel.",
        };

        private static readonly string[] ClinicLeaflets =
        {
            "the county medical bulletin. A flu-shot schedule for next month, a retirement, a recipe for bran muffins.",
            "a drug company brochure, all smiling grandparents and a free pen offer.",
            "a nursing journal with the crossword done in a neat hand and one answer left blank.",
        };

        private static readonly string[] Tidying =
        {
            "You square the spines up along the shelf edge. It helps, a little.",
            "You put the books back in order, tallest to shortest.",
            "You stack the loose papers and tuck them in at the end of the shelf.",
            "You dust the lower shelves; the quiet scrape feels like a sigh of relief.",
            "You align the bookmarks, their tiny tabs forming a neat row of colors.",
            "You straighten the scattered pens, their caps clicking into place.",
            "You slide the index cards back into their holder, the soft thud echoing in the quiet room.",
            "You smooth the worn carpet runner, feeling the fibers settle under your hand."
        };

        private static int s_Army = -1, s_Clinic = -1, s_Leaflet, s_Tidy;
        private static float s_TidyCalmAt;
        private static Action<InteractContext> s_ReadArmy, s_ReadClinic, s_LeafArmy, s_LeafClinic, s_Straighten;

        /// <summary>The Guard post bookcase's extra actions (after "Browse the shelves").</summary>
        public static void AddArmyActions(InteractContext ctx, List<InteractAction> into) => Add(ctx, into, Shelf.Army);

        /// <summary>The clinic bookcase's extra actions.</summary>
        public static void AddClinicActions(InteractContext ctx, List<InteractAction> into) => Add(ctx, into, Shelf.Clinic);

        private static void Add(InteractContext ctx, List<InteractAction> into, Shelf shelf)
        {
            if (s_Straighten == null)
            {
                s_ReadArmy = c => Read(c, ArmyBooks, ref s_Army);
                s_ReadClinic = c => Read(c, ClinicBooks, ref s_Clinic);
                s_LeafArmy = c => Leaf(c, ArmyLeaflets);
                s_LeafClinic = c => Leaf(c, ClinicLeaflets);
                s_Straighten = Straighten;
            }
            bool army = shelf == Shelf.Army;
            string dark = TooDark(ctx) ? "It's too dark to read \u2014 put a light on" : null;
            into.Add(new InteractAction(army ? "Read a few chapters" : "Read up in a reference book", army ? s_ReadArmy : s_ReadClinic)
                .Timed(CloneZConfig.ReadBookSeconds, "Reading", HumanoidActivity.Read)
                .DisabledIf(dark != null, dark));
            into.Add(new InteractAction(army ? "Leaf through something" : "Leaf through a journal", army ? s_LeafArmy : s_LeafClinic)
                .Timed(CloneZConfig.ReadMagazineSeconds, "Leafing through", HumanoidActivity.Read)
                .DisabledIf(dark != null, dark));
            into.Add(new InteractAction("Straighten the books", s_Straighten));
        }

        // Daylight, a lamp or a lit room round the survivor, or a torch.
        private static bool TooDark(InteractContext ctx)
        {
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod == null || tod.LightLevel >= CloneZConfig.ReadingLightLevel) return false;
            if (PlayerGear.Instance != null && PlayerGear.Instance.FlashlightOn) return false;
            return !RoomLight.IsPointLit(ctx.Chest);
        }

        private static void Read(InteractContext ctx, Book[] books, ref int next)
        {
            if (next < 0) next = UnityEngine.Random.Range(0, books.Length);
            Book b = books[next % books.Length];
            next = (next + 1) % books.Length;
            LivingFixtures.Calm(ctx, CloneZConfig.ReadBookPanicRelief);
            ctx.Notify("You read a few chapters of \u201C" + b.Title + "\u201D by " + b.Author + ". " + b.Thought);
        }

        private static void Leaf(InteractContext ctx, string[] leaflets)
        {
            string s = leaflets[s_Leaflet++ % leaflets.Length];
            LivingFixtures.Calm(ctx, CloneZConfig.ReadMagazinePanicRelief);
            ctx.Notify("You leaf through " + s);
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
