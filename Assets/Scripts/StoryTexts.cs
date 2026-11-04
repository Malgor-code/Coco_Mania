
public static class StoryTexts
{
    static int L => Tr.Index;

    public static string[] Intro => IntroAll()[L];
    public static string[] TutorialOrder => TutorialOrderAll()[L];
    public static string[] TutorialDeuda => TutorialDeudaAll()[L];
    public static string[] TutorialUpgradeIntro => TutorialUpgradeIntroAll()[L];
    public static string[] TutorialTree => TutorialTreeAll()[L];

    public static string[] Chapter(int cycle)
    {
        string[][] arr = ChaptersAll()[L];
        return (cycle >= 0 && cycle < arr.Length) ? arr[cycle] : null;
    }

    static string[][] IntroAll() { return new[] { IntroES, IntroEN, IntroPT, IntroFR, IntroDE, IntroJA }; }
    static string[][] TutorialOrderAll() { return new[] { OrderES, OrderEN, OrderPT, OrderFR, OrderDE, OrderJA }; }
    static string[][] TutorialDeudaAll() { return new[] { DeudaES, DeudaEN, DeudaPT, DeudaFR, DeudaDE, DeudaJA }; }
    static string[][] TutorialUpgradeIntroAll() { return new[] { UpIntroES, UpIntroEN, UpIntroPT, UpIntroFR, UpIntroDE, UpIntroJA }; }
    static string[][] TutorialTreeAll() { return new[] { TreeES, TreeEN, TreePT, TreeFR, TreeDE, TreeJA }; }
    static string[][][] ChaptersAll() { return new[] { ChapES, ChapEN, ChapPT, ChapFR, ChapDE, ChapJA }; }

