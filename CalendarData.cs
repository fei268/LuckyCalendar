using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Linq;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json.Linq;
using System.Reflection.Emit;
using System.Numerics;

public struct NineStarInfo
{
    public int StarNo;         // 星序号 1~9
    public string StarName;    // 一白、二黑……
    public string Luck;        // 旺、生、死、煞、退
    public string DeLing;      // 得令、失令
    public string ShiLing;     // 时令（或空字符串）
    public Color BgColor;      // 背景色（UI）
}

public class CalendarData : MonoBehaviour
{
    #region 基础字段
    // 天干
    private static readonly string[] TianGan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    // 地支
    private static readonly string[] DiZhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
    static readonly string[] ChineseDigits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    // 农历月名称
    private static readonly string[] LunarMonthNames = { "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月" };
    // 农历日名称
    private static readonly string[] LunarDayNames =
    {
        "初一","初二","初三","初四","初五","初六","初七","初八","初九","初十",
        "十一","十二","十三","十四","十五","十六","十七","十八","十九","二十",
        "廿一","廿二","廿三","廿四","廿五","廿六","廿七","廿八","廿九","三十"
    };
    // 天干五行映射
    private static readonly Dictionary<string, string> TianGanWuXing = new Dictionary<string, string>
    {
        { "甲", "木" }, { "乙", "木" },
        { "丙", "火" }, { "丁", "火" },
        { "戊", "土" }, { "己", "土" },
        { "庚", "金" }, { "辛", "金" },
        { "壬", "水" }, { "癸", "水" }
    };

    // 地支五行映射
    private static readonly Dictionary<string, string> DiZhiWuXing = new Dictionary<string, string>
    {
        { "子", "水" }, { "丑", "土" }, { "寅", "木" }, { "卯", "木" },
        { "辰", "土" }, { "巳", "火" }, { "午", "火" }, { "未", "土" },
        { "申", "金" }, { "酉", "金" }, { "戌", "土" }, { "亥", "水" }
    };

    // 三合组
    private static readonly string[][] SanHeGroups =
    {
        new []{"申","子","辰"},
        new []{"巳","酉","丑"},
        new []{"寅","午","戌"},
        new []{"亥","卯","未"}
    };

    // 六合组
    private static readonly string[][] LiuHeGroups =
    {
        new []{"子","丑"},
        new []{"寅","亥"},
        new []{"卯","戌"},
        new []{"辰","酉"},
        new []{"巳","申"},
        new []{"午","未"}
    };

    // 相冲
    private static readonly Dictionary<string, string> ChongPairs = new Dictionary<string, string>
    {
        {"子","午"}, {"午","子"},
        {"丑","未"}, {"未","丑"},
        {"寅","申"}, {"申","寅"},
        {"卯","酉"}, {"酉","卯"},
        {"辰","戌"}, {"戌","辰"},
        {"巳","亥"}, {"亥","巳"}
    };

    // 相害
    private static readonly Dictionary<string, string> HaiPairs = new Dictionary<string, string>
    {
        {"子","未"}, {"未","子"},
        {"丑","午"}, {"午","丑"},
        {"寅","巳"}, {"巳","寅"},
        {"卯","辰"}, {"辰","卯"},
        {"申","亥"}, {"亥","申"},
        {"酉","戌"}, {"戌","酉"}
    };
    // 吉时天干
    private static readonly Dictionary<string, string> ganHe = new Dictionary<string, string>
    {
        {"甲","己"}, {"乙","庚"}, {"丙","辛"}, {"丁","壬"}, {"戊","癸"},
        {"己","甲"}, {"庚","乙"}, {"辛","丙"}, {"壬","丁"}, {"癸","戊"}
    };
    // 凶时天干
    private static readonly Dictionary<string, string> ganKe = new Dictionary<string, string>
    {
        {"甲","庚"}, {"乙","辛"}, {"丙","壬"}, {"丁","癸"}, {"戊","甲"},
        {"己","乙"}, {"庚","丙"}, {"辛","丁"}, {"壬","戊"}, {"癸","己"}
    };
    // 十二时辰
    private static readonly (int startHour, string name)[] ShiChenTable =
    {
        (23, "子"), // 23:00 - 00:59
        (1,  "丑"), // 01:00 - 02:59
        (3,  "寅"), // 03:00 - 04:59
        (5,  "卯"), // 05:00 - 06:59
        (7,  "辰"), // 07:00 - 08:59
        (9,  "巳"), // 09:00 - 10:59
        (11, "午"), // 11:00 - 12:59
        (13, "未"), // 13:00 - 14:59
        (15, "申"), // 15:00 - 16:59
        (17, "酉"), // 17:00 - 18:59
        (19, "戌"), // 19:00 - 20:59
        (21, "亥")  // 21:00 - 22:59
    };
    // 每个日干对应子时起始天干索引
    private static readonly Dictionary<string, int> StartGanIndex = new()
    {
        {"甲", 0}, {"己", 0},
        {"乙", 2}, {"庚", 2},
        {"丙", 4}, {"辛", 4},
        {"丁", 6}, {"壬", 6},
        {"戊", 8}, {"癸", 8}
    };
    //日本固定假日
    private static readonly Dictionary<string, string> FixedHolidays = new()
    {
        {"01-01", "元日"},
        {"02-11", "建国記念の日"},
        {"02-23", "天皇誕生日"},
        {"04-29", "昭和の日"},
        {"05-03", "憲法記念日"},
        {"05-04", "みどりの日"},
        {"05-05", "こどもの日"},
        {"08-11", "山の日"},
        {"11-03", "文化の日"},
        {"11-23", "勤労感謝の日"},
    };
    //洛书吉时对应
    private static readonly Dictionary<string, string> LuoShuPairs = new Dictionary<string, string>
    {
        {"甲","己"}, {"乙","庚"}, {"丙","辛"}, {"丁","壬"}, {"戊","癸"},
        {"己","甲"}, {"庚","乙"}, {"辛","丙"}, {"壬","丁"}, {"癸","戊"}
    };
    //命卦
    private static readonly string[] GuaNames =
    {
        "",       // 占位，索引从1开始
        "坎",     // 1
        "坤",     // 2
        "震",     // 3
        "巽",     // 4
        "中宫",   // 5（特殊处理）
        "乾",     // 6
        "兑",     // 7
        "艮",     // 8
        "离"      // 9
    };
    //称骨 这个字典有另一个方法使用，不能修改甲子顺序
    private static readonly Dictionary<string, decimal> YearWeightTable = new Dictionary<string, decimal>()
{
    {"甲子", 1.2m}, {"乙丑", 0.9m}, {"丙寅", 0.6m}, {"丁卯", 0.7m}, {"戊辰", 1.2m},
    {"己巳", 0.5m}, {"庚午", 0.9m}, {"辛未", 0.8m}, {"壬申", 0.7m}, {"癸酉", 0.8m},
    {"甲戌", 1.5m}, {"乙亥", 0.9m}, {"丙子", 1.6m}, {"丁丑", 0.8m}, {"戊寅", 0.8m},
    {"己卯", 1.9m}, {"庚辰", 1.2m}, {"辛巳", 0.6m}, {"壬午", 0.8m}, {"癸未", 0.7m},
    {"甲申", 0.5m}, {"乙酉", 1.5m}, {"丙戌", 0.6m}, {"丁亥", 1.6m}, {"戊子", 1.5m},
    {"己丑", 0.7m}, {"庚寅", 0.9m}, {"辛卯", 1.2m}, {"壬辰", 1.0m}, {"癸巳", 0.7m},
    {"甲午", 1.5m}, {"乙未", 0.6m}, {"丙申", 0.5m}, {"丁酉", 1.4m}, {"戊戌", 1.4m},
    {"己亥", 0.9m}, {"庚子", 0.7m}, {"辛丑", 0.7m}, {"壬寅", 0.9m}, {"癸卯", 1.2m},
    {"甲辰", 0.8m}, {"乙巳", 0.7m}, {"丙午", 1.3m}, {"丁未", 0.5m}, {"戊申", 1.4m},
    {"己酉", 0.5m}, {"庚戌", 0.9m}, {"辛亥", 1.7m}, {"壬子", 0.5m}, {"癸丑", 0.7m},
    {"甲寅", 1.2m}, {"乙卯", 0.8m}, {"丙辰", 0.8m}, {"丁巳", 0.6m}, {"戊午", 1.9m},
    {"己未", 0.6m}, {"庚申", 0.8m}, {"辛酉", 1.6m}, {"壬戌", 1.0m}, {"癸亥", 0.6m}
};
    private static readonly decimal[] MonthWeightTable =
        { 0.6m, 0.7m, 1.8m, 0.9m, 0.5m, 1.6m, 0.9m, 1.5m, 1.8m, 0.8m, 0.9m, 0.5m };
    private static readonly decimal[] DayWeightTable =
        { 0.5m, 1.0m, 0.8m, 1.5m, 1.6m, 1.5m, 0.8m, 1.6m, 0.8m, 1.6m,
          0.9m, 1.7m, 0.8m, 1.7m, 1.0m, 0.8m, 0.9m, 1.8m, 0.5m, 1.5m,
          1.0m, 0.9m, 0.8m, 0.9m, 1.5m, 1.8m, 0.7m, 0.8m, 1.6m, 0.6m };
    private static readonly decimal[] HourWeightTable =
        { 1.6m, 0.6m, 0.7m, 1.0m, 0.9m, 1.6m,1.0m,0.8m,0.8m,0.9m,0.6m,0.6m };

    // 十二神（顺序固定）
    static string[] huangDao = { "青龙", "明堂", "天刑", "朱雀", "金匮", "天德", "白虎", "司命", "天牢", "玄武", "玉堂", "勾陈" };
    #endregion

    /// <summary>
    /// 计算农历和天干地支，同时返回数字方便其他方法调用
    /// </summary>
    public static (string LunarDate, string RunMonth, int LunarYear, int LunarMonth, int LunarDay, string GanZhiYear, string GanZhiMonth, string GanZhiDay, string GanZhiTime) GetChineseCalendar(DateTime date)
    {
        ChineseLunisolarCalendar chineseCalendar = new ChineseLunisolarCalendar();

        // 获取农历年、月、日
        int lunarYear = chineseCalendar.GetYear(date);
        int lunarMonthRaw = chineseCalendar.GetMonth(date); // 可能是 1..13 (若有闰月)
        int lunarDay = chineseCalendar.GetDayOfMonth(date);

        // 处理闰月：把 lunarMonthRaw 转换为真实 1..12，并记录 isLeapMonth
        int leapMonth = chineseCalendar.GetLeapMonth(lunarYear); // 0 表示无闰月
        bool isLeapMonth = false;
        int lunarMonth = lunarMonthRaw;

        if (leapMonth > 0)
        {
            if (lunarMonthRaw == leapMonth)
            {
                // 当前为闰月，如 leapMonth==9 表示闰8月，这时真实月份为 leapMonth-1
                isLeapMonth = true;
                lunarMonth = leapMonth - 1;
            }
            else if (lunarMonthRaw > leapMonth)
            {
                // 闰月之后的月份索引需减 1
                lunarMonth = lunarMonthRaw - 1;
            }
        }

        // 农历日期文本
        string runMonth = isLeapMonth ? "闰" + LunarMonthNames[lunarMonth - 1] : "";
        string lunarMonthStr = LunarMonthNames[lunarMonth - 1];
        string lunarDayStr = LunarDayNames[lunarDay - 1];

        // 天干地支计算
        string gzYear = GetGanZhiYear(lunarYear);
        string gzMonth = GetGanZhiMonth(date);
        string gzDay = GetGanZhiDay(date);
        string gzTime = GetGanZhiTime(gzDay.Substring(0, 1));

        return ($"{lunarMonthStr}{lunarDayStr}", runMonth, lunarYear, lunarMonth, lunarDay, gzYear, gzMonth, gzDay, gzTime);
    }

    /// <summary>
    /// 获取天干地支年
    /// </summary>
    static string GetGanZhiYear(int lunarYear)
    {
        int ganIndex = Mod(lunarYear - 4, 10); // 甲子年为公元 4 年对应索引 0
        int zhiIndex = Mod(lunarYear - 4, 12);
        return $"{TianGan[ganIndex]}{DiZhi[zhiIndex]}";
    }

    /// <summary>
    /// 获取天干地支月，公历
    /// 已校正数据2025.10.12
    /// </summary>
    public static string GetGanZhiMonth(DateTime date)
    {
        int effYear = date.Year;
        int effMonth = date.Month;

        // 当前月第一个节气时间（节气当天属新月）
        DateTime firstTerm = DateTime.Parse(Get24JieQi(effYear, effMonth)[1], CultureInfo.InvariantCulture).Date;
        if (date.Date < firstTerm)
        {
            effMonth--;
            if (effMonth == 0) { effMonth = 12; effYear--; }
            firstTerm = DateTime.Parse(Get24JieQi(effYear, effMonth)[1], CultureInfo.InvariantCulture).Date;
        }

        // 取立春时间来确定干支年
        DateTime lichun = DateTime.Parse(Get24JieQi(effYear, 2)[1], CultureInfo.InvariantCulture);
        int lichunYear = (lichun <= firstTerm) ? effYear : effYear - 1;

        // 计算立春到当前月的差值（月序，以立春所在2月为0）
        int diff = (effYear * 12 + effMonth) - (lichunYear * 12 + 2);
        int monthNum = ((diff % 12) + 12) % 12; // 0..11, 0=立春所在月

        // 地支：子起（这里寅月会是 index=2）
        int zhiIndex = (monthNum + 2) % 12; // +2 表示寅对应子起的第3位
        string monthZhi = DiZhi[zhiIndex];

        // 月干公式（仍保留 +2 偏移）
        int yearGanIndex = ((lichunYear - 4) % 10 + 10) % 10;
        int monthGanIndex = (yearGanIndex * 2 + monthNum + 2) % 10;
        string monthGan = TianGan[monthGanIndex];

        return monthGan + monthZhi;
    }

    /// <summary>
    /// 简单模运算，保证返回正数
    /// </summary>
    static int Mod(int x, int m)
    {
        int r = x % m;
        if (r < 0) r += m;
        return r;
    }

    /// <summary>
    /// 获取天干地支日
    /// </summary>
    static string GetGanZhiDay(DateTime date)
    {
        DateTime baseDate = new DateTime(1900, 2, 20);//甲子日
        int offsetDays = (date.Date - baseDate.Date).Days;

        // base 是 甲子 -> baseGanIndex = 0, baseZhiIndex = 0
        int ganIndex = Mod(offsetDays, 10);
        int zhiIndex = Mod(offsetDays, 12);

        return $"{TianGan[ganIndex]}{DiZhi[zhiIndex]}";
    }

    /// <summary>
    /// 获取时柱
    /// </summary>
    /// <param name="riGan">当天日干如甲乙丙</param>
    /// <returns></returns>
    static string GetGanZhiTime(string riGan)
    {
        int hour = DateTime.Now.Hour;

        // 找出当前时支
        var shi = ShiChenTable.Last(s => hour >= s.startHour || (s.startHour == 23 && hour < 1));
        string shiZhi = shi.name;
        int shiIndex = Array.IndexOf(DiZhi, shiZhi);

        // 推算时干
        int shiGanIndex = (StartGanIndex[riGan] + shiIndex) % 10;
        string shiGan = TianGan[shiGanIndex];

        return $"{shiGan}{shiZhi}";
    }

    #region 八字简批
    /// <summary>
    /// 根据天干或地支获取对应五行
    /// </summary>
    public static string GetFive(string ganZhi)
    {
        if (TianGanWuXing.ContainsKey(ganZhi))
            return TianGanWuXing[ganZhi];

        if (DiZhiWuXing.ContainsKey(ganZhi))
            return DiZhiWuXing[ganZhi];

        return "未知";
    }

    /// <summary>
    /// 八字五行数量
    /// </summary>
    /// <param name="baZi"></param>
    /// <returns></returns>
    public static Dictionary<string, int> CountFiveElements(string[] baZi)
    {
        var count = new Dictionary<string, int> { { "金", 0 }, { "木", 0 }, { "水", 0 }, { "火", 0 }, { "土", 0 } };

        foreach (var gz in baZi)
        {
            string gan = gz.Substring(0, 1);
            string zhi = gz.Substring(1, 1);
            count[GetFive(gan)]++;
            count[GetFive(zhi)]++;
        }

        return count;
    }

    /// <summary>
    /// 五行命理简判，根据八字五行数量
    /// </summary>
    /// <param name="wuXingCount"></param>
    /// <returns></returns>
    public static string GetDetailedComment(Dictionary<string, int> wuXingCount)
    {
        string comment = "";

        // 1️⃣ 统计五行
        var total = wuXingCount.Values.Sum();
        var max = wuXingCount.OrderByDescending(k => k.Value).First();
        var min = wuXingCount.OrderBy(k => k.Value).First();

        comment = $"五行统计：金{wuXingCount["金"]}、木{wuXingCount["木"]}、水{wuXingCount["水"]}、火{wuXingCount["火"]}、土{wuXingCount["土"]}\n";
        //comment += $"主旺：{max.Key}　偏弱：{min.Key}\n";

        // 2️⃣ 偏旺与偏弱分析
        comment += $"{LangManager.Get("WuXingStrong", max.Key)}\n";//【五行偏旺】
        comment += $"{LangManager.Get("WuXingWeak", min.Key)}\n";//【五行偏弱】

        // 3️⃣ 缺五行判断
        var missing = wuXingCount.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        if (missing.Count > 0)
        {
            foreach (var miss in missing)
                comment += $"{LangManager.Get("WuXingMissing", miss)}\n";//缺{miss}
        }

        // 4️⃣ 五行组合简批
        string comboKey = max.Key + min.Key;
        string comboText = LangManager.Get("WuXingCombo", comboKey);
        if (!string.IsNullOrEmpty(comboText))
            comment += comboText + "\n";

        // 5️⃣ 补益建议
        comment += $"{LangManager.Get("WuXingAdvice", min.Key)}";

        return comment;
    }
    #endregion

    /// <summary>
    /// 根据年返回中国/台湾/日本年份
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    public static (string japaneseYear, string minguoYear, string prcYear) GetYearsName(int year)
    {
        string japaneseYear = "";
        string minguoYear = "";
        string prcYear = "";

        // 日本年号
        if (year >= 2019) japaneseYear = $"令和{year - 2018}";
        else if (year >= 1989) japaneseYear = $"平成{year - 1988}";
        else if (year >= 1926) japaneseYear = $"昭和{year - 1925}";
        else if (year >= 1912) japaneseYear = $"大正{year - 1911}";
        else if (year >= 1868) japaneseYear = $"明治{year - 1867}";
        else japaneseYear = "未知";

        japaneseYear += "年";

        // 民国年
        if (year >= 1912) minguoYear = $"民國{year - 1911}年";

        // 中华人民共和国年
        if (year >= 1949) prcYear = $"中国{year - 1949 + 1}年";

        return (japaneseYear, minguoYear, prcYear);
    }

    /// <summary>
    /// 获取某天的十二建神信息（基于GetChineseCalendar）
    /// 已校正数据 2025.10.13
    /// </summary>
    public static (string Name, string Jx, string Yi, string Ji) GetTwelveGodInfo(DateTime date)
    {
        // 1. 当前日期农历信息
        var lunar = GetChineseCalendar(date);
        string lunarMonthName = lunar.LunarDate.Substring(0, 2); // 如 "正月","二月"...
        string monthZhi = lunar.GanZhiMonth.Substring(1, 1);
        string dayZhi = lunar.GanZhiDay.Substring(1, 1);

        var twelveGodData = LangManager.GetArray("TwelveGodData");
        if (twelveGodData == null || twelveGodData.Count < 12)
            return ("未知", "未知", "未知", "未知");

        // 月建起始表（规则1 使用）
        string[] monthJianStart = { "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥", "子", "丑" };
        // LunarMonthNames 你已有（假定在作用域可见）
        // string[] LunarMonthNames = { "正月","二月","三月","四月","五月","六月","七月","八月","九月","十月","冬月","腊月" };

        // 为了最小改动：我们从一个足够早的起点向前回溯一定天数，建立连续序列
        int daysBack = 30;
        DateTime start = date.AddDays(-daysBack).Date;
        DateTime end = date.Date;

        // 存放每天的建除序号（0..11）
        var indexMap = new Dictionary<DateTime, int>();

        // ---------------------------
        // 关键改动：按“太阳月”来记录规则1是否已触发（而不是按农历月名）。
        // solarMonthKey 采用 "yyyy-MM" 的形式，表示某个公历月的太阳月区段（以该公历月第一个节气为月首）
        // 我们在每个日期计算当日属于哪个 solar month (effYear, effMonth)，并以其为 key。
        // ---------------------------
        var seenRule1BySolarMonth = new HashSet<string>();

        // 记录前一日的索引（用于顺延）
        int? prevIndex = null;

        for (DateTime d = start; d <= end; d = d.AddDays(1))
        {
            var ld = GetChineseCalendar(d);
            string lmName = ld.LunarDate.Substring(0, 2);
            string dMonthZhi = ld.GanZhiMonth.Substring(1, 1);
            string dDayZhi = ld.GanZhiDay.Substring(1, 1);

            // --- 计算当天属于哪个“太阳月”（effYear/effMonth） ---
            int effYear, effMonth;
            {
                // 本月第一个节气
                DateTime firstThis;
                try
                {
                    firstThis = DateTime.Parse(Get24JieQi(d.Year, d.Month)[1], CultureInfo.InvariantCulture).Date;
                }
                catch
                {
                    // 若无法解析节气，则退回到用公历月作为 solar month（保守）
                    firstThis = new DateTime(d.Year, d.Month, 1).Date;
                }

                if (d.Date >= firstThis)
                {
                    effYear = d.Year; effMonth = d.Month;
                }
                else
                {
                    // 属于上一个公历月的太阳月
                    var prev = d.AddMonths(-1);
                    effYear = prev.Year; effMonth = prev.Month;
                }
            }

            string solarKey = $"{effYear:0000}-{effMonth:00}";

            // 判断当天是否为该月第一个节气（节气日：按 Get24JieQi(d.Year,d.Month)[1] 的 Date）
            bool isFirstTermOfSolarMonth = false;
            try
            {
                DateTime firstTerm = DateTime.Parse(Get24JieQi(d.Year, d.Month)[1], CultureInfo.InvariantCulture).Date;
                if (d.Date == firstTerm) isFirstTermOfSolarMonth = true;
            }
            catch { isFirstTermOfSolarMonth = false; }

            // ---------- 决定 d 的建除 idx ----------
            int? todayIdx = null;

            // 规则3：节气当天继承昨日（不进位）
            if (isFirstTermOfSolarMonth && prevIndex.HasValue)
            {
                todayIdx = prevIndex.Value;
                indexMap[d] = todayIdx.Value;
                // prevIndex 保持为 todayIdx，次日将基于该值顺延
                prevIndex = todayIdx;
                continue;
            }

            // 规则1：正月建寅、二月建卯……（但仅在该 solar month 首次触发）
            int mIdx = Array.IndexOf(LunarMonthNames, lmName);
            if (mIdx >= 0)
            {
                // 本 solar month 是否已经触发过规则1
                if (!seenRule1BySolarMonth.Contains(solarKey))
                {
                    string ruleZhi = monthJianStart[mIdx % 12];
                    if (dDayZhi == ruleZhi)
                    {
                        todayIdx = 0; // 建
                        seenRule1BySolarMonth.Add(solarKey); // 标记此 solar month 已触发规则1
                        indexMap[d] = todayIdx.Value;
                        prevIndex = todayIdx;
                        continue;
                    }
                }
            }

            // 规则2：月支 == 日支 直接为建（注意：如果规则1已在该 solar month 触发，规则2 仍可触发建，但不会重置规则1触发状态）
            if (dMonthZhi == dDayZhi)
            {
                todayIdx = 0;
                indexMap[d] = todayIdx.Value;
                prevIndex = todayIdx;
                continue;
            }

            // 其余情况按顺延（若 prevIndex 可用）
            if (prevIndex.HasValue)
            {
                todayIdx = (prevIndex.Value + 1) % 12;
                indexMap[d] = todayIdx.Value;
                prevIndex = todayIdx;
                continue;
            }

            // 如果没有 prevIndex（start 太近未能找到锚点），我们略过（默认不赋值），
            // 但通常 daysBack=30 足够避免这个情况。
        }

        // 取目标日的索引
        if (!indexMap.TryGetValue(date.Date, out int finalIdx))
            return ("未知", "未知", "未知", "未知");

        var entry = twelveGodData[finalIdx];
        if (entry == null) return ("未知", "未知", "未知", "未知");

        return (entry["Name"].ToString(), entry["JiXiong"].ToString(), entry["Yi"].ToString(), entry["Ji"].ToString());
    }

