using System.Collections.Generic;
using System.Threading;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
        private Thread gamingThread;
        public GameStatusMachine gameStatusMachine;

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
                        // 获得用户响应，将向其他用户广播其响应
                        // 判定荣和
                        

                        // 判定鸣牌的可能性
                        List<FuruAble> Furuables = isCanFuru();
                        if (Furuables.Count > 0)
                        {
                            // 发出可以鸣牌的消息到用户
                            // 先根据用户分类
                            var user_canfuru
                        }
                        else
                        {
                            // 没有可以鸣牌的操作，继续
                        }

                        break;
                    case GameStatusMachine.Status.Exit:
                        return;
                }
            }
        }
    }
}
