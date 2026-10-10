using UnityEngine;

public sealed class MooCharacter
{
    public string Name, Looks, Tagline, Special, SpecialInfo;
    // The Kenney mini character she's played by, and the colormap columns its hair is painted from.
    public string Model;
    public int[] HairSwatches;
    public Color Hair, Eyes, Shirt;
    public int HairStyle, MaxGiggles, Power;
    public float Speed, Cooldown;
}

public sealed class MooArea
{
    public string Name, Subtitle, Cow;
    public string[] Map, Signs, Rescue;
    public Color Ground, Grass, Wall, Sky, Sun;
    public float Ambient;
    public int Critter = -1;
}

public static class MooData
{
    public const int HatGoal = 12;
    public static readonly string[] CritterNames = { "bunny", "frog", "raccoon" };
    public static readonly string[] CritterSpeedNames = { "hoppy", "splashy", "sneaky" };
    public static readonly float[] CritterSpeed = { 3.0f, 2.5f, 3.4f };
    public static readonly int[] CritterHp = { 2, 2, 3 };

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }

    public static readonly MooCharacter[] Characters =
    {
        new MooCharacter
        {
            Name = "Kaite", Looks = "Blonde hair · blue eyes",
            Tagline = "Sunny, brave, and always first to yell \"Let's moo-ve!\"",
            Special = "Sunshine Spin", SpecialInfo = "Twirls so fast that everyone nearby gets tickled.",
            Hair = Hex("f2d16b"), Eyes = Hex("2f7fd8"), Shirt = Hex("7fc8f8"),
            HairStyle = 0, Model = "character-female-b", HairSwatches = new[] { 11 }, MaxGiggles = 5, Power = 1, Speed = 5.6f, Cooldown = 4f
        },
        new MooCharacter
        {
            Name = "Laura", Looks = "Brunette hair · brown eyes",
            Tagline = "Quick as a calf on roller skates. The fastest boots on the farm.",
            Special = "Chocolate-Milk Dash", SpecialInfo = "Zooms forward in a blur and bonks critters out of the way.",
            Hair = Hex("5b3a23"), Eyes = Hex("6b3e1f"), Shirt = Hex("f49ac2"),
            HairStyle = 1, Model = "character-female-e", HairSwatches = new[] { 1 }, MaxGiggles = 4, Power = 1, Speed = 7f, Cooldown = 2.5f
        },
        new MooCharacter
        {
            Name = "Grace", Looks = "Auburn hair · brown eyes",
            Tagline = "Small but mighty. She tosses hay bales like pillows.",
            Special = "Hay-Bale Toss", SpecialInfo = "Bowls a rolling hay bale that bowls over critters.",
            Hair = Hex("b8452a"), Eyes = Hex("6b3e1f"), Shirt = Hex("8fd694"),
            HairStyle = 2, Model = "character-female-d", HairSwatches = new[] { 11 }, MaxGiggles = 4, Power = 2, Speed = 5.2f, Cooldown = 3f
        },
        new MooCharacter
        {
            Name = "Audrey", Looks = "Light brown hair · hazel eyes",
            Tagline = "Kind and cozy. Her songs could make a grumpy goat yawn.",
            Special = "Moo-sic Lullaby", SpecialInfo = "Sings a sleepy song. Critters nap and goats get dizzy.",
            Hair = Hex("a47c56"), Eyes = Hex("8a7f3c"), Shirt = Hex("c9a7f0"),
            HairStyle = 3, Model = "character-female-f", HairSwatches = new[] { 13, 11 }, MaxGiggles = 6, Power = 1, Speed = 5.4f, Cooldown = 6f
        },
    };

    // Legend: # wall, T tree, o rock, w water, ~ mud, R barn, S sign, N Bessie, q confused cow,
    // C cow to rescue, c critter, G goat, B hay bale, D log gate, k golden acorn, m milk, $ moo-nie,
    // P start, E exit to barn, 1-4 barn gates.
    public static readonly MooArea[] Areas =
    {
        new MooArea
        {
            Name = "Moo-ville Barnyard", Subtitle = "Home sweet home (now with 100% more quacking)",
            Ground = Hex("8fbf5a"), Grass = Hex("a6d16b"), Wall = Hex("c99a5b"), Sky = Hex("bfe6ff"), Sun = Hex("fff2d6"), Ambient = .55f,
            Map = new[]
            {
                "##########2#########",
                "#T...RRRR........T.#",
                "#....RRRR...q......#",
                "#....RRRR..........#",
                "#..q.....P.....$...#",
                "1........N.........3",
                "#...S..........q...#",
                "#..........m.......#",
                "#T......$.......S.T#",
                "#########4##########",
            },
            Signs = new[]
            {
                "Moo-ville Barn. Established a really, really long time ago. No goats allowed. (Maybe we should fix that.)",
                "Today's forecast: sunny, with a 100% chance of cows.",
            },
        },
        new MooArea
        {
            Name = "Clover Meadow", Subtitle = "Where the bunnies are bouncy and the clover is clover-y",
            Cow = "Buttercup", Critter = 0,
            Ground = Hex("7fbf4f"), Grass = Hex("b5e07a"), Wall = Hex("4f8f3a"), Sky = Hex("c9ecff"), Sun = Hex("fff6dd"), Ambient = .55f,
            Map = new[]
            {
                "########################",
                "#E.....T.......c.....T.#",
                "#P.S.....$........c....#",
                "#......###....TT.......#",
                "#..c...#m#....TT...$...#",
                "#......#.#.............#",
                "#.....................T#",
                "#..T.....c....S....c...#",
                "#.......$.....###......#",
                "#..m..........#C#...c..#",
                "#T...........$.........#",
                "########################",
            },
            Signs = new[]
            {
                "Clover Meadow! {Tickle} to tickle. {Special} for your special move. Milk bottles refill your giggles!",
                "Fun fact: bunnies are 98% fluff and 2% wiggle.",
            },
            Rescue = new[]
            {
                "Buttercup|Oh! Hello! I was chasing the bell piece, and then... clover. So. Much. Clover.",
                "Buttercup|Here, take it! It's shiny and it keeps going BONG at me.",
                "|You got Cowbell Piece 1 of 3!",
            },
        },
        new MooArea
        {
            Name = "Mudpuddle Marsh", Subtitle = "Squelchy, splashy, and slightly smelly",
            Cow = "Moo-donna", Critter = 1,
            Ground = Hex("6f9a55"), Grass = Hex("8fb86a"), Wall = Hex("5c7a3f"), Sky = Hex("cfe3d8"), Sun = Hex("f4f1d8"), Ambient = .5f,
            Map = new[]
            {
                "########################",
                "#E~~~....c.....~~~~....#",
                "#P.~~..S....m....~~.c..#",
                "#....~~.....B.......$..#",
                "#..c.~~...........B....#",
                "#.......$.....~~~......#",
                "#wwwwwwwwwwwwwwwwwwwwww#",
                "#....c.......~~~.......#",
                "#..T.....$.......c..T..#",
                "#.....m.....S.......C..#",
                "#T....................T#",
                "########################",
            },
            Signs = new[]
            {
                "Mudpuddle Marsh. Mud is slippery! Push a hay bale into the water to build a bridge. Stuck? Walk out and back in to reset the bales.",
                "Frogs say ribbit. Ducks say quack. Confused cows say BOTH.",
            },
            Rescue = new[]
            {
                "Moo-donna|Darling! You crossed the marsh! My hooves are simply COVERED in mud.",
                "Moo-donna|Take this bell piece. It clashes with my spots anyway.",
                "|You got Cowbell Piece 2 of 3!",
            },
        },
        new MooArea
        {
            Name = "Moonberry Woods", Subtitle = "Twinkly, tangly, and full of sneaky raccoons",
            Cow = "Sir Moos-a-Lot", Critter = 2,
            Ground = Hex("3f6b4f"), Grass = Hex("5a8a66"), Wall = Hex("2f5a46"), Sky = Hex("2b3358"), Sun = Hex("b9c3ff"), Ambient = .38f,
            Map = new[]
            {
                "##########################",
                "#E.TT....c....TT.....k..T#",
                "#P.TT.........TT.........#",
                "#..TT..TTTTT..TT..TTTTT..#",
                "#......T...T.....c...T...#",
                "#..S...T.m.T..TT.....T.$.#",
                "#TTTT..T...T..TT..TTTT...#",
                "#......TT.TT..........c..#",
                "#..c.............TTTDTTTT#",
                "#T..$...TT.TT....T.....T.#",
                "#.......T...T..S.T..C..m.#",
                "#T....m.T.$.T....T.......#",
                "##########################",
            },
            Signs = new[]
            {
                "Moonberry Woods. A golden acorn opens log gates. Raccoons are sneaky, so tickle fast!",
                "Lost? Follow the fireflies. Or don't. They are terrible at directions.",
            },
            Rescue = new[]
            {
                "Sir Moos-a-Lot|Huzzah! A hero! I've been playing Pong with the raccoons out here. They cheat.",
                "Sir Moos-a-Lot|The final bell piece is yours. Now go teach that goat some manners... nicely!",
                "|You got Cowbell Piece 3 of 3!",
            },
        },
        new MooArea
        {
            Name = "Grumbleweed's Hilltop", Subtitle = "Grumpy goat territory. Keep out. (Please?)",
            Ground = Hex("a4b86a"), Grass = Hex("c4d38a"), Wall = Hex("8a8478"), Sky = Hex("ffd9b8"), Sun = Hex("ffe2c2"), Ambient = .5f,
            Map = new[]
            {
                "####################",
                "#E.................#",
                "#P...o........o....#",
                "#..................#",
                "#........G.........#",
                "#..m...........m...#",
                "#.....o......o.....#",
                "#..................#",
                "#........$.........#",
                "####################",
            },
            Signs = new string[0],
        },
    };

    public static readonly string[] GateNames = { "", "Clover Meadow", "Mudpuddle Marsh", "Moonberry Woods", "Grumbleweed's Hilltop" };
    public static readonly string[] Cows = { "Buttercup", "Moo-donna", "Sir Moos-a-Lot" };
    public static readonly Vector2Int[] HubCowSpots = { new Vector2Int(10, 6), new Vector2Int(12, 6), new Vector2Int(6, 5) };

    public static readonly string[] Intro =
    {
        "Bessie|Oh, thank goodness you're here, {name}! Something UDDERLY terrible has happened!",
        "Bessie|Grumbleweed the Goat swiped the Golden Cowbell right off the barn!",
        "Bessie|Without it, the cows forgot how to moo. Listen...",
        "Confused Cow|QUACK! ...Wait. That's not right.",
        "Bessie|Buttercup, Moo-donna, and Sir Moos-a-Lot each chased a piece of the bell and got lost.",
        "Bessie|Find them, bring back all three pieces, and we'll have a little chat with that grumpy goat.",
        "Bessie|The critters out there are mischievous but harmless. {Tickle} to tickle them until they giggle away. {Special} for your special move!",
        "Bessie|Start with the west gate to Clover Meadow. Off you go, partner!",
    };

    public static readonly string[] Hints =
    {
        "Clover Meadow is through the west gate. Buttercup is probably napping in the clover.",
        "The north gate is open! Mudpuddle Marsh is soggy, so push hay bales into the water to make a bridge.",
        "The east gate leads to Moonberry Woods. Find the golden acorn. It opens locked log gates.",
        "All three pieces! The south gate goes up Grumbleweed's Hilltop. When he bonks into a rock, he gets dizzy. Tickle him then!",
        "You saved the day! Every cow on the farm is mooing again. Moo-tiful work!",
    };

    public static readonly string[] ConfusedLines =
    {
        "Quack quack! ...I'm so embarrassed.", "Oink? I mean... oink.", "HONK! Honk honk! (Help.)",
        "Ribbit. Is that a moo? That's not a moo.", "Baa-aa-aa... wait, I'm not a sheep!", "Cock-a-doodle... MOO? No. Doodle.",
    };

    public static readonly string[] HappyCowLines =
    {
        "MOOOOO! It's good to be back.", "Moo-velous! Simply moo-velous.", "I'll never take my moo for granted again.",
        "Moo-ve over, I'm dancing!",
    };

    public static readonly string[] RescuedLines =
    {
        "Thanks for finding me! That was the best clover nap of my life.",
        "I've had a mud bath, a bubble bath, AND a frog bath. I'm sparkling.",
        "Fancy a game of Pong later? I promise not to cheat like those raccoons.",
    };

    public static readonly string[] RespawnLines =
    {
        "You giggled so hard you rolled all the way back to the barn! Have some milk.",
        "You laughed yourself silly! Happens to the best of us. Giggles refilled!",
        "Back so soon? Those critters sure are tickly. You're all topped up!",
        "Remember: tickle first, giggle later! Now go get 'em.",
    };

    public static readonly string[] GotTickled = { "Hee hee! That tickles!", "Ha ha! No fair!", "Pfft-hahaha!", "Giggle attack!" };
    public static readonly string[] CritterGiggles = { "Hee hee hee!", "Okay, okay, you win!", "Giggle-snort!", "I'm too ticklish!", "Teehee! Bye!" };
    public static readonly string[] BossHurt = { "Hee! I mean... HMPH!", "That does NOT tickle! (It does.)", "Bleh-heh-heh!", "Stop that! Pfft!" };

    public static readonly string[] BossIntro =
    {
        "Grumbleweed|Bleeeh! Who goes there? This is MY hill and MY shiny bell!",
        "{name}|We just want the cowbell back. Pretty please?",
        "Grumbleweed|Never! Not unless you can make me laugh. Which you CAN'T. I am extremely grumpy!",
        "|Tip: when Grumbleweed bonks into a rock or a wall, he gets dizzy. Tickles do double giggles then!",
    };

    public static readonly string[] BossEnding =
    {
        "Grumbleweed|Hee... hee hee... HA HA HA! Stop, stop! That tickles!",
        "Grumbleweed|Okay, okay. Truth is... I took the bell because nobody ever invites me to anything.",
        "Grumbleweed|It's lonely up here. The bell sounded like a party.",
        "Bessie|Well, why didn't you say so? You're invited to every party from now on!",
        "Grumbleweed|...Really? Then what are we waiting for? Let's DANCE!",
        "|The Golden Cowbell is whole again, and the cows can moo! MOOOOOOO!",
    };
}
