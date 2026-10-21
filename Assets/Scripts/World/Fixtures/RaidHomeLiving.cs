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
            "a backyard gardening guide, with tips for winter vegetables and a reminder to mulch.",
            "a neighborhood watch bulletin, warnings about prowlers and a list of emergency numbers.",
            "a local theater program, featuring a midnight screening of a classic and a bake sale.",
            "a seasonal recipe pamphlet, \"Hearty Stew for Cold Nights\" with a handwritten note.",
"a local farming almanac, pages curled, listing winter wheat yields and a reminder to check the barn doors.",
"a church hymn sheet, the verses smudged, with a margin note: “Sing louder on Sunday.”",
"a school bulletin board notice: “Parent‑Teacher meetings postponed – snow day.”",
"a handwritten newspaper clipping titled “Winter Storm Outlook”, the ink blotched where rain ran.",
        };

        private static readonly string[] Plumping =
        {
            "You plump the cushions and set them square. It looks like somebody still lives here.",
            "You beat the dust out of the cushions and stand them up against the back. Old habits.",
            "You shake the cushions, sending a puff of old flour dust into the air; someone must've baked recently.",
            "You fluff the cushions, and a whisper of pine from the fireplace drifts over them.",
            "You straighten the cushions, noticing a crooked photo of a winter family portrait tucked behind an arm.",
            "You press the cushions firm, feeling the worn stitching that someone kept mending through the cold nights.",
"You smooth the cushions, noticing a faint lavender scent from a long‑forgotten sachet tucked inside.",
"You pat the cushions, hearing the faint creak of the sofa frame as the house settles in the cold.",
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
            new[]
            {
                "A folded flyer from the community garden, announcing a seed swap on Saturday.",
                "Hand-drawn map of the garden beds, with a doodle of a carrot.",
                "A reminder: bring your own gloves.",
            },
            new[]
            {
                "A short poem scribbled on recycled paper, titled \"Winter's Whisper\".",
                "It speaks of snow drifts and quiet streets, ending with a hopeful line.",
            },
            new[]
            {
                "An anonymous postcard with a picture of a sunrise over the hills.",
                "The back reads: \"Stay safe, keep the fire alive.\"",
                "No return address.",
            },
            new[]
            {
                "A folded flyer announcing a community potluck at the town hall, featuring a stew and live music.",
                "\"All are welcome. Bring a dish to share.\"",
                "A doodle of a ladle appears in the margin."
            },
            new[]
            {
                "A torn newspaper clipping about a recent power outage, dated Dec 5.",
                "\"The lights went out at 3pm. No restoration timeline provided.\"",
                "A coffee stain drips down the page."
            },
            new[]
            {
                "A handwritten invitation to a secret meet-up at the old mill, signed only with initials \"J.\"",
                "\"Meet me at dusk, bring the old map.\"",
                "The paper is creased and smells of tobacco."
            },
            new[]
            {
                "A weather warning posted on a bulletin board: \"Heavy snowfall expected tomorrow. Stay indoors.\"",
                "The paper flutters in the wind.",
                "Someone crossed out \"tomorrow\" and wrote \"tonight\"."
            },
            new[]
            {
                "A recipe card for \"Spiced Apple Cider\", inked with a doodle of a mug.",
                "\"Boil water, add apples, cinnamon, and sugar. Serve warm.\"",
                "A small cider stain darkens the corner."
            },
            };

private static readonly string[] NumberLines =
        {
            "Number {0}, says the enamel plate by the door. Somebody's home, once.",
            "Number {0}. A dried-out Christmas wreath still hangs under it.",
            "Number {0}, in chipped enamel, with a horseshoe nailed up over it for luck.",
            "Number {0}. Somebody's chalked a cross beside it, and a date: 12/15.",
            "Number {0}, etched in faded paint above the porch, a lone pine silhouette beside it.",
            "Number {0}, stamped on a rusted metal plate, the letters half torn away.",
            "Number {0}, scrawled in chalk on the sidewalk, washed away by a recent rain.",
            "Number {0}, the frost‑bitten metal plate is dulled by snow and ice.",
            "Number {0}, a handwritten note tacked to the side reads \"Home Sweet Home\".",
            "Number {0}, cracked and leaning, vines creep over its edge, a sign of recent abandonment.",
            "Number {0}, etched with a rabbit’s foot glyph, a local superstition for safe returns.",
            "Number {0}, beside a rusted lantern that still flickers in the dusk.",
            "Number {0}, a weather‑worn wooden post leans against the porch, its paint peeled by relentless wind.",
            "Number {0}, a faded photograph of a smiling family clings to the back of the door, edges curled with age.",
            "Number {0}, a rusted nail holds together a cracked plank, a hurried fix after a storm.",
            "Number {0}, a painted barn owl perched above, believed to shield the home from wandering spirits.",
            "Number {0}, a tattered dog collar lies tangled in the doorway, forgotten but still warm from recent use.",
            "Number {0}, a tiny scrap of paper reads \"One day the snow will melt and light return,\" tucked beneath the plate.",
"Number {0}, a faded grocery list is pinned to the post, the ink smeared by winter rain.",
"Number {0}, a child’s drawing of a snowman hangs from a nail, charcoal lines softened by frost.",
"Number {0}, an old wind‑chime clatters on a gust, its metal rods dented from years of neglect.",
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
