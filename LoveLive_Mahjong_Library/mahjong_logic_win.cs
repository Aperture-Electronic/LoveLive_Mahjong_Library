using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoveLive_Mahjong_Library
{
    partial class MahjongLogic
    {
        public List<MahjongYaku> YAKU() => _Yaku(false, false);

        /// <summary>
        /// 表示前一次调用_IsHu函数所计算出的可和牌组
        /// 请在使用_IsHu后立即调用以免数据被覆盖
        /// </summary>
        private List<HuCard> huCard = new List<HuCard>();

        // 和牌判定和番役计算
        public bool Waiting(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards) => _IsHu(Hand_Cards, Furu_Cards);

        /// <summary>
        /// 听牌判定（包括无役）
        /// </summary>
        /// <param name="Hand_Cards">手牌</param>
        /// <param name="Furu_Cards">副露牌</param>
        /// <returns>是否可和</returns>
        private bool _IsWaiting(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards)
        {
            List<MahjongCard> waiting = new List<MahjongCard>();

            for (int i = 0; i < LoveLive_MahjongClass.CardInfo.Count; i++)
            {
                List<MahjongCard> new_hand_cards = new List<MahjongCard>(Hand_Cards);
                new_hand_cards.Add(LoveLive_MahjongClass.CardInfo[i]);
                bool Hu = _IsHu(new_hand_cards, Furu_Cards);
                if (Hu) waiting.Add(LoveLive_MahjongClass.CardInfo[i]);
            }

            if (waiting.Count > 0) return true;
            else return false;
        }

        /// <summary>
        /// 和牌判定（包括无役）(请在调用一次_IsHu后立即调用计算番役的程序以便计算本次判定的番役）
        /// </summary>
        /// <param name="Hand_Cards">手牌</param>
        /// <param name="Furu_Cards">副露牌</param>
        /// <returns>是否可和</returns>
        private bool _IsHu(List<MahjongCard> Hand_Cards, List<MahjongCardFuru> Furu_Cards)
        {
            List<MahjongCard> hand_cards = new List<MahjongCard>(Hand_Cards);
            List<MahjongCardFuru> furu_cards = new List<MahjongCardFuru>(Furu_Cards);

            // 清空和牌牌组以便计算番役
            huCard.Clear();

            // 判断数量
            if (hand_cards.Count + furu_cards.Count * 3 < 14) return false;

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
                if ((from card in Hand_Cards where card.Yao9 == true select card).Count() == 14)
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
                    if (_IsHu(hu_cards))
                    {
                        // 将副露区的牌加入和牌牌组
                        foreach(MahjongCardFuru furu in Furu_Cards)
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
        private bool _IsHu(List<MahjongCard> cards) => _IsHu(cards, 0);

        /// <summary>
        ///  和牌判定（除去雀头和副露）
        /// </summary>
        /// <param name="cards">手牌</param>
        /// <param name="start">从第几张手牌开始找刻子</param>
        /// <returns>是否和牌</returns>
        private bool _IsHu(List<MahjongCard> cards, int start)
        {
            if (cards.Count == 0) return true; // 空牌和
            if (cards.Count < 3) return false; // 剩牌不和

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
                for (int m = 0; m < 3; m++) cards.RemoveAt(start);

                // 递归判和
                return _IsHu(cards);
            }
            else
            {
                if (start + 1 < cards.Count)
                {
                    return _IsHu(cards, start + 1);
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
                            else return false;

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
                                    if (!(p || q || r)) break;
                                }

                                // 递归判和
                                return _IsHu(cards);
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
        private List<MahjongYaku> _Yaku(bool Status, bool TankiOrW13)
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
                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.DDWait13]);
                else
                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.DD]);

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
                if (query.Count() > 0) isFuru = true; // 有副露行为，是副露状态
                else isFuru = false; // 没有副露行为，是门前清状态

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
                                if(TankiOrW13)
                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Anko4Wait1]); // 四暗刻单骑
                                else
                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Anko4]); // 四暗刻
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
                                where (hu.cards.First().type == MahjongCardType.Group)
                                select hu;

                        subquery = from hu in huCard
                                   where hu.cards.First().type == MahjongCardType.Char
                                   select hu;

                        // 四种角色+一种团体
                        if ((query.Count() == 1) && (subquery.Count() == 4))
                        {
                            MahjongCardGroupType group_oshi = query.First().cards.First().group; // 记录团体
                            query = from hu in subquery
                                    where hu.cards.First().@group == group_oshi
                                    select hu;

                            if (query.Count() == 4) // 角色只有一种团体
                            {
                                isGroup = true;
                                yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Group]);
                            }
                        }
                    }

                    // 六番
                    // 纯正单推
                    query = from hu in huCard where hu.cards.First().type == MahjongCardType.Char select hu;
                    if (query.Count() == 5)
                    {
                        subquery = from hu in query group hu by hu.cards.First().@group into g select g.First();
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
                                    yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Honoshi]);
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
                    if (query.Count() == 5) yakus.Add(LoveLive_MahjongClass.YakuInfo[(int)MahjongYakuType.Char]);

                    // 断幺九
                    bool danyau = true;
                    foreach (HuCard hu in huCard)
                    {
                        if ((hu.type == HuCardType.Finch) || (hu.type == HuCardType.PongKong))
                        {
                            if (hu.cards.First().Yao9 == true)
                            {
                                danyau = false;
                                break;
                            }
                        }
                        else
                        {
                            foreach (MahjongCard card in hu.cards)
                            {
                                if (card.Yao9 == true)
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
            }

            // 检查状态役
            if (Status)
            {
                yakus.AddRange(_StatusYaku(isYakuman));
            }

            return yakus;
        }

        /// <summary>
        /// 检查并返回状态役
        /// </summary>
        /// <param name="Yakuman">是否有役满</param>
        /// <returns>状态役表</returns>
        private List<MahjongYaku> _StatusYaku(bool Yakuman)
        {
            return new List<MahjongYaku>();
        }

        /// <summary>
        /// 符的计算
        /// </summary>
        /// <returns>符数</returns>
        private int _Fu()
        {
            return 40;
        }
    }
}
