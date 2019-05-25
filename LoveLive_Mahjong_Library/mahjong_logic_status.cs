using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LoveLive_Mahjong_Library
{
    partial class MahjongLogic
    {
        private bool gameStarted = false;

        public class GameStateMachine
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
                /// 接受玩家操作
                /// </summary>
                AcceptingPlayerOperation,

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
            private Queue<PlayerAction> queueActions;

            /// <summary>
            /// 从消息队列中取出一个玩家操作
            /// </summary>
            /// <returns></returns>
            public PlayerAction GetPlayerAction() => queueActions.Dequeue();
            
            /// <summary>
            /// （线程内使用）阻塞线程，等待状态的改变（always@）
            /// </summary>
            /// <returns></returns>
            public bool WaitSemaphore() => semaphore.WaitOne();

            /// <summary>
            /// 创建游戏状态机
            /// </summary>
            public GameStateMachine()
            {
                // 创建信号量
                semaphore = new Semaphore(0, 4);

                // 创建消息队列
                queueActions = new Queue<PlayerAction>();

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
            /// 玩家动作通道状态
            /// </summary>
            private bool playerActionChannelStatus = false;

            /// <summary>
            /// 发送玩家动作
            /// </summary>
            /// <param name="action"></param>
            public void SendPlayerAction(PlayerAction action)
            {
                // 若未打开通道，直接忽略消息
                if (playerActionChannelStatus)
                {
                    // 信号入队
                    queueActions.Enqueue(action);

                    // 处理状态
                    status = Status.AcceptingPlayerOperation;

                    // 释放信号量
                    semaphore.Release();
                }
            }

            /// <summary>
            /// 清空玩家动作消息队列
            /// </summary>
            public void ClearAction() => queueActions.Clear();

            /// <summary>
            /// 打开玩家动作通道
            /// </summary>
            public void OpenPlayerActionChannel()
            {
                // 清空动作消息队列
                ClearAction();

                // 重置信号量
                semaphore = new Semaphore(0, 4);

                // 打开通道
                playerActionChannelStatus = true;
            }

            /// <summary>
            /// 关闭玩家动作通道
            /// </summary>
            public void ClosePlayerActionChannel()
            {
                // 关闭通道
                playerActionChannelStatus = false;

                // 清空动作消息队列
                ClearAction();

                // 重置信号量
                semaphore = new Semaphore(0, 4);
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
