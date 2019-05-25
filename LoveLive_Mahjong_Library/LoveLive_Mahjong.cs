using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace LoveLive_Mahjong_Library
{
    /// <summary>
    /// 牌名（顺序用于确定宝牌）
    /// </summary>
    public enum MahjongCardName
    {
        // μ's
        Honoka = 0x01,
        Eli = 0x02,
        Kotori = 0x03,
        Umi = 0x04,
        Rin = 0x05,
        Maki = 0x06,
        Nozomi = 0x07,
        Hanayo = 0x08,
        Nico = 0x09,

        // Aqours
        Chika = 0x0B,
        Riko = 0x0C,
        Kanan = 0x0D,
        Dia = 0x0E,
        You = 0x0F,
        Yoshiko = 0x10,
        Hanamaru = 0x11,
        Mari = 0x12,
        Ruby = 0x13,

        // 虹ヶ咲学園スクールアイドル同好会
        Ayu = 0x15,
        Kasumi = 0x16,
        Shizuku = 0x17,
        Karin = 0x18,
        Ai = 0x19,
        Kanata = 0x1A,
        Setsuna = 0x1B,
        Emma = 0x1C,
        Rina = 0x1D,

        // A-RISE
        Tsubasa = 0x1F,
        Erena = 0x20,
        Anju = 0x21,

        // Saint Snow
        Seira = 0x23,
        Ria = 0x24,

        // 团体
        Muse = 0x26,
        Aqours = 0x27,
        Nijigasaki = 0x28,
        ARISE = 0x29,
        SaintSnow = 0x2A,
    };

    /// <summary>
    /// 牌种
    /// </summary>
    public enum MahjongCardType
    {
        Char,
        Group,
        Assist,
        Assgp,
    }

    /// <summary>
    /// 小队（仅限角色牌）
    /// </summary>
    public enum MahjongCardSquadType
    {
        Printemps,
        BiBi,
        LilyWhite,
        CYaRon,
        GulityKiss,
        AZALEA,
        Fanmitsu,
        Dengeki,
        SIF,
    };

    /// <summary>
    /// 年级（仅限角色牌）
    /// </summary>
    public enum MahjongCardGradeType
    {
        G1,
        G2,
        G3
    };

    /// <summary>
    /// 团体（仅限角色牌，应援角色牌）
    /// </summary>
    public enum MahjongCardGroupType
    {
        Muse,
        Aqours,
        Nijigasaki,
        ARISE,
        SaintSnow
    };

    /// <summary>
    /// 麻将牌（通用类）
    /// </summary>
    public struct MahjongCard : IEquatable<MahjongCard>
    {
        /// <summary>
        /// 牌面
        /// </summary>
        public MahjongCardName name;

        /// <summary>
        /// 牌种
        /// </summary>
        public MahjongCardType type;

        /// <summary>
        /// 牌名
        /// </summary>
        public string c_name;

        /// <summary>
        /// 是否幺九
        /// </summary>
        public bool yao9;

        /// <summary>
        /// 宝牌
        /// 初始宝牌只有非幺九牌有效
        /// 宝牌等级计算累加宝牌，0不是宝牌
        /// </summary>
        public int treasure;

        /// <summary>
        /// 为当前牌添加宝牌等级（只影响当前实例）
        /// </summary>
        public void AddTreasure() => treasure++;

        // 只有角色牌有效的属性
        /// <summary>
        /// 小队
        /// </summary>
        public MahjongCardSquadType squad;

        /// <summary>
        /// 年级
        /// </summary>
        public MahjongCardGradeType grade;

        // 只有角色牌和应援角色牌有效的属性
        /// <summary>
        /// 团体
        /// </summary>
        public MahjongCardGroupType group;

        /// <summary>
        /// 返回一个能代表此张牌的名称
        /// </summary>
        /// <returns></returns>
        public override string ToString() => c_name;

        /// <summary>
        /// 比对两张牌
        /// </summary>
        /// <param name="other">另一张牌</param>
        /// <returns></returns>
        public bool Equals(MahjongCard other) => name == other.name;

        /// <summary>
        /// 比对两张牌
        /// </summary>
        /// <param name="obj">另一张牌</param>
        /// <returns></returns>
        public override bool Equals(object obj) => Equals((MahjongCard)obj);

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(MahjongCard left, MahjongCard right) => left.name == right.name;

        public static bool operator !=(MahjongCard left, MahjongCard right) => left.name != right.name;
    }

    public static class LoveLive_MahjongClass
    {
        private const string ResoursePath = "LoveLiveMahjong.xml";

        public static readonly List<MahjongCard> CardInfo = new List<MahjongCard>();
        public static readonly List<MahjongYaku> YakuInfo = new List<MahjongYaku>();

        public static void InitializeMahjongClass()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string Path = (from path in assembly.GetManifestResourceNames() where path.Contains(ResoursePath) select path).First();
            Stream xStream = assembly.GetManifestResourceStream(Path);
            XElement xElement = XElement.Load(xStream);
            IEnumerable<XElement> cards = xElement.Elements("cards").Elements("card");

            CardInfo.Clear();

            foreach (XElement card in cards)
            {
                MahjongCard mahjongCard = new MahjongCard
                {
                    name = (MahjongCardName)Enum.Parse(typeof(MahjongCardName), card.Element("name").Value),
                    type = (MahjongCardType)Enum.Parse(typeof(MahjongCardType), card.Element("type").Value),
                    c_name = card.Element("c_name").Value,
                    yao9 = card.Element("yao9").Value.Equals("true") ? true : false,
                    group = (MahjongCardGroupType)Enum.Parse(typeof(MahjongCardGroupType), card.Element("group").Value),
                };


                if (mahjongCard.yao9)
                {
                    mahjongCard.treasure = 0;
                }
                else
                {
                    mahjongCard.treasure = card.Element("treasure").Value.Equals("true") ? 1 : 0;
                }

                if (mahjongCard.type == MahjongCardType.Char)
                {
                    mahjongCard.squad = (MahjongCardSquadType)Enum.Parse(typeof(MahjongCardSquadType), card.Element("squad").Value);
                    mahjongCard.grade = (MahjongCardGradeType)Enum.Parse(typeof(MahjongCardGradeType), $"G{card.Element("grade").Value}");
                }
                CardInfo.Add(mahjongCard);
            }

            IEnumerable<XElement> yakus = xElement.Elements("yakus").Elements("yaku");

            YakuInfo.Clear();

            foreach (XElement yaku in yakus)
            {
                MahjongYaku mahjongYaku = new MahjongYaku()
                {
                    type = (MahjongYakuType)Enum.Parse(typeof(MahjongYakuType), yaku.Element("name").Value),
                    c_name = yaku.Element("c_name").Value,
                    level = Convert.ToInt32(yaku.Element("level").Value),
                    no_furu = yaku.Element("nofuru").Value.Equals("true") ? true : false,
                    furu_n = yaku.Element("furun").Value.Equals("true") ? true : false,
                };

                YakuInfo.Add(mahjongYaku);
            }
        }

        public static MahjongCard GetCard(MahjongCardName cardName) => (from c in CardInfo where c.name == cardName select c).First();
    }

    /// <summary>
    /// 麻将场次
    /// </summary>
    public enum MahjongGame
    {
        /// <summary>
        /// 场风：应援角色牌
        /// </summary>
        East,
        /// <summary>
        /// 场风：μ's团体牌
        /// </summary>
        South,
        /// <summary>
        /// 场风：Aqours团体牌
        /// </summary>
        West,
        /// <summary>
        /// 场风：虹之咲团体牌
        /// </summary>
        North
    };

    /// <summary>
    /// 副露种类
    /// </summary>
    public enum FuruType
    {
        /// <summary>
        /// 吃（年级）
        /// </summary>
        ChiGrade,
        /// <summary>
        /// 吃（小组）
        /// </summary>
        ChiSquad,
        /// <summary>
        /// 碰
        /// </summary>
        Pong,
        /// <summary>
        /// 大明杠
        /// </summary>
        Kong,
        /// <summary>
        /// 暗杠
        /// </summary>
        Kong_Self,
        /// <summary>
        /// 加杠（小明杠）
        /// </summary>
        Kong_Add
    }

    /// <summary>
    /// 副露牌
    /// </summary>
    public class MahjongCardFuru
    {
        public MahjongCard this[int index] => cards[index];

        /// <summary>
        /// 副露的牌
        /// </summary>
        public List<MahjongCard> cards;

        /// <summary>
        /// 副露种类
        /// </summary>
        public FuruType type;

        /// <summary>
        /// 副露对象
        /// </summary>
        public int target;
    }

    /// <summary>
    /// 役种
    /// </summary>
    public enum MahjongYakuType
    {
        /// <summary>
        /// 宝牌
        /// </summary>
        Dora,
        /// <summary>
        /// 里宝牌
        /// </summary>
        Ridora,
        /// <summary>
        /// 赤宝牌
        /// </summary>
        Akdora,
        /// <summary>
        /// 立直
        /// </summary>
        Richi,
        /// <summary>
        /// 一発
        /// </summary>
        Ippatsu,
        /// <summary>
        /// 自摸
        /// </summary>
        Tsumo,
        /// <summary>
        /// 平和
        /// </summary>
        Pinfu,
        /// <summary>
        /// 一盃口
        /// </summary>
        Ipeko,
        /// <summary>
        /// キャラ純
        /// </summary>
        Char,
        /// <summary>
        /// 役牌
        /// </summary>
        Yaku,
        /// <summary>
        /// リーダー応援無し
        /// </summary>
        Danyao,
        /// <summary>
        /// 搶槓
        /// </summary>
        Kong,
        /// <summary>
        /// 嶺上開花
        /// </summary>
        Saki,
        /// <summary>
        /// 海底摸月
        /// </summary>
        Sea,
        /// <summary>
        /// 河底撈魚
        /// </summary>
        River,
        /// <summary>
        /// ダブル立直
        /// </summary>
        DoubleRichi,
        /// <summary>
        /// 三団同年
        /// </summary>
        SameGrade,
        /// <summary>
        /// 七対子
        /// </summary>
        Chitoi,
        /// <summary>
        /// 対々和
        /// </summary>
        Toitoi,
        /// <summary>
        /// 三暗刻
        /// </summary>
        Anko3,
        /// <summary>
        /// 三槓子
        /// </summary>
        Kong3,
        /// <summary>
        /// 混単推し
        /// </summary>
        Honoshi,
        /// <summary>
        /// Saint Snow
        /// </summary>
        Saint,
        /// <summary>
        /// リーダー推し
        /// </summary>
        Leader,
        /// <summary>
        /// A-RISE
        /// </summary>
        Arise,
        /// <summary>
        ///  二盃口
        /// </summary>
        Nipeko,
        /// <summary>
        /// 団体単推し
        /// </summary>
        Group,
        /// <summary>
        /// 全力応援
        /// </summary>
        Zenryoku,
        /// <summary>
        /// 流し満貫
        /// </summary>
        Nagashi,
        /// <summary>
        /// 純正単推し
        /// </summary>
        Junsei,
        /// <summary>
        /// 誰でも大好き
        /// </summary>
        DD,
        /// <summary>
        /// 四暗刻
        /// </summary>
        Anko4,
        /// <summary>
        /// 三団体推し
        /// </summary>
        Daisangen,
        /// <summary>
        /// 四槓子
        /// </summary>
        Kong4,
        /// <summary>
        /// 応援推し
        /// </summary>
        Ouen,
        /// <summary>
        /// 天和
        /// </summary>
        Tenho,
        /// <summary>
        /// 地和
        /// </summary>
        Chiho,
        /// <summary>
        /// 応援四暗刻
        /// </summary>
        Anko4Ouen,
        /// <summary>
        /// 四暗刻単騎
        /// </summary>
        Anko4Wait1,
        /// <summary>
        /// 誰でも大好き十三面待ち
        /// </summary>
        DDWait13
    }

    /// <summary>
    /// 役
    /// </summary>
    public class MahjongYaku
    {
        public MahjongYakuType type;
        public string c_name;
        public int level;
        public bool no_furu;
        public bool furu_n;

        public override string ToString()
        {
            string f;
            if (level < 13)
            {
                f = $"{level}番";
            }
            else if (level == 13)
            {
                f = "役满";
            }
            else
            {
                f = "二倍满";
            }

            return $"[{f}] {c_name}";
        }
    }

    /// <summary>
    /// 牌组和法
    /// </summary>
    public enum HuCardType
    {
        /// <summary>
        /// 雀头
        /// </summary>
        Finch,
        /// <summary>
        /// 刻子/杠子
        /// </summary>
        PongKong,
        /// <summary>
        /// 年级顺子
        /// </summary>
        GradeChi,
        /// <summary>
        /// 小队顺子
        /// </summary>
        SquadChi,
        /// <summary>
        /// 十三幺（特殊）
        /// </summary>
        Yao13
    }

    /// <summary>
    /// 可和牌组
    /// </summary>
    public class HuCard
    {
        /// <summary>
        ///  要和的牌的牌组类型
        /// </summary>
        public HuCardType type;

        /// <summary>
        /// 是否副露得来
        /// </summary>
        public bool furu;

        /// <summary>
        /// 要和的牌组
        /// </summary>
        public List<MahjongCard> cards;

        public override string ToString() => $"牌组类型：{Enum.GetName(typeof(HuCardType), type)}，副露 = {furu}," +
                $" 牌组={string.Join(",", cards.Select(card => card.ToString()).ToArray())}";
    }

    /// <summary>
    /// 振听方式
    /// </summary>
    public enum WaitingTsumo
    {
        /// <summary>
        /// 没有振听
        /// </summary>
        None,
        /// <summary>
        /// 临时振听
        /// </summary>
        Temporary,
        /// <summary>
        /// 舍张振听
        /// </summary>
        Abandon,
        /// <summary>
        /// 立直振听
        /// </summary>
        Richi,
    }

    /// <summary>
    /// 玩家信息
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// 牌河
        /// </summary>
        public List<MahjongCard> card_played = new List<MahjongCard>();

        /// <summary>
        /// 手牌
        /// </summary>
        public List<MahjongCard> card_onhand = new List<MahjongCard>();

        /// <summary>
        /// 副露
        /// </summary>
        public List<MahjongCardFuru> card_furu = new List<MahjongCardFuru>();

        /// <summary>
        /// 清除该玩家所有的牌，并清除相应状态
        /// </summary>
        public void ClearCards()
        {
            card_played.Clear();
            card_onhand.Clear();
            card_furu.Clear();

            waiting_tsumo = WaitingTsumo.None;
            tanki = false;
            richi = false;
        }

        /// <summary>
        /// 向手牌中加入一张牌
        /// </summary>
        /// <param name="card">要加入的牌</param>
        public void AddHandCard(MahjongCard card) => card_onhand.Add(card);

        /// <summary>
        /// 向牌河中打一张牌
        /// </summary>
        /// <param name="card">要打的牌</param>
        public void PlayCard(MahjongCard card)
        {
            // 将牌从手牌打到牌河
            card_played.Add(card);

            // 从手牌中删除这张牌
            card_onhand.Remove(card);
        }

        /// <summary>
        /// 设置宝牌
        /// </summary>
        /// <param name="card"></param>
        public void SetTreasureCard(MahjongCardName cardName)
        {
            // 手牌中的宝牌
            for (int i = 0; i < card_onhand.Count; i++)
            {
                if (card_onhand[i].name == cardName)
                {
                    card_onhand[i].AddTreasure();
                }
            }

            // 副露里的宝牌
            for (int i = 0; i < card_furu.Count; i++)
            {
                for (int j = 0; j < card_furu[i].cards.Count; j++)
                {
                    if (card_furu[i].cards[j].name == cardName)
                    {
                        card_furu[i].cards[j].AddTreasure();
                    }
                }
            }
        }

        /// <summary>
        /// 听牌
        /// </summary>
        public List<MahjongCard> waiting = new List<MahjongCard>();

        /// <summary>
        /// 振听
        /// </summary>
        public WaitingTsumo waiting_tsumo = WaitingTsumo.None;

        /// <summary>
        /// 单骑听牌
        /// </summary>
        public bool tanki = false;

        /// <summary>
        /// 十三面听牌
        /// </summary>
        public bool waiting13 = false;

        /// <summary>
        /// 立直
        /// </summary>
        public bool richi = false;

        /// <summary>
        /// 点数
        /// </summary>
        public int points = 0;
    }

    /// <summary>
    /// 可副露状态
    /// </summary>
    public class FuruAble
    {
        /// <summary>
        /// 可以副露的牌组和种类
        /// </summary>
        public List<MahjongCardFuru> FuruableList;

        /// <summary>
        /// 对应的玩家
        /// </summary>
        public readonly int playerId;

        /// <summary>
        /// 创建一个可副露牌组
        /// </summary>
        public FuruAble(int playerId)
        {
            this.playerId = playerId;
            FuruableList = new List<MahjongCardFuru>();
        }
    }

    /// <summary>
    /// 可荣和状态
    /// </summary>
    public class RonAble
    {
        /// <summary>
        /// 可以荣和的牌
        /// </summary>
        public MahjongCard RonCard;

        /// <summary>
        /// 对应的玩家
        /// </summary>
        public readonly int playerId;

        /// <summary>
        /// 创建一个可荣牌组
        /// </summary>
        public RonAble(int playerId, MahjongCard RonCard)
        {
            this.playerId = playerId;
            this.RonCard = RonCard;
        }
    }

    /// <summary>
    /// 玩家可执行动作类型
    /// </summary>
    public enum PlayerActionType
    {
        /// <summary>
        /// 荣和
        /// </summary>
        Ron,
        /// <summary>
        /// 吃（年级）
        /// </summary>
        ChiGrade,
        /// <summary>
        /// 吃（小组）
        /// </summary>
        ChiSquad,
        /// <summary>
        /// 碰
        /// </summary>
        Pong,
        /// <summary>
        /// 大明杠
        /// </summary>
        Kong,
        /// <summary>
        /// 暗杠
        /// </summary>
        Kong_Self,
        /// <summary>
        /// 加杠（小明杠）
        /// </summary>
        Kong_Add,
        /// <summary>
        /// 自摸
        /// </summary>
        Tsumo,
        /// <summary>
        /// 取消
        /// </summary>
        Cancel
    }

    /// <summary>
    /// 玩家可执行动作
    /// </summary>
    public class PlayerAction
    {
        /// <summary>
        /// 玩家编号
        /// </summary>
        public int playerId;

        public PlayerAction(int playerId) => this.playerId = playerId;

        /// <summary>
        /// 此操作所影响的牌（如要吃碰杠的牌，要荣和自摸的牌）
        /// </summary>
        public List<MahjongCard> effectCards;

        /// <summary>
        /// 此操作的类型
        /// </summary>
        public PlayerActionType actionType;

        public PlayerAction(RonAble ronable)
        {
            playerId = ronable.playerId;
            actionType = PlayerActionType.Ron;
            effectCards = new List<MahjongCard>() { ronable.RonCard };
        }

        public PlayerAction(MahjongCardFuru furuable, int playerId)
        {
            this.playerId = playerId;
            switch (furuable.type)
            {
                case FuruType.ChiGrade:
                    actionType = PlayerActionType.ChiGrade;
                    break;
                case FuruType.ChiSquad:
                    actionType = PlayerActionType.ChiSquad;
                    break;
                case FuruType.Pong:
                    actionType = PlayerActionType.Pong;
                    break;
                case FuruType.Kong:
                    actionType = PlayerActionType.Kong;
                    break;
                case FuruType.Kong_Self:
                    actionType = PlayerActionType.Kong_Self;
                    break;
                case FuruType.Kong_Add:
                    actionType = PlayerActionType.Kong_Add;
                    break;
            }

            effectCards = furuable.cards;
        }

        /// <summary>
        /// 返回本操作的优先级
        /// </summary>
        /// <returns></returns>
        public int Priority
        {
            get
            {
                switch (actionType)
                {
                    case PlayerActionType.Tsumo:
                        return 0;
                    case PlayerActionType.Ron:
                        return 1;
                    case PlayerActionType.Kong:
                        return 2;
                    case PlayerActionType.Kong_Self:
                        return 2;
                    case PlayerActionType.Kong_Add:
                        return 2;
                    case PlayerActionType.Pong:
                        return 2;
                    case PlayerActionType.ChiGrade:
                        return 3;
                    case PlayerActionType.ChiSquad:
                        return 4;
                    case PlayerActionType.Cancel:
                        return 5;
                    default:
                        return 5;
                }
            }
        }

        public override string ToString()
        {
            return $"PlayerId = {playerId}, ActionType = {Enum.GetName(typeof(PlayerActionType), actionType)}";
        }
    }
}