    static readonly string[] IntroES =
    {
        "Soy un emprendedor del puerto, y si hay algo que disfruto de verdad es destruir cocos.",
        "Cada coco que parto suelta agua fresca, y esa agua es mi negocio.",
        "Tengo un machete oxidado, mucha energía y ningún plan mejor. ¡Ayúdame a romper todos los cocos que pueda antes de quedarme sin aliento!"
    };
    static readonly string[] OrderES =
    {
        "¡Primera ronda terminada! Ya hay agua de coco en las jarras.",
        "Pero ojo: tienes que entregar tu pedido.",
        "Mi tía Rosa me pidió {ml} de agua de coco para su cumpleaños, ¡y la fiesta es dentro de {days} días!",
        "Vamos a ver cómo va ese pedido."
    };
    static readonly string[] DeudaES =
    {
        "Aquí ves cuánta agua te falta y cuántos días te quedan para juntarla.",
        "Cuando tengas suficiente, el botón de entregar se activa. Cierra este panel cuando estés listo."
    };
    static readonly string[] UpIntroES =
    {
        "Con la misma agua también puedes mejorarte: ser más fuerte, más rápido y más listo.",
        "Echemos un vistazo a las mejoras."
    };
    static readonly string[] TreeES =
    {
        "Este es el árbol de mejoras. Muévete con el scroll: la rueda del mouse acerca y aleja, y arrastrando con el botón central te desplazas.",
        "Desde aquí también puedes comprar machetes en la tienda y probar suerte en la gacha con las monedas que te dan los cocos.",
        "Pero ojo: todo se paga con la misma agua del pedido, así que elige con cabeza. ¡Ahora sigue tu camino, emprendedor!"
    };
    static readonly string[][] ChapES =
    {
        null,
        new[] { "Pedido {cycle}: mi tía Rosa quiere {ml} para su cumpleaños. ¡Tengo {days} días!" },
        new[] { "¡Tía Rosa quedó feliz! La noticia corrió por toda la familia.",
                "Mi primo Mateo cumple 6 años y quiere una piñata... de coco. Pidió {ml}. Tengo {days} días." },
        new[] { "La fiesta de Mateo fue un éxito: los niños pedían \"otro coco, otro coco\".",
                "Ahora la escuela del barrio organiza una kermés y necesita {ml}. ¡{days} días!" },
        new[] { "Los niños de la kermés salieron saltando de felicidad.",
                "Mi prima Luna se casa en la playa y quiere brindar con agua de coco fresca: {ml} para dentro de {days} días." },
        new[] { "Una boda preciosa, y varios invitados preguntaron quién surte el agua.",
                "Mis abuelos cumplen 50 años de casados y toda la familia llegará al puerto. Piden {ml}. Tengo {days} días." },
        new[] { "Los abuelos lloraron de emoción (y se tomaron como tres litros).",
                "Ahora el parque central hará la Gran Fiesta Infantil, con payasos y 200 niños. Piden {ml} en {days} días." },
        new[] { "200 niños y ni una gota sobrante. Soy un profesional.",
                "El pueblo entero celebra la Fiesta del Puerto y el comité me pide {ml}. ¡Tengo {days} días!" },
        new[] { "Todo el pueblo habla de mi agua de coco.",
                "El alcalde de Villa Marea, el pueblo vecino, quiere {ml} para su feria anual. Plazo: {days} días." },
        new[] { "Villa Marea me nombró \"Cortador Honorario\".",
                "La ciudad de San Cocotal organizará su Festival de Faros y necesita {ml}. Plazo: {days} días." },
        new[] { "Las luces del festival se reflejaban en mi machete.",
                "Ahora la capital quiere {ml} para el concierto del estadio. Ya no es una fiesta: es una multitud. {days} días." },
        new[] { "Cincuenta mil personas y ni una sola con sed. Anotado.",
                "El estado de Costa Dorada va a declarar el Día del Coco y pide {ml}. Tengo {days} días." },
        new[] { "El gobernador me regaló una medalla de latón con forma de coco.",
                "Cinco estados se juntan en el Carnaval Unido y piden {ml} entre todos. ¡{days} días!" },
        new[] { "Los carnavales nunca vieron tanta agua de coco.",
                "Llega un mensajero real desde el Reino de Palmeralia: la Reina quiere {ml} para su banquete. Plazo: {days} días." },
        new[] { "La Reina Palmira probó el agua y dijo: \"¡Exquisita!\".",
                "Pero la República de Cocolandia, su eterna rival, exige {ml} para su Gran Desfile. ¡{days} días!" },
        new[] { "Las dos naciones firmaron la paz... brindando con coco.",
                "Las Mil Islas convocan la Cumbre de las Naciones y piden {ml}. Es el pedido más grande hasta ahora: {days} días." },
        new[] { "Si hasta reyes y presidentes me escriben...",
                "Último pedido: el Festival Mundial de las Fiestas, el cumpleaños de todos los niños de todos los reinos. Piden {ml} en {days} días.",
                "Es el pedido de mi vida. ¡Vamos con todo!" },
        new[] { "¡Lo logré! Entregué el último pedido y el mundo entero brindó con agua de coco.",
                "Desde el puerto, un emprendedor, un machete y muchísimos cocos hicieron una fiesta para todos.",
                "¡GRACIAS POR JUGAR!",
                "El juego sigue en modo infinito, por si quieres romper unos cocos más." }
    };

