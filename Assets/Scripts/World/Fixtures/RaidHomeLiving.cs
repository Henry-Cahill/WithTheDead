using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The small pastimes and the paperwork of the raid town's houses: plumping the sofa's cushions, flicking through the
    /// magazines left on the coffee table (it needs light to read by, like the starter home's bookcase), what's come in
    /// each house's mailbox while nobody was home to read it, and the line its house-number plate gives. All of it the
    /// town's own: a Kentucky December, the week the county was emptied.
    /// </summary>
    internal static class RaidHomeLiving
    {
        private static readonly string[] Magazines =
        {
            "a TV listings guide for the week of December 13th. Somebody's circled the Christmas specials in red.",
            "an outdoors monthly. Deer season, a map of the public land up the Cumberland, and an advert for thermal socks.",
            "a homemaker's magazine: \u201CTwelve Cookie Swaps for the Holidays\u201D and a quiz you don't finish.",
            "a pickup buyer's guide, dog-eared at the four-by-fours. The prices make you laugh out loud.",
            "a church bulletin that's done duty as a coaster, a coffee ring over the hymn numbers.",
            "a kids' puzzle book with the word searches done in crayon and the hard ones left blank.",
        };

        private static readonly string[] Plumping =
        {
            "You plump the cushions and set them square. It looks like somebody still lives here.",
            "You beat the dust out of the cushions and stand them up against the back. Old habits.",
        };

        // The post that came to the town's houses in its last days, a delivery at a time (all original).
        private static readonly string[][] Letters =
        {
            new[]
            {
                "A Christmas card with a robin on it, the envelope addressed in careful capitals.",
                "\u201CMerry Christmas to you all! We'll see you on the 23rd if the roads are clear. Love, Aunt Dot & Ray.\u201D",
                "A photo of two grinning kids in Santa hats is tucked in the fold.",
            },
            new[]
            {
                "Cumberland Valley Power & Light: the December statement. Amount due: $61.40.",
                "Stamped across the top in red: SERVICE INTERRUPTIONS POSSIBLE \u2014 SEE ENCLOSED.",
                "The enclosure asks customers to keep their phone lines free for emergency calls. Nothing else.",
            },
            new[]
            {
                "A buff envelope from the county's emergency management office. OFFICIAL NOTICE.",
                "All residents within the town limits are to report to the Guard post on Main Street. ONE bag per person.",
                "Pets cannot be taken to the assembly point. Leave them food and water.",
                "Across the top, in pencil: \u201CWE WENT. \u2014 R.\u201D",
            },
            new[]
            {
                "A letter with an army post office address, the handwriting cramped and hurried.",
                "\u201CMom \u2014 they've cancelled all leave and nobody will say why. Don't believe the radio when it says it's under control.\u201D",
                "\u201CStay inside and keep the doors locked. I love you. \u2014 Danny\u201D",
            },
            new[]
            {
                "The Laurel Fork Baptist newsletter, folded in three.",
                "The Christmas pageant is CANCELLED until further notice. Please keep the Hollis family in your prayers.",
                "On the back, the choir rota for a Sunday that never came.",
            },
            new[]
            {
                "A sale flyer from Hargrove's Hardware & Feed: WINTER CLEARANCE \u2014 EVERYTHING MUST GO!",
                "Snow shovels, rock salt and kerosene heaters, all circled in ballpoint.",
                "Somebody was getting ready for a hard winter. Not this one.",
            },
            new[]
            {
                "A reminder card from the clinic: an appointment on the 20th, 10:15 a.m.",
                "Somebody's crossed the time out and written over it in red: CLOSED \u2014 GO TO THE TENT.",
            },
            new[]
            {
                "No stamp: a note on lined paper, pushed in by hand.",
                "\u201CBill \u2014 your dog's been barking two days straight. I've taken him in at ours. Come get him when you're back. \u2014 Earl, next door\u201D",
            },
            new[]
            {
                "A card from the county library: two books overdue. Fines to date: 40 cents.",
                "\u201CHome Canning for Beginners\u201D and a paperback western. Somebody meant to bring them back.",
            },
            new[]
            {
                "Junk mail: YOU MAY ALREADY HAVE WON ONE MILLION DOLLARS!",
                "The name's printed down the page in three different typefaces.",
                "You're fairly sure nobody's won anything.",
            },
        };

        private static readonly string[] NumberLines =
        {
            "Number {0}, says the enamel plate by the door. Somebody's home, once.",
            "Number {0}. A dried-out Christmas wreath still hangs under it.",
            "Number {0}, in chipped enamel, with a horseshoe nailed up over it for luck.",
            "Number {0}. Somebody's chalked a cross beside it, and a date: 12/15.",
        };

        private static int s_Magazine = -1, s_Plump;
        private static float s_MagazineCalmAt, s_PlumpCalmAt;

        /// <summary>The sofa cushions' extra action (after the search down the back of them).</summary>
        public static void AddCushionActions(InteractContext ctx, List<InteractAction> into)
            => into.Add(new InteractAction("Plump the cushions", Plump));

        /// <summary>The coffee table's extra action (after putting things on it): a magazine from the pile.</summary>
        public static void AddCoffeeTableActions(InteractContext ctx, List<InteractAction> into)
        {
            string dark = TooDark(ctx) ? "It's too dark to read \u2014 put a light on" : null;
            into.Add(new InteractAction("Flick through a magazine", ReadMagazine)
                .Timed(CloneZConfig.ReadMagazineSeconds, "Flicking through a magazine", HumanoidActivity.Read)
                .DisabledIf(dark != null, dark));
        }

        /// <summary>A house's post: two or three of the town's last deliveries, none twice.</summary>
        public static string[][] Post(System.Random rng)
        {
            int count = 2 + (rng.NextDouble() < 0.4 ? 1 : 0), used = 0;
            var post = new string[count][];
            for (int i = 0; i < count; i++)
            {
                int pick = rng.Next(Letters.Length);
                while ((used & (1 << pick)) != 0) pick = (pick + 1) % Letters.Length;
                used |= 1 << pick;
                post[i] = Letters[pick];
            }
            return post;
        }

        /// <summary>What a raid house's number plate says about it.</summary>
        public static string NumberLine(string number, System.Random rng)
            => string.Format(NumberLines[rng.Next(NumberLines.Length)], number);

        // Daylight, a lamp (or the TV) lighting the survivor, or a torch.
        private static bool TooDark(InteractContext ctx)
        {
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod == null || tod.LightLevel >= CloneZConfig.ReadingLightLevel) return false;
            if (PlayerGear.Instance != null && PlayerGear.Instance.FlashlightOn) return false;
            return !RoomLight.IsPointLit(ctx.Chest);
        }

        private static void ReadMagazine(InteractContext ctx)
        {
            if (s_Magazine < 0) s_Magazine = Random.Range(0, Magazines.Length);
            string m = Magazines[s_Magazine % Magazines.Length];
            s_Magazine = (s_Magazine + 1) % Magazines.Length;
            if (Time.time >= s_MagazineCalmAt)
            {
                s_MagazineCalmAt = Time.time + CloneZConfig.MagazineCalmCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.ReadMagazinePanicRelief);
            }
            ctx.Notify("You flick through " + m);
        }

        private static void Plump(InteractContext ctx)
        {
            if (Time.time >= s_PlumpCalmAt)
            {
                s_PlumpCalmAt = Time.time + CloneZConfig.TidyCooldown;
                LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief);
                ctx.Notify(Plumping[s_Plump++ % Plumping.Length]);
            }
            else ctx.Notify("The cushions are as plump as they're going to get.");
        }
    }
}
