using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
        private Thread gamingThread;
        public GameStateMachine gameStateMachine;

        /// <summary>
        /// 当前可供进行的玩家操作
        /// </summary>
        private List<IGrouping<int, PlayerAction>> PlayerActionsList;

        /// <summary>
        /// 当前动作预备列表
        /// </summary>
        private readonly List<PlayerAction> PrepareActionsList = new List<PlayerAction>();

        /// <summary>
        /// 打开游戏进度主线程
        /// </summary>
        public void StartGamingThread()
        {
            // 如果当前有线程在运行，则结束它
            if (gamingThread?.ThreadState == ThreadState.Running)
            {
                gamingThread.Abort();
            }

            // 创建新的线程
            gamingThread = new Thread(GamingThread);
            gamingThread.Start();

            // 创建进程控制状态机
            gameStateMachine = new GameStateMachine();
        }

        private void GamingThread()
        {
            // 多线程状态机
            for (; ; )
            {
                // 等待信号量
                gameStateMachine.WaitSemaphore();

                switch (gameStateMachine.status)
                {
                    case GameStateMachine.Status.Idle:

                        break;
                    case GameStateMachine.Status.WaitPlayerOperation:

                        break;
                    case GameStateMachine.Status.SendPlayerOperate:
                        // 创建响应
                        List<PlayerAction> playerActions = new List<PlayerAction>();

                        // 判定荣和
                        List<RonAble> RonAbles = isCanRon();
                        if (RonAbles.Count() > 0)
                        {
                            // 发出可以荣和的消息到玩家
                            // 先根据玩家编号分类
                            IEnumerable<RonAble> player_ronable = from ronable in RonAbles group ronable by ronable.playerId into g select g.First();
                            foreach (RonAble ronable in player_ronable)
                            {
                                playerActions.Add(new PlayerAction(ronable));
                            }
                        }

                        // 判定鸣牌的可能性
                        List<FuruAble> Furuables = isCanFuru();
                        if (Furuables.Count > 0)
                        {
                            // 发出可以鸣牌的消息到玩家
                            // 先根据玩家编号分类
                            IEnumerable<FuruAble> player_furuable = from furuable in Furuables group furuable by furuable.playerId into g select g.First();
                            foreach (FuruAble furuable in player_furuable)
                            {
                                foreach (MahjongCardFuru furu in furuable.FuruableList)
                                {
                                    playerActions.Add(new PlayerAction(furu, furuable.playerId));
                                }
                            }
                        }

                        // 清空动作预备列表
                        PrepareActionsList.Clear();

                        // 打开玩家动作通道
                        gameStateMachine.OpenPlayerActionChannel();

                        if ((RonAbles.Count > 0) || (Furuables.Count > 0))
                        {
                            // 保存可能的玩家操作列表
                            PlayerActionsList = (from act in playerActions group act by act.playerId into g select g).ToList();

                            // 获得用户响应，将向其他用户广播其响应
                            // 使用响应回调函数
                            PlayerActionResponseCallback(playerActions);
                        }

                        // 进入接受用户响应的状态
                        gameStateMachine.SetStatus(GameStateMachine.Status.AcceptingPlayerOperation);

                        break;
                    case GameStateMachine.Status.AcceptingPlayerOperation:
                        // 取出队列
                        PlayerAction action = gameStateMachine.GetPlayerAction();

                        // 加入动作列表
                        PrepareActionsList.Add(action);

                        // 从可用列表中删除所有该玩家其他操作
                        PlayerActionsList.RemoveAll((match) => match.Key == action.playerId);

                        // 遍历其他所有动作进行优先级比对
                        bool isHighestPriority = true;
                        foreach (IGrouping<int, PlayerAction> player in PlayerActionsList)
                        {
                            foreach (PlayerAction player_act in player)
                            {
                                if (player_act.Priority <= action.Priority)
                                {
                                    isHighestPriority = false;
                                    break;
                                }
                            }
                            if (isHighestPriority) break;
                        }

                        if (isHighestPriority || (PlayerActionsList.Count == 0))
                        {
                            // 最高优先级
                            // 关闭动作通道
                            gameStateMachine.ClosePlayerActionChannel();

                            // 处理动作列表
                            int highestPriority = PrepareActionsList.Min((selector) => (selector.Priority));
                            IEnumerable<PlayerAction> accept_list = PrepareActionsList.FindAll((match) => match.Priority == highestPriority);
                            IEnumerable<PlayerAction> refuse_list = PrepareActionsList.Except(accept_list);

                            foreach(PlayerAction act in accept_list)
                            {
                                // 接受这些请求
                                PlayerActionAcceptedCallback(act.playerId, true);
                            }

                            foreach(PlayerAction act in refuse_list)
                            {
                                // 拒绝/取消这些请求
                                PlayerActionAcceptedCallback(act.playerId, false);
                            }

                            // 将没有反应（被忽略）的玩家拒绝
                            for (int i = 0; i < 4; i++)
                            {
                                if (i == Playing) continue;
                                if (PrepareActionsList.FindIndex((match) => match.playerId == i) < 0)
                                    PlayerActionAcceptedCallback(i, false);
                            }
                        }

                        break;
                    case GameStateMachine.Status.Exit:
                        return;
                }
            }
        }
    }
}