    static readonly string[] IntroEN =
    {
        "I'm a harbor entrepreneur, and if there's one thing I truly enjoy, it's smashing coconuts.",
        "Every coconut I crack open spills fresh water, and that water is my business.",
        "I've got a rusty machete, plenty of energy and no better plan. Help me break as many coconuts as I can before I run out of breath!"
    };
    static readonly string[] OrderEN =
    {
        "First round done! There's already coconut water in the jars.",
        "But watch out: you have to deliver your order.",
        "My aunt Rosa asked me for {ml} of coconut water for her birthday, and the party is in {days} days!",
        "Let's see how that order is going."
    };
    static readonly string[] DeudaEN =
    {
        "Here you can see how much water you still need and how many days you have left to collect it.",
        "Once you have enough, the deliver button lights up. Close this panel when you're ready."
    };
    static readonly string[] UpIntroEN =
    {
        "With that same water you can also improve yourself: get stronger, faster and smarter.",
        "Let's take a look at the upgrades."
    };
    static readonly string[] TreeEN =
    {
        "This is the upgrade tree. Move around with the scroll: the mouse wheel zooms in and out, and dragging with the middle button pans the view.",
        "From here you can also buy machetes in the shop and try your luck at the gacha with the coins the coconuts give you.",
        "But careful: everything is paid with the same water as the order, so choose wisely. Now go on your way, entrepreneur!"
    };
    static readonly string[][] ChapEN =
    {
        null,
        new[] { "Order {cycle}: my aunt Rosa wants {ml} for her birthday. I have {days} days!" },
        new[] { "Aunt Rosa is thrilled! The news spread through the whole family.",
                "My cousin Mateo turns 6 and wants a piñata... made of coconut. He asked for {ml}. I have {days} days." },
        new[] { "Mateo's party was a hit: the kids kept shouting \"another coconut, another coconut\".",
                "Now the neighborhood school is throwing a fair and needs {ml}. {days} days!" },
        new[] { "The kids at the fair left jumping with joy.",
                "My cousin Luna is getting married on the beach and wants to toast with fresh coconut water: {ml} in {days} days." },
        new[] { "A beautiful wedding, and several guests asked who supplies the water.",
                "My grandparents are celebrating 50 years of marriage and the whole family is coming to the harbor. They want {ml}. I have {days} days." },
        new[] { "My grandparents cried with joy (and drank about three liters).",
                "Now the central park is holding the Great Children's Party, with clowns and 200 kids. They want {ml} in {days} days." },
        new[] { "200 kids and not a single drop left over. I'm a professional.",
                "The whole town is celebrating the Harbor Festival and the committee asks me for {ml}. I have {days} days!" },
        new[] { "The whole town is talking about my coconut water.",
                "The mayor of Villa Marea, the neighboring town, wants {ml} for its annual fair. Deadline: {days} days." },
        new[] { "Villa Marea named me \"Honorary Cutter\".",
                "The city of San Cocotal is organizing its Lighthouse Festival and needs {ml}. Deadline: {days} days." },
        new[] { "The festival lights shimmered on my machete.",
                "Now the capital wants {ml} for the stadium concert. It's no longer a party: it's a crowd. {days} days." },
        new[] { "Fifty thousand people and not one of them thirsty. Noted.",
                "The state of Costa Dorada will declare Coconut Day and asks for {ml}. I have {days} days." },
        new[] { "The governor gave me a brass medal shaped like a coconut.",
                "Five states are joining the United Carnival and ask for {ml} all together. {days} days!" },
        new[] { "Carnivals have never seen so much coconut water.",
                "A royal messenger arrives from the Kingdom of Palmeralia: the Queen wants {ml} for her banquet. Deadline: {days} days." },
        new[] { "Queen Palmira tasted the water and said: \"Exquisite!\".",
                "But the Republic of Cocolandia, her eternal rival, demands {ml} for its Grand Parade. {days} days!" },
        new[] { "The two nations signed a peace treaty... toasting with coconut.",
                "The Thousand Islands call the Summit of Nations and ask for {ml}. It's the biggest order yet: {days} days." },
        new[] { "Even kings and presidents are writing to me...",
                "Final order: the World Festival of Celebrations, the birthday of every child in every kingdom. They want {ml} in {days} days.",
                "It's the order of my life. Let's give it everything!" },
        new[] { "I did it! I delivered the final order and the whole world toasted with coconut water.",
                "From the harbor, one entrepreneur, one machete and a whole lot of coconuts threw a party for everyone.",
                "THANKS FOR PLAYING!",
                "The game continues in endless mode, in case you want to smash a few more coconuts." }
    };

