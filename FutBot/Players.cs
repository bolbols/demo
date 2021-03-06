using System.Collections.Generic;

namespace FutBot
{
    public class Players : List<Player>
    {
        public Players()
        {
            AddRange(new List<Player>
            {
                new(182160, "Knudtzon", "LM", 64),
                new(228804, "De Sart", "CM", 70),
                new(259916, "Granath", "CB", 60),
                new(242079, "Bistrovic", "CDM", 70),
                new(229910, "Ingelsson", "CM", 67),
                new(201526, "Bech", "RM", 66),
                new(241645, "Torp", "CAM", 66),
                new(206520, "Kim Won Sik", "CDM", 63),
                new(257771, "Juranovic", "RM", 71, sellPrice: 300, deprecated: true),
                new(169708, "Hutchinson", "CB", 68),
                new(237930, "Hausner", "CB", 68),
                new(239179, "Arsenic", "CB", 66),
                new(205929, "Thomsen", "LM", 65),
                new(162512, "Cuthbert", "CB", 63),
                new(193091, "Nuytinck", "CB", 74, maxBuyPrice: 800, sellPrice: 1200),
                new(232000, "Lee Seung Woo", "LM", 67),
                new(221875, "Son Jun Ho", "CDM", 71),
                new(248471, "Taferner", "CM", 67),
                new(259853, "Mukhin", "CM", 68),
                new(226518, "Milic", "CB", 67),
                new(170923, "Eschenko", "RB", 70),
                new(202043, "Holmberg", "ST", 67),
                new(224855, "Byers", "CM", 66),
                new(233791, "Maksimenko", "GK", 74),
                new(228802, "Zhivoglyadov", "RB", 72),
                new(232695, "Sbuttoni", "CB", 68),
                new(163156, "Keogh", "CB", 67),
                new(235544, "Memišević", "CDM", 70, sellPrice: 300, deprecated: true),
                new(251446, "Mihaj", "CB", 65),
                new(247042, "Lofgren", "CB", 61),
                new(202660, "Sjostedt", "CB", 62),
                new(220847, "Maertens", "CM", 69),
                new(208422, "Sanusi", "CM", 69),
                new(240045, "Ljubic", "CM", 70, sellPrice: 300, deprecated: true),
                new(201438, "Van Der Bruggen", "CM", 69),
                new(198429, "Tecl", "ST", 70),
                new(260352, "Arai", "RB", 62),
                new(254903, "Ichimori", "GK", 64),
                new(173734, "James Wilson", "CB", 63),
                new(235064, "Calum Macdonald", "LB", 62),
                new(257080, "Marlon Suliman Mustapha", "ST", 61),
                new(242545, "Hausjel", "RM", 62),
                new(238511, "Basila", "CB", 63),
                new(225084, "Dekoke", "LB", 62),
                new(228238, "Gundersen", "CB", 63),
                new(240150, "Totland", "RWB", 61)
            });
        }
    }
}