    /// <summary>
    /// 黄黑道十二神
    /// 已校对数据 2025.10.13
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static (string Name, string Type) GetHuangDaoShen(DateTime date)
    {
        var lunar = GetChineseCalendar(date);
        string monthZhi = lunar.GanZhiMonth.Substring(1, 1);
        string dayZhi = lunar.GanZhiDay.Substring(1, 1);

        // 对应起点
        var startMap = new Dictionary<string, string>
    {
        { "子", "申" }, { "午", "申" },
        { "丑", "戌" }, { "未", "戌" },
        { "寅", "子" }, { "申", "子" },
        { "卯", "寅" }, { "酉", "寅" },
        { "辰", "辰" }, { "戌", "辰" },
        { "巳", "午" }, { "亥", "午" }
    };

        if (!startMap.ContainsKey(monthZhi))
            return ("", "");

        int startIndex = Array.IndexOf(DiZhi, startMap[monthZhi]);
        int dayIndex = Array.IndexOf(DiZhi, dayZhi);

        if (startIndex == -1 || dayIndex == -1)
            return ("", "");

        int offset = (dayIndex - startIndex + 12) % 12;
        string name = huangDao[offset];

        // 黄道/黑道判定（假设固定规律：青龙、明堂、天德、朱雀、金匮、天德为黄道，其余黑道）
        string[] huang = { "青龙", "明堂", "天德", "金匮", "司命", "玉堂" };
        string type = huang.Contains(name) ? "黄道" : "黑道";

        return (name, type);
    }

    /// <summary>
    /// 根据干支获取对应的纳音五行
    /// </summary>
    public static string GetNaYin(string ganZhi)
    {
        return LangManager.Get("NaYinDict", ganZhi);
    }

    /// <summary>
    /// 根据出生日期判断生肖
    /// </summary>
    /// <param name="birth"></param>
    /// <returns>林下之猪</returns>
    public static string GetShengXiao(DateTime birth)
    {
        int year = birth.Year;

        // 立春处理
        string[] liChunStr = Get24JieQi(year, 2);
        DateTime liChun = DateTime.Parse(liChunStr[1]);
        if (birth < liChun) year--;

        // 计算干支
        int tgIndex = (year - 4) % 10;
        int dzIndex = (year - 4) % 12;
        string ganZhi = TianGan[tgIndex] + DiZhi[dzIndex];

        // 返回专有称谓
        return LangManager.Get("GanZhiName", ganZhi);
    }

    /// <summary>
    /// 判断当前地支与命主日支是否构成三合
    /// </summary>
    /// <param name="riZhi">命主日支</param>
    /// <param name="targetZhi">当前待检测地支</param>
    /// <returns>是否为吉时</returns>
    public static bool IsSanHe(string riZhi, string targetZhi)
    {
        // 找出日支所在的三合组
        var group = SanHeGroups.FirstOrDefault(g => g.Contains(riZhi));
        if (group == null)
            return false;

        // 日支本身不算，只需看其他两个地支是否包含目标地支
        return group.Contains(targetZhi) && targetZhi != riZhi;
    }

    /// 獲取彭祖百忌
    /// </summary>
    /// <param name="riGanZhi">日柱（如 "丙子"）</param>
    /// <returns>日干、日支彭祖百忌</returns>
    public static string GetBaiJi(string riGanZhi)
    {
        if (string.IsNullOrWhiteSpace(riGanZhi) || riGanZhi.Length < 2)
            return "未知";

        string riGan = riGanZhi.Substring(0, 1); // 取天干
        string riZhi = riGanZhi.Substring(1, 1); // 取地支

        string ganText = LangManager.Get("GanBaiJiDict", riGan);
        string zhiText = LangManager.Get("ZhiBaiJiDict", riZhi);

        return $"{ganText}，{zhiText}";
    }

    /// <summary>
    /// 将数字年份转换为中文大写
    /// </summary>
    /// <param name="year">公历或农历年份</param>
    /// <returns>中文大写年份，如 2025 -> "二零二五"</returns>
    public static string ConvertYearToChinese(int year)
    {
        string yearStr = year.ToString();
        string result = string.Empty;

        foreach (char digit in yearStr)
        {
            int num = digit - '0';
            result += ChineseDigits[num];
        }

        return result;
    }

    /// <summary>
    /// 获取十二时辰
    /// </summary>
    public static string GetShiChen(int hour)
    {
        // 23点 - 0点 属于子时
        if (hour == 23 || hour == 0)
            return "子";

        foreach (var (startHour, name) in ShiChenTable)
        {
            if (hour >= startHour && hour < startHour + 2)
                return name;
        }
        return "子"; // 默认返回子时
    }

    /// <summary>
    /// 获取命卦信息（卦名 + 东四/西四命）
    /// </summary>
    /// <param name="birthday">出生日期（注意立春分界）</param>
    /// <param name="isMale">是否男性，null 默认视为男</param>
    /// <returns></returns>
    public static string GetMingGua(DateTime birthday, bool? isMale = null)
    {
        bool male = isMale ?? true; // 默认男

        // 获取生日所在年的立春
        int year = birthday.Year;
        string[] liChunStr = Get24JieQi(year, 2); // 立春时间
        DateTime liChun = DateTime.Parse(liChunStr[1]);

        // 如果出生在立春前，算前一年
        if (birthday < liChun)
        {
            year -= 1;
        }

        int yy = year % 100; // 年份后两位
        int gua;

        if (male)
        {
            gua = 100 - yy;
            gua = gua % 9;
            if (gua == 0) gua = 9;
        }
        else
        {
            gua = yy + 5;
            gua = gua % 9;
            if (gua == 0) gua = 9;
        }

        // 特殊处理 5
        if (gua == 5)
        {
            gua = male ? 2 : 8;
        }

        string guaName = LangManager.GetArrayValue("Bagua", gua);

        JArray arr = JArray.Parse(LangManager.Get("UI", "LifeTypes")); //这里返回的结构是 [ "East Four Destiny", "West Four Destiny" ]

        // 判断东四命 / 西四命
        string dongXi = (gua == 1 || gua == 3 || gua == 4 || gua == 9)
            ? arr[0]?.ToString()
            : arr[1]?.ToString();

        return $"{guaName}-{dongXi}";
    }