    static readonly string[] IntroPT =
    {
        "Sou um empreendedor do porto, e se há algo de que gosto de verdade é destruir cocos.",
        "Cada coco que racho solta água fresca, e essa água é o meu negócio.",
        "Tenho um machete enferrujado, muita energia e nenhum plano melhor. Me ajude a quebrar todos os cocos que eu conseguir antes de ficar sem fôlego!"
    };
    static readonly string[] OrderPT =
    {
        "Primeira rodada terminada! Já tem água de coco nas jarras.",
        "Mas atenção: você precisa entregar o seu pedido.",
        "Minha tia Rosa me pediu {ml} de água de coco para o aniversário dela, e a festa é daqui a {days} dias!",
        "Vamos ver como está esse pedido."
    };
    static readonly string[] DeudaPT =
    {
        "Aqui você vê quanta água ainda falta e quantos dias restam para juntá-la.",
        "Quando tiver o suficiente, o botão de entregar é ativado. Feche este painel quando estiver pronto."
    };
    static readonly string[] UpIntroPT =
    {
        "Com essa mesma água você também pode se melhorar: ficar mais forte, mais rápido e mais esperto.",
        "Vamos dar uma olhada nas melhorias."
    };
    static readonly string[] TreePT =
    {
        "Esta é a árvore de melhorias. Mova-se com o scroll: a roda do mouse aproxima e afasta, e arrastando com o botão do meio você se desloca.",
        "Daqui você também pode comprar machetes na loja e tentar a sorte no gacha com as moedas que os cocos dão.",
        "Mas atenção: tudo é pago com a mesma água do pedido, então escolha com cuidado. Agora siga seu caminho, empreendedor!"
    };
    static readonly string[][] ChapPT =
    {
        null,
        new[] { "Pedido {cycle}: minha tia Rosa quer {ml} para o aniversário dela. Tenho {days} dias!" },
        new[] { "A tia Rosa ficou feliz! A notícia correu por toda a família.",
                "Meu primo Mateus faz 6 anos e quer uma piñata... de coco. Ele pediu {ml}. Tenho {days} dias." },
        new[] { "A festa do Mateus foi um sucesso: as crianças pediam \"outro coco, outro coco\".",
                "Agora a escola do bairro está organizando uma quermesse e precisa de {ml}. {days} dias!" },
        new[] { "As crianças da quermesse saíram pulando de alegria.",
                "Minha prima Luna vai se casar na praia e quer brindar com água de coco fresca: {ml} daqui a {days} dias." },
        new[] { "Um casamento lindo, e vários convidados perguntaram quem fornece a água.",
                "Meus avós fazem 50 anos de casados e a família inteira vai chegar ao porto. Eles querem {ml}. Tenho {days} dias." },
        new[] { "Os avós choraram de emoção (e beberam uns três litros).",
                "Agora o parque central vai fazer a Grande Festa Infantil, com palhaços e 200 crianças. Querem {ml} em {days} dias." },
        new[] { "200 crianças e nem uma gota sobrando. Sou um profissional.",
                "A cidade inteira celebra a Festa do Porto e a comissão me pede {ml}. Tenho {days} dias!" },
        new[] { "A cidade inteira fala da minha água de coco.",
                "O prefeito de Vila Maré, a cidade vizinha, quer {ml} para a feira anual. Prazo: {days} dias." },
        new[] { "Vila Maré me nomeou \"Cortador Honorário\".",
                "A cidade de San Cocotal vai organizar o Festival dos Faróis e precisa de {ml}. Prazo: {days} dias." },
        new[] { "As luzes do festival se refletiam no meu machete.",
                "Agora a capital quer {ml} para o show no estádio. Já não é uma festa: é uma multidão. {days} dias." },
        new[] { "Cinquenta mil pessoas e nenhuma com sede. Anotado.",
                "O estado de Costa Dourada vai declarar o Dia do Coco e pede {ml}. Tenho {days} dias." },
        new[] { "O governador me deu uma medalha de latão em forma de coco.",
                "Cinco estados se juntam no Carnaval Unido e pedem {ml} ao todo. {days} dias!" },
        new[] { "Os carnavais nunca viram tanta água de coco.",
                "Chega um mensageiro real do Reino de Palmeralia: a Rainha quer {ml} para o banquete dela. Prazo: {days} dias." },
        new[] { "A Rainha Palmira provou a água e disse: \"Deliciosa!\".",
                "Mas a República de Cocolândia, sua eterna rival, exige {ml} para o seu Grande Desfile. {days} dias!" },
        new[] { "As duas nações assinaram a paz... brindando com coco.",
                "As Mil Ilhas convocam a Cúpula das Nações e pedem {ml}. É o maior pedido até agora: {days} dias." },
        new[] { "Até reis e presidentes me escrevem...",
                "Último pedido: o Festival Mundial das Festas, o aniversário de todas as crianças de todos os reinos. Pedem {ml} em {days} dias.",
                "É o pedido da minha vida. Vamos com tudo!" },
        new[] { "Consegui! Entreguei o último pedido e o mundo inteiro brindou com água de coco.",
                "Do porto, um empreendedor, um machete e muitíssimos cocos fizeram uma festa para todos.",
                "OBRIGADO POR JOGAR!",
                "O jogo continua no modo infinito, caso queira quebrar mais uns cocos." }
    };

