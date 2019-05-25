using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
        private Thread gamingThread;
        public GameStatusMachine gameStatusMachine;

        /// <summary>
        /// 当前可供进行的玩家操作
        /// </summary>
        private IEnumerable<IGrouping<int, PlayerAction>> PlayerActionsList;

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
            gameStatusMachine = new GameStatusMachine();
        }

        private void GamingThread()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // 多线程状态机
            for (; ; )
            {
                // 等待信号量
                gameStatusMachine.WaitSemaphore();

                switch (gameStatusMachine.status)
                {
                    case GameStatusMachine.Status.Idle:

                        break;
                    case GameStatusMachine.Status.WaitPlayerOperation:

                        break;
                    case GameStatusMachine.Status.SendPlayerOperate:
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

                        // 清空动作消息队列
                        gameStatusMachine.ClearAction();

                        if ((RonAbles.Count > 0) || (Furuables.Count > 0))
                        {
                            // 保存可能的玩家操作列表
                            PlayerActionsList = from act in playerActions group act by act.playerId into g select g;

                            // 获得用户响应，将向其他用户广播其响应
                            // 使用响应回调函数
                            PlayerActionResponseCallback(playerActions);
                        }

                        break;
                    case GameStatusMachine.Status.AcceptingPlayerOperation:
                        // 取出队列
                        PlayerAction action = gameStatusMachine.GetPlayerAction();

                        // 按照优先级处理

                        // 查询是否有别的同或更高优先级动作
                        IEnumerable<IGrouping<int, PlayerAction>> expected = from act in PlayerActionsList
                                                                             where act.Key != action.playerId
                                                                             select act;

                        bool hasHhighPriorityAction = false;
                        foreach (IGrouping<int, PlayerAction> player_acts in expected)
                        {
                            // 查询高优先级的可能动作
                            IEnumerable<PlayerAction> highPriorityActives = from act in player_acts
                                                                            where act.Priority <= action.Priority
                                                                            select act;

                            if (highPriorityActives.Count() > 0)
                            {
                                hasHhighPriorityAction = true;
                            }
                        }

                        if (hasHhighPriorityAction)
                        {
                            // 若有高优先级动作， 则将当前动作保存到预备列表，等待其他动作完成
                            PrepareActionsList.Add(action);
                        }
                        else
                        {
                            // 若无高优先级动作，则当前动作是最终动作，向其他玩家发送拒绝指令
                            // 并将拒绝指令添加到预备动作列表
                            for (int player = 0; player < 4; player++)
                            {
                                if (player == action.playerId) continue;
                                PlayerActionAcceptedCallback(player, false);
                                PrepareActionsList.Add(new PlayerAction(player)
                                {
                                    actionType = PlayerActionType.Cancel,
                                });
                            }
                        }

                        if (PrepareActionsList.Count >= 3)
                        {

                        }

                        break;
                    case GameStatusMachine.Status.Exit:
                        return;
                }
            }
        }
    }
}
