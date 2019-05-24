using LoveLive_Mahjong_Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace UnitTestMahjong
{
    [TestClass]
    public class UnitTestMahjong
    {
        [TestMethod]
        public void TestLisiten()
        {
            LoveLive_MahjongClass.InitializeMahjongClass();

            // 设置一些要和的牌
            List<MahjongCard> Hand_Cards;
            List<MahjongCardFuru> Furu_Cards;
            Hand_Cards = new List<MahjongCard>()
            {
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Hanayo - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Rin - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Maki - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Hanamaru - 1 - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Yoshiko - 1 - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Ruby - 1 - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Shizuku - 2 - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Rina - 2 - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Kasumi - 2 - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Nico  - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Eli - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Maki - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Aqours - 5 - 1],
            };

            Furu_Cards = new List<MahjongCardFuru>()
            {

            };

            MahjongLogic logic = new MahjongLogic();

            List<MahjongCard> waiting = logic.utIsWaiting(Hand_Cards, Furu_Cards);

            Trace.WriteLine($"You are waiting for {waiting.Count} cards.");
            foreach (MahjongCard card in waiting)
            {
                Trace.WriteLine(card);
            }
        }

        [TestMethod]
        public void TestYaku()
        {
            LoveLive_MahjongClass.InitializeMahjongClass();

            // 设置一些要和的牌
            List<MahjongCard> Hand_Cards;
            List<MahjongCardFuru> Furu_Cards;
            Hand_Cards = new List<MahjongCard>()
            {
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Rin - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Rin - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Maki - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Maki - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Nico - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Nico - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Nozomi - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Nozomi - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Hanayo - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Hanayo  - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Eli - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Eli - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Kotori - 1],
                LoveLive_MahjongClass.CardInfo[(int)MahjongCardName.Kotori - 1],
            };

            Furu_Cards = new List<MahjongCardFuru>()
            {
                 
            };

            MahjongLogic logic = new MahjongLogic();

            bool Hu = logic.utIsHu(Hand_Cards, Furu_Cards, out List<HuCard> huCards);
            Assert.IsTrue(Hu);

            List<MahjongYaku> yakus = logic.utCalcYaku(huCards);
            foreach (MahjongYaku yaku in yakus)
                Trace.WriteLine(yaku);

            Trace.WriteLine($"点数：{logic.utCalcHuPoints(huCards)}");
        }

        [TestMethod]
        public void TestGaming()
        {
            LoveLive_MahjongClass.InitializeMahjongClass();

            MahjongLogic mahjongLogic = new MahjongLogic();

            mahjongLogic.StartGamingThread();

            mahjongLogic.gameStatusMachine.DirectlyExit();
        }

        [TestMethod]
        public void TestFuruRon()
        {
            LoveLive_MahjongClass.InitializeMahjongClass();

            MahjongLogic mahjongLogic = new MahjongLogic();

            // 反射，强行配置玩家顺序
            FieldInfo field = mahjongLogic.GetType().GetField("order", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(mahjongLogic, new int[] { 0, 1, 2, 3 });
            field = mahjongLogic.GetType().GetField("playing", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(mahjongLogic, 3);

            // 配置一些可以副露和荣和的牌组
            // 玩家A（等待杠Ruby）
            mahjongLogic.player_info[0].card_onhand = new List<MahjongCard>()
            {
                LoveLive_MahjongClass.GetCard(MahjongCardName.Ruby),
                LoveLive_MahjongClass.GetCard(MahjongCardName.Ruby),
                LoveLive_MahjongClass.GetCard(MahjongCardName.Ruby),
            };

            // 玩家A（等待吃Ruby（年级））
            mahjongLogic.player_info[1].card_onhand = new List<MahjongCard>()
            {
                LoveLive_MahjongClass.GetCard(MahjongCardName.Yoshiko),
                LoveLive_MahjongClass.GetCard(MahjongCardName.Hanamaru),
            };

            // 玩家C（等待荣Ruby）
            mahjongLogic.player_info[2].waiting.Add(LoveLive_MahjongClass.GetCard(MahjongCardName.Ruby));

            // 当前D （打出了Ruby）
            mahjongLogic.player_info[3].card_played.Add(LoveLive_MahjongClass.GetCard(MahjongCardName.Ruby));

            // 判断荣和
            MethodInfo method = mahjongLogic.GetType().GetMethod("isCanRon", BindingFlags.NonPublic | BindingFlags.Instance);
            List<RonAble> ronable = method.Invoke(mahjongLogic, null) as List<RonAble>;
            method = mahjongLogic.GetType().GetMethod("isCanFuru", BindingFlags.NonPublic | BindingFlags.Instance);
            List<FuruAble> furuable = method.Invoke(mahjongLogic, null) as List<FuruAble>;

            foreach (var ron in ronable)
            {
                Trace.WriteLine($"ローン！{ron.RonCard}");
            }

            foreach (var furu in furuable)
            {
                foreach (var e in furu.FuruableList)
                    Trace.WriteLine($"{e.type}");
            }
        }
    }
}
