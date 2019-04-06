using System;
using System.Collections.Generic;
using System.Linq;

namespace LoveLive_Mahjong_Library
{
    partial class MahjongLogic
    {
        /// <summary>
        /// 初始化逻辑
        /// </summary>
        public MahjongLogic()
        {
            // 初始化所有的变量
            for (int i = 0; i < 4; i++)
            {
                player_info[i] = new PlayerInfo();
            }

            // 重置发牌顺序
            order = new int[4] { 0, 1, 2, 3 };
        }

        /// <summary>
        /// 重置游戏
        /// </summary>
        /// <param name="StartupPoint">初始配点</param>
        /// <param name="TotalGame">总场次</param>
        private void _ResetGame(int StartupPoint, int TotalGame)
        {
            // 重置场次为东1场0本场
            game = MahjongGame.East;
            scene = 0;
            subscene = 0;

            // 设置总场次
            if (TotalGame == 0) total_game = MahjongGame.East;
            else if (TotalGame == 1) total_game = MahjongGame.South;
            else total_game = MahjongGame.North;

            // 初始化配点
            for (int i = 0; i < 4; i++)
            {
                player_info[i].points = StartupPoint;
            }

            // 随机座次
            order.Shuffle();
        }

        /// <summary>
        /// 洗牌（重置和打乱牌山）
        /// </summary>
        private void _ShuffleCards()
        {
            // 清空牌山
            card_stacks.Clear();

            // 从基础牌库获得牌
            foreach (MahjongCard card in LoveLive_MahjongClass.CardInfo)
            {
                // 每种牌4张
                for (int i = 0; i < 4; i++) card_stacks.Add(card);
            }

            // 尝试添加随机宝牌
            // （当前版本该功能未加入）

            // 洗牌
            card_stacks.Shuffle();
        }

        /// <summary>
        /// 摸牌（从牌山取得一张牌）
        /// </summary>
        private MahjongCard TouchCard()
        {
            // 获得牌山顶的牌
            MahjongCard card = card_stacks.First();

            // 从牌山中删除这张牌
            card_stacks.RemoveAt(0);

            // 返回所需的牌
            return card;
        }

        /// <summary>
        /// 砌牌（开局发牌）
        /// </summary>
        private void _MasonryCards()
        {
            // 清除宝牌指示牌，杠总数
            card_indicator = new List<MahjongCard>();
            kong_count = 0;

            // 清除所有玩家的手牌、副露和牌河
            for (int i = 0; i < 4; i++)
                player_info[i].ClearCards();

            // 按照标准麻将规则摸牌发牌，假设洗牌已经做好开门
            // 从庄家开始，每人每次摸2墩（4张），直到每人摸入12张牌（共摸3次）
            // 最后，庄家取得剩余牌山中第一墩和第三墩上的牌各一张，闲家取得剩余的牌共3张
            // 则庄家有14张牌，闲家有13张牌
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 4; i++)
                {
                    // 获得庄闲次序
                    int q = order[i];

                    // 从牌山连续摸四张牌加入手牌
                    player_info[i].AddHandCard(TouchCard());
                    player_info[i].AddHandCard(TouchCard());
                    player_info[i].AddHandCard(TouchCard());
                    player_info[i].AddHandCard(TouchCard());
                }
            }

            // 最后摸牌顺序庄家->闲I->II->III->庄家
            for (int i = 0; i < 4; i++) player_info[order[i]].AddHandCard(TouchCard());
            player_info[order[0]].AddHandCard(TouchCard());
        }

        /// <summary>
        /// 根据指示牌添加宝牌
        /// </summary>
        /// <param name="indicator">指示牌</param>
        private void _add_treasure_card(MahjongCard indicator)
        {
            int t = 0;

            // 将指示牌添加到指示牌列表
            card_indicator.Add(indicator);

            // 获得指示牌的下一张牌
            if (indicator.type == MahjongCardType.Char)
            {
                // 如果是角色牌，则使用官方排序
                // 每种角色9张
                t = (int)indicator.name + 1;
                if ((t == 0x0A) || (t == 0x14) || (t == 0x1E)) t -= 9;
            }
            else
            {
                // 如果是应援角色牌，团体牌，则使用自定义排序
                t = (int)indicator.name + 1;
                if (t == 0x22) t -= 3;
                if (t == 0x25) t -= 2;
                if (t == 0x31) t -= 5;
            }

            // 从所有的牌中找到宝牌，将其的宝牌等级+1
            // 先从牌山找
            for (int i = 0; i < card_stacks.Count(); i++)
            {
                // 确认宝牌
                if (t == (int)card_stacks[i].name) card_stacks[i].Treasure++;
            }

            // 从手牌和副露里找
            for (int i = 0; i < 4; i++)
            {
                player_info[i].SetTreasureCard((MahjongCardName)t);
            }
        }

        /// <summary>
        /// 开局确认宝牌（表宝牌）
        /// </summary>
        private void _RidgeTopCards()
        {
            // 第一张表宝牌指示牌是倒数第6张牌
            int j = total_cards - 6 + 2 * card_indicator.Count;

            // 添加宝牌指示牌
            _add_treasure_card(card_stacks[j]);
         }
    }
}