    static readonly string[] IntroFR =
    {
        "Je suis un entrepreneur du port, et s'il y a bien une chose que j'adore, c'est casser des noix de coco.",
        "Chaque noix de coco que j'ouvre libère de l'eau fraîche, et cette eau, c'est mon commerce.",
        "J'ai une machette rouillée, beaucoup d'énergie et aucun meilleur plan. Aide-moi à casser toutes les noix de coco possibles avant de manquer de souffle !"
    };
    static readonly string[] OrderFR =
    {
        "Première manche terminée ! Il y a déjà de l'eau de coco dans les jarres.",
        "Mais attention : tu dois livrer ta commande.",
        "Ma tante Rosa m'a commandé {ml} d'eau de coco pour son anniversaire, et la fête a lieu dans {days} jours !",
        "Voyons où en est cette commande."
    };
    static readonly string[] DeudaFR =
    {
        "Ici, tu vois combien d'eau il te manque et de combien de jours tu disposes pour la récolter.",
        "Quand tu en auras assez, le bouton de livraison s'active. Ferme ce panneau quand tu es prêt."
    };
    static readonly string[] UpIntroFR =
    {
        "Avec cette même eau, tu peux aussi t'améliorer : devenir plus fort, plus rapide et plus malin.",
        "Jetons un œil aux améliorations."
    };
    static readonly string[] TreeFR =
    {
        "Voici l'arbre d'améliorations. Déplace-toi avec la molette : elle zoome et dézoome, et en faisant glisser avec le bouton central tu te déplaces.",
        "D'ici, tu peux aussi acheter des machettes à la boutique et tenter ta chance au gacha avec les pièces que te donnent les noix de coco.",
        "Mais attention : tout se paie avec la même eau que la commande, alors choisis bien. Maintenant, poursuis ta route, entrepreneur !"
    };
    static readonly string[][] ChapFR =
    {
        null,
        new[] { "Commande {cycle} : ma tante Rosa veut {ml} pour son anniversaire. J'ai {days} jours !" },
        new[] { "Tante Rosa est ravie ! La nouvelle a fait le tour de la famille.",
                "Mon cousin Mateo a 6 ans et veut une piñata... en noix de coco. Il a demandé {ml}. J'ai {days} jours." },
        new[] { "La fête de Mateo a été un succès : les enfants réclamaient « encore une noix de coco, encore une ! ».",
                "Maintenant l'école du quartier organise une kermesse et a besoin de {ml}. {days} jours !" },
        new[] { "Les enfants de la kermesse sont repartis en sautant de joie.",
                "Ma cousine Luna se marie sur la plage et veut trinquer à l'eau de coco fraîche : {ml} dans {days} jours." },
        new[] { "Un très beau mariage, et plusieurs invités ont demandé qui fournit l'eau.",
                "Mes grands-parents fêtent 50 ans de mariage et toute la famille va arriver au port. Ils demandent {ml}. J'ai {days} jours." },
        new[] { "Mes grands-parents ont pleuré d'émotion (et bu environ trois litres).",
                "Maintenant le parc central organise la Grande Fête des Enfants, avec des clowns et 200 enfants. Ils demandent {ml} en {days} jours." },
        new[] { "200 enfants et pas une goutte de reste. Je suis un professionnel.",
                "Tout le village fête la Fête du Port et le comité me demande {ml}. J'ai {days} jours !" },
        new[] { "Tout le village parle de mon eau de coco.",
                "Le maire de Villa Marea, le village voisin, veut {ml} pour sa foire annuelle. Délai : {days} jours." },
        new[] { "Villa Marea m'a nommé « Coupeur honoraire ».",
                "La ville de San Cocotal organise son Festival des Phares et a besoin de {ml}. Délai : {days} jours." },
        new[] { "Les lumières du festival se reflétaient sur ma machette.",
                "Maintenant la capitale veut {ml} pour le concert du stade. Ce n'est plus une fête : c'est une foule. {days} jours." },
        new[] { "Cinquante mille personnes et pas une seule assoiffée. Noté.",
                "L'État de Costa Dorada va décréter le Jour de la Noix de Coco et demande {ml}. J'ai {days} jours." },
        new[] { "Le gouverneur m'a offert une médaille en laiton en forme de noix de coco.",
                "Cinq États se réunissent pour le Carnaval Uni et demandent {ml} à eux tous. {days} jours !" },
        new[] { "Les carnavals n'avaient jamais vu autant d'eau de coco.",
                "Un messager royal arrive du Royaume de Palmeralia : la Reine veut {ml} pour son banquet. Délai : {days} jours." },
        new[] { "La Reine Palmira a goûté l'eau et a dit : « Exquise ! ».",
                "Mais la République de Cocolandia, sa rivale éternelle, exige {ml} pour son Grand Défilé. {days} jours !" },
        new[] { "Les deux nations ont signé la paix... en trinquant à la noix de coco.",
                "Les Mille Îles convoquent le Sommet des Nations et demandent {ml}. C'est la plus grosse commande jusqu'ici : {days} jours." },
        new[] { "Même les rois et les présidents m'écrivent...",
                "Dernière commande : le Festival Mondial des Fêtes, l'anniversaire de tous les enfants de tous les royaumes. Ils demandent {ml} en {days} jours.",
                "C'est la commande de ma vie. Allons-y à fond !" },
        new[] { "J'ai réussi ! J'ai livré la dernière commande et le monde entier a trinqué à l'eau de coco.",
                "Depuis le port, un entrepreneur, une machette et énormément de noix de coco ont fait la fête pour tout le monde.",
                "MERCI D'AVOIR JOUÉ !",
                "Le jeu continue en mode infini, si tu veux casser encore quelques noix de coco." }
    };

