using System;
using System.Collections.Generic;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
        /// <summary>
        /// 专用骰子（随机数生成器）
        /// </summary>
        private readonly Random dice = new Random();

        /// <summary>
        /// 牌山
        /// </summary>
        private readonly List<MahjongCard> card_stacks = new List<MahjongCard>();

        private readonly PlayerInfo[] player_info = new PlayerInfo[4];

        /// <summary>
        /// 游戏场次
        /// </summary>
        private MahjongGame game;

        /// <summary>
        /// 总场次
        /// </summary>
        private MahjongGame total_game;

        /// <summary>
        /// 局和本场
        /// </summary>
        private int scene, subscene;

        /// <summary>
        /// 开杠总数
        /// </summary>
        private int kong_count = 0;

        /// <summary>
        /// 表宝牌指示牌
        /// </summary>
        private List<MahjongCard> card_indicator = new List<MahjongCard>();

        /// <summary>
        /// 玩家出牌顺序，第一位是庄家
        /// </summary>
        private readonly int[] order;

        /// <summary>
        /// 正在考虑出牌的玩家，0是庄家
        /// </summary>
        private readonly int playing;

        /// <summary>
        /// 牌的总数（来自数据库）
        /// </summary>
        private int total_cards => LoveLive_MahjongClass.CardInfo.Count;

        /// <summary>
        /// 每局共计巡数（0~75）
        /// 牌总数 - 开局配牌13张 * 4人 - 庄家1张 - 岭上4张宝牌指示牌10张共14张
        /// </summary>
        private int total_rounds => total_cards - 13 * 4 - 1 - 14;

        /// <summary>
        /// 当前巡数
        /// </summary>
        private int round;
    }
}
