public static class GachaStoryTexts
{
    static readonly string[][] IntroByLang =
    {
        new[] { "¡Mira eso! Un coco soltó una moneda.",
                "Esas monedas no pagan el pedido... pero abren algo mucho mejor.",
                "Vamos a la máquina gacha." },
        new[] { "Look at that! A coconut dropped a coin.",
                "Coins don't pay the order... but they unlock something much better.",
                "Let's check out the gacha machine." },
        new[] { "Olha só! Um coco soltou uma moeda.",
                "Essas moedas não pagam o pedido... mas abrem algo muito melhor.",
                "Vamos até a máquina gacha." },
        new[] { "Regarde ça ! Une noix de coco a lâché une pièce.",
                "Ces pièces ne paient pas la commande... mais elles ouvrent quelque chose de bien mieux.",
                "Allons voir la machine gacha." },
        new[] { "Schau mal! Eine Kokosnuss hat eine Münze fallen lassen.",
                "Diese Münzen bezahlen nicht die Bestellung... aber sie schalten etwas viel Besseres frei.",
                "Gehen wir zur Gacha-Maschine." },
        new[] { "見て！ココナッツがコインを落としたよ。",
                "このコインは注文の支払いには使えない…でももっと良いことに使えるんだ。",
                "ガチャマシンを見に行こう。" },
    };

    static readonly string[][] ExplainByLang =
    {
        new[] { "Esta es la máquina gacha. Aquí gastas tus monedas para ganar objetos y reliquias que te ayudan a destrozar más cocos.",
                "Elige una tirada y agita el mouse de lado a lado. Entre más rápido lo agites, más rápido se sacude la palmera... hasta que ¡PAM! cae el coco.",
                "La tirada normal es la más barata. Con 5 monedas tienes más chance de Raro, Épico y Legendario, y con 15 monedas solo salen Épicos y Legendarios." },
        new[] { "This is the gacha machine. Spend your coins here to win items and relics that help you smash more coconuts.",
                "Pick a pull and shake the mouse side to side. The faster you shake, the faster the palm tree rattles... until BAM! the coconut drops.",
                "The normal pull is the cheapest. With 5 coins you get better odds of Rare, Epic and Legendary, and with 15 coins you only get Epic and Legendary." },
        new[] { "Esta é a máquina gacha. Aqui você gasta suas moedas para ganhar itens e relíquias que ajudam a destruir mais cocos.",
                "Escolha um sorteio e balance o mouse de um lado para o outro. Quanto mais rápido balançar, mais rápido a palmeira treme... até que PAM! o coco cai.",
                "O sorteio normal é o mais barato. Com 5 moedas você tem mais chance de Raro, Épico e Lendário, e com 15 moedas só saem Épicos e Lendários." },
        new[] { "Voici la machine gacha. Dépense tes pièces ici pour gagner des objets et des reliques qui t'aident à écraser plus de noix de coco.",
                "Choisis un tirage et secoue la souris de gauche à droite. Plus tu secoues vite, plus le palmier tremble vite... jusqu'à ce que PAM ! la noix de coco tombe.",
                "Le tirage normal est le moins cher. Avec 5 pièces, tu as plus de chances d'obtenir du Rare, de l'Épique et du Légendaire, et avec 15 pièces, tu n'obtiens que de l'Épique et du Légendaire." },
        new[] { "Das ist die Gacha-Maschine. Hier gibst du deine Münzen aus, um Gegenstände und Relikte zu gewinnen, die dir helfen, mehr Kokosnüsse zu zerschlagen.",
                "Wähle einen Zug und schüttle die Maus von links nach rechts. Je schneller du schüttelst, desto schneller wackelt die Palme... bis es PAM! macht und die Kokosnuss fällt.",
                "Der normale Zug ist der günstigste. Mit 5 Münzen hast du bessere Chancen auf Selten, Episch und Legendär, und mit 15 Münzen gibt es nur Episch und Legendär." },
        new[] { "これがガチャマシンだ。ここでコインを使って、ココナッツをもっと壊すのに役立つアイテムやレリックを手に入れよう。",
                "引き方を選んで、マウスを左右に振ろう。速く振るほどヤシの木も速く揺れて…ついに「パーン！」ココナッツが落ちる。",
                "通常ガチャが一番安い。5コインならレア・エピック・レジェンダリーが出やすく、15コインならエピックとレジェンダリーだけが出る。" },
    };

    public static string[] Intro => Pick(IntroByLang);
    public static string[] Explain => Pick(ExplainByLang);

    static string[] Pick(string[][] table)
    {
        int i = Tr.Index;
        if (i < 0 || i >= table.Length) i = 0;
        return table[i];
    }
}