    static readonly string[] IntroDE =
    {
        "Ich bin ein Unternehmer aus dem Hafen, und was ich wirklich gern mache, ist Kokosnüsse zerschlagen.",
        "Aus jeder Kokosnuss, die ich aufschlage, sprudelt frisches Wasser, und dieses Wasser ist mein Geschäft.",
        "Ich habe eine rostige Machete, jede Menge Energie und keinen besseren Plan. Hilf mir, so viele Kokosnüsse wie möglich zu knacken, bevor mir die Puste ausgeht!"
    };
    static readonly string[] OrderDE =
    {
        "Erste Runde geschafft! In den Krügen ist schon Kokoswasser.",
        "Aber Achtung: Du musst deine Bestellung liefern.",
        "Meine Tante Rosa hat {ml} Kokoswasser für ihren Geburtstag bestellt, und bis zur Feier bleiben {days} Tage!",
        "Schauen wir mal, wie es um die Bestellung steht."
    };
    static readonly string[] DeudaDE =
    {
        "Hier siehst du, wie viel Wasser dir noch fehlt und wie viele Tage dir bleiben, um es zu sammeln.",
        "Sobald du genug hast, wird der Liefern-Button aktiv. Schließe dieses Fenster, wenn du bereit bist."
    };
    static readonly string[] UpIntroDE =
    {
        "Mit demselben Wasser kannst du dich auch verbessern: stärker, schneller und schlauer werden.",
        "Schauen wir uns die Verbesserungen an."
    };
    static readonly string[] TreeDE =
    {
        "Das ist der Verbesserungsbaum. Navigiere mit dem Scrollen: Das Mausrad zoomt rein und raus, und mit gedrückter mittlerer Maustaste verschiebst du die Ansicht.",
        "Von hier aus kannst du auch Macheten im Shop kaufen und mit den Münzen, die die Kokosnüsse dir geben, dein Glück beim Gacha versuchen.",
        "Aber Vorsicht: Alles wird mit demselben Wasser wie die Bestellung bezahlt, also wähle weise. Und jetzt geh deinen Weg, Unternehmer!"
    };
    static readonly string[][] ChapDE =
    {
        null,
        new[] { "Bestellung {cycle}: Meine Tante Rosa will {ml} für ihren Geburtstag. Ich habe {days} Tage!" },
        new[] { "Tante Rosa ist begeistert! Die Nachricht hat sich in der ganzen Familie verbreitet.",
                "Mein Cousin Mateo wird 6 und wünscht sich eine Piñata... aus Kokosnuss. Er hat {ml} bestellt. Ich habe {days} Tage." },
        new[] { "Mateos Fest war ein Erfolg: Die Kinder riefen \"noch eine Kokosnuss, noch eine!\".",
                "Jetzt veranstaltet die Schule im Viertel ein Fest und braucht {ml}. {days} Tage!" },
        new[] { "Die Kinder vom Schulfest sind vor Freude hüpfend nach Hause gegangen.",
                "Meine Cousine Luna heiratet am Strand und will mit frischem Kokoswasser anstoßen: {ml}, Frist: {days} Tage." },
        new[] { "Eine wunderschöne Hochzeit, und mehrere Gäste fragten, wer das Wasser liefert.",
                "Meine Großeltern feiern 50 Jahre Ehe und die ganze Familie kommt in den Hafen. Sie wollen {ml}. Ich habe {days} Tage." },
        new[] { "Die Großeltern weinten vor Rührung (und tranken ungefähr drei Liter).",
                "Jetzt veranstaltet der Stadtpark das Große Kinderfest, mit Clowns und 200 Kindern. Sie wollen {ml}, Frist: {days} Tage." },
        new[] { "200 Kinder und kein Tropfen übrig. Ich bin ein Profi.",
                "Das ganze Dorf feiert das Hafenfest und das Komitee bestellt {ml}. Ich habe {days} Tage!" },
        new[] { "Das ganze Dorf spricht über mein Kokoswasser.",
                "Der Bürgermeister von Villa Marea, dem Nachbardorf, will {ml} für seinen Jahrmarkt. Frist: {days} Tage." },
        new[] { "Villa Marea hat mich zum \"Ehren-Kokoshacker\" ernannt.",
                "Die Stadt San Cocotal veranstaltet ihr Leuchtturmfest und braucht {ml}. Frist: {days} Tage." },
        new[] { "Die Lichter des Festes spiegelten sich in meiner Machete.",
                "Jetzt will die Hauptstadt {ml} für das Stadionkonzert. Das ist kein Fest mehr: Das ist eine Menschenmenge. {days} Tage." },
        new[] { "Fünfzigtausend Menschen und keiner davon durstig. Notiert.",
                "Der Bundesstaat Costa Dorada will den Kokosnuss-Tag ausrufen und bestellt {ml}. Ich habe {days} Tage." },
        new[] { "Der Gouverneur schenkte mir eine Messingmedaille in Form einer Kokosnuss.",
                "Fünf Bundesstaaten treffen sich zum Vereinten Karneval und bestellen zusammen {ml}. {days} Tage!" },
        new[] { "Die Karnevalsfeiern haben noch nie so viel Kokoswasser gesehen.",
                "Ein königlicher Bote kommt aus dem Königreich Palmeralia: Die Königin will {ml} für ihr Bankett. Frist: {days} Tage." },
        new[] { "Königin Palmira probierte das Wasser und sagte: \"Exquisit!\".",
                "Aber die Republik Cocolandia, ihre ewige Rivalin, verlangt {ml} für ihre Große Parade. {days} Tage!" },
        new[] { "Die beiden Nationen schlossen Frieden... mit Kokosnuss im Glas.",
                "Die Tausend Inseln rufen den Gipfel der Nationen aus und bestellen {ml}. Das ist die bisher größte Bestellung: {days} Tage." },
        new[] { "Sogar Könige und Präsidenten schreiben mir...",
                "Letzte Bestellung: das Weltfest der Feiern, der Geburtstag aller Kinder aller Königreiche. Sie wollen {ml}, Frist: {days} Tage.",
                "Das ist die Bestellung meines Lebens. Volle Kraft voraus!" },
        new[] { "Geschafft! Ich habe die letzte Bestellung geliefert und die ganze Welt hat mit Kokoswasser angestoßen.",
                "Vom Hafen aus haben ein Unternehmer, eine Machete und unzählige Kokosnüsse ein Fest für alle gefeiert.",
                "DANKE FÜRS SPIELEN!",
                "Das Spiel geht im Endlosmodus weiter, falls du noch ein paar Kokosnüsse zerschlagen willst." }
    };

