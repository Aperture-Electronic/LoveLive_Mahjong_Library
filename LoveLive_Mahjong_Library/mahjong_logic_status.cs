using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LoveLive_Mahjong_Library
{
    partial class MahjongLogic
    {
        private bool gameStarted = false;

        public class GameStatusMachine
        {
            public enum Status
            {
                // 无操作状态
                Idle,

                /// <summary>
                /// 向玩家请求操作
                /// </summary>
                SendPlayerOperate,

                /// <summary>
                /// 等待玩家操作
                /// </summary>
                WaitPlayerOperation,

                /// <summary>
                /// 退出
                /// </summary>
                Exit,
            }

            /// <summary>
            /// 当前状态机状态
            /// </summary>
            public Status status { get; private set; }
            private Semaphore semaphore;
            
            /// <summary>
            /// （线程内使用）阻塞线程，等待状态的改变（always@）
            /// </summary>
            /// <returns></returns>
            public bool WaitSemaphore() => semaphore.WaitOne();

            /// <summary>
            /// 创建游戏状态机
            /// </summary>
            public GameStatusMachine()
            {
                // 创建信号量
                semaphore = new Semaphore(0, 1);

                // 重置状态
                status = Status.Idle;
            }

            /// <summary>
            /// 传递一个新的状态到线程
            /// </summary>
            /// <param name="status">新状态</param>
            public void SetStatus(Status status)
            {
                if (status == Status.Exit) throw new Exception($"不可以直接中断线程, 请调用{nameof(DirectlyExit)}()函数。");
                this.status = status;
                semaphore.Release();
            }

            /// <summary>
            /// 传递一个退出状态到线程，令线程退出
            /// </summary>
            public void DirectlyExit()
            {
                status = Status.Exit;

                // 释放信号量
                semaphore.Release();
            }
        }
    }
}
