using System;
using System.Collections.Generic;
using System.Text;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
        /// <summary>
        /// 清场，开局
        /// </summary>
        /// <param name="startPoint">配点</param>
        /// <param name="gamesCount">总场数</param>
        /// <returns>成功状态</returns>
        public bool NewGame_Handle(int startPoint, int gamesCount)
        {
            // 只有在完全初始化下才能开局
            if (gameStarted == false)
            {
                gameStarted = true; // 指示游戏已经开局

                // 重置游戏
                _ResetGame(startPoint, gamesCount);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 开始新的场次
        /// </summary>
        /// <returns>成功状态</returns>
        public bool NextScene()
        {
            // 由当前记录场次开始
            // 洗牌
            _ShuffleCards();

            // 砌牌
            _MasonryCards();

            // 翻开一张表宝牌
            _RidgeTopCards();

            // 重置巡数
            round = 0;

            return true;
        }

        /// <summary>
        /// 获得当前出牌的玩家
        /// </summary>
        public int Playing => order[playing];

        /// <summary>
        /// 获得玩家顺序，以便外部确认庄家和座次
        /// </summary>
        public int[] PlayerOrder => order;

        /// <summary>
        /// 获得玩家手牌
        /// </summary>
        /// <param name="player">玩家编号（按照外部玩家编号）</param>
        /// <returns></returns>
        public List<MahjongCard> GetPlayerCardOnHand(int player) => player_info[player].card_onhand;

        /// <summary>
        /// 打牌
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="card">要打的牌</param>
        /// <returns>成功状态</returns>
        public bool PlayCard(int player, MahjongCard card)
        {
            // 只有当前的玩家可以打牌
            if (player == Playing)
            {


                return true;
            }

            return false;
        }
    }
}