    static readonly string[] IntroJA =
    {
        "俺は港町の起業家。本当に好きなことといえば、ココナッツを叩き割ることさ。",
        "割るたびに新鮮な水があふれ出す。その水が俺の商売だ。",
        "さびたマチェーテと有り余る元気はあるけど、他にいい計画はない。息が切れる前に、できるだけたくさんのココナッツを割るのを手伝ってくれ！"
    };
    static readonly string[] OrderJA =
    {
        "最初のラウンド終了！もうジャーにココナッツウォーターが入ってるぞ。",
        "でも気をつけて。注文を届けなきゃいけない。",
        "ローザおばさんが誕生日用にココナッツウォーターを{ml}注文してきた。パーティーまで{days}日だ！",
        "注文の進み具合を見てみよう。"
    };
    static readonly string[] DeudaJA =
    {
        "ここで、あと水がどれだけ必要か、集めるのに何日残っているかが分かる。",
        "十分に集まると、届けるボタンが押せるようになる。準備ができたらこのパネルを閉じてくれ。"
    };
    static readonly string[] UpIntroJA =
    {
        "同じ水で自分を強化することもできる。もっと強く、もっと速く、もっと賢くなれるんだ。",
        "アップグレードを見てみよう。"
    };
    static readonly string[] TreeJA =
    {
        "これがアップグレードツリーだ。スクロールで移動する。マウスホイールで拡大・縮小、中ボタンをドラッグすると移動できる。",
        "ここではショップでマチェーテを買ったり、ココナッツから手に入るコインでガチャに挑戦したりもできる。",
        "ただし注意。支払いはすべて注文と同じ水だ。よく考えて選んでくれ。さあ、進もう、起業家！"
    };
    static readonly string[][] ChapJA =
    {
        null,
        new[] { "注文{cycle}：ローザおばさんが誕生日用に{ml}欲しいって。期限は{days}日！" },
        new[] { "ローザおばさん大喜び！ニュースは家族中に広まった。",
                "いとこのマテオは6歳になって、ココナッツのピニャータが欲しいらしい。注文は{ml}。期限は{days}日。" },
        new[] { "マテオのパーティーは大成功。子どもたちは「もう一個、もう一個！」とせがんだ。",
                "今度は近所の学校がバザーを開くので{ml}が必要だ。{days}日！" },
        new[] { "バザーの子どもたちは大喜びで跳ねながら帰っていった。",
                "いとこのルナが浜辺で結婚式を挙げる。新鮮なココナッツウォーターで乾杯したいそうだ：{ml}、期限は{days}日。" },
        new[] { "素敵な結婚式で、何人ものゲストが誰が水を用意しているのか尋ねてきた。",
                "祖父母が結婚50周年で、家族みんなが港に集まる。注文は{ml}。期限は{days}日。" },
        new[] { "祖父母は感激して泣いた（そして3リットルほど飲んだ）。",
                "今度は中央公園で、ピエロと200人の子どもたちが集まる大子ども祭りだ。注文は{ml}、期限は{days}日。" },
        new[] { "200人の子どもたちで、水は一滴も余らなかった。俺はプロだ。",
                "町中が港祭りを祝っていて、委員会から{ml}の注文だ。期限は{days}日！" },
        new[] { "町中が俺のココナッツウォーターの話をしている。",
                "隣町ビリャ・マレアの町長が、毎年の市のために{ml}欲しいそうだ。期限：{days}日。" },
        new[] { "ビリャ・マレアは俺を「名誉カッター」に任命してくれた。",
                "サン・ココタルの街が灯台祭りを開くので{ml}が必要だ。期限：{days}日。" },
        new[] { "祭りの灯りが俺のマチェーテに映っていた。",
                "今度は首都がスタジアムのコンサート用に{ml}欲しいって。もはや祭りじゃない、大群衆だ。{days}日。" },
        new[] { "五万人もいて、誰ひとり喉が渇いていなかった。メモしとこう。",
                "コスタ・ドラダ州がココナッツの日を制定するそうで、{ml}の注文だ。期限は{days}日。" },
        new[] { "知事がココナッツの形をした真鍮のメダルをくれた。",
                "五つの州が合同カーニバルに集まり、全部で{ml}欲しいそうだ。{days}日！" },
        new[] { "カーニバルでこんなにココナッツウォーターが飲まれたのは初めてだ。",
                "パルメラリア王国から王の使者が到着：女王が晩餐会用に{ml}欲しいそうだ。期限：{days}日。" },
        new[] { "パルマ女王は水を味わって言った。「絶品じゃ！」",
                "だが宿敵のココランディア共和国が、大パレード用に{ml}を要求してきた。{days}日！" },
        new[] { "二つの国はココナッツで乾杯しながら和平を結んだ。",
                "千島諸島が諸国民サミットを招集し、{ml}を注文してきた。これまでで最大の注文だ：{days}日。" },
        new[] { "王様や大統領まで手紙をくれるなんて……",
                "最後の注文：世界祭典フェスティバル、すべての王国のすべての子どもたちの誕生日だ。注文は{ml}、期限は{days}日。",
                "俺の人生をかけた注文だ。全力でいくぞ！" },
        new[] { "やった！最後の注文を届けて、世界中がココナッツウォーターで乾杯した。",
                "港から、一人の起業家と一本のマチェーテと数えきれないココナッツが、みんなのためのパーティーを開いた。",
                "遊んでくれてありがとう！",
                "ゲームは無限モードで続くよ。もっとココナッツを割りたければどうぞ。" }
    };
}