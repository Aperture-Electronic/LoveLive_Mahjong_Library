using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
        private Thread gamingThread;
        public GameStatusMachine gameStatusMachine;

        // 当前可供进行的玩家操作
        private IEnumerable<IGrouping<int, PlayerAction>> PlayerActionsList;

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

                switch(gameStatusMachine.status)
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
                        // 1. 自摸
                        // 2. 荣和
                        if (action.actionType == PlayerActionType.Ron)
                        {
                            // 查询是否有别的同或更高优先级动作
                            // 选择其他玩家
                            IEnumerable<IGrouping<int, PlayerAction>> expected = from act in PlayerActionsList
                                                                                 where act.Key != action.playerId
                                                                                 select act;

                            foreach (IGrouping<int, PlayerAction> player_acts in expected)
                            {
                                IEnumerable<PlayerAction> highPriorityActives = from act in player_acts
                                                                                where act.Priority <= action.Priority
                                                                                select act;

                            }
                        }

                        break;
                    case GameStatusMachine.Status.Exit:
                        return;
                }
            }
        }
    }
}
