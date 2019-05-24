#if DEBUG
#define UNITTEST
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace LoveLive_Mahjong_Library
{
    public partial class MahjongLogic
    {
#if UNITTEST
        // 单元测试接口
        public bool utIsHu(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards, out List<HuCard> huCards) => isHu(Hand_Cards, Furu_Cards, out huCards);
        public List<MahjongYaku> utCalcYaku(List<HuCard> huCards) => calcYaku(huCards, false, false);
        public int utCalcHuPoints(List<HuCard> huCards) => calcHuPoints(false, false, false, utCalcYaku(huCards), false, huCards);
        public List<MahjongCard> utIsWaiting(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards) => isWaiting(Hand_Cards, Furu_Cards);
#endif

        // 和牌判定和番役计算
        /// <summary>
        /// 听牌判定（包括无役）
        /// </summary>
        /// <param name="Hand_Cards">手牌</param>
        /// <param name="Furu_Cards">副露牌</param>
        /// <returns>是否可和</returns>
        private List<MahjongCard> isWaiting(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards)
        {
            List<MahjongCard> waiting = new List<MahjongCard>();

            for (int i = 0; i < LoveLive_MahjongClass.CardInfo.Count; i++)
            {
                List<MahjongCard> new_hand_cards = new List<MahjongCard>(Hand_Cards)
                {
                    LoveLive_MahjongClass.CardInfo[i]
                };
                bool Hu = isHu(new_hand_cards, Furu_Cards, out _);
                if (Hu)
                {
                    waiting.Add(LoveLive_MahjongClass.CardInfo[i]);
                }
            }

            return waiting;
        }

        /// <summary>
        /// 和牌判定（包括无役）(请在调用一次_IsHu后立即调用计算番役的程序以便计算本次判定的番役）
        /// </summary>
        /// <param name="Hand_Cards">手牌</param>
        /// <param name="Furu_Cards">副露牌</param>
        /// <returns>是否可和</returns>
        private bool isHu(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards, out List<HuCard> huCard)
        {
            List<MahjongCard> hand_cards = new List<MahjongCard>(Hand_Cards);
            List<MahjongCardFuru> furu_cards = new List<MahjongCardFuru>(Furu_Cards);

            // 清空和牌牌组以便计算番役
            huCard = new List<HuCard>();

            // 判断数量
            if (hand_cards.Count + furu_cards.Count * 3 < 14)
            {
                return false;
            }

            // 计算两个特殊役种：七对子，十三幺
            if (Furu_Cards.Count == 0)
            {
                // 七对子
                IEnumerable<MahjongCard> cards = from card in Hand_Cards group card by card.name into z where z.Count() == 2 select z.First();
                if (cards.Count() == 7)
                {
                    // 将要和的牌加入和牌牌组以便计算番役
                    foreach (MahjongCard card in cards)
                    {
                        HuCard hucard = new HuCard()
                        {
                            furu = false,
                            type = HuCardType.Finch,
                        };
                        hucard.cards = new List<MahjongCard>() { card, card };
                        huCard.Add(hucard);
                    }

                    return true;
                }

                // 十三幺
                if ((from card in Hand_Cards where card.yao9 == true select card).Count() == 14)
                {
                    // 将要和的牌加入和牌牌组以便计算番役
                    cards = from card in Hand_Cards group card by card.name into z where z.Count() == 1 select z.First();
                    if (cards.Count() == 12)
                    {
                        HuCard hucard = new HuCard()
                        {
                            furu = false,
                            type = HuCardType.Yao13,
                            cards = new List<MahjongCard>(Hand_Cards)
                        };
                        huCard.Add(hucard);
                        return true;
                    }
                }
            }

            // 排序手牌
            hand_cards.Sort((MahjongCard n1, MahjongCard n2) => n1.name.CompareTo(n2.name));

            // 寻找所有的雀头，并将其剩下的牌放入列表
            // 遍历所有的牌 （只遍历到最后一张牌的前一张）
            for (int i = 0; i < hand_cards.Count - 1; i++)
            {
                List<MahjongCard> hu_cards = new List<MahjongCard>(hand_cards);
                IEnumerable<MahjongCard> finch = from card in hand_cards where card.name == hand_cards[i].name select card;

                // 如果有两张以上重复，则判定为雀头
                if (finch.Count() >= 2)
                {
                    // 清空和牌牌组以便计算番役
                    huCard.Clear();

                    // 将雀头加入牌组
                    HuCard hucard = new HuCard()
                    {
                        type = HuCardType.Finch,
                        furu = false,
                        cards = new List<MahjongCard>() { hand_cards[i], hand_cards[i + 1] },
                    };
                    huCard.Add(hucard);

                    // 把雀头从手牌单独出来
                    hu_cards.RemoveAt(i);
                    hu_cards.RemoveAt(i);

                    // 避免重复，跳过同种的其他牌
                    i += finch.Count() - 1;

                    // 判断和牌
                    if (isHu(hu_cards, ref huCard))
                    {
                        // 将副露区的牌加入和牌牌组
                        foreach (MahjongCardFuru furu in Furu_Cards)
                        {
                            HuCard hu = new HuCard
                            {
                                furu = true,
                                cards = new List<MahjongCard>(furu.cards),
                            };
                            switch (furu.type)
                            {
                                case FuruType.ChiGrade:
                                    hu.type = HuCardType.GradeChi;
                                    break;
                                case FuruType.ChiSquad:
                                    hu.type = HuCardType.SquadChi;
                                    break;
                                case FuruType.Pong:
                                case FuruType.Kong_Add:
                                case FuruType.Kong:
                                    hu.type = HuCardType.PongKong;
                                    break;
                                case FuruType.Kong_Self: // 暗杠不破坏门前清
                                    hu.furu = false;
                                    hu.type = HuCardType.PongKong;
                                    break;
                            }
                            huCard.Add(hu);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 和牌判定（除去雀头和副露）
        /// </summary>
        /// <param name="cards">手牌</param>
        /// <returns>是否可和</returns>
        private bool isHu(List<MahjongCard> cards, ref List<HuCard> huCard) => isHu(cards, 0, ref huCard);

        /// <summary>
        ///  和牌判定（除去雀头和副露）
        /// </summary>
        /// <param name="cards">手牌</param>
        /// <param name="start">从第几张手牌开始找刻子</param>
        /// <returns>是否和牌</returns>
        private bool isHu(List<MahjongCard> cards, int start, ref List<HuCard> huCard)
        {
            if (cards.Count == 0)
            {
                return true; // 空牌和
            }

            if (cards.Count < 3)
            {
                return false; // 剩牌不和
            }

            // 针对第一张牌开始寻找刻子和顺子
            IEnumerable<MahjongCard> u = from card in cards where card.name == cards[start].name select card;
            int count = u.Count();

            if (count >= 3)
            {
                // 将刻子加入牌组
                HuCard hucard = new HuCard()
                {
                    type = HuCardType.PongKong,
                    furu = false,
                    cards = new List<MahjongCard>() { cards[start], cards[start + 1], cards[start + 2] },
                };
                huCard.Add(hucard);

                // 删除这个刻子
                for (int m = 0; m < 3; m++)
                {
                    cards.RemoveAt(start);
                }

                // 递归判和
                return isHu(cards, ref huCard);
            }
            else
            {
                if (start + 1 < cards.Count)
                {
                    return isHu(cards, start + 1, ref huCard);
                }
                else
                {
                    // 寻找顺子（年级和小组顺子）
                    MahjongCard cur_card = u.First();

                    // 获得牌种
                    MahjongCardType type = cur_card.type;

                    // 只有角色牌才可以凑成年级或小组顺子
                    if (type == MahjongCardType.Char)
                    {
                        // 获得团体、年级和小组信息
                        MahjongCardGroupType group = cur_card.group;
                        MahjongCardGradeType grade = cur_card.grade;
                        MahjongCardSquadType squad = cur_card.squad;

                        // 确认同团体的牌有至少三张
                        IEnumerable<MahjongCard> same_group = from card in cards where (card.name != cur_card.name) && (card.@group == @group) && (card.type == MahjongCardType.Char) select card;
                        if (same_group.Count() >= 2)
                        {
                            // 判断同年级/同小组
                            IEnumerable<MahjongCard> same_grade = from card in same_group where card.grade == grade select card;
                            IEnumerable<MahjongCard> same_squad = from card in same_group where card.squad == squad select card;
                            IEnumerable<MahjongCard> determine;

                            bool GradeOrSquad = false;
                            if (same_grade.Count() >= 2)
                            {
                                GradeOrSquad = false;
                                determine = same_grade;            // 判断同年级
                            }
                            else if (same_squad.Count() >= 2)
                            {
                                GradeOrSquad = true;
                                determine = same_squad;   // 判断同小队
                            }
                            else
                            {
                                return false;
                            }

                            // 每种牌只找出一张
                            IEnumerable<MahjongCard> not_same = from card in determine group card by card.name into g select g.First();

                            if (not_same.Count() >= 2)
                            {
                                MahjongCard[] not_same_cards = not_same.ToArray();

                                // 将顺子加入牌组
                                HuCard hucard = new HuCard()
                                {
                                    type = (GradeOrSquad) ? HuCardType.SquadChi : HuCardType.GradeChi,
                                    furu = false,
                                    cards = new List<MahjongCard>() { cur_card, not_same_cards[0], not_same_cards[1] },
                                };
                                huCard.Add(hucard);

                                // 从手牌中删除
                                bool p = true, q = true, r = true; // 防止重复删除
                                for (int i = 0; i < cards.Count; i++)
                                {
                                    if ((cards[i].name == cur_card.name) && p) { p = false; cards.RemoveAt(i); i--; continue; }
                                    if ((cards[i].name == not_same_cards[0].name) && q) { q = false; cards.RemoveAt(i); i--; continue; }
                                    if ((cards[i].name == not_same_cards[1].name) && r) { r = false; cards.RemoveAt(i); i--; continue; }
                                    if (!(p || q || r))
                                    {
                                        break;
                                    }
                                }

                                // 递归判和
                                return isHu(cards, ref huCard);
                            }
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 番役的计算
        /// （根据调用_IsHu函数后的huCard计算，请勿在这之前调用）
        /// </summary>
        /// <param name="Status">是否考虑状况役（当听牌判役时不考虑）</param>
        /// <param name="TankiOrW13">是否处于单骑听牌或13面听牌状态</param>
        private List<MahjongYaku> calcYaku(List<HuCard> huCard, bool Status, bool TankiOrW13)
        {
            // 为避免役型冲突设立的状态布尔
            bool isFuru = false; // 副露
            bool isChitoi = false; // 七对：可和断幺九、混一色、清一色、混老头，覆盖一杯口，被二杯口覆盖
            bool isYakuman = false; // 已经有役满以上的役种，不再累加役满一下的役种
            bool isKong4 = false; // 四杠子：不再计算四暗刻、四暗刻单骑和应援四暗刻
            bool isDaisangen = false; // 三团体推：和应援推冲突
            bool isGroup = false; // 团单推/纯正单推：和混单推冲突
            bool isLeader = false; // 队长同刻：和应援同组冲突
            bool isNipeko = false; // 二杯口：和一杯口冲突

            List<MahjongYaku> yakus = new List<MahjongYaku>();
            // 状况役：与手牌无关，与场上状况有关的役 
            // 行为役：立直 門前清自摸和 ダブル立直
            // 偶然役：一発 搶槓 嶺上開花 海底摸月 河底撈魚 ダブル立直 天和 地和
            // 特殊役：流し満貫
            // 加成役：单骑 十三面
            // 手役： 与手牌有关的役

            // 加番（累加）：宝牌 赤宝牌 里宝牌

            // 检查特殊役种 十三幺
            if (huCard.First().type == HuCardType.Yao13)
            {
                // 确定十三幺性质（十三面/正常十三幺）
                if (TankiOrW13)
                {
                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.DDWait13]);
                }
                else
                {
                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.DD]);
                }

                // 返回
                return yakus;
            }
            else
            {
                // 检查特殊役种 七对子
                IEnumerable<HuCard> query = from hu in huCard where hu.type == HuCardType.Finch select hu;
                IEnumerable<HuCard> subquery;
                if (query.Count() == 7)
                {
                    // 确认含有七对子
                    isChitoi = true; // 设置防止其它役冲突
                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Chitoi]); // 加入役
                }

                // 确定副露状态
                query = from hu in huCard where hu.furu == true select hu;
                if (query.Count() > 0)
                {
                    isFuru = true; // 有副露行为，是副露状态
                }
                else
                {
                    isFuru = false; // 没有副露行为，是门前清状态
                }

                // 由高到低确定番役
                // 计算刻子数
                query = from hu in huCard where hu.type == HuCardType.PongKong select hu;
                int pongkong_cnt = query.Count();

                // 役满以上
                // 非门前清役种
                if (pongkong_cnt == 4)
                {
                    // 多数刻子
                    // 応援推し
                    query = from hu in huCard
                            where hu.cards.First().type == MahjongCardType.Assist
                            group hu by hu.cards.First().name into gp
                            select gp.First();

                    if (query.Count() == 5) //包含四个刻子和一个雀头
                    {
                        isYakuman = true;
                        yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Ouen]);
                    }

                    // 四杠子
                    query = from hu in huCard where hu.cards.Count == 4 select hu;
                    if (query.Count() == 4)
                    {
                        isKong4 = true;
                        isYakuman = true;
                        yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Kong4]);
                    }
                }
                else if (pongkong_cnt == 3)
                {
                    // 没有应援推的情况下
                    if (!isDaisangen)
                    {
                        // 三刻子
                        // 三团体推
                        query = from hu in huCard where (hu.cards.First().type == MahjongCardType.Group) && (hu.type == HuCardType.PongKong) select hu;
                        if (query.Count() == 3)
                        {
                            isYakuman = true;
                            yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Daisangen]);
                        }
                    }
                }

                // 门前清役种
                if (!isFuru)
                {
                    if (!isKong4)
                    {
                        // 在没有四杠子的情况下
                        if (pongkong_cnt == 4)
                        {
                            // 多数刻子
                            // 四暗刻（包括四暗刻，応援四暗刻）
                            // 先查询带条件的应援四暗刻
                            query = from hu in huCard where (hu.cards.First().type == MahjongCardType.Assist) && (hu.type == HuCardType.PongKong) select hu;
                            if (query.Count() == 4)
                            {
                                isYakuman = true;
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Anko4Ouen]); // 应援四暗刻
                            }
                            else
                            {
                                isYakuman = true;
                                if (TankiOrW13)
                                {
                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Anko4Wait1]); // 四暗刻单骑
                                }
                                else
                                {
                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Anko4]); // 四暗刻
                                }
                            }
                        }
                    }
                }

                // 役满以下
                if (!isYakuman)
                {
                    // 满贯
                    if (pongkong_cnt == 4)
                    {
                        // 多数刻子
                        // 全力应援
                        // 四组应援角色刻子
                        query = from hu in huCard
                                where (hu.cards.First().type == MahjongCardType.Assist) && (hu.type == HuCardType.PongKong)
                                select hu;
                        if (query.Count() == 4)
                        {
                            // 角色雀头
                            query = from hu in huCard where hu.type == HuCardType.Finch select hu;
                            if (query.First().cards.First().type == MahjongCardType.Char)
                            {
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Zenryoku]);
                            }
                        }
                    }
                    else
                    {
                        // 団体単推し
                        query = from hu in huCard
                                where hu.cards.First().type == MahjongCardType.Group
                                select hu;

                        // 一种团体
                        if (query.Count() == 1)
                        {
                            MahjongCardGroupType group_oshi = query.First().cards.First().group; // 记录团体
                            // 剩余的牌全部是同一团体的角色牌
                            query = from hu in huCard where hu.cards.First().@group != group_oshi select hu;
                            subquery = from hu in huCard where hu.cards.First().type != MahjongCardType.Char select hu;

                            if ((query.Count() == 0) && (subquery.Count() == 1)) // 角色只有一种团体
                            {
                                isGroup = true;
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Group]);
                            }
                        }
                    }

                    // 六番
                    // 纯正单推
                    query = from hu in huCard where hu.cards.First().type != MahjongCardType.Char select hu;
                    if (query.Count() == 0) // 只有角色牌
                    {
                        subquery = from hu in huCard group hu by hu.cards.First().@group into g select g.First();
                        if (subquery.Count() == 1)
                        {
                            isGroup = true;
                            yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Junsei]);
                        }
                    }

                    // 三番
                    if (pongkong_cnt == 3)
                    {
                        // 三刻子
                        // 队长同刻
                        query = from hu in huCard
                                where (hu.type == HuCardType.PongKong) &&
                                ((hu.cards.First().name == MahjongCardName.Honoka) ||
                                (hu.cards.First().name == MahjongCardName.Chika) ||
                                (hu.cards.First().name == MahjongCardName.Ayu))
                                select hu;

                        if (query.Count() == 3)
                        {
                            isLeader = true;
                            yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Leader]);
                        }

                        // 没有队长同刻的情况下
                        if (!isLeader)
                        {
                            //应援同组(A-RISE)
                            query = from hu in huCard
                                    where (hu.type == HuCardType.PongKong) &&
                                    ((hu.cards.First().name == MahjongCardName.Tsubasa) ||
                                    (hu.cards.First().name == MahjongCardName.Anju) ||
                                    (hu.cards.First().name == MahjongCardName.Erena))
                                    select hu;

                            if (query.Count() == 3)
                            {
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Arise]);
                            }
                        }
                    }
                    else
                    {
                        if (!isFuru)
                        {
                            // 二杯口
                            query = from hu in huCard
                                    where (hu.type == HuCardType.GradeChi) || (hu.type == HuCardType.SquadChi)
                                    select hu;

                            if (query.Count() == 4)
                            {
                                // 至少有四组年级或小组的顺子
                                subquery = from hu in query
                                           group hu by hu.cards.First().name into g
                                           select g.First();

                                if (subquery.Count() == 2)
                                {
                                    isNipeko = true;

                                    // 只有两种顺子
                                    // 如果有七对子先删除七对子
                                    if (isChitoi)
                                    {
                                        for (int i = 0; i < yakus.Count; i++)
                                        {
                                            if (yakus[i].type == MahjongYakuType.Chitoi)
                                            {
                                                yakus.RemoveAt(i);
                                                break;
                                            }
                                        }
                                        isChitoi = false;
                                    }

                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Nipeko]);
                                }
                            }
                        }
                    }

                    // 二番
                    if (pongkong_cnt == 4)
                    {
                        // 多数刻子
                        // 对对和
                        yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Toitoi]);
                    }
                    else if (pongkong_cnt == 3)
                    {
                        // 三刻子
                        // 三暗刻
                        if (!isFuru)
                        {
                            yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Anko3]);
                        }

                        // 三杠子
                        query = from hu in huCard
                                where (hu.type == HuCardType.PongKong) && (hu.cards.Count() == 4)
                                select hu;

                        if (query.Count() == 3)
                        {
                            yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Kong3]);
                        }
                    }
                    else
                    {
                        // 没有团单推或纯正单推
                        if (!isGroup)
                        {
                            // 混单推
                            query = from hu in huCard where (hu.type == HuCardType.PongKong) && (hu.cards.First().type == MahjongCardType.Char) select hu;
                            subquery = from hu in query group hu by hu.cards.First().@group into g select g.First();
                            if (subquery.Count() == 1)
                            {
                                if ((from hu in huCard where (hu.type == HuCardType.Finch) select hu.cards.First()).First().type != MahjongCardType.Char)
                                {
                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Honoshi]);
                                }
                            }
                        }

                        // 没有队长同刻的情况下
                        if (!isLeader)
                        {
                            //应援同组(Saint Snow)
                            query = from hu in huCard
                                    where (hu.type == HuCardType.PongKong) &&
                                    ((hu.cards.First().name == MahjongCardName.Seira) ||
                                    (hu.cards.First().name == MahjongCardName.Ria))
                                    select hu;

                            if (query.Count() == 2)
                            {
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Saint]);
                            }
                        }

                        // 三团同年
                        query = from hu in huCard
                                where hu.type == HuCardType.GradeChi
                                group hu by hu.cards.First().@group into g
                                select g.First();

                        if (query.Count() == 3)
                        {
                            // 有三个团体都有
                            // 找到只有一组的团体，以该团体作为基准
                            query = from hu in huCard
                                    where hu.type == HuCardType.GradeChi
                                    group hu by hu.cards.First().@group into g
                                    where g.Count() == 1
                                    select g.First();

                            MahjongCardGroupType group = query.First().cards.First().group;
                            MahjongCardGradeType grade = query.First().cards.First().grade;
                            // 找出所有符合同年级的但非同组的，按组分组到不同的组中
                            query = from hu in huCard
                                    where (hu.type == HuCardType.GradeChi) && (hu.cards.First().grade == grade)
                                    && (hu.cards.First().@group != @group)
                                    group hu by hu.cards.First().@group into g
                                    select g.First();

                            if (query.Count() == 2)
                            {
                                // 如果剩余两组都拥有同年级
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.SameGrade]);
                            }
                        }
                    }

                    // 一番
                    // 角色纯
                    query = from hu in huCard
                            where (hu.cards.First().type == MahjongCardType.Char) || (hu.cards.First().type == MahjongCardType.Assist)
                            select hu;
                    if (query.Count() == 5)
                    {
                        yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Char]);
                    }

                    // 断幺九
                    bool danyau = true;
                    foreach (HuCard hu in huCard)
                    {
                        if ((hu.type == HuCardType.Finch) || (hu.type == HuCardType.PongKong))
                        {
                            if (hu.cards.First().yao9 == true)
                            {
                                danyau = false;
                                break;
                            }
                        }
                        else
                        {
                            foreach (MahjongCard card in hu.cards)
                            {
                                if (card.yao9 == true)
                                {
                                    danyau = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (danyau)
                    {
                        yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Danyao]);
                    }

                    if ((!isFuru) && (!isNipeko))
                    {
                        // 一杯口
                        query = from hu in huCard
                                where (hu.type == HuCardType.GradeChi) || (hu.type == HuCardType.SquadChi)
                                select hu;

                        if (query.Count() >= 2)
                        {
                            // 至少有两组年级或小组的顺子
                            IEnumerable<int> subsubquery = from hu in query
                                                           group hu by hu.cards.First().name into g
                                                           select g.Count();

                            if (subsubquery.Contains(2) || subsubquery.Contains(3))
                            {
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Ipeko]);
                            }
                        }
                    }
                }
                else
                {
                    // 有役满以上，检查并删除七对子
                    if (isChitoi)
                    {
                        for (int i = 0; i < yakus.Count; i++)
                        {
                            if (yakus[i].type == MahjongYakuType.Chitoi)
                            {
                                yakus.RemoveAt(i);
                                break;
                            }
                        }
                        isChitoi = false;
                    }
                }
            }

            // 检查状态役
            if (Status)
            {
                yakus.AddRange(calcStatusYaku(isYakuman));
            }

            return yakus;
        }

        /// <summary>
        /// 检查并返回状态役
        /// </summary>
        /// <param name="Yakuman">是否有役满</param>
        /// <returns>状态役表</returns>
        private List<MahjongYaku> calcStatusYaku(bool Yakuman)
        {
            // 状况役：与手牌无关，与场上状况有关的役 
            // 行为役：立直 門前清自摸和 ダブル立直
            // 偶然役：一発 搶槓 嶺上開花 海底摸月 河底撈魚 ダブル立直 天和 地和
            // 特殊役：流し満貫
            if (Yakuman)
            {
                // 役满以上，只计算天和，地和

            }
            else
            {

            }

            return new List<MahjongYaku>();
        }

        /// <summary>
        /// 符数计算
        /// </summary>
        /// <param name="chitoi">七对子</param>
        /// <returns>符数</returns>
        private int calcFu(List<HuCard> huCard, bool chitoi)
        {
            int fu = chitoi ? 25 : 20;
            int fu_add;

            // 遍历所有的和种
            foreach (HuCard hu in huCard)
            {
                if (hu.type == HuCardType.PongKong)
                {
                    // 刻子和杠子
                    fu_add = 2; // 两符起算
                    if (hu.cards.Count == 4)
                    {
                        fu_add *= 4; // 杠子: 4倍
                    }

                    if (hu.furu == false)
                    {
                        fu_add *= 2; // 暗刻/暗杠: 2倍
                    }

                    if (hu.cards[0].yao9 == true)
                    {
                        fu_add *= 2; // 幺九: 2倍
                    }

                    fu += fu_add;
                }

                if (hu.type == HuCardType.Finch)
                {
                    if (hu.cards[0].yao9 == true)
                    {
                        fu += 2;
                    }
                }
            }

            // 向上取整到十位
            fu = (int)(10 * Math.Ceiling(fu / 10.0));

            return fu;
        }

        /// <summary>
        /// 番数计算
        /// </summary>
        /// <param name="yakus">役种</param>
        /// <param name="furu">副露状态</param>
        /// <returns></returns>
        private int calcBan(List<MahjongYaku> yakus, bool furu)
        {
            int ban = 0;
            foreach (MahjongYaku yaku in yakus)
            {
                if (furu && yaku.furu_n)
                {
                    ban += yaku.level - 1;  //部分役种副露减一番
                }
                else
                {
                    ban += yaku.level;
                }
            }

            return ban;

        }

        /// <summary>
        /// 点数计算
        /// </summary>
        /// <param name="order">是否是庄家</param>
        /// <param name="tsumo">是否自摸</param>
        /// <param name="tsumo_order">自摸者是否庄家</param>
        /// <param name="yakus">役种</param>
        /// <param name="furu">副露状态</param>
        /// <returns></returns>
        private int calcHuPoints(bool order, bool tsumo, bool tsumo_order, List<MahjongYaku> yakus, bool furu, List<HuCard> huCards)
        {
            // 点数公式 100 * 向上取整(a*b*2^(c+2)/100)
            // a: 庄闲系数 b:符数 c: 番数
            int a, b, c;
            if (tsumo)  // 自摸
            {
                if (tsumo_order)
                {
                    a = 2;
                }
                else
                {
                    if (order)
                    {
                        a = 2;
                    }
                    else
                    {
                        a = 1;
                    }
                }
            }
            else
            {
                // 荣和
                if (order)
                {
                    a = 6;
                }
                else
                {
                    a = 4;
                }
            }

            // 先算番数
            c = calcBan(yakus, furu);

            if (c < 5)
            {
                // 5番（满贯）以下

                IEnumerable<MahjongYaku> chitoi = from yaku in yakus where yaku.type == MahjongYakuType.Chitoi select yaku;
                b = calcFu(huCards, chitoi.Count() > 0);

                double e = a * b * Math.Pow(2, c + 2) / 100.0;

                return 100 * (int)Math.Ceiling(e);
            }

            // 满贯以上
            switch (c)
            {
                case 5:
                case 6:
                    return a * 2000; // 满贯
                case 7:
                case 8:
                    return a * 3000; // 跳满
                case 9:
                case 10:
                    return a * 4000; // 倍满
                case 11:
                case 12:
                    return a * 6000; // 三倍满
                default:
                    return a * 8000 * (c / 13); //役满以上
            }
        }

        /// <summary>
        /// 能够鸣牌计算
        /// </summary>
        private List<FuruAble> isCanFuru()
        {
            // 获得刚刚打牌的玩家的牌河和打出的牌
            List<MahjongCard> played_cards = GetPlayerCardPlayed(Playing);
            MahjongCard last_played = played_cards.Last(); // 最后一张

            // 比对其他三家的手牌
            List<FuruAble> furuAbles = new List<FuruAble>();

            // 记录是否有玩家可以碰或者杠以减少不必要的计算
            bool hasPongKong = false;

            // 遍历所有其它玩家
            for (int player = 0; player < 4; player++)
            {
                if (player == Playing)
                {
                    continue; // 跳过自己
                }

                // 获得手牌
                List<MahjongCard> player_hand = GetPlayerCardOnHand(player);

                // 记录可副露牌组
                FuruAble furuAble = new FuruAble(player);

                // 如果已经有别的玩家可以碰杠，则不可能再有玩家可以碰杠
                if (hasPongKong == false)
                {
                    // 优先找出杠子和刻子
                    IEnumerable<MahjongCard> PongKong = from card in player_hand where card == last_played select card;
                    switch (PongKong.Count())
                    {
                        case 3:
                            // 可以杠
                            furuAble.FuruableList.Add(new MahjongCardFuru()
                            {
                                cards = Enumerable.Repeat(last_played, 4).ToList(),
                                target = Playing,
                                type = FuruType.Kong,
                            });
                            hasPongKong = true;
                            break;
                        case 2:
                            // 可以碰
                            furuAble.FuruableList.Add(new MahjongCardFuru()
                            {
                                cards = Enumerable.Repeat(last_played, 3).ToList(),
                                target = Playing,
                                type = FuruType.Pong,
                            });
                            hasPongKong = true;
                            break;
                        default:
                            break;
                    }
                }

                // 年级和小组顺子（吃）
                // 年级顺子
                MahjongCardName name = last_played.name;
                MahjongCardGradeType grade = last_played.grade;
                IEnumerable<IGrouping<MahjongCardName, MahjongCard>> grade_chi = from card in player_hand
                                                                                 where (card.grade == grade) && (card.name != name)
                                                                                 group card by card.name into g
                                                                                 select g;
                if (grade_chi.Count() == 2)
                {
                    // 至少要有两种同年级的牌才可以吃
                    // 获得要吃的牌
                    List<MahjongCard> chi = new List<MahjongCard>();
                    foreach (IGrouping<MahjongCardName, MahjongCard> cards in grade_chi)
                    {
                        chi.Add(cards.First());
                    }

                    // 加入可副露列表
                    furuAble.FuruableList.Add(new MahjongCardFuru()
                    {
                        cards = chi,
                        target = Playing,
                        type = FuruType.ChiGrade,
                    });
                }

                // 小组顺子
                MahjongCardSquadType squad = last_played.squad;
                IEnumerable<IGrouping<MahjongCardName, MahjongCard>> squad_chi = from card in player_hand
                                                                                 where (card.squad == squad) && (card.name != name)
                                                                                 group card by card.name into g
                                                                                 select g;
                if (squad_chi.Count() == 2)
                {
                    // 至少要有两种同小组的牌才可以吃
                    // 获得要吃的牌
                    List<MahjongCard> chi = new List<MahjongCard>();
                    foreach (IGrouping<MahjongCardName, MahjongCard> cards in squad_chi)
                    {
                        chi.Add(cards.First());
                    }

                    // 加入可副露列表
                    furuAble.FuruableList.Add(new MahjongCardFuru()
                    {
                        cards = chi,
                        target = Playing,
                        type = FuruType.ChiSquad,
                    });
                }

                furuAbles.Add(furuAble);
            }

            return furuAbles;
        }

        /// <summary>
        /// 能够荣和计算（根据当前所有玩家的听牌）
        /// </summary>
        private List<RonAble> isCanRon()
        {
            // 获得刚刚打牌的玩家的牌河和打出的牌
            List<MahjongCard> played_cards = GetPlayerCardPlayed(Playing);
            MahjongCard last_played = played_cards.Last(); // 最后一张

            // 比对其他三家的手牌
            List<RonAble> ronAbles = new List<RonAble>();

            // 遍历所有其它玩家
            for (int player = 0; player < 4; player++)
            {
                if (player == Playing)
                {
                    continue; // 跳过自己
                }

                // 获得玩家信息
                PlayerInfo info = player_info[player];

                // 先查询振听状态，如果振听则不能荣和
                if(info.waiting_tsumo == WaitingTsumo.None)
                {
                    // 查询是否是被听的牌
                    IEnumerable<MahjongCard> huCard = from card in info.waiting where card == last_played select card;

                    if (huCard.Count() > 0)
                    {
                        ronAbles.Add(new RonAble(player, last_played));
                    }
                }
            }

            return ronAbles;
        }
    }
}
