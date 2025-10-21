using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Label = UnityEngine.UIElements.Label;

public static class ZiWeiComplete
{
    public class ZiWeiPalace
    {
        public string Name;        // 宫名
        public string Branch;      // 地支
        public string Stem;        // 天干
        public List<string> MainStars = new();  // 主星
        public List<string> SecondaryStars = new(); // 辅星
        public List<string> Gods = new(); // 神煞
        public bool IsFlowYear;
        public bool IsThreePower;
    }

    // 十二宫顺序：右下为寅，顺时针
    static readonly string[] PalaceNames = { "命宫", "兄弟", "夫妻", "子女", "财帛", "疾厄", "迁移", "交友", "官禄", "田宅", "福德", "父母" };
    static readonly string[] Branches = { "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥", "子", "丑" };
    static readonly string[] HeavenlyStems = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

    static readonly string[] MainStars = { "紫微", "天机", "太阳", "武曲", "天同", "廉贞", "天府", "太阴", "贪狼", "巨门", "天相", "天梁", "七杀", "破军" };
    static readonly string[] SecondaryStars = { "文昌", "文曲", "左辅", "右弼", "天魁", "天钺", "天马", "火星", "铃星" };
    static readonly string[] Gods = { "将星", "天喜", "太极", "红鸾", "天姚" };

    /// <summary>
    /// 核心入口：根据农历信息生成紫微排盘并填充UI
    /// </summary>
    /// <summary>
    /// 根据日期、时辰、流年生成完整紫微排盘数据（仅返回数据，不操作UI）
    /// </summary>
    public static List<ZiWeiPalace> FillZiWeiChart(DateTime date, int bronHour, int currentYear)
    {
        var lunar = CalendarData.GetChineseCalendar(date);

        int lunarYear = lunar.LunarYear;
        int lunarMonth = lunar.LunarMonth;
        int lunarDay = lunar.LunarDay;
        string ganZhiDay = lunar.GanZhiDay;

        var palaces = GetFullChart(lunarYear, lunarMonth, lunarDay, bronHour, currentYear, ganZhiDay);
        return palaces.ToList();
    }

    public static ZiWeiPalace[] GetFullChart(int lunarYear, int lunarMonth, int lunarDay, int hour, int currentYear, string ganZhiDay)
    {
        ZiWeiPalace[] palaces = new ZiWeiPalace[12];
        for (int i = 0; i < 12; i++)
            palaces[i] = new ZiWeiPalace { Name = PalaceNames[i], Branch = Branches[i] };

        // 年干序号
        int yearStemIndex = (lunarYear - 4) % 10;
        int yearBranchIndex = (lunarYear - 4) % 12;

        // 五虎遁求月干
        string stemStart = yearStemIndex switch
        {
            0 or 5 => "丙", // 甲己年
            1 or 6 => "戊", // 乙庚年
            2 or 7 => "庚", // 丙辛年
            3 or 8 => "壬", // 丁壬年
            4 or 9 => "甲", // 戊癸年
            _ => "甲"
        };

        int stemStartIndex = Array.IndexOf(HeavenlyStems, stemStart);
        string monthStem = HeavenlyStems[(stemStartIndex + lunarMonth - 1) % 10];

        // 寅宫为起点，顺数生月求月宫
        int monthIndex = (Array.IndexOf(Branches, "寅") + lunarMonth - 1) % 12;

        // 逆数生时求命宫（每两小时为一支）
        int hourBranchIndex = hour / 2;
        int mingIndex = (monthIndex - hourBranchIndex + 12) % 12;

        // 五行局
        string fiveElementBureau = yearStemIndex switch
        {
            0 or 1 => "木局",
            2 or 3 => "火局",
            4 or 5 => "土局",
            6 or 7 => "金局",
            8 or 9 => "水局",
            _ => "未知局"
        };

        // 紫微落宫偏移（含五行局位移）
        int ziweiIndex = (mingIndex + (fiveElementBureau switch
        {
            "水局" => 2,
            "木局" => 4,
            "火局" => 6,
            "土局" => 8,
            "金局" => 10,
            _ => 0
        })) % 12;

        // 主星排布（十四主星分布）
        for (int i = 0; i < MainStars.Length; i++)
        {
            int idx = (ziweiIndex + i) % 12;
            palaces[idx].MainStars.Add(MainStars[i]);
        }

        // 辅星分布（按月支与命宫扩展）
        for (int i = 0; i < 12; i++)
        {
            int secIndex = (lunarMonth + i * 3) % SecondaryStars.Length;
            palaces[i].SecondaryStars.Add(SecondaryStars[secIndex]);
            if (i % 2 == 0 && i < Gods.Length)
                palaces[i].Gods.Add(Gods[i]);
            else
                palaces[i].Gods.Add(Gods[(i + 3) % Gods.Length]);
        }

        // 宫干赋值
        for (int i = 0; i < 12; i++)
        {
            palaces[i].Stem = HeavenlyStems[(yearStemIndex + i) % 10];
        }

        // 三方四正标记
        int opposite = (mingIndex + 6) % 12;
        int left = (mingIndex + 3) % 12;
        int right = (mingIndex + 9) % 12;
        palaces[mingIndex].IsThreePower = true;
        palaces[opposite].IsThreePower = true;
        palaces[left].IsThreePower = true;
        palaces[right].IsThreePower = true;

        // 流年宫位（以命宫起顺推流年）
        int liuNianIndex = (mingIndex + (currentYear - lunarYear) % 12 + 12) % 12;
        palaces[liuNianIndex].IsFlowYear = true;

        // 输出调试信息
        Debug.Log(
            $"命宫在 {palaces[mingIndex].Branch} 宫（{palaces[mingIndex].Name}），" +
            $"紫微星在 {palaces[ziweiIndex].Branch} 宫（{palaces[ziweiIndex].Name}），" +
            $"五行局：{fiveElementBureau}\n" +
            $"月干：{monthStem}，流年宫：{palaces[liuNianIndex].Name}");

        return palaces;
    }
}