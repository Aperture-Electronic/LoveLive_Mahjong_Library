using System;
using System.Collections.Generic;
using System.Linq;

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
        /// 获得当前出牌的玩家（玩家编号）
        /// </summary>
        public int Playing => order[playing];

        /// <summary>
        /// 获得玩家顺序，以便外部确认庄家和座次
        /// </summary>
        public int[] PlayerOrder => order;

        /// <summary>
        /// 获得玩家信息
        /// </summary>
        /// <param name="player">玩家编号（按照外部玩家编号）</param>
        /// <returns></returns>
        public PlayerInfo GetPlayerInfo(int player) => player_info[player];

        /// <summary>
        /// 获得玩家手牌
        /// </summary>
        /// <param name="player">玩家编号（按照外部玩家编号）</param>
        /// <returns></returns>
        public List<MahjongCard> GetPlayerCardOnHand(int player) => player_info[player].card_onhand;

        /// <summary>
        /// 获得玩家牌河
        /// </summary>
        /// <param name="player">玩家编号（按照外部玩家编号）</param>
        /// <returns></returns>
        private List<MahjongCard> GetPlayerCardPlayed(int player) => player_info[player].card_played;

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
                // 获得当前玩家手牌
                List<MahjongCard> player_onhand = GetPlayerCardOnHand(player);

                // 只能从手牌中打出牌来
                IEnumerable<MahjongCard> card_to_play = from c in player_onhand where c == card select c;
                if (card_to_play.Count() == 0)
                {
                    return false;
                }

                // 玩家打出牌
                GetPlayerInfo(player).PlayCard(card_to_play.First());

                // 向游戏线程发送完成请求
                gameStateMachine.SetState(GameStateMachine.State.SendPlayerAction);
                gameStateMachine.ReleaseSemaphore();

                return true;
            }

            return false;
        }


        /// <summary>
        /// 玩家动作响应回调函数
        /// </summary>
        public Action<List<PlayerAction>> PlayerActionResponseCallback;

        /// <summary>
        /// 玩家动作请求接受回调函数
        /// </summary>
        public Action<int, bool> PlayerActionAcceptedCallback;

        /// <summary>
        /// 向当前等待队列发送玩家动作请求
        /// </summary>
        /// <param name="action"></param>
        public void SendPlayerAction(PlayerAction action)
        {
            // 状态机必须要在等待状态才可以接受请求
            // 多线程同步由状态机管理
            gameStateMachine.SendPlayerAction(action);
        }
    }
}