    /// <summary>
    /// 根据出生年月日时计算总骨重并直接返回中文解读
    /// </summary>
    public static string CalculateWeight(DateTime birth, string bronTime)
    {
        // 获取农历信息
        var lunar = GetChineseCalendar(birth);
        int lunarYear = lunar.LunarYear;
        int lunarMonth = lunar.LunarMonth; // 1~12
        int lunarDay = lunar.LunarDay;     // 1~30

        // ===== 年权重 =====
        string ganZhi = lunar.GanZhiYear;
        decimal yearWeight = YearWeightTable.ContainsKey(ganZhi) ? YearWeightTable[ganZhi] : 0m;

        // ===== 月权重 =====
        decimal monthWeight = 0m;
        if (MonthWeightTable != null && lunarMonth >= 1 && lunarMonth <= MonthWeightTable.Length)
            monthWeight = MonthWeightTable[lunarMonth - 1];

        // ===== 日权重 =====
        decimal dayWeight = 0m;
        if (DayWeightTable != null && lunarDay >= 1 && lunarDay <= DayWeightTable.Length)
            dayWeight = DayWeightTable[lunarDay - 1];

        // ===== 时权重 =====
        decimal hourWeight = 0m;
        if (!string.IsNullOrEmpty(bronTime))
        {
            string[] parts = bronTime.Split('-');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0].Split(':')[0], out int startHour))
                {
                    int hourIndex = startHour / 2;
                    hourIndex = hourIndex % 12; // 0~11
                    if (HourWeightTable != null && hourIndex >= 0 && hourIndex < HourWeightTable.Length)
                        hourWeight = HourWeightTable[hourIndex];
                }
            }
        }

        decimal totalWeight = yearWeight + monthWeight + dayWeight + hourWeight;

        // ===== 查找解读 =====
        decimal key = Math.Round(totalWeight, 1);
        var list = LangManager.GetArray("ChengGu");
        if (list == null || list.Count == 0) return "";

        foreach (var item in list)
        {
            if (item.TryGetValue("Weight", out var val) && decimal.TryParse(val.ToString(), out var w))
            {
                if (Math.Abs(w - key) < 0.01m)
                return item.TryGetValue("Text", out var text) ? text.ToString() : "No interpretation.";
            }
        }

        return "";
    }

    /// <summary>
    /// 十二星座
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static (int No, string Name) GetZodiacSign(DateTime date)
    {
        int month = date.Month;
        int day = date.Day;
        int index = (month, day) switch
        {
            (3, >= 21) or (4, <= 19) => index = 0,
            (4, >= 20) or (5, <= 20) => index = 1,
            (5, >= 21) or (6, <= 21) => index = 2,
            (6, >= 22) or (7, <= 22) => index = 3,
            (7, >= 23) or (8, <= 22) => index = 4,
            (8, >= 23) or (9, <= 22) => index = 5,
            (9, >= 23) or (10, <= 23) => index = 6,
            (10, >= 24) or (11, <= 22) => index = 7,
            (11, >= 23) or (12, <= 21) => index = 8,
            (12, >= 22) or (1, <= 19) => index = 9,
            (1, >= 20) or (2, <= 18) => index = 10,
            (2, >= 19) or (3, <= 20) => index = 11,
            _ => index = -1
        };

        if (index >= 0)
            return (index, LangManager.GetArrayValue("ZodiacSign", index));
        else
            return (index, "未知");
    }

    /// <summary>
    /// 获取二十四节气 新历日期（1911-2040）
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns>返回数组格式: "立春,2021-02-03 22:58:39,雨水,2021-02-18 18:43:49"</returns>
    public static string[] Get24JieQi(int year, int month)
    {
        #region 节气字典
        string[] xiaohan = new string[]
        {
"1911-01-06 18:20:52",
"1912-01-07 00:07:29",
"1913-01-06 05:57:54",
"1914-01-06 11:42:51",
"1915-01-06 17:40:16",
"1916-01-06 23:27:47",
"1917-01-06 05:09:27",
"1918-01-06 11:04:23",
"1919-01-06 16:51:28",
"1920-01-06 22:40:47",
"1921-01-06 04:33:40",
"1922-01-06 10:16:55",
"1923-01-06 16:14:00",
"1924-01-06 22:05:33",
"1925-01-06 03:53:14",
"1926-01-06 09:54:17",
"1927-01-06 15:44:37",
"1928-01-06 21:31:11",
"1929-01-06 03:22:01",
"1930-01-06 09:02:32",
"1931-01-06 14:55:35",
"1932-01-06 20:45:03",
"1933-01-06 02:23:20",
"1934-01-06 08:16:27",
"1935-01-06 14:02:19",
"1936-01-06 19:46:37",
"1937-01-06 01:43:44",
"1938-01-06 07:31:08",
"1939-01-06 13:27:51",
"1940-01-06 19:23:40",
"1941-01-06 01:03:54",
"1942-01-06 07:02:18",
"1943-01-06 12:54:50",
"1944-01-06 18:39:15",
"1945-01-06 00:34:26",
"1946-01-06 06:16:19",
"1947-01-06 12:06:20",
"1948-01-06 18:00:13",
"1949-01-05 23:41:08",
"1950-01-06 05:38:43",
"1951-01-06 11:30:22",
"1952-01-06 17:09:45",
"1953-01-05 23:02:02",
"1954-01-06 04:45:17",
"1955-01-06 10:35:52",
"1956-01-06 16:30:17",
"1957-01-05 22:10:25",
"1958-01-06 04:04:20",
"1959-01-06 09:58:18",
"1960-01-06 15:42:27",
"1961-01-05 21:42:36",
"1962-01-06 03:34:56",
"1963-01-06 09:26:26",
"1964-01-06 15:22:20",
"1965-01-05 21:01:57",
"1966-01-06 02:54:20",
"1967-01-06 08:48:19",
"1968-01-06 14:26:10",
"1969-01-05 20:16:48",
"1970-01-06 02:01:39",
"1971-01-06 07:45:06",
"1972-01-06 13:41:50",
"1973-01-05 19:25:19",
"1974-01-06 01:19:55",
"1975-01-06 07:17:30",
"1976-01-06 12:57:22",
"1977-01-05 18:51:03",
"1978-01-06 00:43:12",
"1979-01-06 06:31:33",
"1980-01-06 12:28:53",
"1981-01-05 18:12:38",
"1982-01-06 00:02:35",
"1983-01-06 05:58:42",
"1984-01-06 11:40:51",
"1985-01-05 17:35:05",
"1986-01-05 23:28:02",
"1987-01-06 05:13:00",
"1988-01-06 11:03:30",
"1989-01-05 16:45:55",
"1990-01-05 22:33:14",
"1991-01-06 04:28:07",
"1992-01-06 10:08:31",
"1993-01-05 15:56:31",
"1994-01-05 21:48:07",
"1995-01-06 03:34:05",
"1996-01-06 09:31:27",
"1997-01-05 15:24:28",
"1998-01-05 21:18:09",
"1999-01-06 03:17:09",
"2000-01-06 09:00:42",
"2001-01-05 14:49:16",
"2002-01-05 20:43:30",
"2003-01-06 02:27:43",
"2004-01-06 08:18:33",
"2005-01-05 14:02:59",
"2006-01-05 19:46:57",
"2007-01-06 01:40:10",
"2008-01-06 07:24:50",
"2009-01-05 13:14:07",
"2010-01-05 19:08:46",
"2011-01-06 00:54:37",
"2012-01-06 06:43:54",
"2013-01-05 12:33:37",
"2014-01-05 18:24:10",
"2015-01-06 00:20:32",
"2016-01-06 06:08:21",
"2017-01-05 11:55:42",
"2018-01-05 17:48:41",
"2019-01-05 23:38:52",
"2020-01-06 05:29:59",
"2021-01-05 11:23:17",
"2022-01-05 17:13:54",
"2023-01-05 23:04:39",
"2024-01-06 04:49:09",
"2025-01-05 10:32:31",
"2026-01-05 16:22:53",
"2027-01-05 22:09:39",
"2028-01-06 03:54:19",
"2029-01-05 09:41:34",
"2030-01-05 15:30:11",
"2031-01-05 21:22:44",
"2032-01-06 03:15:40",
"2033-01-05 09:07:39",
"2034-01-05 15:04:02",
"2035-01-05 20:55:13",
"2036-01-06 02:42:59",
"2037-01-05 08:33:31",
"2038-01-05 14:26:13",
"2039-01-05 20:16:04",
"2040-01-06 02:03:00"
        };
        string[] dahan = new string[]
        {
            "1911-01-21 11:51:23",
"1912-01-21 17:29:06",
"1913-01-20 23:19:04",
"1914-01-21 05:11:49",
"1915-01-21 10:59:30",
"1916-01-21 16:53:33",
"1917-01-20 22:37:18",
"1918-01-21 04:24:32",
"1919-01-21 10:20:40",
"1920-01-21 16:04:20",
"1921-01-20 21:54:39",
"1922-01-21 03:47:54",
"1923-01-21 09:34:45",
"1924-01-21 15:28:21",
"1925-01-20 21:20:08",
"1926-01-21 03:12:25",
"1927-01-21 09:11:48",
"1928-01-21 14:56:36",
"1929-01-20 20:42:10",
"1930-01-21 02:32:56",
"1931-01-21 08:17:27",
"1932-01-21 14:06:45",
"1933-01-20 19:52:39",
"1934-01-21 01:36:51",
"1935-01-21 07:28:17",
"1936-01-21 13:12:13",
"1937-01-20 19:00:57",
"1938-01-21 00:58:42",
"1939-01-21 06:50:40",
"1940-01-21 12:44:03",
"1941-01-20 18:33:37",
"1942-01-21 00:23:28",
"1943-01-21 06:18:52",
"1944-01-21 12:07:04",
"1945-01-20 17:53:36",
"1946-01-20 23:44:35",
"1947-01-21 05:31:30",
"1948-01-21 11:18:22",
"1949-01-20 17:08:32",
"1950-01-20 22:59:35",
"1951-01-21 04:52:02",
"1952-01-21 10:38:22",
"1953-01-20 16:21:18",
"1954-01-20 22:11:01",
"1955-01-21 04:01:50",
"1956-01-21 09:48:16",
"1957-01-20 15:38:35",
"1958-01-20 21:28:27",
"1959-01-21 03:18:50",
"1960-01-21 09:10:02",
"1961-01-20 15:01:06",
"1962-01-20 20:57:53",
"1963-01-21 02:53:51",
"1964-01-21 08:41:03",
"1965-01-20 14:28:51",
"1966-01-20 20:19:39",
"1967-01-21 02:07:30",
"1968-01-21 07:54:06",
"1969-01-20 13:38:10",
"1970-01-20 19:23:47",
"1971-01-21 01:12:39",
"1972-01-21 06:58:59",
"1973-01-20 12:48:12",
"1974-01-20 18:45:40",
"1975-01-21 00:36:14",
"1976-01-21 06:25:06",
"1977-01-20 12:14:25",
"1978-01-20 18:03:59",
"1979-01-20 23:59:56",
"1980-01-21 05:48:37",
"1981-01-20 11:35:58",
"1982-01-20 17:30:53",
"1983-01-20 23:16:56",
"1984-01-21 05:05:02",
"1985-01-20 10:57:33",
"1986-01-20 16:46:12",
"1987-01-20 22:40:23",
"1988-01-21 04:24:17",
"1989-01-20 10:06:59",
"1990-01-20 16:01:33",
"1991-01-20 21:47:05",
"1992-01-21 03:32:29",
"1993-01-20 09:22:49",
"1994-01-20 15:07:24",
"1995-01-20 21:00:27",
"1996-01-21 02:52:30",
"1997-01-20 08:42:31",
"1998-01-20 14:46:04",
"1999-01-20 20:37:21",
"2000-01-21 02:23:03",
"2001-01-20 08:16:18",
"2002-01-20 14:02:01",
"2003-01-20 19:52:35",
"2004-01-21 01:42:22",
"2005-01-20 07:21:34",
"2006-01-20 13:15:18",
"2007-01-20 19:00:49",
"2008-01-21 00:43:31",
"2009-01-20 06:40:20",
"2010-01-20 12:27:41",
"2011-01-20 18:18:32",
"2012-01-21 00:09:49",
"2013-01-20 05:51:42",
"2014-01-20 11:51:14",
"2015-01-20 17:43:14",
"2016-01-20 23:27:04",
"2017-01-20 05:23:33",
"2018-01-20 11:08:58",
"2019-01-20 16:59:27",
"2020-01-20 22:54:33",
"2021-01-20 04:39:42",
"2022-01-20 10:38:56",
"2023-01-20 16:29:20",
"2024-01-20 22:07:08",
"2025-01-20 03:59:52",
"2026-01-20 09:44:39",
"2027-01-20 15:29:32",
"2028-01-20 21:21:37",
"2029-01-20 03:00:32",
"2030-01-20 08:54:01",
"2031-01-20 14:47:33",
"2032-01-20 20:30:53",
"2033-01-20 02:32:20",
"2034-01-20 08:26:49",
"2035-01-20 14:13:46",
"2036-01-20 20:10:31",
"2037-01-20 01:53:11",
"2038-01-20 07:48:17",
"2039-01-20 13:43:04",
"2040-01-20 19:20:26"
        };
        string[] lichun = new string[]
        {
            "1911-02-05 06:10:16",
"1912-02-05 11:53:31",
"1913-02-04 17:42:38",
"1914-02-04 23:29:16",
"1915-02-05 05:25:26",
"1916-02-05 11:13:58",
"1917-02-04 16:57:32",
"1918-02-04 22:53:05",
"1919-02-05 04:39:23",
"1920-02-05 10:26:26",
"1921-02-04 16:20:12",
"1922-02-04 22:06:24",
"1923-02-05 04:00:17",
"1924-02-05 09:49:32",
"1925-02-04 15:36:45",
"1926-02-04 21:38:16",
"1927-02-05 03:30:02",
"1928-02-05 09:16:22",
"1929-02-04 15:08:43",
"1930-02-04 20:51:07",
"1931-02-05 02:40:38",
"1932-02-05 08:29:20",
"1933-02-04 14:09:16",
"1934-02-04 20:03:37",
"1935-02-05 01:48:41",
"1936-02-05 07:29:16",
"1937-02-04 13:25:33",
"1938-02-04 19:14:58",
"1939-02-05 01:10:26",
"1940-02-05 07:07:32",
"1941-02-04 12:49:44",
"1942-02-04 18:48:34",
"1943-02-05 00:40:04",
"1944-02-05 06:22:55",
"1945-02-04 12:19:22",
"1946-02-04 18:03:53",
"1947-02-04 23:50:21",
"1948-02-05 05:42:00",
"1949-02-04 11:22:49",
"1950-02-04 17:20:46",
"1951-02-04 23:13:26",
"1952-02-05 04:52:54",
"1953-02-04 10:45:53",
"1954-02-04 16:30:41",
"1955-02-04 22:17:36",
"1956-02-05 04:11:55",
"1957-02-04 09:54:37",
"1958-02-04 15:49:11",
"1959-02-04 21:42:10",
"1960-02-05 03:23:09",
"1961-02-04 09:22:26",
"1962-02-04 15:17:20",
"1963-02-04 21:07:44",
"1964-02-05 03:04:55",
"1965-02-04 08:46:06",
"1966-02-04 14:37:48",
"1967-02-04 20:30:49",
"1968-02-05 02:07:23",
"1969-02-04 07:58:52",
"1970-02-04 13:45:42",
"1971-02-04 19:25:25",
"1972-02-05 01:20:13",
"1973-02-04 07:04:12",
"1974-02-04 13:00:05",
"1975-02-04 18:59:12",
"1976-02-05 00:39:28",
"1977-02-04 06:33:25",
"1978-02-04 12:26:57",
"1979-02-04 18:12:18",
"1980-02-05 00:09:28",
"1981-02-04 05:55:23",
"1982-02-04 11:45:28",
"1983-02-04 17:39:42",
"1984-02-04 23:18:44",
"1985-02-04 05:11:47",
"1986-02-04 11:07:42",
"1987-02-04 16:51:40",
"1988-02-04 22:42:49",
"1989-02-04 04:27:09",
"1990-02-04 10:14:00",
"1991-02-04 16:08:24",
"1992-02-04 21:48:17",
"1993-02-04 03:37:09",
"1994-02-04 09:30:56",
"1995-02-04 15:12:51",
"1996-02-04 21:07:54",
"1997-02-04 03:01:57",
"1998-02-04 08:56:52",
"1999-02-04 14:57:03",
"2000-02-04 20:40:24",
"2001-02-04 02:28:49",
"2002-02-04 08:24:05",
"2003-02-04 14:05:20",
"2004-02-04 19:56:13",
"2005-02-04 01:43:02",
"2006-02-04 07:27:16",
"2007-02-04 13:18:12",
"2008-02-04 19:00:24",
"2009-02-04 00:49:47",
"2010-02-04 06:47:51",
"2011-02-04 12:32:56",
"2012-02-04 18:22:23",
"2013-02-04 00:13:25",
"2014-02-04 06:03:15",
"2015-02-04 11:58:27",
"2016-02-04 17:46:00",
"2017-02-03 23:34:01",
"2018-02-04 05:28:25",
"2019-02-04 11:14:14",
"2020-02-04 17:03:12",
"2021-02-03 22:58:39",
"2022-02-04 04:50:36",
"2023-02-04 10:42:21",
"2024-02-04 16:26:53",
"2025-02-03 22:10:13",
"2026-02-04 04:01:51",
"2027-02-04 09:46:00",
"2028-02-04 15:30:53",
"2029-02-03 21:20:25",
"2030-02-04 03:08:04",
"2031-02-04 08:57:55",
"2032-02-04 14:48:32",
"2033-02-03 20:41:08",
"2034-02-04 02:40:41",
"2035-02-04 08:31:04",
"2036-02-04 14:19:25",
"2037-02-03 20:11:04",
"2038-02-04 02:03:12",
"2039-02-04 07:52:19",
"2040-02-04 13:39:17"
        };
        string[] yushui = new string[]
        {
            "1911-02-20 02:20:16",
"1912-02-20 07:55:34",
"1913-02-19 13:44:12",
"1914-02-19 19:37:54",
"1915-02-20 01:23:03",
"1916-02-20 07:17:59",
"1917-02-19 13:04:44",
"1918-02-19 18:52:41",
"1919-02-20 00:47:25",
"1920-02-20 06:28:57",
"1921-02-19 12:19:56",
"1922-02-19 18:16:09",
"1923-02-19 23:59:40",
"1924-02-20 05:51:16",
"1925-02-19 11:42:59",
"1926-02-19 17:34:43",
"1927-02-19 23:34:13",
"1928-02-20 05:19:12",
"1929-02-19 11:06:48",
"1930-02-19 16:59:45",
"1931-02-19 22:40:14",
"1932-02-20 04:28:19",
"1933-02-19 10:16:15",
"1934-02-19 16:01:37",
"1935-02-19 21:51:56",
"1936-02-20 03:33:00",
"1937-02-19 09:20:41",
"1938-02-19 15:19:33",
"1939-02-19 21:09:15",
"1940-02-20 03:03:42",
"1941-02-19 08:56:21",
"1942-02-19 14:46:46",
"1943-02-19 20:40:13",
"1944-02-20 02:27:05",
"1945-02-19 08:14:51",
"1946-02-19 14:08:30",
"1947-02-19 19:51:53",
"1948-02-20 01:36:38",
"1949-02-19 07:27:03",
"1950-02-19 13:17:29",
"1951-02-19 19:09:38",
"1952-02-20 00:56:40",
"1953-02-19 06:41:05",
"1954-02-19 12:32:18",
"1955-02-19 18:18:45",
"1956-02-20 00:04:37",
"1957-02-19 05:57:58",
"1958-02-19 11:48:26",
"1959-02-19 17:37:33",
"1960-02-19 23:26:17",
"1961-02-19 05:16:27",
"1962-02-19 11:14:34",
"1963-02-19 17:08:34",
"1964-02-19 22:57:17",
"1965-02-19 04:47:48",
"1966-02-19 10:37:47",
"1967-02-19 16:23:38",
"1968-02-19 22:09:13",
"1969-02-19 03:54:28",
"1970-02-19 09:41:46",
"1971-02-19 15:26:55",
"1972-02-19 21:11:23",
"1973-02-19 03:01:09",
"1974-02-19 08:58:41",
"1975-02-19 14:49:41",
"1976-02-19 20:39:55",
"1977-02-19 02:30:25",
"1978-02-19 08:20:57",
"1979-02-19 14:13:13",
"1980-02-19 20:01:38",
"1981-02-19 01:51:38",
"1982-02-19 07:46:31",
"1983-02-19 13:30:34",
"1984-02-19 19:16:13",
"1985-02-19 01:07:21",
"1986-02-19 06:57:31",
"1987-02-19 12:49:57",
"1988-02-19 18:35:07",
"1989-02-19 00:20:30",
"1990-02-19 06:14:01",
"1991-02-19 11:58:20",
"1992-02-19 17:43:30",
"1993-02-18 23:35:10",
"1994-02-19 05:21:38",
"1995-02-19 11:10:44",
"1996-02-19 17:00:43",
"1997-02-18 22:51:29",
"1998-02-19 04:54:53",
"1999-02-19 10:46:50",
"2000-02-19 16:33:18",
"2001-02-18 22:27:16",
"2002-02-19 04:13:18",
"2003-02-19 10:00:13",
"2004-02-19 15:49:59",
"2005-02-18 21:31:57",
"2006-02-19 03:25:34",
"2007-02-19 09:08:56",
"2008-02-19 14:49:33",
"2009-02-18 20:46:06",
"2010-02-19 02:35:37",
"2011-02-19 08:25:19",
"2012-02-19 14:17:35",
"2013-02-18 20:01:35",
"2014-02-19 01:59:29",
"2015-02-19 07:49:47",
"2016-02-19 13:33:41",
"2017-02-18 19:31:16",
"2018-02-19 01:17:57",
"2019-02-19 07:03:51",
"2020-02-19 12:56:53",
"2021-02-18 18:43:49",
"2022-02-19 00:42:50",
"2023-02-19 06:34:05",
"2024-02-19 12:12:58",
"2025-02-18 18:06:18",
"2026-02-18 23:51:39",
"2027-02-19 05:33:10",
"2028-02-19 11:25:42",
"2029-02-18 17:07:34",
"2030-02-18 22:59:34",
"2031-02-19 04:50:30",
"2032-02-19 10:31:49",
"2033-02-18 16:33:22",
"2034-02-18 22:29:43",
"2035-02-19 04:15:39",
"2036-02-19 10:13:46",
"2037-02-18 15:58:22",
"2038-02-18 21:51:33",
"2039-02-19 03:45:09",
"2040-02-19 09:23:12"
        };
        string[] jingzhe = new string[]
        {
            "1911-03-07 00:38:50",
"1912-03-06 06:20:59",
"1913-03-06 12:08:58",
"1914-03-06 17:55:48",
"1915-03-06 23:48:16",
"1916-03-06 05:37:21",
"1917-03-06 11:24:48",
"1918-03-06 17:20:55",
"1919-03-06 23:05:29",
"1920-03-06 04:51:02",
"1921-03-06 10:45:09",
"1922-03-06 16:33:49",
"1923-03-06 22:24:26",
"1924-03-06 04:12:12",
"1925-03-06 09:59:50",
"1926-03-06 15:59:41",
"1927-03-06 21:50:16",
"1928-03-06 03:37:14",
"1929-03-06 09:31:57",
"1930-03-06 15:16:33",
"1931-03-06 21:02:06",
"1932-03-06 02:49:19",
"1933-03-06 08:31:24",
"1934-03-06 14:26:20",
"1935-03-06 20:10:10",
"1936-03-06 01:49:06",
"1937-03-06 07:44:24",
"1938-03-06 13:33:46",
"1939-03-06 19:26:11",
"1940-03-06 01:23:58",
"1941-03-06 07:10:04",
"1942-03-06 13:09:20",
"1943-03-06 18:58:30",
"1944-03-06 00:40:26",
"1945-03-06 06:37:59",
"1946-03-06 12:24:38",
"1947-03-06 18:07:56",
"1948-03-05 23:57:53",
"1949-03-06 05:39:16",
"1950-03-06 11:35:26",
"1951-03-06 17:26:40",
"1952-03-05 23:07:18",
"1953-03-06 05:02:26",
"1954-03-06 10:48:32",
"1955-03-06 16:30:57",
"1956-03-05 22:24:27",
"1957-03-06 04:10:07",
"1958-03-06 10:04:52",
"1959-03-06 15:56:35",
"1960-03-05 21:36:06",
"1961-03-06 03:34:39",
"1962-03-06 09:29:29",
"1963-03-06 15:17:09",
"1964-03-05 21:15:59",
"1965-03-06 03:00:38",
"1966-03-06 08:51:21",
"1967-03-06 14:41:53",
"1968-03-05 20:17:45",
"1969-03-06 02:10:34",
"1970-03-06 07:58:27",
"1971-03-06 13:34:44",
"1972-03-05 19:28:04",
"1973-03-06 01:12:36",
"1974-03-06 07:07:06",
"1975-03-06 13:05:47",
"1976-03-05 18:48:06",
"1977-03-06 00:44:09",
"1978-03-06 06:38:11",
"1979-03-06 12:19:38",
"1980-03-05 18:16:29",
"1981-03-06 00:05:07",
"1982-03-06 05:54:34",
"1983-03-06 11:47:12",
"1984-03-05 17:24:39",
"1985-03-05 23:16:21",
"1986-03-06 05:12:08",
"1987-03-06 10:53:37",
"1988-03-05 16:46:32",
"1989-03-05 22:34:08",
"1990-03-06 04:19:18",
"1991-03-06 10:12:15",
"1992-03-05 15:52:08",
"1993-03-05 21:42:32",
"1994-03-06 03:37:42",
"1995-03-06 09:16:04",
"1996-03-05 15:09:39",
"1997-03-05 21:04:07",
"1998-03-06 02:57:15",
"1999-03-06 08:57:42",
"2000-03-05 14:42:40",
"2001-03-05 20:32:28",
"2002-03-06 02:27:33",
"2003-03-06 08:04:52",
"2004-03-05 13:55:38",
"2005-03-05 19:45:10",
"2006-03-06 01:28:40",
"2007-03-06 07:17:59",
"2008-03-05 12:58:48",
"2009-03-05 18:47:31",
"2010-03-06 00:46:21",
"2011-03-06 06:29:58",
"2012-03-05 12:21:02",
"2013-03-05 18:14:51",
"2014-03-06 00:02:15",
"2015-03-06 05:55:39",
"2016-03-05 11:43:30",
"2017-03-05 17:32:40",
"2018-03-05 23:28:06",
"2019-03-06 05:09:39",
"2020-03-05 10:56:44",
"2021-03-05 16:53:32",
"2022-03-05 22:43:34",
"2023-03-06 04:36:02",
"2024-03-05 10:22:31",
"2025-03-05 16:07:02",
"2026-03-05 21:58:43",
"2027-03-06 03:39:14",
"2028-03-05 09:24:27",
"2029-03-05 15:17:15",
"2030-03-05 21:02:55",
"2031-03-06 02:50:38",
"2032-03-05 08:39:48",
"2033-03-05 14:31:54",
"2034-03-05 20:31:54",
"2035-03-06 02:21:08",
"2036-03-05 08:11:18",
"2037-03-05 14:05:38",
"2038-03-05 19:54:55",
"2039-03-06 01:42:27",
"2040-03-05 07:30:37"
        };
        string[] chunfen = new string[]
        {
            "1911-03-22 01:54:20",
"1912-03-21 07:29:19",
"1913-03-21 13:17:55",
"1914-03-21 19:10:42",
"1915-03-22 00:51:14",
"1916-03-21 06:46:50",
"1917-03-21 12:37:11",
"1918-03-21 18:25:37",
"1919-03-22 00:19:05",
"1920-03-21 05:59:15",
"1921-03-21 11:50:58",
"1922-03-21 17:48:33",
"1923-03-21 23:28:42",
"1924-03-21 05:20:05",
"1925-03-21 11:12:06",
"1926-03-21 17:01:08",
"1927-03-21 22:59:02",
"1928-03-21 04:44:11",
"1929-03-21 10:34:45",
"1930-03-21 16:29:42",
"1931-03-21 22:06:14",
"1932-03-21 03:53:32",
"1933-03-21 09:43:03",
"1934-03-21 15:27:53",
"1935-03-21 21:17:43",
"1936-03-21 02:57:48",
"1937-03-21 08:45:01",
"1938-03-21 14:43:02",
"1939-03-21 20:28:26",
"1940-03-21 02:23:41",
"1941-03-21 08:20:19",
"1942-03-21 14:10:34",
"1943-03-21 20:02:34",
"1944-03-21 01:48:32",
"1945-03-21 07:37:10",
"1946-03-21 13:32:37",
"1947-03-21 19:12:38",
"1948-03-21 00:56:43",
"1949-03-21 06:48:01",
"1950-03-21 12:35:06",
"1951-03-21 18:25:41",
"1952-03-21 00:13:42",
"1953-03-21 06:00:30",
"1954-03-21 11:53:23",
"1955-03-21 17:35:04",
"1956-03-20 23:20:15",
"1957-03-21 05:16:27",
"1958-03-21 11:05:47",
"1959-03-21 16:54:29",
"1960-03-20 22:42:38",
"1961-03-21 04:32:04",
"1962-03-21 10:29:31",
"1963-03-21 16:19:39",
"1964-03-20 22:09:50",
"1965-03-21 04:04:44",
"1966-03-21 09:52:54",
"1967-03-21 15:36:46",
"1968-03-20 21:22:01",
"1969-03-21 03:08:04",
"1970-03-21 08:56:19",
"1971-03-21 14:38:06",
"1972-03-20 20:21:25",
"1973-03-21 02:12:26",
"1974-03-21 08:06:38",
"1975-03-21 13:56:39",
"1976-03-20 19:49:36",
"1977-03-21 01:42:15",
"1978-03-21 07:33:34",
"1979-03-21 13:21:55",
"1980-03-20 19:09:40",
"1981-03-21 01:02:49",
"1982-03-21 06:55:50",
"1983-03-21 12:38:44",
"1984-03-20 18:24:19",
"1985-03-21 00:13:43",
"1986-03-21 06:02:41",
"1987-03-21 11:51:58",
"1988-03-20 17:38:35",
"1989-03-20 23:28:15",
"1990-03-21 05:19:15",
"1991-03-21 11:01:56",
"1992-03-20 16:48:04",
"1993-03-20 22:40:39",
"1994-03-21 04:28:01",
"1995-03-21 10:14:27",
"1996-03-20 16:03:04",
"1997-03-20 21:54:40",
"1998-03-21 03:54:32",
"1999-03-21 09:45:50",
"2000-03-20 15:35:15",
"2001-03-20 21:30:44",
"2002-03-21 03:16:07",
"2003-03-21 08:59:46",
"2004-03-20 14:48:38",
"2005-03-20 20:33:26",
"2006-03-21 02:25:34",
"2007-03-21 08:07:25",
"2008-03-20 13:48:17",
"2009-03-20 19:43:38",
"2010-03-21 01:32:12",
"2011-03-21 07:20:44",
"2012-03-20 13:14:25",
"2013-03-20 19:01:55",
"2014-03-21 00:57:06",
"2015-03-21 06:45:07",
"2016-03-20 12:30:08",
"2017-03-20 18:28:35",
"2018-03-21 00:15:24",
"2019-03-21 05:58:20",
"2020-03-20 11:49:29",
"2021-03-20 17:37:19",
"2022-03-20 23:33:15",
"2023-03-21 05:24:14",
"2024-03-20 11:06:12",
"2025-03-20 17:01:14",
"2026-03-20 22:45:42",
"2027-03-21 04:24:24",
"2028-03-20 10:16:49",
"2029-03-20 16:01:37",
"2030-03-20 21:51:43",
"2031-03-21 03:40:34",
"2032-03-20 09:21:29",
"2033-03-20 15:22:17",
"2034-03-20 21:17:01",
"2035-03-21 03:02:12",
"2036-03-20 09:02:19",
"2037-03-20 14:49:43",
"2038-03-20 20:40:04",
"2039-03-21 02:31:26",
"2040-03-20 08:11:05"
        };
        string[] qingming = new string[]
        {
            "1911-04-06 06:04:32",
"1912-04-05 11:48:15",
"1913-04-05 17:35:51",
"1914-04-05 23:21:50",
"1915-04-06 05:09:15",
"1916-04-05 10:57:49",
"1917-04-05 16:49:54",
"1918-04-05 22:45:12",
"1919-04-06 04:28:44",
"1920-04-05 10:14:54",
"1921-04-05 16:08:41",
"1922-04-05 21:58:00",
"1923-04-06 03:45:47",
"1924-04-05 09:33:07",
"1925-04-05 15:22:27",
"1926-04-05 21:18:17",
"1927-04-06 03:06:06",
"1928-04-05 08:54:31",
"1929-04-05 14:51:13",
"1930-04-05 20:37:22",
"1931-04-06 02:20:26",
"1932-04-05 08:06:19",
"1933-04-05 13:50:29",
"1934-04-05 19:43:39",
"1935-04-06 01:26:21",
"1936-04-05 07:06:44",
"1937-04-05 13:01:22",
"1938-04-05 18:48:39",
"1939-04-06 00:37:24",
"1940-04-05 06:34:34",
"1941-04-05 12:24:55",
"1942-04-05 18:23:50",
"1943-04-06 00:11:10",
"1944-04-05 05:53:58",
"1945-04-05 11:51:46",
"1946-04-05 17:38:32",
"1947-04-05 23:20:08",
"1948-04-05 05:09:20",
"1949-04-05 10:51:56",
"1950-04-05 16:44:27",
"1951-04-05 22:32:38",
"1952-04-05 04:15:02",
"1953-04-05 10:12:36",
"1954-04-05 15:59:10",
"1955-04-05 21:38:44",
"1956-04-05 03:31:09",
"1957-04-05 09:18:49",
"1958-04-05 15:12:21",
"1959-04-05 21:03:02",
"1960-04-05 02:43:33",
"1961-04-05 08:42:07",
"1962-04-05 14:34:14",
"1963-04-05 20:18:39",
"1964-04-05 02:18:20",
"1965-04-05 08:06:43",
"1966-04-05 13:56:29",
"1967-04-05 19:44:41",
"1968-04-05 01:20:53",
"1969-04-05 07:14:51",
"1970-04-05 13:01:44",
"1971-04-05 18:36:00",
"1972-04-05 00:28:50",
"1973-04-05 06:13:53",
"1974-04-05 12:05:00",
"1975-04-05 18:01:30",
"1976-04-04 23:46:27",
"1977-04-05 05:45:44",
"1978-04-05 11:39:20",
"1979-04-05 17:17:57",
"1980-04-04 23:14:42",
"1981-04-05 05:05:02",
"1982-04-05 10:52:41",
"1983-04-05 16:44:23",
"1984-04-04 22:22:20",
"1985-04-05 04:13:35",
"1986-04-05 10:06:07",
"1987-04-05 15:44:08",
"1988-04-04 21:39:04",
"1989-04-05 03:29:54",
"1990-04-05 09:12:56",
"1991-04-05 15:04:42",
"1992-04-04 20:45:08",
"1993-04-05 02:37:11",
"1994-04-05 08:31:48",
"1995-04-05 14:08:06",
"1996-04-04 20:02:01",
"1997-04-05 01:56:16",
"1998-04-05 07:44:57",
"1999-04-05 13:44:37",
"2000-04-04 19:31:58",
"2001-04-05 01:24:22",
"2002-04-05 07:18:17",
"2003-04-05 12:52:29",
"2004-04-04 18:43:19",
"2005-04-05 00:34:17",
"2006-04-05 06:15:31",
"2007-04-05 12:04:39",
"2008-04-04 17:45:51",
"2009-04-04 23:33:46",
"2010-04-05 05:30:29",
"2011-04-05 11:11:58",
"2012-04-04 17:05:36",
"2013-04-04 23:02:27",
"2014-04-05 04:46:39",
"2015-04-05 10:39:07",
"2016-04-04 16:27:29",
"2017-04-04 22:17:16",
"2018-04-05 04:12:43",
"2019-04-05 09:51:21",
"2020-04-04 15:38:02",
"2021-04-04 21:34:58",
"2022-04-05 03:20:03",
"2023-04-05 09:12:52",
"2024-04-04 15:02:03",
"2025-04-04 20:48:21",
"2026-04-05 02:39:43",
"2027-04-05 08:17:12",
"2028-04-04 14:02:45",
"2029-04-04 19:58:02",
"2030-04-05 01:40:37",
"2031-04-05 07:27:59",
"2032-04-04 13:17:10",
"2033-04-04 19:07:41",
"2034-04-05 01:05:45",
"2035-04-05 06:53:21",
"2036-04-04 12:45:44",
"2037-04-04 18:43:29",
"2038-04-05 00:28:53",
"2039-04-05 06:15:11",
"2040-04-04 12:04:54"
        };
        string[] guyu = new string[]
        {
            "1911-04-21 13:35:55",
"1912-04-20 19:12:21",
"1913-04-21 01:02:51",
"1914-04-21 06:53:12",
"1915-04-21 12:28:46",
"1916-04-20 18:24:35",
"1917-04-21 00:17:22",
"1918-04-21 06:05:23",
"1919-04-21 11:58:35",
"1920-04-20 17:39:07",
"1921-04-20 23:32:14",
"1922-04-21 05:28:32",
"1923-04-21 11:05:33",
"1924-04-20 16:58:33",
"1925-04-20 22:51:05",
"1926-04-21 04:36:03",
"1927-04-21 10:31:38",
"1928-04-20 16:16:40",
"1929-04-20 22:10:15",
"1930-04-21 04:05:45",
"1931-04-21 09:39:45",
"1932-04-20 15:28:01",
"1933-04-20 21:18:14",
"1934-04-21 03:00:07",
"1935-04-21 08:50:04",
"1936-04-20 14:31:04",
"1937-04-20 20:19:08",
"1938-04-21 02:14:41",
"1939-04-21 07:55:05",
"1940-04-20 13:50:52",
"1941-04-20 19:50:24",
"1942-04-21 01:39:05",
"1943-04-21 07:31:29",
"1944-04-20 13:17:45",
"1945-04-20 19:06:51",
"1946-04-21 01:02:08",
"1947-04-21 06:39:22",
"1948-04-20 12:24:50",
"1949-04-20 18:17:15",
"1950-04-20 23:59:06",
"1951-04-21 05:48:03",
"1952-04-20 11:36:37",
"1953-04-20 17:25:21",
"1954-04-20 23:19:32",
"1955-04-21 04:57:50",
"1956-04-20 10:43:25",
"1957-04-20 16:41:12",
"1958-04-20 22:26:58",
"1959-04-21 04:16:27",
"1960-04-20 10:05:51",
"1961-04-20 15:55:02",
"1962-04-20 21:50:40",
"1963-04-21 03:36:07",
"1964-04-20 09:27:08",
"1965-04-20 15:26:03",
"1966-04-20 21:11:30",
"1967-04-21 02:55:07",
"1968-04-20 08:41:07",
"1969-04-20 14:26:51",
"1970-04-20 20:14:56",
"1971-04-21 01:54:13",
"1972-04-20 07:37:29",
"1973-04-20 13:30:21",
"1974-04-20 19:18:51",
"1975-04-21 01:07:12",
"1976-04-20 07:02:56",
"1977-04-20 12:57:12",
"1978-04-20 18:49:32",
"1979-04-21 00:35:21",
"1980-04-20 06:22:41",
"1981-04-20 12:18:31",
"1982-04-20 18:07:27",
"1983-04-20 23:50:09",
"1984-04-20 05:38:06",
"1985-04-20 11:25:46",
"1986-04-20 17:12:08",
"1987-04-20 22:57:32",
"1988-04-20 04:44:47",
"1989-04-20 10:38:56",
"1990-04-20 16:26:32",
"1991-04-20 22:08:23",
"1992-04-20 03:56:53",
"1993-04-20 09:49:01",
"1994-04-20 15:36:00",
"1995-04-20 21:21:29",
"1996-04-20 03:09:53",
"1997-04-20 09:02:49",
"1998-04-20 14:56:43",
"1999-04-20 20:46:00",
"2000-04-20 02:39:30",
"2001-04-20 08:35:53",
"2002-04-20 14:20:28",
"2003-04-20 20:02:48",
"2004-04-20 01:50:25",
"2005-04-20 07:37:15",
"2006-04-20 13:26:03",
"2007-04-20 19:07:05",
"2008-04-20 00:51:08",
"2009-04-20 06:44:25",
"2010-04-20 12:29:48",
"2011-04-20 18:17:26",
"2012-04-20 00:12:04",
"2013-04-20 06:03:18",
"2014-04-20 11:55:32",
"2015-04-20 17:41:50",
"2016-04-19 23:29:23",
"2017-04-20 05:26:58",
"2018-04-20 11:12:29",
"2019-04-20 16:55:10",
"2020-04-19 22:45:21",
"2021-04-20 04:33:14",
"2022-04-20 10:24:07",
"2023-04-20 16:13:26",
"2024-04-19 21:59:33",
"2025-04-20 03:55:45",
"2026-04-20 09:38:51",
"2027-04-20 15:17:20",
"2028-04-19 21:09:10",
"2029-04-20 02:55:20",
"2030-04-20 08:43:13",
"2031-04-20 14:30:49",
"2032-04-19 20:13:43",
"2033-04-20 02:12:40",
"2034-04-20 08:03:14",
"2035-04-20 13:48:28",
"2036-04-19 19:49:58",
"2037-04-20 01:39:46",
"2038-04-20 07:27:58",
"2039-04-20 13:17:11",
"2040-04-19 18:58:57"
        };
        string[] lixia = new string[]
        {
            "1911-05-07 00:00:18",
"1912-05-06 05:47:03",
"1913-05-06 11:34:39",
"1914-05-06 17:20:03",
"1915-05-06 23:02:44",
"1916-05-06 04:49:45",
"1917-05-06 10:45:42",
"1918-05-06 16:38:11",
"1919-05-06 22:22:00",
"1920-05-06 04:11:17",
"1921-05-06 10:04:17",
"1922-05-06 15:52:50",
"1923-05-06 21:38:14",
"1924-05-06 03:25:39",
"1925-05-06 09:17:51",
"1926-05-06 15:08:20",
"1927-05-06 20:53:04",
"1928-05-06 02:43:29",
"1929-05-06 08:40:20",
"1930-05-06 14:26:59",
"1931-05-06 20:09:35",
"1932-05-06 01:55:08",
"1933-05-06 07:41:45",
"1934-05-06 13:30:44",
"1935-05-06 19:12:02",
"1936-05-06 00:56:30",
"1937-05-06 06:50:35",
"1938-05-06 12:35:09",
"1939-05-06 18:21:02",
"1940-05-06 00:16:16",
"1941-05-06 06:09:50",
"1942-05-06 12:06:50",
"1943-05-06 17:53:21",
"1944-05-05 23:39:43",
"1945-05-06 05:36:35",
"1946-05-06 11:21:29",
"1947-05-06 17:02:57",
"1948-05-05 22:52:13",
"1949-05-06 04:36:34",
"1950-05-06 10:24:41",
"1951-05-06 16:09:15",
"1952-05-05 21:54:01",
"1953-05-06 03:52:18",
"1954-05-06 09:38:10",
"1955-05-06 15:17:58",
"1956-05-05 21:09:58",
"1957-05-06 02:58:22",
"1958-05-06 08:49:10",
"1959-05-06 14:38:42",
"1960-05-05 20:22:33",
"1961-05-06 02:21:16",
"1962-05-06 08:09:28",
"1963-05-06 13:51:57",
"1964-05-05 19:51:01",
"1965-05-06 01:41:32",
"1966-05-06 07:30:26",
"1967-05-06 13:17:26",
"1968-05-05 18:55:47",
"1969-05-06 00:49:47",
"1970-05-06 06:33:47",
"1971-05-06 12:08:08",
"1972-05-05 18:01:10",
"1973-05-05 23:46:23",
"1974-05-06 05:33:52",
"1975-05-06 11:27:11",
"1976-05-05 17:14:24",
"1977-05-05 23:16:00",
"1978-05-06 05:08:32",
"1979-05-06 10:47:10",
"1980-05-05 16:44:28",
"1981-05-05 22:34:47",
"1982-05-06 04:19:59",
"1983-05-06 10:10:51",
"1984-05-05 15:50:57",
"1985-05-05 21:42:32",
"1986-05-06 03:30:36",
"1987-05-06 09:05:35",
"1988-05-05 15:01:43",
"1989-05-05 20:53:55",
"1990-05-06 02:35:26",
"1991-05-06 08:26:53",
"1992-05-05 14:08:40",
"1993-05-05 20:01:43",
"1994-05-06 01:54:05",
"1995-05-06 07:30:03",
"1996-05-05 13:26:02",
"1997-05-05 19:19:26",
"1998-05-06 01:03:10",
"1999-05-06 07:01:00",
"2000-05-05 12:50:10",
"2001-05-05 18:44:50",
"2002-05-06 00:37:18",
"2003-05-06 06:10:29",
"2004-05-05 12:02:28",
"2005-05-05 17:52:50",
"2006-05-05 23:30:39",
"2007-05-06 05:20:23",
"2008-05-05 11:03:25",
"2009-05-05 16:50:49",
"2010-05-05 22:44:01",
"2011-05-06 04:23:12",
"2012-05-05 10:19:40",
"2013-05-05 16:18:09",
"2014-05-05 21:59:25",
"2015-05-06 03:52:35",
"2016-05-05 09:41:50",
"2017-05-05 15:30:59",
"2018-05-05 21:25:18",
"2019-05-06 03:02:40",
"2020-05-05 08:51:16",
"2021-05-05 14:47:01",
"2022-05-05 20:25:46",
"2023-05-06 02:18:34",
"2024-05-05 08:09:51",
"2025-05-05 13:56:57",
"2026-05-05 19:48:27",
"2027-05-06 01:24:53",
"2028-05-05 07:11:51",
"2029-05-05 13:07:24",
"2030-05-05 18:45:54",
"2031-05-06 00:34:47",
"2032-05-05 06:25:25",
"2033-05-05 12:13:18",
"2034-05-05 18:08:40",
"2035-05-05 23:54:25",
"2036-05-05 05:48:51",
"2037-05-05 11:48:55",
"2038-05-05 17:30:37",
"2039-05-05 23:17:33",
"2040-05-05 05:08:44"
        };
        string[] xiaoman = new string[]
        {
            "1911-05-22 13:18:33",
"1912-05-21 18:57:06",
"1913-05-22 00:49:51",
"1914-05-22 06:37:38",
"1915-05-22 12:10:23",
"1916-05-21 18:05:50",
"1917-05-21 23:58:31",
"1918-05-22 05:45:27",
"1919-05-22 11:39:04",
"1920-05-21 17:21:43",
"1921-05-21 23:16:40",
"1922-05-22 05:10:13",
"1923-05-22 10:45:13",
"1924-05-21 16:40:25",
"1925-05-21 22:32:53",
"1926-05-22 04:14:26",
"1927-05-22 10:07:47",
"1928-05-21 15:52:19",
"1929-05-21 21:47:33",
"1930-05-22 03:41:59",
"1931-05-22 09:15:21",
"1932-05-21 15:06:33",
"1933-05-21 20:56:45",
"1934-05-22 02:34:51",
"1935-05-22 08:24:47",
"1936-05-21 14:07:24",
"1937-05-21 19:57:07",
"1938-05-22 01:50:08",
"1939-05-22 07:26:38",
"1940-05-21 13:23:00",
"1941-05-21 19:22:47",
"1942-05-22 01:08:38",
"1943-05-22 07:02:49",
"1944-05-21 12:50:46",
"1945-05-21 18:40:12",
"1946-05-22 00:33:53",
"1947-05-22 06:09:00",
"1948-05-21 11:57:35",
"1949-05-21 17:50:38",
"1950-05-21 23:27:07",
"1951-05-22 05:15:22",
"1952-05-21 11:03:49",
"1953-05-21 16:52:47",
"1954-05-21 22:47:22",
"1955-05-22 04:24:19",
"1956-05-21 10:12:32",
"1957-05-21 16:10:23",
"1958-05-21 21:51:00",
"1959-05-22 03:42:05",
"1960-05-21 09:33:28",
"1961-05-21 15:22:14",
"1962-05-21 21:16:30",
"1963-05-22 02:58:07",
"1964-05-21 08:49:45",
"1965-05-21 14:50:13",
"1966-05-21 20:32:01",
"1967-05-22 02:17:52",
"1968-05-21 08:05:50",
"1969-05-21 13:49:42",
"1970-05-21 19:37:19",
"1971-05-22 01:14:59",
"1972-05-21 06:59:29",
"1973-05-21 12:53:49",
"1974-05-21 18:36:02",
"1975-05-22 00:23:40",
"1976-05-21 06:21:04",
"1977-05-21 12:14:20",
"1978-05-21 18:08:26",
"1979-05-21 23:53:50",
"1980-05-21 05:42:02",
"1981-05-21 11:39:25",
"1982-05-21 17:22:53",
"1983-05-21 23:06:26",
"1984-05-21 04:57:36",
"1985-05-21 10:42:55",
"1986-05-21 16:27:55",
"1987-05-21 22:10:01",
"1988-05-21 03:56:40",
"1989-05-21 09:53:32",
"1990-05-21 15:37:23",
"1991-05-21 21:20:14",
"1992-05-21 03:12:08",
"1993-05-21 09:01:43",
"1994-05-21 14:48:28",
"1995-05-21 20:34:11",
"1996-05-21 02:23:06",
"1997-05-21 08:17:53",
"1998-05-21 14:05:26",
"1999-05-21 19:52:25",
"2000-05-21 01:49:24",
"2001-05-21 07:44:12",
"2002-05-21 13:29:06",
"2003-05-21 19:12:25",
"2004-05-21 00:59:12",
"2005-05-21 06:47:24",
"2006-05-21 12:31:33",
"2007-05-21 18:11:56",
"2008-05-21 00:00:53",
"2009-05-21 05:51:10",
"2010-05-21 11:33:53",
"2011-05-21 17:21:10",
"2012-05-20 23:15:31",
"2013-05-21 05:09:30",
"2014-05-21 10:59:02",
"2015-05-21 16:44:45",
"2016-05-20 22:36:26",
"2017-05-21 04:30:53",
"2018-05-21 10:14:33",
"2019-05-21 15:59:01",
"2020-05-20 21:49:09",
"2021-05-21 03:36:58",
"2022-05-21 09:22:25",
"2023-05-21 15:08:59",
"2024-05-20 20:59:17",
"2025-05-21 02:54:23",
"2026-05-21 08:36:28",
"2027-05-21 14:17:55",
"2028-05-20 20:09:28",
"2029-05-21 01:55:30",
"2030-05-21 07:40:42",
"2031-05-21 13:27:29",
"2032-05-20 19:14:33",
"2033-05-21 01:10:30",
"2034-05-21 06:56:24",
"2035-05-21 12:42:56",
"2036-05-20 18:44:21",
"2037-05-21 00:34:54",
"2038-05-21 06:22:08",
"2039-05-21 12:10:17",
"2040-05-20 17:55:06"
        };
        string[] mangzhong = new string[]
        {
            "1911-06-07 04:37:52",
"1912-06-06 10:27:29",
"1913-06-06 16:13:24",
"1914-06-06 21:59:56",
"1915-06-07 03:40:07",
"1916-06-06 09:25:39",
"1917-06-06 15:23:10",
"1918-06-06 21:10:57",
"1919-06-07 02:56:36",
"1920-06-06 08:50:22",
"1921-06-06 14:41:25",
"1922-06-06 20:30:15",
"1923-06-07 02:14:18",
"1924-06-06 08:01:31",
"1925-06-06 13:56:22",
"1926-06-06 19:41:37",
"1927-06-07 01:24:45",
"1928-06-06 07:17:09",
"1929-06-06 13:10:47",
"1930-06-06 18:58:02",
"1931-06-07 00:41:45",
"1932-06-06 06:27:43",
"1933-06-06 12:17:19",
"1934-06-06 18:01:21",
"1935-06-06 23:41:35",
"1936-06-06 05:30:40",
"1937-06-06 11:22:48",
"1938-06-06 17:06:37",
"1939-06-06 22:51:38",
"1940-06-06 04:44:02",
"1941-06-06 10:39:12",
"1942-06-06 16:32:31",
"1943-06-06 22:18:57",
"1944-06-06 04:10:53",
"1945-06-06 10:05:24",
"1946-06-06 15:48:42",
"1947-06-06 21:31:12",
"1948-06-06 03:20:19",
"1949-06-06 09:06:49",
"1950-06-06 14:51:00",
"1951-06-06 20:32:32",
"1952-06-06 02:20:18",
"1953-06-06 08:16:04",
"1954-06-06 14:00:49",
"1955-06-06 19:43:25",
"1956-06-06 01:35:47",
"1957-06-06 07:24:43",
"1958-06-06 13:12:11",
"1959-06-06 19:00:03",
"1960-06-06 00:48:34",
"1961-06-06 06:46:00",
"1962-06-06 12:31:15",
"1963-06-06 18:14:26",
"1964-06-06 00:11:43",
"1965-06-06 06:02:06",
"1966-06-06 11:49:37",
"1967-06-06 17:36:18",
"1968-06-05 23:19:05",
"1969-06-06 05:11:29",
"1970-06-06 10:52:13",
"1971-06-06 16:28:51",
"1972-06-05 22:21:59",
"1973-06-06 04:06:50",
"1974-06-06 09:51:39",
"1975-06-06 15:42:01",
"1976-06-05 21:31:13",
"1977-06-06 03:32:01",
"1978-06-06 09:23:05",
"1979-06-06 15:05:11",
"1980-06-05 21:03:44",
"1981-06-06 02:52:39",
"1982-06-06 08:35:53",
"1983-06-06 14:25:42",
"1984-06-05 20:08:37",
"1985-06-06 01:59:56",
"1986-06-06 07:44:23",
"1987-06-06 13:18:58",
"1988-06-05 19:14:53",
"1989-06-06 01:05:13",
"1990-06-06 06:46:18",
"1991-06-06 12:38:17",
"1992-06-05 18:22:19",
"1993-06-06 00:15:13",
"1994-06-06 06:04:52",
"1995-06-06 11:42:28",
"1996-06-05 17:40:47",
"1997-06-05 23:32:31",
"1998-06-06 05:13:22",
"1999-06-06 11:09:07",
"2000-06-05 16:58:34",
"2001-06-05 22:53:35",
"2002-06-06 04:44:46",
"2003-06-06 10:19:43",
"2004-06-05 16:13:46",
"2005-06-05 22:01:52",
"2006-06-06 03:36:59",
"2007-06-06 09:27:04",
"2008-06-05 15:11:43",
"2009-06-05 20:59:03",
"2010-06-06 02:49:23",
"2011-06-06 08:27:20",
"2012-06-05 14:25:53",
"2013-06-05 20:23:19",
"2014-06-06 02:03:02",
"2015-06-06 07:58:09",
"2016-06-05 13:48:28",
"2017-06-05 19:36:33",
"2018-06-06 01:29:04",
"2019-06-06 07:06:18",
"2020-06-05 12:58:18",
"2021-06-05 18:51:57",
"2022-06-06 00:25:37",
"2023-06-06 06:18:10",
"2024-06-05 12:09:40",
"2025-06-05 17:56:16",
"2026-06-05 23:48:04",
"2027-06-06 05:25:29",
"2028-06-05 11:15:39",
"2029-06-05 17:09:36",
"2030-06-05 22:44:06",
"2031-06-06 04:35:17",
"2032-06-05 10:27:32",
"2033-06-05 16:12:58",
"2034-06-05 22:06:12",
"2035-06-06 03:50:19",
"2036-06-05 09:46:28",
"2037-06-05 15:46:17",
"2038-06-05 21:25:03",
"2039-06-06 03:14:53",
"2040-06-05 09:07:24"
        };
        string[] xiazhi = new string[]
        {
            "1911-06-22 21:35:30",
"1912-06-22 03:16:51",
"1913-06-22 09:09:26",
"1914-06-22 14:55:00",
"1915-06-22 20:29:20",
"1916-06-22 02:24:21",
"1917-06-22 08:14:16",
"1918-06-22 13:59:34",
"1919-06-22 19:53:30",
"1920-06-22 01:39:45",
"1921-06-22 07:35:35",
"1922-06-22 13:26:39",
"1923-06-22 19:02:42",
"1924-06-22 00:59:18",
"1925-06-22 06:49:54",
"1926-06-22 12:29:58",
"1927-06-22 18:22:07",
"1928-06-22 00:06:23",
"1929-06-22 06:00:33",
"1930-06-22 11:52:45",
"1931-06-22 17:28:00",
"1932-06-21 23:22:34",
"1933-06-22 05:11:45",
"1934-06-22 10:47:51",
"1935-06-22 16:37:51",
"1936-06-21 22:21:34",
"1937-06-22 04:11:56",
"1938-06-22 10:03:32",
"1939-06-22 15:39:22",
"1940-06-21 21:36:22",
"1941-06-22 03:33:15",
"1942-06-22 09:16:13",
"1943-06-22 15:12:17",
"1944-06-21 21:02:14",
"1945-06-22 02:52:00",
"1946-06-22 08:44:17",
"1947-06-22 14:18:47",
"1948-06-21 20:10:32",
"1949-06-22 02:02:43",
"1950-06-22 07:36:00",
"1951-06-22 13:24:48",
"1952-06-21 19:12:30",
"1953-06-22 00:59:53",
"1954-06-22 06:54:00",
"1955-06-22 12:31:19",
"1956-06-21 18:23:41",
"1957-06-22 00:20:28",
"1958-06-22 05:56:51",
"1959-06-22 11:49:44",
"1960-06-21 17:42:15",
"1961-06-21 23:30:04",
"1962-06-22 05:24:05",
"1963-06-22 11:04:00",
"1964-06-21 16:56:47",
"1965-06-21 22:55:40",
"1966-06-22 04:33:21",
"1967-06-22 10:22:49",
"1968-06-21 16:13:16",
"1969-06-21 21:55:01",
"1970-06-22 03:42:38",
"1971-06-22 09:19:34",
"1972-06-21 15:06:10",
"1973-06-21 21:00:34",
"1974-06-22 02:37:36",
"1975-06-22 08:26:25",
"1976-06-21 14:24:11",
"1977-06-21 20:13:44",
"1978-06-22 02:09:33",
"1979-06-22 07:56:09",
"1980-06-21 13:47:00",
"1981-06-21 19:44:40",
"1982-06-22 01:22:59",
"1983-06-22 07:08:41",
"1984-06-21 13:02:14",
"1985-06-21 18:44:07",
"1986-06-22 00:29:57",
"1987-06-22 06:10:45",
"1988-06-21 11:56:31",
"1989-06-21 17:53:00",
"1990-06-21 23:32:46",
"1991-06-22 05:18:47",
"1992-06-21 11:14:08",
"1993-06-21 16:59:44",
"1994-06-21 22:47:32",
"1995-06-22 04:34:22",
"1996-06-21 10:23:44",
"1997-06-21 16:19:56",
"1998-06-21 22:02:34",
"1999-06-22 03:49:07",
"2000-06-21 09:47:43",
"2001-06-21 15:37:43",
"2002-06-21 21:24:24",
"2003-06-22 03:10:28",
"2004-06-21 08:56:51",
"2005-06-21 14:46:07",
"2006-06-21 20:25:51",
"2007-06-22 02:06:26",
"2008-06-21 07:59:21",
"2009-06-21 13:45:30",
"2010-06-21 19:28:24",
"2011-06-22 01:16:29",
"2012-06-21 07:08:48",
"2013-06-21 13:03:56",
"2014-06-21 18:51:13",
"2015-06-22 00:37:53",
"2016-06-21 06:34:09",
"2017-06-21 12:24:06",
"2018-06-21 18:07:12",
"2019-06-21 23:54:09",
"2020-06-21 05:43:33",
"2021-06-21 11:32:00",
"2022-06-21 17:13:40",
"2023-06-21 22:57:37",
"2024-06-21 04:50:46",
"2025-06-21 10:42:00",
"2026-06-21 16:24:12",
"2027-06-21 22:10:30",
"2028-06-21 04:01:39",
"2029-06-21 09:47:55",
"2030-06-21 15:30:54",
"2031-06-21 21:16:43",
"2032-06-21 03:08:19",
"2033-06-21 09:00:40",
"2034-06-21 14:43:42",
"2035-06-21 20:32:38",
"2036-06-21 02:31:42",
"2037-06-21 08:21:52",
"2038-06-21 14:08:49",
"2039-06-21 19:56:50",
"2040-06-21 01:45:46"
        };
        string[] xiaoshu = new string[]
        {
            "1911-07-08 15:04:55",
"1912-07-07 20:56:42",
"1913-07-08 02:38:52",
"1914-07-08 08:27:12",
"1915-07-08 14:07:45",
"1916-07-07 19:53:33",
"1917-07-08 01:50:13",
"1918-07-08 07:32:07",
"1919-07-08 13:20:30",
"1920-07-07 19:18:36",
"1921-07-08 01:06:34",
"1922-07-08 06:57:25",
"1923-07-08 12:42:11",
"1924-07-07 18:29:24",
"1925-07-08 00:24:54",
"1926-07-08 06:05:36",
"1927-07-08 11:49:55",
"1928-07-07 17:44:15",
"1929-07-07 23:31:38",
"1930-07-08 05:19:40",
"1931-07-08 11:05:34",
"1932-07-07 16:52:15",
"1933-07-07 22:44:17",
"1934-07-08 04:24:25",
"1935-07-08 10:05:32",
"1936-07-07 15:58:18",
"1937-07-07 21:45:55",
"1938-07-08 03:31:21",
"1939-07-08 09:18:20",
"1940-07-07 15:08:01",
"1941-07-07 21:03:04",
"1942-07-08 02:51:46",
"1943-07-08 08:38:50",
"1944-07-07 14:36:02",
"1945-07-07 20:26:46",
"1946-07-08 02:10:48",
"1947-07-08 07:55:48",
"1948-07-07 13:43:28",
"1949-07-07 19:31:35",
"1950-07-08 01:13:17",
"1951-07-08 06:53:51",
"1952-07-07 12:44:38",
"1953-07-07 18:34:54",
"1954-07-08 00:19:10",
"1955-07-08 06:05:52",
"1956-07-07 11:57:59",
"1957-07-07 17:48:09",
"1958-07-07 23:33:25",
"1959-07-08 05:19:52",
"1960-07-07 11:12:39",
"1961-07-07 17:06:35",
"1962-07-07 22:51:05",
"1963-07-08 04:37:37",
"1964-07-07 10:32:07",
"1965-07-07 16:21:22",
"1966-07-07 22:06:59",
"1967-07-08 03:53:19",
"1968-07-07 09:41:37",
"1969-07-07 15:31:31",
"1970-07-07 21:10:31",
"1971-07-08 02:51:07",
"1972-07-07 08:42:53",
"1973-07-07 14:27:21",
"1974-07-07 20:11:06",
"1975-07-08 01:59:24",
"1976-07-07 07:50:50",
"1977-07-07 13:47:52",
"1978-07-07 19:36:57",
"1979-07-08 01:24:37",
"1980-07-07 07:23:56",
"1981-07-07 13:11:52",
"1982-07-07 18:54:35",
"1983-07-08 00:43:13",
"1984-07-07 06:29:06",
"1985-07-07 12:18:35",
"1986-07-07 18:00:45",
"1987-07-07 23:38:39",
"1988-07-07 05:32:54",
"1989-07-07 11:19:25",
"1990-07-07 17:00:28",
"1991-07-07 22:52:59",
"1992-07-07 04:40:15",
"1993-07-07 10:32:02",
"1994-07-07 16:19:22",
"1995-07-07 22:01:00",
"1996-07-07 04:00:00",
"1997-07-07 09:49:23",
"1998-07-07 15:30:25",
"1999-07-07 21:24:59",
"2000-07-07 03:13:56",
"2001-07-07 09:06:42",
"2002-07-07 14:56:11",
"2003-07-07 20:35:39",
"2004-07-07 02:31:16",
"2005-07-07 08:16:34",
"2006-07-07 13:51:27",
"2007-07-07 19:41:44",
"2008-07-07 01:26:49",
"2009-07-07 07:13:29",
"2010-07-07 13:02:23",
"2011-07-07 18:42:00",
"2012-07-07 00:40:43",
"2013-07-07 06:34:36",
"2014-07-07 12:14:45",
"2015-07-07 18:12:14",
"2016-07-07 00:03:18",
"2017-07-07 05:50:38",
"2018-07-07 11:41:47",
"2019-07-07 17:20:25",
"2020-07-06 23:14:20",
"2021-07-07 05:05:19",
"2022-07-07 10:37:49",
"2023-07-07 16:30:29",
"2024-07-06 22:19:49",
"2025-07-07 04:04:43",
"2026-07-07 09:56:40",
"2027-07-07 15:36:44",
"2028-07-06 21:29:57",
"2029-07-07 03:22:01",
"2030-07-07 08:55:05",
"2031-07-07 14:48:26",
"2032-07-06 20:40:27",
"2033-07-07 02:24:29",
"2034-07-07 08:17:09",
"2035-07-07 14:00:39",
"2036-07-06 19:57:00",
"2037-07-07 01:54:35",
"2038-07-07 07:31:56",
"2039-07-07 13:25:33",
"2040-07-06 19:18:36"
        };
        string[] dashu = new string[]
        {
            "1911-07-24 08:28:36",
"1912-07-23 14:13:40",
"1913-07-23 20:03:41",
"1914-07-24 01:46:52",
"1915-07-24 07:26:21",
"1916-07-23 13:21:08",
"1917-07-23 19:07:45",
"1918-07-24 00:51:23",
"1919-07-24 06:44:26",
"1920-07-23 12:34:53",
"1921-07-23 18:30:15",
"1922-07-24 00:19:38",
"1923-07-24 06:00:29",
"1924-07-23 11:57:27",
"1925-07-23 17:44:47",
"1926-07-23 23:24:44",
"1927-07-24 05:16:41",
"1928-07-23 11:02:13",
"1929-07-23 16:53:14",
"1930-07-23 22:41:53",
"1931-07-24 04:21:20",
"1932-07-23 10:17:59",
"1933-07-23 16:05:22",
"1934-07-23 21:42:07",
"1935-07-24 03:32:54",
"1936-07-23 09:17:50",
"1937-07-23 15:06:53",
"1938-07-23 20:57:02",
"1939-07-24 02:36:36",
"1940-07-23 08:34:03",
"1941-07-23 14:26:08",
"1942-07-23 20:07:23",
"1943-07-24 02:04:30",
"1944-07-23 07:55:49",
"1945-07-23 13:45:23",
"1946-07-23 19:37:00",
"1947-07-24 01:14:09",
"1948-07-23 07:07:29",
"1949-07-23 12:56:39",
"1950-07-23 18:29:53",
"1951-07-24 00:20:37",
"1952-07-23 06:07:25",
"1953-07-23 11:52:06",
"1954-07-23 17:44:54",
"1955-07-23 23:24:30",
"1956-07-23 05:19:52",
"1957-07-23 11:14:48",
"1958-07-23 16:50:26",
"1959-07-23 22:45:25",
"1960-07-23 04:37:25",
"1961-07-23 10:23:34",
"1962-07-23 16:17:56",
"1963-07-23 21:59:11",
"1964-07-23 03:52:43",
"1965-07-23 09:48:09",
"1966-07-23 15:23:10",
"1967-07-23 21:15:49",
"1968-07-23 03:07:23",
"1969-07-23 08:48:08",
"1970-07-23 14:36:52",
"1971-07-23 20:14:43",
"1972-07-23 02:02:30",
"1973-07-23 07:55:32",
"1974-07-23 13:30:10",
"1975-07-23 19:21:44",
"1976-07-23 01:18:26",
"1977-07-23 07:03:38",
"1978-07-23 13:00:14",
"1979-07-23 18:48:32",
"1980-07-23 00:41:59",
"1981-07-23 06:39:43",
"1982-07-23 12:15:23",
"1983-07-23 18:04:07",
"1984-07-22 23:58:12",
"1985-07-23 05:36:26",
"1986-07-23 11:24:23",
"1987-07-23 17:06:02",
"1988-07-22 22:51:05",
"1989-07-23 04:45:28",
"1990-07-23 10:21:30",
"1991-07-23 16:11:08",
"1992-07-22 22:08:49",
"1993-07-23 03:50:49",
"1994-07-23 09:41:00",
"1995-07-23 15:29:40",
"1996-07-22 21:18:42",
"1997-07-23 03:15:26",
"1998-07-23 08:55:22",
"1999-07-23 14:44:06",
"2000-07-22 20:42:41",
"2001-07-23 02:26:14",
"2002-07-23 08:14:51",
"2003-07-23 14:04:08",
"2004-07-22 19:50:09",
"2005-07-23 01:40:42",
"2006-07-23 07:17:42",
"2007-07-23 13:00:10",
"2008-07-22 18:54:47",
"2009-07-23 00:35:42",
"2010-07-23 06:21:12",
"2011-07-23 12:11:48",
"2012-07-22 18:00:51",
"2013-07-22 23:55:58",
"2014-07-23 05:41:21",
"2015-07-23 11:30:25",
"2016-07-22 17:30:10",
"2017-07-22 23:15:18",
"2018-07-23 05:00:16",
"2019-07-23 10:50:16",
"2020-07-22 16:36:44",
"2021-07-22 22:26:16",
"2022-07-23 04:06:49",
"2023-07-23 09:50:15",
"2024-07-22 15:44:11",
"2025-07-22 21:29:11",
"2026-07-23 03:12:48",
"2027-07-23 09:04:20",
"2028-07-22 14:53:38",
"2029-07-22 20:41:43",
"2030-07-23 02:24:29",
"2031-07-23 08:10:03",
"2032-07-22 14:04:18",
"2033-07-22 19:52:21",
"2034-07-23 01:35:51",
"2035-07-23 07:28:11",
"2036-07-22 13:22:08",
"2037-07-22 19:12:02",
"2038-07-23 00:59:19",
"2039-07-23 06:47:33",
"2040-07-22 12:40:11"

        };
        string[] liqiu = new string[]
        {
            "1911-08-09 00:44:25",
"1912-08-08 06:37:10",
"1913-08-08 12:15:47",
"1914-08-08 18:05:11",
"1915-08-08 23:47:41",
"1916-08-08 05:34:55",
"1917-08-08 11:30:07",
"1918-08-08 17:07:24",
"1919-08-08 22:58:01",
"1920-08-08 04:58:14",
"1921-08-08 10:43:25",
"1922-08-08 16:37:08",
"1923-08-08 22:24:29",
"1924-08-08 04:12:14",
"1925-08-08 10:07:05",
"1926-08-08 15:44:12",
"1927-08-08 21:31:23",
"1928-08-08 03:27:30",
"1929-08-08 09:08:41",
"1930-08-08 14:56:58",
"1931-08-08 20:44:52",
"1932-08-08 02:31:48",
"1933-08-08 08:25:30",
"1934-08-08 14:03:38",
"1935-08-08 19:47:48",
"1936-08-08 01:43:10",
"1937-08-08 07:25:20",
"1938-08-08 13:12:41",
"1939-08-08 19:03:27",
"1940-08-08 00:51:29",
"1941-08-08 06:45:52",
"1942-08-08 12:30:18",
"1943-08-08 18:18:30",
"1944-08-08 00:18:51",
"1945-08-08 06:05:03",
"1946-08-08 11:51:35",
"1947-08-08 17:40:51",
"1948-08-07 23:26:17",
"1949-08-08 05:14:56",
"1950-08-08 10:55:11",
"1951-08-08 16:37:26",
"1952-08-07 22:30:57",
"1953-08-08 04:14:35",
"1954-08-08 09:59:04",
"1955-08-08 15:50:02",
"1956-08-07 21:40:12",
"1957-08-08 03:32:03",
"1958-08-08 09:17:10",
"1959-08-08 15:04:04",
"1960-08-07 20:59:44",
"1961-08-08 02:48:19",
"1962-08-08 08:33:40",
"1963-08-08 14:25:24",
"1964-08-07 20:16:09",
"1965-08-08 02:04:36",
"1966-08-08 07:48:56",
"1967-08-08 13:34:51",
"1968-08-07 19:27:11",
"1969-08-08 01:14:06",
"1970-08-08 06:54:06",
"1971-08-08 12:40:12",
"1972-08-07 18:28:29",
"1973-08-08 00:12:48",
"1974-08-08 05:57:10",
"1975-08-08 11:44:53",
"1976-08-07 17:38:21",
"1977-08-07 23:30:14",
"1978-08-08 05:17:40",
"1979-08-08 11:10:53",
"1980-08-07 17:08:30",
"1981-08-07 22:57:09",
"1982-08-08 04:41:45",
"1983-08-08 10:29:37",
"1984-08-07 16:17:53",
"1985-08-07 22:04:16",
"1986-08-08 03:45:36",
"1987-08-08 09:29:13",
"1988-08-07 15:20:15",
"1989-08-07 21:03:52",
"1990-08-08 02:45:32",
"1991-08-08 08:37:15",
"1992-08-07 14:27:24",
"1993-08-07 20:17:58",
"1994-08-08 02:04:22",
"1995-08-08 07:51:44",
"1996-08-07 13:48:49",
"1997-08-07 19:36:18",
"1998-08-08 01:19:50",
"1999-08-08 07:14:06",
"2000-08-07 13:02:59",
"2001-08-07 18:52:21",
"2002-08-08 00:39:18",
"2003-08-08 06:24:18",
"2004-08-07 12:19:36",
"2005-08-07 18:03:21",
"2006-08-07 23:40:47",
"2007-08-08 05:31:14",
"2008-08-07 11:16:10",
"2009-08-07 17:01:09",
"2010-08-07 22:49:07",
"2011-08-08 04:33:26",
"2012-08-07 10:30:32",
"2013-08-07 16:20:21",
"2014-08-07 22:02:28",
"2015-08-08 04:01:23",
"2016-08-07 09:52:58",
"2017-08-07 15:39:58",
"2018-08-07 21:30:34",
"2019-08-08 03:12:57",
"2020-08-07 09:06:03",
"2021-08-07 14:53:48",
"2022-08-07 20:28:57",
"2023-08-08 02:22:41",
"2024-08-07 08:09:01",
"2025-08-07 13:51:19",
"2026-08-07 19:42:26",
"2027-08-08 01:26:27",
"2028-08-07 07:20:50",
"2029-08-07 13:11:22",
"2030-08-07 18:46:56",
"2031-08-08 00:42:31",
"2032-08-07 06:32:16",
"2033-08-07 12:15:17",
"2034-08-07 18:08:37",
"2035-08-07 23:53:49",
"2036-08-07 05:48:24",
"2037-08-07 11:42:28",
"2038-08-07 17:20:44",
"2039-08-07 23:17:29",
"2040-08-07 05:09:25"
        };
        string[] chushu = new string[]
        {
            "1911-08-24 15:12:57",
"1912-08-23 21:01:19",
"1913-08-24 02:48:09",
"1914-08-24 08:29:34",
"1915-08-24 14:14:58",
"1916-08-23 20:08:31",
"1917-08-24 01:53:38",
"1918-08-24 07:37:07",
"1919-08-24 13:28:16",
"1920-08-23 19:21:15",
"1921-08-24 01:15:07",
"1922-08-24 07:04:09",
"1923-08-24 12:51:45",
"1924-08-23 18:47:53",
"1925-08-24 00:33:05",
"1926-08-24 06:13:54",
"1927-08-24 12:05:23",
"1928-08-23 17:53:05",
"1929-08-23 23:41:12",
"1930-08-24 05:26:17",
"1931-08-24 11:10:13",
"1932-08-23 17:06:10",
"1933-08-23 22:52:19",
"1934-08-24 04:32:00",
"1935-08-24 10:23:56",
"1936-08-23 16:10:28",
"1937-08-23 21:57:49",
"1938-08-24 03:45:46",
"1939-08-24 09:31:08",
"1940-08-23 15:28:30",
"1941-08-23 21:16:51",
"1942-08-24 02:58:10",
"1943-08-24 08:54:59",
"1944-08-23 14:46:26",
"1945-08-23 20:35:16",
"1946-08-24 02:26:19",
"1947-08-24 08:08:56",
"1948-08-23 14:02:30",
"1949-08-23 19:48:11",
"1950-08-24 01:23:09",
"1951-08-24 07:16:05",
"1952-08-23 13:02:47",
"1953-08-23 18:45:09",
"1954-08-24 00:35:52",
"1955-08-24 06:18:53",
"1956-08-23 12:14:46",
"1957-08-23 18:07:31",
"1958-08-23 23:45:53",
"1959-08-24 05:43:29",
"1960-08-23 11:34:22",
"1961-08-23 17:18:30",
"1962-08-23 23:12:27",
"1963-08-24 04:57:29",
"1964-08-23 10:51:01",
"1965-08-23 16:42:40",
"1966-08-23 22:17:42",
"1967-08-24 04:12:24",
"1968-08-23 10:02:51",
"1969-08-23 15:43:20",
"1970-08-23 21:33:53",
"1971-08-24 03:15:14",
"1972-08-23 09:03:01",
"1973-08-23 14:53:27",
"1974-08-23 20:28:40",
"1975-08-24 02:23:37",
"1976-08-23 08:18:15",
"1977-08-23 14:00:14",
"1978-08-23 19:56:46",
"1979-08-24 01:46:43",
"1980-08-23 07:40:38",
"1981-08-23 13:38:10",
"1982-08-23 19:15:13",
"1983-08-24 01:07:29",
"1984-08-23 07:00:10",
"1985-08-23 12:35:41",
"1986-08-23 18:25:47",
"1987-08-24 00:09:50",
"1988-08-23 05:54:00",
"1989-08-23 11:46:13",
"1990-08-23 17:20:49",
"1991-08-23 23:12:51",
"1992-08-23 05:10:06",
"1993-08-23 10:50:18",
"1994-08-23 16:43:45",
"1995-08-23 22:34:50",
"1996-08-23 04:22:50",
"1997-08-23 10:19:11",
"1998-08-23 15:58:56",
"1999-08-23 21:51:05",
"2000-08-23 03:48:31",
"2001-08-23 09:27:08",
"2002-08-23 15:16:58",
"2003-08-23 21:08:10",
"2004-08-23 02:53:15",
"2005-08-23 08:45:27",
"2006-08-23 14:22:34",
"2007-08-23 20:07:57",
"2008-08-23 02:02:14",
"2009-08-23 07:38:33",
"2010-08-23 13:26:57",
"2011-08-23 19:20:38",
"2012-08-23 01:06:50",
"2013-08-23 07:01:41",
"2014-08-23 12:45:58",
"2015-08-23 18:37:15",
"2016-08-23 00:38:26",
"2017-08-23 06:20:09",
"2018-08-23 12:08:30",
"2019-08-23 18:01:53",
"2020-08-22 23:44:48",
"2021-08-23 05:34:48",
"2022-08-23 11:15:59",
"2023-08-23 17:01:06",
"2024-08-22 22:54:48",
"2025-08-23 04:33:35",
"2026-08-23 10:18:31",
"2027-08-23 16:13:59",
"2028-08-22 22:00:35",
"2029-08-23 03:51:14",
"2030-08-23 09:36:00",
"2031-08-23 15:22:54",
"2032-08-22 21:17:53",
"2033-08-23 03:01:22",
"2034-08-23 08:47:16",
"2035-08-23 14:43:39",
"2036-08-22 20:31:52",
"2037-08-23 02:21:28",
"2038-08-23 08:09:33",
"2039-08-23 13:58:04",
"2040-08-22 19:52:41"
        };
        string[] bailu = new string[]
        {
            "1911-09-09 03:13:16",
"1912-09-08 09:05:39",
"1913-09-08 14:42:24",
"1914-09-08 20:32:26",
"1915-09-09 02:17:05",
"1916-09-08 08:04:59",
"1917-09-08 13:59:21",
"1918-09-08 19:35:26",
"1919-09-09 01:27:37",
"1920-09-08 07:26:32",
"1921-09-08 13:09:39",
"1922-09-08 19:06:19",
"1923-09-09 00:57:09",
"1924-09-08 06:45:30",
"1925-09-08 12:40:01",
"1926-09-08 18:15:51",
"1927-09-09 00:05:25",
"1928-09-08 06:01:45",
"1929-09-08 11:39:34",
"1930-09-08 17:28:22",
"1931-09-08 23:17:15",
"1932-09-08 05:02:52",
"1933-09-08 10:57:26",
"1934-09-08 16:36:08",
"1935-09-08 22:24:04",
"1936-09-08 04:20:35",
"1937-09-08 09:59:23",
"1938-09-08 15:48:08",
"1939-09-08 21:42:01",
"1940-09-08 03:29:14",
"1941-09-08 09:23:48",
"1942-09-08 15:06:07",
"1943-09-08 20:55:08",
"1944-09-08 02:55:32",
"1945-09-08 08:38:07",
"1946-09-08 14:27:25",
"1947-09-08 20:21:03",
"1948-09-08 02:04:59",
"1949-09-08 07:54:09",
"1950-09-08 13:33:39",
"1951-09-08 19:18:10",
"1952-09-08 01:13:42",
"1953-09-08 06:52:43",
"1954-09-08 12:37:51",
"1955-09-08 18:31:46",
"1956-09-08 00:18:56",
"1957-09-08 06:12:11",
"1958-09-08 11:58:49",
"1959-09-08 17:47:54",
"1960-09-07 23:45:22",
"1961-09-08 05:29:12",
"1962-09-08 11:15:20",
"1963-09-08 17:11:50",
"1964-09-07 22:59:26",
"1965-09-08 04:47:50",
"1966-09-08 10:32:01",
"1967-09-08 16:17:42",
"1968-09-07 22:11:24",
"1969-09-08 03:55:25",
"1970-09-08 09:37:53",
"1971-09-08 15:30:12",
"1972-09-07 21:15:06",
"1973-09-08 02:59:24",
"1974-09-08 08:45:04",
"1975-09-08 14:33:17",
"1976-09-07 20:28:12",
"1977-09-08 02:15:41",
"1978-09-08 08:02:24",
"1979-09-08 13:59:45",
"1980-09-07 19:53:27",
"1981-09-08 01:43:13",
"1982-09-08 07:31:43",
"1983-09-08 13:20:03",
"1984-09-07 19:09:50",
"1985-09-08 00:53:01",
"1986-09-08 06:34:37",
"1987-09-08 12:24:07",
"1988-09-07 18:11:31",
"1989-09-07 23:53:53",
"1990-09-08 05:37:28",
"1991-09-08 11:27:21",
"1992-09-07 17:18:20",
"1993-09-07 23:07:47",
"1994-09-08 04:55:07",
"1995-09-08 10:48:34",
"1996-09-07 16:42:25",
"1997-09-07 22:28:49",
"1998-09-08 04:15:55",
"1999-09-08 10:09:59",
"2000-09-07 15:59:10",
"2001-09-07 21:46:11",
"2002-09-08 03:31:02",
"2003-09-08 09:20:14",
"2004-09-07 15:12:55",
"2005-09-07 20:56:40",
"2006-09-08 02:39:01",
"2007-09-08 08:29:28",
"2008-09-07 14:14:08",
"2009-09-07 19:57:36",
"2010-09-08 01:44:40",
"2011-09-08 07:34:13",
"2012-09-07 13:29:00",
"2013-09-07 19:16:16",
"2014-09-08 01:01:25",
"2015-09-08 06:59:33",
"2016-09-07 12:51:02",
"2017-09-07 18:38:34",
"2018-09-08 00:29:37",
"2019-09-08 06:16:46",
"2020-09-07 12:07:54",
"2021-09-07 17:52:46",
"2022-09-07 23:32:07",
"2023-09-08 05:26:31",
"2024-09-07 11:11:06",
"2025-09-07 16:51:41",
"2026-09-07 22:40:59",
"2027-09-08 04:28:08",
"2028-09-07 10:21:49",
"2029-09-07 16:11:32",
"2030-09-07 21:52:26",
"2031-09-08 03:49:45",
"2032-09-07 09:37:27",
"2033-09-07 15:19:53",
"2034-09-07 21:13:29",
"2035-09-08 03:01:58",
"2036-09-07 08:54:27",
"2037-09-07 14:45:00",
"2038-09-07 20:25:42",
"2039-09-08 02:23:26",
"2040-09-07 08:13:28"
        };
        string[] qiufen = new string[]
        {
            "1911-09-24 12:17:30",
"1912-09-23 18:07:59",
"1913-09-23 23:52:41",
"1914-09-24 05:33:47",
"1915-09-24 11:23:45",
"1916-09-23 17:14:43",
"1917-09-23 23:00:06",
"1918-09-24 04:45:37",
"1919-09-24 10:35:18",
"1920-09-23 16:28:05",
"1921-09-23 22:19:42",
"1922-09-24 04:09:32",
"1923-09-24 10:03:30",
"1924-09-23 15:58:13",
"1925-09-23 21:43:19",
"1926-09-24 03:26:32",
"1927-09-24 09:16:52",
"1928-09-23 15:05:26",
"1929-09-23 20:52:15",
"1930-09-24 02:35:52",
"1931-09-24 08:23:15",
"1932-09-23 14:15:49",
"1933-09-23 20:01:06",
"1934-09-24 01:45:08",
"1935-09-24 07:38:07",
"1936-09-23 13:25:54",
"1937-09-23 19:12:54",
"1938-09-24 00:59:27",
"1939-09-24 06:49:25",
"1940-09-23 12:45:32",
"1941-09-23 18:32:42",
"1942-09-24 00:16:24",
"1943-09-24 06:11:41",
"1944-09-23 12:01:35",
"1945-09-23 17:49:44",
"1946-09-23 23:40:34",
"1947-09-24 05:28:35",
"1948-09-23 11:21:39",
"1949-09-23 17:05:48",
"1950-09-23 22:43:32",
"1951-09-24 04:36:50",
"1952-09-23 10:23:39",
"1953-09-23 16:05:52",
"1954-09-23 21:55:13",
"1955-09-24 03:40:50",
"1956-09-23 09:35:02",
"1957-09-23 15:26:02",
"1958-09-23 21:08:49",
"1959-09-24 03:08:24",
"1960-09-23 08:58:50",
"1961-09-23 14:42:25",
"1962-09-23 20:35:11",
"1963-09-24 02:23:27",
"1964-09-23 08:16:38",
"1965-09-23 14:05:57",
"1966-09-23 19:43:08",
"1967-09-24 01:38:01",
"1968-09-23 07:26:10",
"1969-09-23 13:06:53",
"1970-09-23 18:58:59",
"1971-09-24 00:44:52",
"1972-09-23 06:32:45",
"1973-09-23 12:21:06",
"1974-09-23 17:58:26",
"1975-09-23 23:55:12",
"1976-09-23 05:48:11",
"1977-09-23 11:29:13",
"1978-09-23 17:25:24",
"1979-09-23 23:16:22",
"1980-09-23 05:08:40",
"1981-09-23 11:05:11",
"1982-09-23 16:46:11",
"1983-09-23 22:41:37",
"1984-09-23 04:32:53",
"1985-09-23 10:07:27",
"1986-09-23 15:58:52",
"1987-09-23 21:45:16",
"1988-09-23 03:28:50",
"1989-09-23 09:19:37",
"1990-09-23 14:55:29",
"1991-09-23 20:48:06",
"1992-09-23 02:42:46",
"1993-09-23 08:22:30",
"1994-09-23 14:19:13",
"1995-09-23 20:13:00",
"1996-09-23 02:00:06",
"1997-09-23 07:55:47",
"1998-09-23 13:37:11",
"1999-09-23 19:31:31",
"2000-09-23 01:27:35",
"2001-09-23 07:04:28",
"2002-09-23 12:55:23",
"2003-09-23 18:46:49",
"2004-09-23 00:29:50",
"2005-09-23 06:23:11",
"2006-09-23 12:03:22",
"2007-09-23 17:51:13",
"2008-09-22 23:44:29",
"2009-09-23 05:18:35",
"2010-09-23 11:09:02",
"2011-09-23 17:04:37",
"2012-09-22 22:48:59",
"2013-09-23 04:44:08",
"2014-09-23 10:29:04",
"2015-09-23 16:20:31",
"2016-09-22 22:21:05",
"2017-09-23 04:01:44",
"2018-09-23 09:54:01",
"2019-09-23 15:50:02",
"2020-09-22 21:30:32",
"2021-09-23 03:20:55",
"2022-09-23 09:03:31",
"2023-09-23 14:49:46",
"2024-09-22 20:43:27",
"2025-09-23 02:19:04",
"2026-09-23 08:04:56",
"2027-09-23 14:01:23",
"2028-09-22 19:44:59",
"2029-09-23 01:38:09",
"2030-09-23 07:26:30",
"2031-09-23 13:14:54",
"2032-09-22 19:10:27",
"2033-09-23 00:51:12",
"2034-09-23 06:39:04",
"2035-09-23 12:38:26",
"2036-09-22 18:22:46",
"2037-09-23 00:12:31",
"2038-09-23 06:01:40",
"2039-09-23 11:49:00",
"2040-09-22 17:44:17"
        };
        string[] hanlu = new string[]
        {
            "1911-10-09 18:14:56",
"1912-10-09 00:06:42",
"1913-10-09 05:43:40",
"1914-10-09 11:34:47",
"1915-10-09 17:20:52",
"1916-10-08 23:07:51",
"1917-10-09 05:02:08",
"1918-10-09 10:40:17",
"1919-10-09 16:33:20",
"1920-10-08 22:29:08",
"1921-10-09 04:10:36",
"1922-10-09 10:09:25",
"1923-10-09 16:03:22",
"1924-10-08 21:52:09",
"1925-10-09 03:47:22",
"1926-10-09 09:24:51",
"1927-10-09 15:15:05",
"1928-10-08 21:09:51",
"1929-10-09 02:47:02",
"1930-10-09 08:37:28",
"1931-10-09 14:26:51",
"1932-10-08 20:09:38",
"1933-10-09 02:03:53",
"1934-10-09 07:44:59",
"1935-10-09 13:35:40",
"1936-10-08 19:32:25",
"1937-10-09 01:10:54",
"1938-10-09 07:01:24",
"1939-10-09 12:56:36",
"1940-10-08 18:42:23",
"1941-10-09 00:38:12",
"1942-10-09 06:21:42",
"1943-10-09 12:10:29",
"1944-10-08 18:08:43",
"1945-10-08 23:49:07",
"1946-10-09 05:40:47",
"1947-10-09 11:37:17",
"1948-10-08 17:20:16",
"1949-10-08 23:11:02",
"1950-10-09 04:51:39",
"1951-10-09 10:36:23",
"1952-10-08 16:32:25",
"1953-10-08 22:10:25",
"1954-10-09 03:57:18",
"1955-10-09 09:52:08",
"1956-10-08 15:35:53",
"1957-10-08 21:29:58",
"1958-10-09 03:19:08",
"1959-10-09 09:09:48",
"1960-10-08 15:08:39",
"1961-10-08 20:50:56",
"1962-10-09 02:37:53",
"1963-10-09 08:36:14",
"1964-10-08 14:21:30",
"1965-10-08 20:11:07",
"1966-10-09 01:56:43",
"1967-10-09 07:41:11",
"1968-10-08 13:34:23",
"1969-10-08 19:16:40",
"1970-10-09 01:01:32",
"1971-10-09 06:58:34",
"1972-10-08 12:41:45",
"1973-10-08 18:27:15",
"1974-10-09 00:14:39",
"1975-10-09 06:02:04",
"1976-10-08 11:58:03",
"1977-10-08 17:43:56",
"1978-10-08 23:30:54",
"1979-10-09 05:30:02",
"1980-10-08 11:19:14",
"1981-10-08 17:09:32",
"1982-10-08 23:02:09",
"1983-10-09 04:51:04",
"1984-10-08 10:42:35",
"1985-10-08 16:24:33",
"1986-10-08 22:06:45",
"1987-10-09 03:59:40",
"1988-10-08 09:44:30",
"1989-10-08 15:27:19",
"1990-10-08 21:13:49",
"1991-10-09 03:01:07",
"1992-10-08 08:51:29",
"1993-10-08 14:40:02",
"1994-10-08 20:29:05",
"1995-10-09 02:27:12",
"1996-10-08 08:18:42",
"1997-10-08 14:05:10",
"1998-10-08 19:55:45",
"1999-10-09 01:48:21",
"2000-10-08 07:38:13",
"2001-10-08 13:25:01",
"2002-10-08 19:09:18",
"2003-10-09 01:00:33",
"2004-10-08 06:49:18",
"2005-10-08 12:33:18",
"2006-10-08 18:21:23",
"2007-10-09 00:11:29",
"2008-10-08 05:56:37",
"2009-10-08 11:40:03",
"2010-10-08 17:26:29",
"2011-10-08 23:19:05",
"2012-10-08 05:11:42",
"2013-10-08 10:58:29",
"2014-10-08 16:47:29",
"2015-10-08 22:42:47",
"2016-10-08 04:33:20",
"2017-10-08 10:22:05",
"2018-10-08 16:14:37",
"2019-10-08 22:05:32",
"2020-10-08 03:55:07",
"2021-10-08 09:38:53",
"2022-10-08 15:22:16",
"2023-10-08 21:15:23",
"2024-10-08 02:59:43",
"2025-10-08 08:40:57",
"2026-10-08 14:28:59",
"2027-10-08 20:16:46",
"2028-10-08 02:08:10",
"2029-10-08 07:57:45",
"2030-10-08 13:44:52",
"2031-10-08 19:42:33",
"2032-10-08 01:29:58",
"2033-10-08 07:13:28",
"2034-10-08 13:06:36",
"2035-10-08 18:57:10",
"2036-10-08 00:48:26",
"2037-10-08 06:37:16",
"2038-10-08 12:20:59",
"2039-10-08 18:16:40",
"2040-10-08 00:04:54"
        };
        string[] shuangjiang = new string[]
        {
            "1911-10-24 20:58:11",
"1912-10-24 02:50:00",
"1913-10-24 08:34:49",
"1914-10-24 14:17:16",
"1915-10-24 20:09:39",
"1916-10-24 01:57:11",
"1917-10-24 07:43:37",
"1918-10-24 13:32:47",
"1919-10-24 19:21:14",
"1920-10-24 01:12:39",
"1921-10-24 07:02:15",
"1922-10-24 12:52:52",
"1923-10-24 18:50:49",
"1924-10-24 00:44:22",
"1925-10-24 06:31:02",
"1926-10-24 12:18:14",
"1927-10-24 18:06:37",
"1928-10-23 23:54:28",
"1929-10-24 05:41:24",
"1930-10-24 11:25:57",
"1931-10-24 17:15:26",
"1932-10-23 23:03:50",
"1933-10-24 04:48:03",
"1934-10-24 10:36:15",
"1935-10-24 16:29:09",
"1936-10-23 22:18:02",
"1937-10-24 04:06:30",
"1938-10-24 09:53:44",
"1939-10-24 15:45:49",
"1940-10-23 21:39:18",
"1941-10-24 03:27:09",
"1942-10-24 09:15:08",
"1943-10-24 15:08:15",
"1944-10-23 20:55:56",
"1945-10-24 02:43:34",
"1946-10-24 08:34:39",
"1947-10-24 14:25:50",
"1948-10-23 20:17:57",
"1949-10-24 02:02:57",
"1950-10-24 07:44:43",
"1951-10-24 13:36:00",
"1952-10-23 19:22:10",
"1953-10-24 01:06:14",
"1954-10-24 06:56:19",
"1955-10-24 12:43:01",
"1956-10-23 18:34:19",
"1957-10-24 00:24:10",
"1958-10-24 06:11:18",
"1959-10-24 12:10:58",
"1960-10-23 18:01:49",
"1961-10-23 23:47:21",
"1962-10-24 05:39:59",
"1963-10-24 11:28:48",
"1964-10-23 17:20:40",
"1965-10-23 23:09:55",
"1966-10-24 04:50:45",
"1967-10-24 10:43:44",
"1968-10-23 16:29:34",
"1969-10-23 22:11:02",
"1970-10-24 04:04:14",
"1971-10-24 09:53:08",
"1972-10-23 15:41:27",
"1973-10-23 21:30:09",
"1974-10-24 03:10:36",
"1975-10-24 09:06:01",
"1976-10-23 14:58:01",
"1977-10-23 20:40:39",
"1978-10-24 02:37:09",
"1979-10-24 08:27:50",
"1980-10-23 14:17:30",
"1981-10-23 20:12:49",
"1982-10-24 01:57:47",
"1983-10-24 07:54:17",
"1984-10-23 13:45:39",
"1985-10-23 19:21:52",
"1986-10-24 01:14:11",
"1987-10-24 07:00:52",
"1988-10-23 12:44:06",
"1989-10-23 18:35:08",
"1990-10-24 00:13:56",
"1991-10-24 06:05:10",
"1992-10-23 11:57:07",
"1993-10-23 17:37:08",
"1994-10-23 23:36:01",
"1995-10-24 05:31:31",
"1996-10-23 11:18:42",
"1997-10-23 17:14:45",
"1998-10-23 22:58:35",
"1999-10-24 04:52:14",
"2000-10-23 10:47:28",
"2001-10-23 16:25:36",
"2002-10-23 22:17:49",
"2003-10-24 04:08:27",
"2004-10-23 09:48:49",
"2005-10-23 15:42:20",
"2006-10-23 21:26:28",
"2007-10-24 03:15:23",
"2008-10-23 09:08:38",
"2009-10-23 14:43:28",
"2010-10-23 20:35:04",
"2011-10-24 02:30:18",
"2012-10-23 08:13:33",
"2013-10-23 14:09:48",
"2014-10-23 19:57:03",
"2015-10-24 01:46:41",
"2016-10-23 07:45:30",
"2017-10-23 13:26:36",
"2018-10-23 19:22:18",
"2019-10-24 01:19:37",
"2020-10-23 06:59:25",
"2021-10-23 12:51:00",
"2022-10-23 18:35:31",
"2023-10-24 00:20:39",
"2024-10-23 06:14:32",
"2025-10-23 11:50:39",
"2026-10-23 17:37:39",
"2027-10-23 23:32:33",
"2028-10-23 05:13:03",
"2029-10-23 11:07:45",
"2030-10-23 17:00:10",
"2031-10-23 22:49:01",
"2032-10-23 04:45:47",
"2033-10-23 10:27:08",
"2034-10-23 16:15:57",
"2035-10-23 22:15:39",
"2036-10-23 03:58:17",
"2037-10-23 09:49:18",
"2038-10-23 15:40:05",
"2039-10-23 21:24:27",
"2040-10-23 03:19:07"
        };
        string[] lidong = new string[]
        {
            "1911-11-08 20:47:00",
"1912-11-08 02:38:38",
"1913-11-08 08:17:42",
"1914-11-08 14:11:01",
"1915-11-08 19:57:38",
"1916-11-08 01:42:15",
"1917-11-08 07:36:54",
"1918-11-08 13:18:52",
"1919-11-08 19:11:30",
"1920-11-08 01:04:54",
"1921-11-08 06:45:30",
"1922-11-08 12:45:12",
"1923-11-08 18:40:20",
"1924-11-08 00:29:11",
"1925-11-08 06:26:13",
"1926-11-08 12:07:42",
"1927-11-08 17:56:54",
"1928-11-07 23:49:30",
"1929-11-08 05:27:27",
"1930-11-08 11:20:12",
"1931-11-08 17:09:51",
"1932-11-07 22:49:40",
"1933-11-08 04:42:58",
"1934-11-08 10:26:41",
"1935-11-08 16:17:31",
"1936-11-07 22:14:38",
"1937-11-08 03:55:15",
"1938-11-08 09:48:19",
"1939-11-08 15:43:30",
"1940-11-07 21:26:46",
"1941-11-08 03:24:03",
"1942-11-08 09:11:07",
"1943-11-08 14:58:43",
"1944-11-07 20:54:39",
"1945-11-08 02:34:11",
"1946-11-08 08:27:09",
"1947-11-08 14:24:22",
"1948-11-07 20:06:32",
"1949-11-08 01:59:46",
"1950-11-08 07:43:43",
"1951-11-08 13:26:36",
"1952-11-07 19:21:34",
"1953-11-08 01:00:57",
"1954-11-08 06:50:34",
"1955-11-08 12:45:09",
"1956-11-07 18:25:54",
"1957-11-08 00:20:01",
"1958-11-08 06:11:53",
"1959-11-08 12:02:03",
"1960-11-07 18:02:01",
"1961-11-07 23:46:11",
"1962-11-08 05:34:54",
"1963-11-08 11:32:19",
"1964-11-07 17:15:06",
"1965-11-07 23:06:32",
"1966-11-08 04:55:15",
"1967-11-08 10:37:23",
"1968-11-07 16:29:17",
"1969-11-07 22:11:20",
"1970-11-08 03:57:43",
"1971-11-08 09:56:37",
"1972-11-07 15:39:23",
"1973-11-07 21:27:38",
"1974-11-08 03:17:58",
"1975-11-08 09:02:36",
"1976-11-07 14:58:34",
"1977-11-07 20:45:49",
"1978-11-08 02:34:01",
"1979-11-08 08:32:47",
"1980-11-07 14:18:13",
"1981-11-07 20:08:29",
"1982-11-08 02:04:06",
"1983-11-08 07:52:12",
"1984-11-07 13:45:32",
"1985-11-07 19:29:29",
"1986-11-08 01:12:49",
"1987-11-08 07:05:40",
"1988-11-07 12:48:55",
"1989-11-07 18:33:32",
"1990-11-08 00:23:30",
"1991-11-08 06:07:50",
"1992-11-07 11:57:02",
"1993-11-07 17:45:33",
"1994-11-07 23:35:36",
"1995-11-08 05:35:35",
"1996-11-07 11:26:33",
"1997-11-07 17:14:38",
"1998-11-07 23:08:23",
"1999-11-08 04:57:51",
"2000-11-07 10:48:04",
"2001-11-07 16:36:52",
"2002-11-07 22:21:49",
"2003-11-08 04:13:11",
"2004-11-07 09:58:33",
"2005-11-07 15:42:26",
"2006-11-07 21:34:51",
"2007-11-08 03:24:01",
"2008-11-07 09:10:33",
"2009-11-07 14:56:15",
"2010-11-07 20:42:29",
"2011-11-08 02:34:55",
"2012-11-07 08:25:56",
"2013-11-07 14:13:52",
"2014-11-07 20:06:40",
"2015-11-08 01:58:36",
"2016-11-07 07:47:38",
"2017-11-07 13:37:45",
"2018-11-07 19:31:39",
"2019-11-08 01:24:15",
"2020-11-07 07:13:46",
"2021-11-07 12:58:37",
"2022-11-07 18:45:18",
"2023-11-08 00:35:23",
"2024-11-07 06:19:49",
"2025-11-07 12:03:48",
"2026-11-07 17:51:46",
"2027-11-07 23:38:15",
"2028-11-07 05:26:54",
"2029-11-07 11:16:23",
"2030-11-07 17:08:20",
"2031-11-07 23:05:15",
"2032-11-07 04:53:49",
"2033-11-07 10:40:36",
"2034-11-07 16:33:09",
"2035-11-07 22:23:20",
"2036-11-07 04:14:07",
"2037-11-07 10:03:30",
"2038-11-07 15:50:16",
"2039-11-07 21:42:17",
"2040-11-07 03:28:40"
        };
        string[] xiaoxue = new string[]
        {
            "1911-11-23 17:55:53",
"1912-11-22 23:48:08",
"1913-11-23 05:35:12",
"1914-11-23 11:20:21",
"1915-11-23 17:13:26",
"1916-11-22 22:57:44",
"1917-11-23 04:44:52",
"1918-11-23 10:38:02",
"1919-11-23 16:25:07",
"1920-11-22 22:15:23",
"1921-11-23 04:04:27",
"1922-11-23 09:55:09",
"1923-11-23 15:53:36",
"1924-11-22 21:46:22",
"1925-11-23 03:35:24",
"1926-11-23 09:27:33",
"1927-11-23 15:13:55",
"1928-11-22 21:00:12",
"1929-11-23 02:48:03",
"1930-11-23 08:34:25",
"1931-11-23 14:24:38",
"1932-11-22 20:10:05",
"1933-11-23 01:53:24",
"1934-11-23 07:44:20",
"1935-11-23 13:35:22",
"1936-11-22 19:25:00",
"1937-11-23 01:16:27",
"1938-11-23 07:06:02",
"1939-11-23 12:58:28",
"1940-11-22 18:48:55",
"1941-11-23 00:37:47",
"1942-11-23 06:30:21",
"1943-11-23 12:21:26",
"1944-11-22 18:07:30",
"1945-11-22 23:55:11",
"1946-11-23 05:46:15",
"1947-11-23 11:37:36",
"1948-11-22 17:28:47",
"1949-11-22 23:16:02",
"1950-11-23 05:02:29",
"1951-11-23 10:51:03",
"1952-11-22 16:35:36",
"1953-11-22 22:22:03",
"1954-11-23 04:14:10",
"1955-11-23 10:00:51",
"1956-11-22 15:49:50",
"1957-11-22 21:39:00",
"1958-11-23 03:29:06",
"1959-11-23 09:26:53",
"1960-11-22 15:18:20",
"1961-11-22 21:07:40",
"1962-11-23 03:01:50",
"1963-11-23 08:49:20",
"1964-11-22 14:38:53",
"1965-11-22 20:29:08",
"1966-11-23 02:14:05",
"1967-11-23 08:04:26",
"1968-11-22 13:48:31",
"1969-11-22 19:31:04",
"1970-11-23 01:24:32",
"1971-11-23 07:13:56",
"1972-11-22 13:02:41",
"1973-11-22 18:54:00",
"1974-11-23 00:38:27",
"1975-11-23 06:30:42",
"1976-11-22 12:21:28",
"1977-11-22 18:06:59",
"1978-11-23 00:04:36",
"1979-11-23 05:54:06",
"1980-11-22 11:41:23",
"1981-11-22 17:35:56",
"1982-11-22 23:23:18",
"1983-11-23 05:18:20",
"1984-11-22 11:10:38",
"1985-11-22 16:50:46",
"1986-11-22 22:44:20",
"1987-11-23 04:29:23",
"1988-11-22 10:11:59",
"1989-11-22 16:04:37",
"1990-11-22 21:46:55",
"1991-11-23 03:35:45",
"1992-11-22 09:25:51",
"1993-11-22 15:06:51",
"1994-11-22 21:05:58",
"1995-11-23 03:01:23",
"1996-11-22 08:49:24",
"1997-11-22 14:47:33",
"1998-11-22 20:34:12",
"1999-11-23 02:24:50",
"2000-11-22 08:19:20",
"2001-11-22 14:00:28",
"2002-11-22 19:53:44",
"2003-11-23 01:43:21",
"2004-11-22 07:21:40",
"2005-11-22 13:14:58",
"2006-11-22 19:01:45",
"2007-11-23 00:49:53",
"2008-11-22 06:44:19",
"2009-11-22 12:22:33",
"2010-11-22 18:14:33",
"2011-11-23 00:07:48",
"2012-11-22 05:50:07",
"2013-11-22 11:48:06",
"2014-11-22 17:38:11",
"2015-11-22 23:25:15",
"2016-11-22 05:22:20",
"2017-11-22 11:04:34",
"2018-11-22 17:01:24",
"2019-11-22 22:58:48",
"2020-11-22 04:39:38",
"2021-11-22 10:33:34",
"2022-11-22 16:20:18",
"2023-11-22 22:02:29",
"2024-11-22 03:56:16",
"2025-11-22 09:35:18",
"2026-11-22 15:23:03",
"2027-11-22 21:15:54",
"2028-11-22 02:54:02",
"2029-11-22 08:49:01",
"2030-11-22 14:44:12",
"2031-11-22 20:32:11",
"2032-11-22 02:30:44",
"2033-11-22 08:15:42",
"2034-11-22 14:04:28",
"2035-11-22 20:02:41",
"2036-11-22 01:44:45",
"2037-11-22 07:37:50",
"2038-11-22 13:30:44",
"2039-11-22 19:11:35",
"2040-11-22 01:04:53"
        };
        string[] daxue = new string[]
        {
            "1911-12-08 13:07:34",
"1912-12-07 18:58:53",
"1913-12-08 00:41:01",
"1914-12-08 06:37:05",
"1915-12-08 12:23:53",
"1916-12-07 18:06:09",
"1917-12-08 00:00:59",
"1918-12-08 05:46:29",
"1919-12-08 11:37:47",
"1920-12-07 17:30:16",
"1921-12-07 23:11:25",
"1922-12-08 05:10:38",
"1923-12-08 11:04:33",
"1924-12-07 16:52:59",
"1925-12-07 22:52:17",
"1926-12-08 04:38:39",
"1927-12-08 10:26:18",
"1928-12-07 16:17:16",
"1929-12-07 21:56:24",
"1930-12-08 03:50:37",
"1931-12-08 09:40:15",
"1932-12-07 15:18:22",
"1933-12-07 21:11:05",
"1934-12-08 02:56:31",
"1935-12-08 08:44:50",
"1936-12-07 14:42:13",
"1937-12-07 20:26:16",
"1938-12-08 02:21:58",
"1939-12-08 08:17:00",
"1940-12-07 13:57:51",
"1941-12-07 19:55:58",
"1942-12-08 01:46:47",
"1943-12-08 07:32:50",
"1944-12-07 13:27:38",
"1945-12-07 19:07:39",
"1946-12-08 01:00:11",
"1947-12-08 06:56:11",
"1948-12-07 12:37:37",
"1949-12-07 18:33:24",
"1950-12-08 00:21:40",
"1951-12-08 06:02:18",
"1952-12-07 11:55:33",
"1953-12-07 17:36:59",
"1954-12-07 23:28:29",
"1955-12-08 05:22:46",
"1956-12-07 11:02:06",
"1957-12-07 16:55:56",
"1958-12-07 22:49:35",
"1959-12-08 04:37:16",
"1960-12-07 10:37:44",
"1961-12-07 16:25:54",
"1962-12-07 22:16:39",
"1963-12-08 04:12:36",
"1964-12-07 09:53:03",
"1965-12-07 15:45:32",
"1966-12-07 21:37:45",
"1967-12-08 03:17:28",
"1968-12-07 09:08:15",
"1969-12-07 14:51:18",
"1970-12-07 20:37:19",
"1971-12-08 02:35:42",
"1972-12-07 08:18:42",
"1973-12-07 14:10:23",
"1974-12-07 20:04:37",
"1975-12-08 01:46:09",
"1976-12-07 07:40:56",
"1977-12-07 13:30:49",
"1978-12-07 19:20:01",
"1979-12-08 01:17:48",
"1980-12-07 07:01:15",
"1981-12-07 12:51:15",
"1982-12-07 18:48:05",
"1983-12-08 00:33:40",
"1984-12-07 06:28:03",
"1985-12-07 12:16:21",
"1986-12-07 18:00:56",
"1987-12-07 23:52:12",
"1988-12-07 05:34:28",
"1989-12-07 11:20:57",
"1990-12-07 17:14:10",
"1991-12-07 22:56:00",
"1992-12-07 04:44:12",
"1993-12-07 10:33:49",
"1994-12-07 16:22:53",
"1995-12-07 22:22:15",
"1996-12-07 04:14:00",
"1997-12-07 10:04:52",
"1998-12-07 16:01:35",
"1999-12-07 21:47:27",
"2000-12-07 03:37:02",
"2001-12-07 09:28:53",
"2002-12-07 15:14:14",
"2003-12-07 21:05:09",
"2004-12-07 02:48:57",
"2005-12-07 08:32:41",
"2006-12-07 14:26:49",
"2007-12-07 20:14:04",
"2008-12-07 02:02:17",
"2009-12-07 07:52:14",
"2010-12-07 13:38:22",
"2011-12-07 19:28:59",
"2012-12-07 01:18:55",
"2013-12-07 07:08:31",
"2014-12-07 13:04:05",
"2015-12-07 18:53:19",
"2016-12-07 00:41:05",
"2017-12-07 06:32:35",
"2018-12-07 12:25:48",
"2019-12-07 18:18:21",
"2020-12-07 00:09:21",
"2021-12-07 05:56:55",
"2022-12-07 11:46:04",
"2023-12-07 17:32:44",
"2024-12-06 23:16:47",
"2025-12-07 05:04:20",
"2026-12-07 10:52:14",
"2027-12-07 16:37:21",
"2028-12-06 22:24:19",
"2029-12-07 04:13:25",
"2030-12-07 10:07:13",
"2031-12-07 16:02:27",
"2032-12-06 21:52:52",
"2033-12-07 03:44:27",
"2034-12-07 09:36:19",
"2035-12-07 15:25:00",
"2036-12-06 21:15:29",
"2037-12-07 03:06:43",
"2038-12-07 08:55:48",
"2039-12-07 14:44:28",
"2040-12-06 20:29:25"
        };
        string[] dongzhi = new string[]
        {
            "1911-12-23 06:53:09",
"1912-12-22 12:44:39",
"1913-12-22 18:34:51",
"1914-12-23 00:22:22",
"1915-12-23 06:15:44",
"1916-12-22 11:58:29",
"1917-12-22 17:45:37",
"1918-12-22 23:41:27",
"1919-12-23 05:27:01",
"1920-12-22 11:16:56",
"1921-12-22 17:07:23",
"1922-12-22 22:56:52",
"1923-12-23 04:53:13",
"1924-12-22 10:45:24",
"1925-12-22 16:36:35",
"1926-12-22 22:33:18",
"1927-12-23 04:18:26",
"1928-12-22 10:03:36",
"1929-12-22 15:52:41",
"1930-12-22 21:39:29",
"1931-12-23 03:29:31",
"1932-12-22 09:14:12",
"1933-12-22 14:57:26",
"1934-12-22 20:49:22",
"1935-12-23 02:37:03",
"1936-12-22 08:26:37",
"1937-12-22 14:21:36",
"1938-12-22 20:13:21",
"1939-12-23 02:05:55",
"1940-12-22 07:54:41",
"1941-12-22 13:44:06",
"1942-12-22 19:39:31",
"1943-12-23 01:29:04",
"1944-12-22 07:14:45",
"1945-12-22 13:03:32",
"1946-12-22 18:53:17",
"1947-12-23 00:42:43",
"1948-12-22 06:33:13",
"1949-12-22 12:22:51",
"1950-12-22 18:13:18",
"1951-12-23 00:00:01",
"1952-12-22 05:43:06",
"1953-12-22 11:31:25",
"1954-12-22 17:24:19",
"1955-12-22 23:10:52",
"1956-12-22 04:59:27",
"1957-12-22 10:48:34",
"1958-12-22 16:39:40",
"1959-12-22 22:34:18",
"1960-12-22 04:25:53",
"1961-12-22 10:19:26",
"1962-12-22 16:15:14",
"1963-12-22 22:01:52",
"1964-12-22 03:49:31",
"1965-12-22 09:40:23",
"1966-12-22 15:28:08",
"1967-12-22 21:16:17",
"1968-12-22 02:59:45",
"1969-12-22 08:43:41",
"1970-12-22 14:35:40",
"1971-12-22 20:23:53",
"1972-12-22 02:12:53",
"1973-12-22 08:07:41",
"1974-12-22 13:55:56",
"1975-12-22 19:45:33",
"1976-12-22 01:35:06",
"1977-12-22 07:23:08",
"1978-12-22 13:20:57",
"1979-12-22 19:09:45",
"1980-12-22 00:56:04",
"1981-12-22 06:50:31",
"1982-12-22 12:38:09",
"1983-12-22 18:29:55",
"1984-12-22 00:22:48",
"1985-12-22 06:07:40",
"1986-12-22 12:02:07",
"1987-12-22 17:45:52",
"1988-12-21 23:27:53",
"1989-12-22 05:22:00",
"1990-12-22 11:06:59",
"1991-12-22 16:53:38",
"1992-12-21 22:43:13",
"1993-12-22 04:25:48",
"1994-12-22 10:22:43",
"1995-12-22 16:16:47",
"1996-12-21 22:05:53",
"1997-12-22 04:07:02",
"1998-12-22 09:56:27",
"1999-12-22 15:43:48",
"2000-12-21 21:37:26",
"2001-12-22 03:21:30",
"2002-12-22 09:14:22",
"2003-12-22 15:03:48",
"2004-12-21 20:41:36",
"2005-12-22 02:34:56",
"2006-12-22 08:22:05",
"2007-12-22 14:07:48",
"2008-12-21 20:03:45",
"2009-12-22 01:46:47",
"2010-12-22 07:38:27",
"2011-12-22 13:30:01",
"2012-12-21 19:11:35",
"2013-12-22 01:10:59",
"2014-12-22 07:03:01",
"2015-12-22 12:47:55",
"2016-12-21 18:44:07",
"2017-12-22 00:27:53",
"2018-12-22 06:22:38",
"2019-12-22 12:19:18",
"2020-12-21 18:02:12",
"2021-12-21 23:59:09",
"2022-12-22 05:48:01",
"2023-12-22 11:27:09",
"2024-12-21 17:20:20",
"2025-12-21 23:02:48",
"2026-12-22 04:49:55",
"2027-12-22 10:41:50",
"2028-12-21 16:19:19",
"2029-12-21 22:13:45",
"2030-12-22 04:09:13",
"2031-12-22 09:55:07",
"2032-12-21 15:55:29",
"2033-12-21 21:45:32",
"2034-12-22 03:33:30",
"2035-12-22 09:30:21",
"2036-12-21 15:12:20",
"2037-12-21 21:07:11",
"2038-12-22 03:01:44",
"2039-12-22 08:39:59",
"2040-12-21 14:32:13"
        };
        #endregion

        string[] jieQiNames = new string[]
    {
        "小寒","大寒","立春","雨水","惊蛰","春分",
        "清明","谷雨","立夏","小满","芒种","夏至",
        "小暑","大暑","立秋","处暑","白露","秋分",
        "寒露","霜降","立冬","小雪","大雪","冬至"
    };
        string[][] jieQiTimes = new string[][] {
        xiaohan, dahan, lichun, yushui,jingzhe,chunfen,qingming,guyu,lixia,xiaoman,mangzhong,xiazhi,
        xiaoshu,dashu,liqiu,chushu,bailu,qiufen,hanlu,shuangjiang,lidong,xiaoxue,daxue,dongzhi
    };

        //int yearIndex = year - 1901; // 以2021为起点，如果你的数组是从2021年开始的
        int yearIndex = year - 1911;

        // 每月固定两个节气
        int index = (month - 1) * 2;

        // 防止越界
        if (index < 0 || index + 1 >= 24 || yearIndex < 0)
            return new string[] { "立春", "1900-02-03 22:58:39", "雨水", "1900-02-18 18:43:49" };//无数据的情况

        return new string[]
    {
        LangManager.GetArrayValue("SolarTerms", index),
        jieQiTimes[index][yearIndex],
        LangManager.GetArrayValue("SolarTerms", index+1),
        jieQiTimes[index + 1][yearIndex]
    };
    }

    /// <summary>
    /// 六曜
    /// </summary>
    /// <param name="lDate">农历日期：正月初一</param>
    /// <returns></returns>
    public static (string Name, string JiXiong) Get6Yao(string lDate)
    {
        // 获取六曜表
        var yaoList = LangManager.GetArray("SixYao");
        if (yaoList == null || yaoList.Count == 0) return ("", "");

        int month = Array.IndexOf(LunarMonthNames, LunarMonthNames.FirstOrDefault(m => lDate.StartsWith(m))) + 1;
        if (month == 0) return ("", "");

        // 解析农历日
        string dayStr = lDate.Substring(lDate.IndexOf('月') + 1);
        int day = Array.IndexOf(LunarDayNames, dayStr) + 1;
        if (day == 0) return ("", "");

        // 计算六曜序号
        int index = (month + day) % 6;

        var item = yaoList[index] as Dictionary<string, object>;
        if (item == null) return ("", "");

        return (item["Name"].ToString(), item["JiShi"].ToString());
    }

    /// <summary>
    /// 计算年飞星并将结果直接填入九宫格 Label
    /// 顺飞 567891234 逆飞 543219876
    /// </summary>
    public static int[] GetYearStar(DateTime date)
    {
        int year = date.Year;
        int lastTwo = year % 100;
        int sum = (lastTwo / 10) + (lastTwo % 10);
        int baseNum = year < 2000 ? 10 : 9;

        if (sum > baseNum)
            sum = (sum / 10) + (sum % 10);

        int yearStar = baseNum - sum;
        if (yearStar == 0)
            yearStar = 9;

        bool isForward = year < 2000 ? true : false; // 2000年后逆飞，之前顺飞

        // 九宫格中 label 的顺序（0~8）
        // 0北, 1东北, 2东, 3东南, 4中, 5西南, 6西, 7西北, 8南
        int[] forwardSeq = { 4, 8, 5, 6, 1, 7, 2, 3, 0 }; // 顺飞（起于中宫）
        int[] backwardSeq = { 4, 0, 3, 2, 7, 1, 6, 5, 8 }; // 逆飞（起于中宫）

        int[] seq = isForward ? forwardSeq : backwardSeq;

        // 找到起始位置（入中宫的星）
        int startIndex = Array.IndexOf(seq, 4);
        if (startIndex < 0) startIndex = 0;

        // 生成飞星序列
        int[] stars = new int[9];
        for (int i = 0; i < 9; i++)
        {
            int pos = seq[(startIndex + i) % 9]; // 对应label下标
            int num = Wrap9(yearStar, i, isForward); // 计算当前飞星号
            stars[pos] = num;
        }

        Debug.Log($"[{year}] 年飞星：{yearStar} {(isForward ? "顺飞" : "逆飞")}");
        return stars;
    }

    // 飞星号循环（顺飞 +1，逆飞 -1）
    static int Wrap9(int baseStar, int offset, bool forward)
    {
        int n = forward ? (baseStar + offset - 1) % 9 + 1
                        : ((baseStar - offset - 1 + 9 * 10) % 9) + 1;
        return n;
    }

    /// <summary>
    /// 月-九宫飞星
    /// </summary>
    public static int[] GetMonthStar(DateTime date)
    {
        // 获取年干支
        string gzYear = GetChineseCalendar(date).GanZhiYear; // 例如 "甲子"
        char diZhi = gzYear[1];

        int firstMonthStar;
        bool forward; // true=顺飞, false=逆飞

        // 按地支判断正月入中宫的飞星及方向
        switch (diZhi)
        {
            case '子':
            case '午':
            case '卯':
            case '酉':
                firstMonthStar = 8; // 八白
                forward = false;    // 逆飞
                break;

            case '寅':
            case '申':
            case '巳':
            case '亥':
                firstMonthStar = 2; // 二黑
                forward = true;     // 顺飞
                break;

            case '辰':
            case '戌':
            case '丑':
            case '未':
                firstMonthStar = 5; // 五黄
                forward = false;    // 逆飞
                break;

            default:
                throw new Exception("年份地支不在规则范围内");
        }

        // 获取农历月份（处理闰月）
        ChineseLunisolarCalendar chineseCalendar = new ChineseLunisolarCalendar();
        int lunarYear = chineseCalendar.GetYear(date);
        int lunarMonthRaw = chineseCalendar.GetMonth(date);
        int leapMonth = chineseCalendar.GetLeapMonth(lunarYear); // 0表示无闰月

        int lunarMonth;
        if (leapMonth > 0)
        {
            if (lunarMonthRaw == leapMonth)
                lunarMonth = leapMonth - 1; // 闰月按前月算
            else if (lunarMonthRaw > leapMonth)
                lunarMonth = lunarMonthRaw - 1;
            else
                lunarMonth = lunarMonthRaw;
        }
        else
        {
            lunarMonth = lunarMonthRaw;
        }

        // 计算当月中宫飞星
        int offset = lunarMonth - 1;
        int monthStar = forward
            ? ((firstMonthStar - 1 + offset) % 9) + 1
            : ((firstMonthStar - 1 - offset + 90) % 9) + 1; // 确保正数

        // 对应九宫Label位置：{4,8,5,6,1,7,2,3,0}（左东右西，上南下北）
        int[] uiOrder = { 4, 8, 5, 6, 1, 7, 2, 3, 0 };
        int[] stars = new int[9];

        // 依次顺飞/逆飞生成每宫飞星
        for (int i = 0; i < 9; i++)
        {
            int starNo = ((monthStar - 1 + (forward ? i : -i) + 9) % 9) + 1;
            stars[uiOrder[i]] = starNo;
        }

        return stars;
    }

    /// <summary>
    /// 日-九宫飞星序号（1-9）和飞行方向（true=顺飞，false=逆飞）
    /// </summary>
    public static int[] GetDayStar(DateTime date)
    {
        var solarStartStars = new Dictionary<string, (int startStar, bool forward)>
    {
        { "冬至", (1, true) }, // 一白顺飞
        { "雨水", (7, true) },
        { "谷雨", (4, true) },
        { "夏至", (9, false) }, // 九紫逆飞
        { "处暑", (3, false) },
        { "霜降", (6, false) }
    };

        string[] relevantJieQi = { "冬至", "雨水", "谷雨", "夏至", "处暑", "霜降" };

        DateTime FindFirstJiaZiAfter(DateTime startInclusive, int maxDays = 120)
        {
            DateTime d = startInclusive.Date;
            for (int i = 0; i <= maxDays; i++)
            {
                var gz = GetChineseCalendar(d).GanZhiDay;
                if (!string.IsNullOrEmpty(gz) && gz.Contains("甲子"))
                    return d;
                d = d.AddDays(1);
            }
            throw new Exception($"在 {startInclusive:yyyy-MM-dd} 后 {maxDays} 天内未找到甲子日。");
        }

        // 收集三年节气
        var jieqiList = new List<(string name, DateTime dt)>();
        for (int y = date.Year - 1; y <= date.Year + 1; y++)
        {
            string[] allJieQi = Get24JieQiYear(y);
            for (int i = 0; i < allJieQi.Length; i += 2)
            {
                string name = allJieQi[i];
                DateTime dt = Convert.ToDateTime(allJieQi[i + 1]);
                if (relevantJieQi.Contains(name))
                    jieqiList.Add((name, dt));
            }
        }

        // 计算每个节气后的第一个甲子日
        var jieqiWithFirstJiaZi = jieqiList.Select(item =>
            (item.name, item.dt, firstJiaZi: FindFirstJiaZiAfter(item.dt, 200))
        ).ToList();

        // 找到当前日期对应节气周期
        var chosen = jieqiWithFirstJiaZi
            .Where(x => x.firstJiaZi.Date <= date.Date)
            .OrderByDescending(x => x.firstJiaZi)
            .FirstOrDefault();

        if (chosen == default)
            throw new Exception("未找到适用节气。");

        var (startStar, forward) = solarStartStars[chosen.name];
        int offsetDays = (date.Date - chosen.firstJiaZi.Date).Days;
        int step = offsetDays % 9;

        int dayStar = forward
            ? ((startStar - 1 + step) % 9) + 1
            : ((startStar - 1 - step + 9 * 10) % 9) + 1; // 保证正数

        // ✅ 九宫Label顺序：左东右西，上南下北
        int[] uiOrder = { 4, 8, 5, 6, 1, 7, 2, 3, 0 };
        int[] stars = new int[9];

        // 根据顺逆飞填入九宫对应飞星
        for (int i = 0; i < 9; i++)
        {
            int starNo = ((dayStar - 1 + (forward ? i : -i) + 9) % 9) + 1;
            stars[uiOrder[i]] = starNo;
        }

        return stars;
    }

    /// <summary>
    /// 时-九宫飞星
    /// </summary>
    public static int[] GetHourStar(DateTime date)
    {
        // 取最近冬至或夏至
        string[] allJieQi = Get24JieQiYear(date.Year);
        List<(string name, DateTime dt)> jieqiList = new List<(string, DateTime)>();
        for (int i = 0; i < allJieQi.Length; i += 2)
            jieqiList.Add((allJieQi[i], Convert.ToDateTime(allJieQi[i + 1])));

        var filtered = jieqiList
            .Where(x => x.name == "冬至" || x.name == "夏至")
            .OrderBy(x => x.dt)
            .ToList();

        var lastSolar = filtered[0];
        foreach (var jq in filtered)
        {
            if (jq.dt <= date) lastSolar = jq;
            else break;
        }

        bool forward = lastSolar.name == "冬至"; // 冬至后顺飞，夏至后逆飞

        // 当天日地支
        string dayGZ = GetChineseCalendar(date).GanZhiDay;
        char dayDZ = dayGZ.Length >= 2 ? dayGZ[1] : '子';

        // 子时起始飞星
        int startHourStar = 1;
        if (forward)
        {
            if ("子午卯酉".Contains(dayDZ)) startHourStar = 1;
            else if ("辰戌丑未".Contains(dayDZ)) startHourStar = 4;
            else if ("寅申巳亥".Contains(dayDZ)) startHourStar = 7;
        }
        else
        {
            if ("子午卯酉".Contains(dayDZ)) startHourStar = 9;
            else if ("辰戌丑未".Contains(dayDZ)) startHourStar = 6;
            else if ("寅申巳亥".Contains(dayDZ)) startHourStar = 3;
        }

        // 生成全天12时辰飞星
        int[] hourStars = new int[12];
        for (int hourIndex = 0; hourIndex < 12; hourIndex++)
        {
            if (forward)
                hourStars[hourIndex] = (startHourStar + hourIndex - 1) % 9 + 1;
            else
                hourStars[hourIndex] = ((startHourStar - 1 - hourIndex + 9) % 9) + 1;
        }

        // 当前时辰索引（子时 = 0）
        int currentHour = date.Hour;
        int hourIndexCur = ((currentHour + 1) / 2) % 12;
        if (currentHour == 23) hourIndexCur = 0;

        int hourStar = hourStars[hourIndexCur]; // 当前时辰飞星

        // ✅ 生成九宫数组（UI 顺序）
        int[] uiOrder = { 4, 8, 5, 6, 1, 7, 2, 3, 0 };
        int[] stars = new int[9];
        for (int i = 0; i < 9; i++)
        {
            stars[uiOrder[i]] = ((hourStar - 1 + (forward ? i : -i) + 9) % 9) + 1;
        }

        return stars;
    }

    /// <summary>
    /// 九星吉凶
    /// </summary>
    /// <param name="stars"></param>
    /// <param name="year"></param>
    /// <returns></returns>
    public static List<NineStarInfo> GetNineStarLuck(int[] stars, int year)
    {
        List<NineStarInfo> result = new List<NineStarInfo>();

        var starList = LangManager.GetArray("NineStars");
        var starJx = LangManager.Get2DArray("FlyingStarLuck");

        int yearIndex = GetYearIndex(year);
        if (yearIndex < 0 || yearIndex > 8) yearIndex = 0;

        Dictionary<int, int[]> deLingMap = new Dictionary<int, int[]>
    {
        { 1, new int[] { 8, 9, 1 } },
        { 2, new int[] { 9, 1, 2 } },
        { 3, new int[] { 1, 2, 3 } },
        { 4, new int[] { 2, 3, 4 } },
        { 5, new int[] { 3, 4, 5 } },
        { 6, new int[] { 4, 5, 6 } },
        { 7, new int[] { 5, 6, 7 } },
        { 8, new int[] { 6, 7, 8 } },
        { 9, new int[] { 7, 8, 9 } }
    };

        Dictionary<int, Color> starColors = new Dictionary<int, Color>
    {
        { 1, new Color(0.4f, 0.8f, 1f, 0.35f) },
        { 2, new Color(0.3f, 0.3f, 0.3f, 0.35f) },
        { 3, new Color(0.0f, 0.6f, 0.4f, 0.35f) },
        { 4, new Color(0.0f, 0.4f, 0.2f, 0.35f) },
        { 5, new Color(0.85f, 0.65f, 0.2f, 0.35f) },
        { 6, new Color(0.8f, 0.8f, 0.85f, 0.35f) },
        { 7, new Color(0.9f, 0.45f, 0.2f, 0.35f) },
        { 8, new Color(1f, 0.75f, 0.4f, 0.35f) },
        { 9, new Color(0.6f, 0.3f, 0.7f, 0.35f) }
    };

        foreach (int starNo in stars)
        {
            var starItem = starList[starNo - 1] as Dictionary<string, object>;
            string starName = starItem["Name"].ToString();
            string luck = starJx[starNo - 1][yearIndex];
            string deLing = "";
            string shiLing = "";

            if (deLingMap.TryGetValue(starNo, out var arr) && arr.Contains(yearIndex))
                deLing = "得令";

            Color color = starColors.ContainsKey(starNo) ? starColors[starNo] : Color.white;

            result.Add(new NineStarInfo
            {
                StarNo = starNo,
                StarName = starName,
                Luck = luck,
                DeLing = deLing,
                ShiLing = shiLing,
                BgColor = color
            });
        }

        return result;
    }

    // 运周期计算
    static int GetYearIndex(int year)
    {
        int baseYear = 1864;       // 上元一运起始年份
        int diff = year - baseYear;
        int yunIndex = (diff / 20) % 9;  // 每运20年
        return yunIndex;
    }

    /// <summary>
    /// 根据日干支判断十二时辰吉凶（主要依据天干相合、相克、地支三合、六合、相冲）
    /// </summary>
    /// <returns>返回一个Dictionary<string, string>，key为时辰（子~亥），value为“吉”或“凶”或“平”</returns>
    public static List<(string Name, string Luck)> GetShiChenLuck(DateTime date)
    {
        var result = new List<(string, string)>();
        string ganZhi = GetChineseCalendar(date).GanZhiDay;
        string dayGan = ganZhi.Substring(0, 1);
        string dayZhi = ganZhi.Substring(1, 1);
        string dayFive = GetFive(dayZhi);

        var sheng = new Dictionary<string, string>
        {
            ["木"] = "火",
            ["火"] = "土",
            ["土"] = "金",
            ["金"] = "水",
            ["水"] = "木"
        };

        foreach (var hourZhi in DiZhi)
        {
            string luck = "平";
            string hourFive = GetFive(hourZhi);

            if (GetSanHe(ganZhi).Contains(hourZhi)) luck = "吉";
            if (GetChong(ganZhi).Contains(hourZhi)) luck = "凶";
            if (GetHai(ganZhi).Contains(hourZhi)) luck = "凶";
            if (GetXing(ganZhi).Contains(hourZhi)) luck = "凶";
            if (GetRiLu(date, ganZhi).Contains(hourZhi)) luck = "禄";
            if (sheng[dayFive] == hourFive) luck = "进";// 主“向外有益”，凡事进展、施为顺遂
            if (sheng[hourFive] == dayFive) luck = "生";// 主“得人相助”，凡事得助、得益

            result.Add((hourZhi, luck));
        }
        return result;
    }

    /// <summary>
    /// 杨公十三忌日
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string GetYangGong13(DateTime date)
    {
        string lunarDate = GetChineseCalendar(date).LunarDate;
        string[] ygDict = { "正月十三", "二月十一", "三月初九", "四月初七", "五月初五", "六月初三", "七月初一", "七月廿九", "八月廿七", "九月廿五", "十月廿三", "冬月廿一", "腊月十九" };

        if (ygDict.Contains(lunarDate))
            return "杨公十三日，诸事皆忌";

        return "";
    }

    /// <summary>
    /// 获取整年24节气，这里用于九宫飞星
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    public static string[] Get24JieQiYear(int year)
    {
        List<string> allJieQi = new List<string>();
        for (int month = 1; month <= 12; month++)
        {
            string[] jq = Get24JieQi(year, month);
            if (jq != null && jq.Length > 0)
                allJieQi.AddRange(jq);
        }
        return allJieQi.ToArray();
    }

    /// <summary>
    /// 喜神贵神财神鹤神胎神位
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static (string XiShen, string GuiShen, string CaiShen, string HeShen, string TaiShen) GetFiveShenDirections(DateTime date)
    {
        var result = GetChineseCalendar(date);
        string ganZhiDay = result.GanZhiDay;

        // --- 提取天干、地支 ---
        string gan = ganZhiDay.Substring(0, 1);
        string zhi = ganZhiDay.Substring(1, 1);

        // 1️⃣ 喜神方位（依日干）
        string xiShen = gan switch
        {
            "甲" or "己" => "东北（艮）",
            "乙" or "庚" => "西南（坤）",
            "丙" or "辛" => "正西（兑）",
            "丁" or "壬" => "西北（乾）",
            "戊" or "癸" => "东南（巽）",
            _ => "未知"
        };

        // 2️⃣ 贵神方位（天乙贵人，依日干 + 阴阳）
        string[] yangGan = { "甲", "丙", "戊", "庚", "壬" };
        bool isYangGan = yangGan.Contains(gan);
        string guiShen = "未知";

        switch (gan)
        {
            case "甲":
            case "戊":
            case "庚":
                guiShen = isYangGan ? "东北（丑）" : "西南（未）";
                break;
            case "乙":
            case "己":
                guiShen = isYangGan ? "正北（子）" : "西南偏西（申）";
                break;
            case "丙":
            case "丁":
                guiShen = isYangGan ? "西北偏北（亥）" : "正西（酉）";
                break;
            case "辛":
                guiShen = isYangGan ? "正北（子）" : "西南偏西（申）";
                break;
            case "壬":
            case "癸":
                guiShen = isYangGan ? "东南（巳）" : "正东（卯）";
                break;
        }

        // 3️⃣ 财神方位（依日干）
        string caiShen = gan switch
        {
            "甲" or "乙" => "东方",
            "丙" or "丁" => "南方",
            "戊" or "己" => "中央",
            "庚" or "辛" => "西方",
            "壬" or "癸" => "北方",
            _ => "未知"
        };

        // 4️⃣ 鹤神方位（依日支）
        string heShen = zhi switch
        {
            "子" or "午" => "南方",
            "丑" or "未" => "东方",
            "寅" or "申" => "东北",
            "卯" or "酉" => "北方",
            "辰" or "戌" => "西方",
            "巳" or "亥" => "西南",
            _ => "未知"
        };

        // 5️⃣ 胎神方位（简化算法：依月支，假设 result.GanZhiMonth 可取第二位为月支）
        string zhiMonth = result.GanZhiMonth.Substring(1, 1);
        string taiShen = zhiMonth switch
        {
            "寅" or "申" => "房中、堂中",
            "卯" or "酉" => "门内、户内",
            "辰" or "戌" => "厨灶、仓库",
            "巳" or "亥" => "床下、房床",
            "子" => "仓库、厨灶",
            "丑" or "未" => "房内、厕内",
            _ => "未知"
        };

        return (xiShen, guiShen, caiShen, heShen, taiShen);
    }

    /// <summary>
    /// 几龙治水
    /// </summary>
    /// <returns></returns>
    public static string GetLongNiu(DateTime date)
    {
        ChineseLunisolarCalendar chineseCalendar = new ChineseLunisolarCalendar();

        // 年、月（原始）、日
        int lunarYear = chineseCalendar.GetYear(date);
        int lunarMonthRaw = chineseCalendar.GetMonth(date); // 1..12 或 1..13（若有闰月则可能为闰月索引）
        int lunarDay = chineseCalendar.GetDayOfMonth(date);

        // 判断是否为闰月（仅供信息使用）
        int leapMonth = chineseCalendar.GetLeapMonth(lunarYear); // 0 表示无闰月
        bool isLeapMonth = (leapMonth > 0 && lunarMonthRaw == leapMonth);

        // 计算距正月初一的天数 offset（用 lunarMonthRaw 遍历每一"原始"月份，包含闰月）
        int offset = 0;
        for (int m = 1; m < lunarMonthRaw; m++)
        {
            offset += chineseCalendar.GetDaysInMonth(lunarYear, m);
        }
        offset += (lunarDay - 1); // 到当天已过的天数

        // 当日干支（你已有此方法）
        string dayGZ = GetGanZhiDay(date);

        // 用字典 keys 的添加顺序当作 60 甲子序号（你已保证字典按甲子顺序定义）
        var keys = YearWeightTable.Keys.ToList();
        int todayIndex = keys.IndexOf(dayGZ);
        if (todayIndex < 0) throw new Exception($"无法在 YearWeightTable 中找到干支：{dayGZ}");

        // 推算正月初一的甲子表索引
        int firstDayIndex = (todayIndex - (offset % 60) + 60) % 60;
        // 寻找辰、丑、丙对应的日子（1..30）
        int dayLong = -1, dayNiu = -1, dayBing = -1;
        for (int i = 1; i <= 30; i++)
        {
            string gz = keys[(firstDayIndex + i - 1) % 60];
            if (dayLong == -1 && gz.EndsWith("辰")) dayLong = i;
            if (dayNiu == -1 && gz.EndsWith("丑")) dayNiu = i;
            if (dayBing == -1 && gz.StartsWith("丙")) dayBing = i;
            if (dayLong != -1 && dayNiu != -1 && dayBing != -1) break;
        }

        // 中文数字映射（1..最多12）
        string[] CN = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "十二" };

        string res = "";
        if (dayLong > 0) res += $"{CN[dayLong - 1]}龙治水";
        if (dayNiu > 0) res += $"{CN[dayNiu - 1]}牛耕田";
        if (dayBing > 0) res += $"{CN[dayBing - 1]}人分饼";

        return res;
    }

    /// <summary>
    /// 岁煞 三煞 日煞；五黄煞为当日飞星5所在，以后处理
    /// </summary>
    /// <param name="yearZhi"></param>
    /// <param name="dayZhi"></param>
    /// <returns></returns>
    public static (string SuiSha, string JieSha, string ZaiSha) GetShaFang(string yearZhi, string dayZhi)
    {
        yearZhi = yearZhi.Substring(1, 1);
        dayZhi = dayZhi.Substring(1, 1);

        string suiSha = dayZhi switch
        {
            "申" or "子" or "辰" => "南方",
            "亥" or "卯" or "未" => "西方",
            "寅" or "午" or "戌" => "北方",
            "巳" or "酉" or "丑" => "东方",
            _ => "未知"
        };

        string jieSha = yearZhi switch
        {
            "申" or "子" or "辰" => "亥",
            "亥" or "卯" or "未" => "丑",
            "寅" or "午" or "戌" => "申",
            "巳" or "酉" or "丑" => "寅",
            _ => "未知"
        };

        string zaiSha = yearZhi switch
        {
            "申" or "子" or "辰" => "午",
            "亥" or "卯" or "未" => "子",
            "寅" or "午" or "戌" => "酉",
            "巳" or "酉" or "丑" => "卯",
            _ => "未知"
        };

        return (suiSha, jieSha, zaiSha);
    }

    /// <summary>
    /// 空亡
    /// </summary>
    /// <param name="ganZhi">干支</param>
    /// <returns></returns>
    public static string GetKongWang(string ganZhi)
    {
        string[] kongGroups = new string[]
    {
        "戌亥", // 甲子~癸酉
        "申酉", // 甲戌~癸未
        "午未", // 甲申~癸巳
        "辰巳", // 甲午~癸卯
        "寅卯", // 甲辰~癸丑
        "子丑"  // 甲寅~癸亥
    };

        // YearWeightTable.Keys 顺序即六十甲子顺序
        var keys = YearWeightTable.Keys.ToList();
        int index = keys.IndexOf(ganZhi);
        if (index == -1) return "";

        // 每10个甲子为一组空亡
        int groupIndex = index / 10;
        return kongGroups[groupIndex];
    }

    /// <summary>
    /// 日禄
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string GetRiLu(DateTime date, string hourGan)
    {
        var lunar = GetChineseCalendar(date);
        string dayGan = lunar.GanZhiDay.Substring(0, 1); // 天干
        string dayZhi = lunar.GanZhiDay.Substring(1, 1); // 地支

        // 十干禄地表 + 互禄干
        var luTable = new Dictionary<string, (string LuZhi, string[] HuLu)>
    {
        { "甲", ("寅", new[] {"己"}) },
        { "乙", ("卯", new[] {"庚"}) },
        { "丙", ("巳", new[] {"戊"}) },
        { "丁", ("午", new[] {"己"}) },
        { "戊", ("巳", new[] {"丙"}) },
        { "己", ("午", new[] {"丁", "甲"}) },
        { "庚", ("申", new[] {"乙"}) },
        { "辛", ("酉", new[] {"壬"}) },
        { "壬", ("亥", new[] {"辛"}) },
        { "癸", ("子", Array.Empty<string>()) }
    };

        if (!luTable.ContainsKey(dayGan))
            return "未知";

        var (luZhi, huLu) = luTable[dayGan];
        string result = $"{dayGan}日禄在{luZhi}";

        // 检查互禄（如果命局中存在互禄干，可扩展）
        // 这里我们仅示例从月干、时干判断
        string monthGan = lunar.GanZhiMonth.Substring(0, 1);

        List<string> others = new() { monthGan, hourGan };

        foreach (var g in others)
        {
            if (huLu.Contains(g))
            {
                result += $"，与{g}命互禄";
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// 获取28宿信息（宿名 + 吉凶 + 宜忌）
    /// </summary>
    /// <param name="date">目标日期</param>
    /// <returns>(宿名, 吉凶, 宜忌)</returns>
    public static (string Name, string Jiang, string Luck, string Yi, string Ji, string Sheng) Get28Xiu(DateTime date)
    {
        // 基准日选择2023-1-12，对应28宿第一个：角木蛟
        DateTime baseDate = new DateTime(2023, 1, 12);

        int daysDiff = (int)(date.Date - baseDate.Date).TotalDays;

        int index = ((daysDiff % 28) + 28) % 28;

        var starArray = LangManager.GetArray("StarInfo");

        if (starArray == null || index < 0 || index >= starArray.Count)
        {
            Debug.LogError($"Invalid StarInfo index: {index}");
            return ("Unknown", "", "", "", "", "");
        }

        var star = starArray[index];

        string name = star.ContainsKey("Name") ? star["Name"].ToString() : "Unknown";
        string jiang = star.ContainsKey("Jiang") ? star["Jiang"].ToString() : "";
        string luck = star.ContainsKey("Luck") ? star["Luck"].ToString() : "";
        string yi = star.ContainsKey("Yi") ? star["Yi"].ToString() : "";
        string ji = star.ContainsKey("Ji") ? star["Ji"].ToString() : "";
        string sheng = star.ContainsKey("Sheng") ? star["Sheng"].ToString() : "";

        return (name, jiang, luck, yi, ji, sheng);
    }

    /// <summary>
    /// 获取三合信息
    /// </summary>
    public static string GetSanHe(string gz)
    {
        string dz = gz.Substring(1, 1);
        foreach (var group in SanHeGroups)
        {
            if (group.Contains(dz))
            {
                return string.Join("｜", group);
            }
        }
        return "";
    }

    /// <summary>
    /// 获取六合信息
    /// </summary>
    public static string GetLiuHe(string gz)
    {
        string dz = gz.Substring(1, 1);
        foreach (var group in LiuHeGroups)
        {
            if (group.Contains(dz))
            {
                return string.Join("｜", group);
            }
        }
        return "";
    }

    /// <summary>
    /// 获取相冲
    /// </summary>
    public static string GetChong(string gz)
    {
        // 提取日支
        string dz = gz.Substring(1, 1);

        // 如果不存在冲关系则返回空
        if (!ChongPairs.ContainsKey(dz))
            return "";

        string chongZhi = ChongPairs[dz];

        // 找出两者在地支表中的索引
        int idx1 = Array.IndexOf(DiZhi, dz);
        int idx2 = Array.IndexOf(DiZhi, chongZhi);

        // 获取对应生肖名
        string shengxiao1 = LangManager.GetArrayValue("ShengXiao", idx1);
        string shengxiao2 = LangManager.GetArrayValue("ShengXiao", idx2);

        // 返回格式：「兔日冲鸡」
        return $"{shengxiao1}日冲{shengxiao2}";
    }

    /// <summary>
    /// 获取相害
    /// </summary>
    public static string GetHai(string gz)
    {
        string dz = gz.Substring(1, 1);
        return HaiPairs.ContainsKey(dz) ? $"害 {HaiPairs[dz]}" : "";
    }

    /// <summary>
    /// 获取相刑
    /// </summary>
    public static string GetXing(string gz)
    {
        string dz = gz.Substring(1, 1);
        string xing = "";
        // 寅巳申互刑
        if (dz == "寅") xing = "巳";
        if (dz == "巳") xing = "申";
        if (dz == "申") xing = "寅";
        if (dz == "子") xing = "卯";
        if (dz == "卯") xing = "子";

        // 丑未戌互刑
        if (dz == "丑") xing = "未戌";
        if (dz == "未") xing = "丑戌";
        if (dz == "戌") xing = "丑未";

        // 自刑
        if (dz == "午" || dz == "亥" || dz == "酉" || dz == "辰")
        {
            return $"{dz}自刑";
        }
        else
        {
            return $"邢{xing}";
        }
    }

    /// <summary>
    /// 判断某天是否为日本节假日
    /// </summary>
    public static List<(int Day, string Name, bool Jia)> GetJapanHolidays(int year, int month)
    {
        var holidays = new List<(int, string, bool)>();

        // 固定节日
        var fixedHolidays = new Dictionary<(int Month, int Day), string>
    {
        {(1, 1), "元日"},
        {(2, 11), "建国記念の日"},
        {(2, 23), "天皇誕生日"}, // 令和天皇
        {(4, 29), "昭和の日"},
        {(5, 3), "憲法記念日"},
        {(5, 4), "みどりの日"},
        {(5, 5), "こどもの日"},
        {(8, 11), "山の日"},
        {(11, 3), "文化の日"},
        {(11, 23), "勤労感謝の日"},
    };

        foreach (var kv in fixedHolidays)
        {
            if (kv.Key.Month == month)
                holidays.Add((kv.Key.Day, kv.Value, true));
        }

        // 浮动节日
        void AddFloating(int m, int n, DayOfWeek dayOfWeek, string name)
        {
            var dt = GetNthWeekdayOfMonth(year, m, dayOfWeek, n);
            if (dt.Month == month)
                holidays.Add((dt.Day, name, true));
        }

        AddFloating(1, 2, DayOfWeek.Monday, "成人の日");
        AddFloating(7, 3, DayOfWeek.Monday, "海の日");
        AddFloating(9, 3, DayOfWeek.Monday, "敬老の日");
        AddFloating(10, 2, DayOfWeek.Monday, "スポーツの日");

        // 春分・秋分
        string[] jieQi = Get24JieQi(year, month);
        if (jieQi != null && jieQi.Length >= 4)
        {
            DateTime chunfen = Convert.ToDateTime(jieQi[1]).Date;
            DateTime qiufen = Convert.ToDateTime(jieQi[3]).Date;
            if (chunfen.Month == month)
                holidays.Add((chunfen.Day, "春分の日", true));
            if (qiufen.Month == month)
                holidays.Add((qiufen.Day, "秋分の日", true));
        }

        // 振替休日（补假）
        foreach (var h in holidays.ToList())
        {
            var date = new DateTime(year, month, h.Item1);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                var next = date.AddDays(1);
                // 如果次日非节日
                if (!holidays.Any(x => x.Item1 == next.Day))
                {
                    if (next.Month == month)
                        holidays.Add((next.Day, "振替休日", true));
                }
            }
        }

        holidays.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        return holidays;
    }

    /// <summary>
    /// 中国节假日
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static List<(int Day, string Name, bool Jia)> GetChinaHolidays(int year, int month)
    {
        var holidays = new List<(int Day, string Name, bool Jia)>();

        // 获取当月天数
        int daysInMonth = DateTime.DaysInMonth(year, month);

        for (int d = 1; d <= daysInMonth; d++)
        {
            DateTime date = new DateTime(year, month, d);
            var cn = GetChineseCalendar(date);
            bool jia = false;
            var names = new List<string>();

            // ===== 一、公历节日 =====
            switch (month)
            {
                case 1:
                    if (d == 1) { names.Add("元旦节"); jia = true; }
                    break;
                case 2:
                    if (d == 14) names.Add("情人节");
                    break;
                case 3:
                    if (d == 8) names.Add("妇女节");
                    break;
                case 4:
                    if (d == 4) { names.Add("清明节"); jia = true; }
                    if (d > 4 && d <= 6) { names.Add("清明节假"); jia = true; }
                    break;
                case 5:
                    if (d == 1) { names.Add("劳动节"); jia = true; }
                    break;
                case 6:
                    if (d == 1) names.Add("儿童节");
                    break;
                case 9:
                    if (d == 10) names.Add("教师节");
                    break;
                case 10:
                    if (d >= 1 && d <= 7) { names.Add("国庆节"); jia = true; }
                    break;
            }

            // ===== 二、农历节日 =====
            string lunar = cn.LunarDate;
            int lm = cn.LunarMonth;
            int ld = cn.LunarDay;
            bool run = !string.IsNullOrEmpty(cn.RunMonth);

            if (!run)
            {
                switch (lm)
                {
                    case 1:
                        if (ld == 1) { names.Add("春节"); jia = true; }
                        if (ld > 1 && ld <= 3) { names.Add("春节假"); jia = true; }
                        if (ld == 15) names.Add("元宵节");
                        break;
                    case 2:
                        if (ld == 2) names.Add("龙抬头");
                        if (ld == 15) names.Add("二月半（鬼节）");
                        break;
                    case 3:
                        if (ld == 3) names.Add("上巳节");
                        break;
                    case 5:
                        if (ld == 5) { names.Add("端午节"); jia = true; }
                        break;
                    case 7:
                        if (ld == 7) names.Add("七夕节");
                        if (ld == 15) names.Add("中元节");
                        break;
                    case 8:
                        if (ld == 15) { names.Add("中秋节"); jia = true; }
                        break;
                    case 9:
                        if (ld == 9) names.Add("重阳节");
                        break;
                    case 12:
                        if (ld == 8) names.Add("腊八节");
                        if (ld >= 23 && ld <= 24) names.Add("小年");

                        // ===== 除夕判断 =====
                        ChineseLunisolarCalendar chineseCalendar = new ChineseLunisolarCalendar();
                        int daysInLunar12 = chineseCalendar.GetDaysInMonth(year, lm); // 获取农历12月天数
                        if (ld == daysInLunar12)
                            names.Add("除夕");
                        break;
                }
            }

            if (names.Count > 0)
            {
                foreach (var n in names)
                {
                    holidays.Add((d, n, jia));
                }
            }
        }

        return holidays;
    }

    /// <summary>
    /// 台湾节假日
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static List<(int Day, string Name, bool Jia)> GetTaiwanHolidays(int year, int month)
    {
        var result = new List<(int, string, bool)>();

        // 本月天数
        int days = DateTime.DaysInMonth(year, month);

        for (int d = 1; d <= days; d++)
        {
            DateTime date = new DateTime(year, month, d);
            var list = new List<string>();
            bool jia = false;

            int m = date.Month;
            int day = date.Day;

            // ===== 公历法定假日 =====
            if (m == 1 && day == 1) { list.Add("元旦"); jia = true; }
            if (m == 2 && day == 28) { list.Add("和平纪念日"); jia = true; }
            if (m == 4 && day == 4) { list.Add("儿童节"); jia = true; }
            if (m == 5 && day == 1) { list.Add("劳动节"); jia = true; }
            if (m == 10 && day == 10) { list.Add("国庆日（双十节）"); jia = true; }

            // ===== 农历节日 =====
            var cn = GetChineseCalendar(date); // 调用农历信息函数
            int lm = cn.LunarMonth;
            int ld = cn.LunarDay;
            bool run = !string.IsNullOrEmpty(cn.RunMonth);

            if (!run)
            {
                switch (lm)
                {
                    case 1:
                        if (ld == 1) { list.Add("春节"); jia = true; }
                        if (ld == 15) list.Add("元宵节");
                        break;

                    case 5:
                        if (ld == 5) { list.Add("端午节"); jia = true; }
                        break;

                    case 7:
                        if (ld == 7) list.Add("七夕节");
                        if (ld == 15) list.Add("中元节（鬼节）");
                        break;

                    case 8:
                        if (ld == 15) { list.Add("中秋节"); jia = true; }
                        break;

                    case 9:
                        if (ld == 9) list.Add("重阳节");
                        break;

                    case 12:
                        if (ld == 8) list.Add("腊八节");
                        break;
                }

                // ===== 除夕判断 =====
                var nextDay = date.AddDays(1);
                var nextCn = GetChineseCalendar(nextDay);
                if (lm == 12 && nextCn.LunarMonth == 1 && nextCn.LunarDay == 1)
                {
                    list.Add("除夕");
                    jia = true;
                }
            }

            if (list.Count > 0)
                result.Add((d, string.Join(",", list), jia));
        }

        return result;
    }

    /// <summary>
    /// 英美加澳等通用假日
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static List<(int Day, string Name,bool Jia)> GetEnglishHolidays(int year, int month)
    {
        var result = new List<(int, string,bool)>();
        int days = DateTime.DaysInMonth(year, month);

        for (int d = 1; d <= days; d++)
        {
            DateTime date = new DateTime(year, month, d);
            var list = new List<string>();
            int m = date.Month;
            int day = date.Day;
            DayOfWeek wd = date.DayOfWeek;

            // ===== 固定国际节日 =====
            if (m == 1 && day == 1) list.Add("New Year's Day");
            if (m == 2 && day == 14) list.Add("Valentine's Day");
            if (m == 3 && day == 17) list.Add("St. Patrick's Day");
            if (m == 4 && day == 1) list.Add("April Fool's Day");
            if (m == 10 && day == 31) list.Add("Halloween");
            if (m == 12 && day == 25) list.Add("Christmas Day");
            if (m == 12 && day == 26) list.Add("Boxing Day");

            // ===== 美国 (USA) =====
            if (m == 7 && day == 4) list.Add("Independence Day (USA)");
            if (m == 11 && wd == DayOfWeek.Thursday)
            {
                int thursdayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 11, i).DayOfWeek == DayOfWeek.Thursday);
                if (thursdayCount == 4) list.Add("Thanksgiving (USA)");
            }

            // ===== 英国 (UK) =====
            if (m == 5 && wd == DayOfWeek.Monday)
            {
                int mondayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 5, i).DayOfWeek == DayOfWeek.Monday);
                if (mondayCount == 1) list.Add("Early May Bank Holiday (UK)");
                if (mondayCount == 4) list.Add("Spring Bank Holiday (UK)");
            }
            if (m == 8 && wd == DayOfWeek.Monday)
            {
                int mondayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 8, i).DayOfWeek == DayOfWeek.Monday);
                if (mondayCount == 4) list.Add("Summer Bank Holiday (UK)");
            }

            // ===== 加拿大 (Canada) =====
            if (m == 7 && day == 1) list.Add("Canada Day");
            if (m == 9 && wd == DayOfWeek.Monday)
            {
                int mondayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 9, i).DayOfWeek == DayOfWeek.Monday);
                if (mondayCount == 1) list.Add("Labour Day (Canada)");
            }
            if (m == 10 && wd == DayOfWeek.Monday)
            {
                int mondayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 10, i).DayOfWeek == DayOfWeek.Monday);
                if (mondayCount == 2) list.Add("Thanksgiving (Canada)");
            }

            // ===== 澳大利亚 (Australia) =====
            if (m == 1 && day == 26) list.Add("Australia Day");
            if (m == 4 && day == 25) list.Add("ANZAC Day");
            if (m == 6 && wd == DayOfWeek.Monday)
            {
                int mondayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 6, i).DayOfWeek == DayOfWeek.Monday);
                if (mondayCount == 2) list.Add("King's Birthday (Australia)");
            }

            // ===== 浮动国际节日 =====
            // Mother’s Day – 2nd Sunday of May
            if (m == 5 && wd == DayOfWeek.Sunday)
            {
                int sundayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 5, i).DayOfWeek == DayOfWeek.Sunday);
                if (sundayCount == 2) list.Add("Mother's Day");
            }

            // Father’s Day – 3rd Sunday of June (except AU: 1st Sunday of Sep)
            if (m == 6 && wd == DayOfWeek.Sunday)
            {
                int sundayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 6, i).DayOfWeek == DayOfWeek.Sunday);
                if (sundayCount == 3) list.Add("Father's Day (USA/UK/CA)");
            }
            if (m == 9 && wd == DayOfWeek.Sunday)
            {
                int sundayCount = Enumerable.Range(1, day)
                    .Count(i => new DateTime(year, 9, i).DayOfWeek == DayOfWeek.Sunday);
                if (sundayCount == 1) list.Add("Father's Day (Australia)");
            }

            // Easter Sunday
            if (date == GetEasterSunday(year))
                list.Add("Easter Sunday");

            if (list.Count > 0)
                result.Add((day, string.Join(", ", list),true));
        }

        return result;
    }

    // ===== 复活节计算函数（高德算法）=====
    public static DateTime GetEasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }

    /// <summary>
    /// 计算五组吉数
    /// </summary>
    /// <param name="birth">生日 DateTime</param>
    /// <param name="calcDate">用于日序号、干支、塔罗等的计算日期</param>
    /// <returns>五组吉数 int[5]</returns>
    public static int[] Calculate(DateTime birth, DateTime calcDate)
    {
        System.Random rand = new System.Random();

        int[] result = new int[5];

        // 获取干支与六曜信息
        var lunar = GetChineseCalendar(calcDate);
        string gzYear = lunar.GanZhiYear;
        string gzMonth = lunar.GanZhiMonth;
        string gzDay = lunar.GanZhiDay;
        string gzTime = lunar.GanZhiTime;

        decimal yearWeight = YearWeightTable.ContainsKey(gzYear) ? YearWeightTable[gzYear] : 1m;
        int liuYaoFix = 0; // 六曜调整数
        var yaoInfo = Get6Yao(lunar.LunarDate);
        if (yaoInfo.JiXiong == "吉") liuYaoFix = 3;
        else if (yaoInfo.JiXiong == "凶") liuYaoFix = -2;

        // 星座
        var zodiac = GetZodiacSign(calcDate);
        int zodiacNum = zodiac.No;

        // 九宫飞星
        var hourStar = GetHourStar(calcDate);
        int nineStarIndex = hourStar[4];

        // -------------------------------
        // 1️⃣ 个人灵数（生日总和）
        int sum = birth.Year + birth.Month + birth.Day;
        int lifeNum = sum;
        while (lifeNum > 9)
        {
            int temp = 0;
            foreach (char c in lifeNum.ToString())
                temp += c - '0';
            lifeNum = temp;
        }
        // 加年份权重微调
        int num1 = lifeNum + (int)Math.Round(yearWeight) + rand.Next(0, 3);
        result[0] = num1 > 9 ? num1 % 9 + 1 : num1;

        // -------------------------------
        // 2️⃣ 九星循环数
        int dayOfYear = calcDate.DayOfYear;
        int lucky2 = (dayOfYear * 3 + nineStarIndex) % 100;
        lucky2 = lucky2 > 81 ? lucky2 - 50 : lucky2;
        lucky2 = lucky2 < 10 ? lucky2 + 10 : lucky2;
        result[1] = lucky2;

        // -------------------------------
        // 3️⃣ 干支六曜数
        int ganZhiIndex = Array.IndexOf(TianGan, gzDay[0].ToString()) * 12 +
                          Array.IndexOf(DiZhi, gzDay[1].ToString());
        int lucky3 = (ganZhiIndex * 7 + liuYaoFix) % 100;
        lucky3 = lucky3 > 81 ? lucky3 - 50 : lucky3;
        lucky3 = lucky3 < 10 ? lucky3 + 12 : lucky3;
        result[2] = lucky3;

        // -------------------------------
        // 4️⃣ 四源塔罗数
        int birthNum = result[0];
        int dayNum = result[1];
        int hourNum = (DateTime.Now.Hour % 24) + 1;
        int baseVal = birthNum + dayNum + hourNum + zodiacNum;
        int tarotNum = (baseVal + birthNum * hourNum + liuYaoFix) % 100;
        tarotNum = tarotNum > 81 ? tarotNum - 50 : tarotNum;
        tarotNum = tarotNum < 10 ? tarotNum + 10 : tarotNum;
        tarotNum = (tarotNum / 10) * 10 + rand.Next(1, 10);
        result[3] = tarotNum;

        // -------------------------------
        // 5️⃣ 蓍草卦象数
        int[] yao = new int[6];
        int[] ms = new int[6];
        for (int i = 0; i < 6; i++)
        {
            yao[i] = rand.Next(2) == 0 ? 1 : -1;
            ms[i] = DateTime.Now.Millisecond;
        }

        int yangSum = 0, yinSum = 0;
        for (int i = 0; i < 6; i++)
        {
            if (yao[i] == 1) yangSum += ms[i] % 10;
            else yinSum += ms[i] / 100;
        }

        int upperIndex = (yao[0] == 1 ? 4 : 0) + (yao[1] == 1 ? 2 : 0) + (yao[2] == 1 ? 1 : 0);
        int lowerIndex = (yao[3] == 1 ? 4 : 0) + (yao[4] == 1 ? 2 : 0) + (yao[5] == 1 ? 1 : 0);
        upperIndex = upperIndex == 0 ? 8 : upperIndex;
        lowerIndex = lowerIndex == 0 ? 8 : lowerIndex;

        int temp1 = 50 - (yangSum + yinSum);
        temp1 = temp1 <= 0 ? Math.Abs(temp1) : temp1;

        int val1 = (temp1 * upperIndex) / (lowerIndex + 1);
        int val2 = (temp1 * lowerIndex) / (upperIndex + 1);

        int lucky5a = val1 > 81 ? val1 - 50 : val1;
        lucky5a = lucky5a < 10 ? 10 + (lucky5a % 10) : lucky5a;
        int lucky5b = val2 > 81 ? val2 - 50 : val2;
        lucky5b = lucky5b < 10 ? 10 + (lucky5b % 10) : lucky5b;

        lucky5b = lucky5b % 10;
        lucky5b = lucky5b == 0 ? rand.Next(1, 10) : lucky5b;
        int lucky5 = (lucky5a / 10) * 10 + lucky5b;

        result[4] = lucky5;

        return result;
    }

    /// <summary>
    /// 计算某年某月第n个指定星期几，用于日本节日
    /// </summary>
    public static DateTime GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int nth)
    {
        DateTime firstDay = new DateTime(year, month, 1);
        int offset = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
        return firstDay.AddDays(offset + 7 * (nth - 1));
    }

}