using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Globalization;
using System.Reflection.Emit;
using Label = UnityEngine.UIElements.Label;
using System.Numerics;
using System.Collections;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Vector2 = UnityEngine.Vector2;

public class Default : MonoBehaviour
{
    #region 定义
    StyleColor goodColor = new StyleColor(new Color32(204, 0, 0, 255));//红
    StyleColor normalColor = new StyleColor(new Color32(0, 102, 102, 255));//深绿

    string langCode;//多语言
    UIDocument uiDocument;//根界面
    VisualElement root;//根界面下的默认首元素
    VisualElement calDetail;//详细内容区
    VisualElement contents;//主要内容区
    VisualElement calMenu;//中间按钮导航
    VisualElement ziweiPan;//紫薇盘
    VisualElement userInput;//用户信息录入

    VisualTreeAsset dayButtonTemplateAsset;//日历模版
    VisualTreeAsset MingZhuTemplateAsset;//输入模版
    VisualTreeAsset nineGongTemplateAsset;

    VisualElement dongnan, nan, xinan, dong, zhong, xi, dongbei, bei, xibei, otherelement;//内容区布局
    List<VisualElement> nineElement;

    List<Label> allLabels;
    List<Label> labelWeeks;
    List<Label> nineLabel1, nineLabel2;

    private DropdownField yearDropdown, monthDropdown, dayDropdown, timeDropdown;

    TextField mzName;
    Toggle mzSexToggle;
    private int mzSex, mzYear, mzMonth, mzDay;
    string mzTime;
    Label manLabel;
    Label womenLabel;

    List<Button> menuBtn;
    string moonPath;

    DateTime currentDate;//手动选择时间

    string[] jieqi24;
    List<(int Day, string Name, bool Jia)> holidays;
    string holiToday;

    float lastClickTime;

    List<Label> luckTime;//吉时
    List<Label> luckTimeChild;

    Label tipsIco;//记事提示

    public TextField noteInput;

    string currentDateKey;

    DropdownField styleMode;
    Styles stylesScript;
    GameOne gameOne;

    static readonly (int startHour, string name)[] ShiChenTable =
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

    //轮换内容
    List<string> lunContent = new();
    Label contentLabel, labelOther;

    Button closeBtn, openBtn;

    #endregion

    //1，先加载根元素
    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        //多语言测试：
        LangManager.Load(langCode);
        /*
#if UNITY_IOS || UNITY_ANDROID
    var langCode = Application.systemLanguage switch
    {
        SystemLanguage.Japanese => "ja",
        SystemLanguage.English => "en",
        _ => "zh"
    };
#else
        var langCode = Application.systemLanguage == SystemLanguage.Japanese ? "ja" :
                       Application.systemLanguage == SystemLanguage.English ? "en" : "zh";
#endif

        LangManager.Load(langCode);
        */

    }

    #region 出生日期选择界面
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        userInput = root.Q<VisualElement>("UserInput");

        // 加载模板
        var mzTemplate = Resources.Load<VisualTreeAsset>("MingZhu");
        var mzRoot = mzTemplate.CloneTree();
        userInput.Add(mzRoot);

        // 绑定 UI 元素
        mzName = mzRoot.Q<TextField>("PlayerName");
        mzSexToggle = mzRoot.Q<Toggle>("SexToggle");
        manLabel = mzSexToggle.Q<Label>("ManLabel");
        womenLabel = mzSexToggle.Q<Label>("WomenLabel");

        yearDropdown = mzRoot.Q<DropdownField>("YearDropdown");
        monthDropdown = mzRoot.Q<DropdownField>("MonthDropdown");
        dayDropdown = mzRoot.Q<DropdownField>("DayDropdown");
        timeDropdown = mzRoot.Q<DropdownField>("TimeDropdown");

        // 读取保存的用户数据
        mzName.value = PlayerPrefs.GetString("UserName", "");
        mzSex = PlayerPrefs.GetInt("UserSex", 1);
        mzYear = PlayerPrefs.GetInt("UserYear", 2000);
        mzMonth = PlayerPrefs.GetInt("UserMonth", 1);
        mzDay = PlayerPrefs.GetInt("UserDay", 1);
        mzTime = PlayerPrefs.GetString("UserBronTime", "23:00-01:00");

        // 初始化控件状态
        mzSexToggle.value = (mzSex == 1);
        UpdateSexToggleColor(mzSexToggle.value);
        InitDropdowns(mzYear, mzMonth, mzDay, mzTime);

        // 注册事件
        mzName.RegisterValueChangedCallback(OnNameChanged);
        mzSexToggle.RegisterValueChangedCallback(evt =>
        {
            UpdateSexToggleColor(evt.newValue);
            SaveUserData();
        });

        // 用循环简化事件注册
        foreach (var dropdown in new[] { yearDropdown, monthDropdown, dayDropdown, timeDropdown })
            dropdown.RegisterValueChangedCallback(_ => SaveUserData());
    }

    void InitDropdowns(int year, int month, int day, string timeRange)
    {
        // 年份：1960 - 当前年
        yearDropdown.choices = Enumerable.Range(1960, DateTime.Now.Year - 1960 + 1)
                                         .Select(y => y.ToString()).ToList();
        yearDropdown.value = yearDropdown.choices.Contains(year.ToString()) ? year.ToString() : yearDropdown.choices.Last();

        // 月份：1 - 12
        monthDropdown.choices = Enumerable.Range(1, 12).Select(m => m.ToString()).ToList();
        monthDropdown.value = monthDropdown.choices.Contains(month.ToString()) ? month.ToString() : "1";

        // 日期：1 - 31
        dayDropdown.choices = Enumerable.Range(1, 31).Select(d => d.ToString()).ToList();
        dayDropdown.value = dayDropdown.choices.Contains(day.ToString()) ? day.ToString() : "1";

        // 时辰：根据表动态生成
        timeDropdown.choices = ShiChenTable.Select(s =>
        {
            int endHour = (s.startHour + 2) % 24;
            return $"{s.startHour:00}:00-{endHour:00}:00";
        }).ToList();
        timeDropdown.value = timeDropdown.choices.Contains(timeRange) ? timeRange : timeDropdown.choices[0];
    }

    void SaveUserData()
    {
        try
        {
            mzSex = mzSexToggle.value ? 1 : 0;
            mzYear = int.Parse(yearDropdown.value);
            mzMonth = int.Parse(monthDropdown.value);
            mzDay = int.Parse(dayDropdown.value);
            mzTime = timeDropdown.value;

            PlayerPrefs.SetString("UserName", mzName.value);
            PlayerPrefs.SetInt("UserSex", mzSex);
            PlayerPrefs.SetInt("UserYear", mzYear);
            PlayerPrefs.SetInt("UserMonth", mzMonth);
            PlayerPrefs.SetInt("UserDay", mzDay);
            PlayerPrefs.SetString("UserBronTime", mzTime);
            PlayerPrefs.Save();

            Debug.Log($"保存成功：{mzName.value}, 性别{mzSex}, 出生 {mzYear}-{mzMonth}-{mzDay} {mzTime}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存用户数据失败：{e.Message}");
        }
    }

    /// <summary>切换男女文字颜色</summary>
    void UpdateSexToggleColor(bool isMan)
    {
        Color blue = new(0.2f, 0.4f, 1f);
        Color black = Color.black;
        manLabel.style.color = isMan ? blue : black;
        womenLabel.style.color = isMan ? black : blue;
    }

    /// <summary>实时保存名字</summary>
    void OnNameChanged(ChangeEvent<string> evt)
    {
        PlayerPrefs.SetString("UserName", evt.newValue);
        PlayerPrefs.Save();
    }

    #endregion

    void Start()
    {
        Application.targetFrameRate = 1;//1秒刷新，减少程序消耗

        root = uiDocument.rootVisualElement;//这个用于改变全局字体颜色
        root.style.color = normalColor;

        GameObject go = GameObject.Find("UIDocument");
        stylesScript = go.GetComponent<Styles>();
        gameOne = go.GetComponent<GameOne>();

        openBtn = uiDocument.rootVisualElement.Q<Button>("Player");
        calDetail = uiDocument.rootVisualElement.Q<VisualElement>("CalendarContents");
        userInput = uiDocument.rootVisualElement.Q<VisualElement>("UserInput");
        labelWeeks = uiDocument.rootVisualElement.Q<VisualElement>("WeeksTitle").Query<Label>().ToList();
        ziweiPan = uiDocument.rootVisualElement.Q<VisualElement>("ZiWeiContents");

        // ✅ 只获取直接子级的 VisualElement，不包括自己和 Label 等子层
        nineElement = calDetail.Children()
            .OfType<VisualElement>()
            .Where(e => e.childCount == 0 || e is not Label) // 可选：排除非容器或 Label
            .ToList();

        allLabels = uiDocument.rootVisualElement.Q<VisualElement>("NineStarMonth").Query<Label>().ToList();

        dongnan = uiDocument.rootVisualElement.Q<VisualElement>("DongNan");
        nan = uiDocument.rootVisualElement.Q<VisualElement>("Nan");
        xinan = uiDocument.rootVisualElement.Q<VisualElement>("XiNan");
        dong = uiDocument.rootVisualElement.Q<VisualElement>("Dong");
        zhong = uiDocument.rootVisualElement.Q<VisualElement>("Zhong");
        xi = uiDocument.rootVisualElement.Q<VisualElement>("Xi");
        dongbei = uiDocument.rootVisualElement.Q<VisualElement>("DongBei");
        bei = uiDocument.rootVisualElement.Q<VisualElement>("Bei");
        xibei = uiDocument.rootVisualElement.Q<VisualElement>("XiBei");
        otherelement = uiDocument.rootVisualElement.Q<VisualElement>("Other");

        contentLabel = otherelement.Q<Label>("LunLabel");
        labelOther = otherelement.Query<Label>("othContent");

        //模式选择
        styleMode = uiDocument.rootVisualElement.Q<DropdownField>("Mode");
        styleMode.value = styleMode.choices[0];
        ApplySelection(styleMode.value);
        styleMode.RegisterValueChangedCallback(evt => ApplySelection(evt.newValue));

        //0，默认隐藏日历界面
        userInput.style.display = DisplayStyle.None;
        calDetail.style.display = DisplayStyle.None;
        ziweiPan.style.display = DisplayStyle.None;

        //1，先生成当前月历
        contents = uiDocument.rootVisualElement.Q<VisualElement>("Contents");
        dayButtonTemplateAsset = Resources.Load<VisualTreeAsset>("DayButtonTemplate");
        if (dayButtonTemplateAsset == null)
        {
            return;
        }

        //2，吉时显示固定
        var luckyTimeContainer = uiDocument.rootVisualElement.Q("luckyTime");
        luckTime = luckyTimeContainer.Children()
            .OfType<Label>()
            .ToList();
        // 所有子 Label（不包括上面父级 Label）
        luckTimeChild = luckyTimeContainer.Query<Label>()
            .Where(l => !luckTime.Contains(l))
            .ToList();

        //3，月份导航按钮
        calMenu = uiDocument.rootVisualElement.Q<VisualElement>("CalendarMenu");
        menuBtn = calMenu.Query<Button>().ToList();

        GetMoonPhaseIndex(DateTime.Now);

        if (menuBtn.Count == 3)
        {
            menuBtn[0].clicked += () => ChangeMonth(-1);
            menuBtn[1].clicked += () => ChangeMonth(0);
            menuBtn[2].clicked += () => ChangeMonth(1);
        }

        //5，月历
        GenerateCalendar(DateTime.Now.Year, DateTime.Now.Month);

        //6，头像
        GetAvatar();

        //当天节日，给背景图用；同时用于整体字体颜色
        foreach (var item in holidays)
        {
            if (item.Day == DateTime.Now.Day)
            {

                holiToday = item.Name;
                root.style.color = goodColor;
                break;
            }
        }

        #region 记事
        noteInput = calMenu.Query<TextField>();
        // 日记注册一次回调（以后不再重复注册/注销）
        noteInput.RegisterValueChangedCallback(OnNoteChanged);

        currentDateKey = DateTime.Now.ToString("yyyyMMdd");
        if (noteInput != null)
        {
            if (PlayerPrefs.HasKey(currentDateKey))
                noteInput.SetValueWithoutNotify(PlayerPrefs.GetString(currentDateKey));
            else
                noteInput.SetValueWithoutNotify(string.Empty);
        }
        #endregion
    }

    void Update()
    {
        //过时隐藏
        if (Time.time - lastClickTime > 40f)
        {
            if (userInput.style.display == DisplayStyle.Flex || calDetail.style.display == DisplayStyle.Flex || ziweiPan.style.display == DisplayStyle.Flex)
            {
                HideV(calDetail);
                HideV(userInput);
                HideV(ziweiPan);
            }
        }

        // 实时显示时间
        if (luckTime.Count == 12)
        {
            bool showColon = DateTime.Now.Second % 2 == 0;
            string colon = showColon ? ":" : " ";
            int hour = DateTime.Now.Hour;
            int minute = DateTime.Now.Minute;

            for (int i = 0; i < 12; i++)
            {
                // 计算当前时辰是否在对应时段（假设子=23点，依次每2小时）
                int startHour = (i * 2 + 23) % 24;
                bool isCurrent = (hour >= startHour && hour < (startHour + 2) % 24)
                                 || (startHour == 23 && hour < 1);

                luckTimeChild[i].text = isCurrent
                    ? $"{hour:D2}{colon}{DateTime.Now.Minute:D2}"
                    : string.Empty;
            }
        }
    }

    /// <summary>
    /// 初始化默认月历
    /// </summary>
    private void GenerateCalendar(int year, int month)
    {
        if (year < 1901 || year > 2040)
        {
            return;
        }
        contents.Clear();

        DateTime today = DateTime.Today;

        // 获取日期信息
        DateTime firstDay = new DateTime(year, month, 1);
        int startOffset = (int)firstDay.DayOfWeek; // Sunday=0
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int totalSlots = startOffset + daysInMonth;

        // 确保父容器布局正确
        contents.style.flexDirection = FlexDirection.Row;
        contents.style.flexWrap = Wrap.Wrap;
        contents.style.justifyContent = Justify.FlexStart;

        //24节气
        jieqi24 = CalendarData.Get24JieQi(year, month);

        //节日
        holidays = langCode switch
        {
            "tw" => CalendarData.GetTaiwanHolidays(year, month),     // 繁体显示民国
            "ja" => CalendarData.GetJapanHolidays(year, month),   // 日文显示日本年号
            "en" => CalendarData.GetEnglishHolidays(year, month),
            _ => CalendarData.GetChinaHolidays(year, month)      //默认显示 
        };

        //时间显示
        LuckTime(today);

        //星期显示
        int dayNumber = (int)today.DayOfWeek;
        labelWeeks[dayNumber].style.backgroundColor = normalColor;
        labelWeeks[dayNumber].style.color = new Color(255, 255, 255, 255);

        //主要区域
        for (int i = 0; i < totalSlots; i++)
        {
            // 克隆模板
            VisualElement clonedRoot = dayButtonTemplateAsset.CloneTree();
            Button newButton = clonedRoot.Q<Button>("DayButtonTemplate");
            tipsIco = clonedRoot.Q<Label>("NoteIco");

            if (newButton == null)
            {
                continue;
            }

            // 设置按钮大小百分比
            newButton.style.width = new StyleLength(new Length(14.28f, LengthUnit.Percent));
            newButton.style.height = new StyleLength(new Length(16.66f, LengthUnit.Percent));

            // 获取四个 Label
            var labels = newButton.Query<Label>().ToList();
            if (labels.Count < 4)
            {
                continue;
            }

            tipsIco.style.display = DisplayStyle.None;//记事标记

            if (i < startOffset)
            {
                // 空白占位
                labels.ForEach(label => label.text = "");
                newButton.SetEnabled(false);
            }
            else
            {
                int day = i - startOffset + 1;
                DateTime selectedDate = new DateTime(year, month, day);
                currentDate = selectedDate;

                if (!string.IsNullOrEmpty(PlayerPrefs.GetString(currentDate.ToString("yyyyMMdd"))))
                {
                    tipsIco.style.display = DisplayStyle.Flex;
                }

                var chineseCal = CalendarData.GetChineseCalendar(selectedDate);

                labels[0].text = day.ToString(); // 数字1号
                foreach (var item in holidays)
                {
                    if (item.Day == day)
                    {
                        labels[0].style.color = Color.red;
                        BackImage(labels[0], item.Name);
                    }
                }

                labels[1].text = CalendarData.GetTwelveGodInfo(selectedDate).Name; //值神

                if (jieqi24[1].Substring(0, 10) == selectedDate.Date.ToString("yyyy-MM-dd"))
                {
                    labels[2].text = jieqi24[0];
                    labels[2].style.color = Color.blue;
                }
                else if (jieqi24[3].Substring(0, 10) == selectedDate.Date.ToString("yyyy-MM-dd"))
                {
                    labels[2].text = jieqi24[2];
                    labels[2].style.color = Color.blue;
                }
                else
                {
                    //初一
                    if (chineseCal.LunarDate.Substring(2, 2) == "初一")
                    {
                        labels[2].text = chineseCal.LunarDate.Substring(0, 2);
                        labels[2].style.color = Color.blue;
                    }
                    else
                    {
                        labels[2].text = chineseCal.LunarDate.Substring(2, 2);
                    }
                }
                string liuyao = CalendarData.Get6Yao(chineseCal.LunarDate).Name;
                string fontColor = GetLiuYaoColor(liuyao);
                labels[3].text = $"<color={fontColor}>{liuyao}</color>"; //大安
                

                if (selectedDate.Date == today.Date)
                {
                    newButton.style.backgroundColor = new StyleColor(new Color(0f, 1.0f, 0f)); // 绿色
                    labels[0].style.color = Color.white; // 让日期文字变白色，突出显示
                }

                int row = (day + startOffset - 1) / 7; // 计算当前行，0-based
                int col = (day + startOffset - 1) % 7; // 当前列

                newButton.clicked += () =>
                {
                    ClickMessage(selectedDate, holidays);
                };
            }

            // 添加到父容器
            contents.Add(newButton);
        }
    }

    /// <summary>
    /// 日期按钮点击
    /// </summary>
    /// <param name="evt"></param>
    private void ClickMessage(DateTime selectedDate, List<(int Day, string Name, bool Jia)> holidays)
    {
        root.style.color = holidays.Any(item => item.Day == selectedDate.Day) ? goodColor : normalColor;

        ShowV(calDetail);

        //1，更新日历
        DisplayCalendar(selectedDate, holidays, 0);
        calDetail.style.display = DisplayStyle.Flex;

        //3，更新月相
        GetMoonPhaseIndex(selectedDate);

        //4，更新星期背景显示
        foreach (var lbl in labelWeeks)
        {
            lbl.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            lbl.style.color = normalColor;
        }
        int dayNumber = (int)selectedDate.DayOfWeek;
        labelWeeks[dayNumber].style.backgroundColor = normalColor;
        labelWeeks[dayNumber].style.color = new Color(255, 255, 255, 255);

        //日记
        currentDateKey = selectedDate.ToString("yyyyMMdd");
        if (PlayerPrefs.HasKey(currentDateKey))
        {
            noteInput.SetValueWithoutNotify(PlayerPrefs.GetString(currentDateKey));
            tipsIco.style.display = DisplayStyle.Flex;
        }
        else
        {
            noteInput.SetValueWithoutNotify(string.Empty);
            tipsIco.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// 日历详细显示区 0日历 1个人
    /// </summary>
    /// <param name="date">点击的那一天或用户生日</param>
    /// <param name="values">节日数组</param>
    /// <param name="type">0日历 1个人</param>
    void DisplayCalendar(DateTime date, List<(int Day, string Name, bool Jia)> values, int type)
    {
        var result = CalendarData.GetChineseCalendar(date);

        //吉时
        LuckTime(date);

        //九星 
        int[] starYears = CalendarData.GetYearStar(date);
        int[] starMonths = CalendarData.GetMonthStar(date);
        int[] starDays = CalendarData.GetDayStar(date);
        int[] starHours = CalendarData.GetHourStar(date);
        var starsInfoYear = CalendarData.GetNineStarLuck(starYears, date.Year);
        var starsInfoMonth = CalendarData.GetNineStarLuck(starMonths, date.Year);
        var starsInfoDay = CalendarData.GetNineStarLuck(starDays, date.Year);
        var starsInfoHour = CalendarData.GetNineStarLuck(starHours, date.Year);

        VisualElement gongYear = bei.Q<VisualElement>("NineGongY");
        VisualElement gongDay = bei.Q<VisualElement>("NineGongD");
        List<Label> nineLabelY = gongYear.Query<Label>().ToList();
        List<Label> nineLabelD = gongDay.Query<Label>().ToList();

        //九宫背景颜色按时显示
        for (int i = 0; i < 9; i++)
        {
            nineElement[i].style.backgroundColor = starsInfoHour[i].BgColor;
            //nineLabel[order[i]].text = infos[i].StarName; // 同步显示名称
        }

        //1,中国77年 民国114年 令和7年
        var yearInfo = CalendarData.GetYearsName(date.Year);
        string displayYear = langCode switch
        {
            "tw" => yearInfo.minguoYear,     // 繁体显示民国
            "ja" => yearInfo.japaneseYear,   // 日文显示日本年号
            _ => yearInfo.prcYear      //默认显示 
        };

        Label labelDongnan = dongnan.Query<Label>();
        labelDongnan.text = $"{displayYear}";
        labelDongnan.text += $"\n{date.Year}-{date.Month}-{date.Day}";
        labelDongnan.text += $"\n{result.GanZhiYear}年{result.LunarDate}";
        labelDongnan.text += $"\n{result.RunMonth}";

        //2 治水 煞
        Label labelNan = nan.Query<Label>();
        labelNan.text = $"{CalendarData.GetLongNiu(date)}";

        var sha = CalendarData.GetShaFang(result.GanZhiYear, result.GanZhiDay);
        labelNan.text += $"\n岁煞{sha.SuiSha}";
        labelNan.text += $" 劫煞{sha.JieSha}";
        labelNan.text += $" 灾煞{sha.ZaiSha}";

        //3,节日 空亡
        Label labelXiNan = xinan.Query<Label>();
        var huangheiDao = CalendarData.GetHuangDaoShen(date);
        var shiChen = CalendarData.GetShiChen(date.Hour);
        labelXiNan.text = string.Join("，",
            values.Where(v => v.Day == date.Day).Select(v => v.Name));
        labelXiNan.text += $"\n{CalendarData.GetRiLu(date, shiChen)}";
        labelXiNan.text += $"\n{huangheiDao.Name}[{huangheiDao.Type}]";
        labelXiNan.text += $"\n年空 {CalendarData.GetKongWang(result.GanZhiYear)}";
        labelXiNan.text += $" 月空 {CalendarData.GetKongWang(result.GanZhiMonth)}";
        labelXiNan.text += $" 日空 {CalendarData.GetKongWang(result.GanZhiDay)}";

        //4 星座 28宿 六曜 十二建 三合
        Label labelDong = dong.Query<Label>();
        var xingZuo = CalendarData.GetZodiacSign(date);
        var liuYao = CalendarData.Get6Yao(result.LunarDate);
        labelDong.text = $"{xingZuo.Name}";
        string fontColor = GetLiuYaoColor(liuYao.Name);
        labelDong.text += $" <color={fontColor}>{liuYao.Name} {liuYao.JiXiong}</color>";
        var twelveGod = CalendarData.GetTwelveGodInfo(date);
        labelDong.text += " " + FormatLuckText(CalendarData.Get28Xiu(date).Name, CalendarData.Get28Xiu(date).Luck);
        labelDong.text += " " + FormatLuckText(twelveGod.Name, twelveGod.Jx);

        labelDong.text += $"\n三合：{CalendarData.GetSanHe(result.GanZhiDay)}";
        labelDong.text += $"\n六合：{CalendarData.GetLiuHe(result.GanZhiDay)}";
        labelDong.text += $"\n{CalendarData.GetChong(result.GanZhiDay)}";
        labelDong.text += $" {CalendarData.GetHai(result.GanZhiDay)}";
        labelDong.text += $" {CalendarData.GetXing(result.GanZhiDay)}";

        //5 八字 纳音 九星 林下之猪 东西四命
        List<Label> labelZhong = zhong.Query<Label>().ToList();
        labelZhong[0].text = $"年 {result.GanZhiYear} {CalendarData.GetNaYin(result.GanZhiYear)} {starsInfoYear[4].StarName}";
        labelZhong[0].text += $"\n月 {result.GanZhiMonth} {CalendarData.GetNaYin(result.GanZhiMonth)} {starsInfoMonth[4].StarName}";
        labelZhong[0].text += $"\n日 {result.GanZhiDay} {CalendarData.GetNaYin(result.GanZhiDay)} {starsInfoDay[4].StarName}";
        labelZhong[0].text += $"\n时 {result.GanZhiTime} {CalendarData.GetNaYin(result.GanZhiTime)} {starsInfoHour[4].StarName}";
        labelZhong[1].text = $"<color=red>{result.GanZhiDay.Substring(0, 1)}{CalendarData.GetFive(result.GanZhiDay.Substring(0, 1))}</color>";
        labelZhong[1].text += $" {CalendarData.GetShengXiao(date)} {CalendarData.GetMingGua(date)}";

        string[] bazi = new string[] { result.GanZhiYear, result.GanZhiMonth, result.GanZhiDay, result.GanZhiTime };
        string wuxingCount = "";
        foreach (var item in CalendarData.CountFiveElements(bazi))
        {
            wuxingCount += $"{item.Key}:{item.Value} ";
        }
        labelZhong[1].text += $"\n{wuxingCount}";

        //6 神方位
        Label labelXi = xi.Query<Label>();

        var shenWei = CalendarData.GetFiveShenDirections(date);
        labelXi.text = $"喜神 {shenWei.XiShen}";
        labelXi.text += $"\n贵神 {shenWei.GuiShen}";
        labelXi.text += $"\n财神 {shenWei.CaiShen}";
        labelXi.text += $"\n胎神 {shenWei.TaiShen}";
        labelXi.text += $"\n鹤神 {shenWei.HeShen}";

        //7，忌
        Label labelDongBei = dongbei.Query<Label>();
        labelDongBei.text = "【忌】";
        labelDongBei.text += $"\n{CalendarData.GetTwelveGodInfo(date).Ji}";
        labelDongBei.text += $"{CalendarData.Get28Xiu(date).Ji}";

        //8 九星
        if (nineLabelY.Count == 9)
        {
            for (int i = 0; i < 9; i++)
            {
                nineLabelY[i].text = starYears[i].ToString();
                nineLabelY[i].style.backgroundColor = starsInfoYear[i].BgColor;
            }  
        }

        if (nineLabelD.Count == 9)
        {
            for (int i = 0; i < 9; i++)
            {
                nineLabelD[i].text = starDays[i].ToString();
                nineLabelD[i].style.backgroundColor = starsInfoDay[i].BgColor;
            }
        }

        //9,宜
        Label labelXiBei = xibei.Query<Label>();
        labelXiBei.text = "【宜】";
        string yg13 = CalendarData.GetYangGong13(date);
        if (yg13 != "")
        {
            labelXiBei.text += "\n"+yg13;
        }
        else
        {
            labelXiBei.text += $"\n{CalendarData.GetTwelveGodInfo(date).Yi}";
            labelXiBei.text += $"{CalendarData.Get28Xiu(date).Yi}";
        }

        //10，其他 画线
        lunContent.Add($"{CalendarData.GetBaiJi(result.GanZhiDay)}");//彭祖百忌
        lunContent.Add($"{jieqi24[0]}:{jieqi24[1]} {jieqi24[2]}:{jieqi24[3]}");//24节气时间
        int[] luckNumber = CalendarData.Calculate(date, DateTime.Now);
        lunContent.Add($"今日吉数：<b>{string.Join(" ", luckNumber)}</b>");//今日吉数
        lunContent.Add($" {CalendarData.CalculateWeight(date, mzTime)}");//称骨算命
        lunContent.Add($"{CalendarData.Get28Xiu(date).Sheng}");//28宿生人
        //lunContent.Add($"{CalendarData.GetDetailedComment(CalendarData.CountFiveElements(bazi))}"); //八字批注，这个是多行

        contentLabel.text = $"{lunContent[0]}";
        contentLabel.text += $" {lunContent[1]}";
        contentLabel.text += $"\n{lunContent[2]}";
        contentLabel.text += $" {lunContent[3]}";
        contentLabel.text += $"\n{lunContent[4]}";

        DrawBiorhythmBars(labelOther, date, DateTime.Now);
        labelOther.text = "<color=red>身体</color> <color=green>情绪</color> <color=blue>思维</color>";
    }

    //上一月下一月
    void ChangeMonth(int offset)
    {
        DateTime curDate = offset switch
        {
            -1 => new DateTime(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month, 1),
            0 => DateTime.Now,
            1 => new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1),
            _ => currentDate
        };
        GenerateCalendar(curDate.Year, curDate.Month);
        ClickMessage(curDate, holidays);
    }

    //获取头像
    void GetAvatar()
    {
        var avater = uiDocument.rootVisualElement.Q<VisualElement>("Avatar");
        string ganZhi = CalendarData.GetChineseCalendar(DateTime.Now).GanZhiDay.Substring(1, 1);

        string sxPath = Path.Combine(Application.streamingAssetsPath, "sxpic", $"{ganZhi}.png");

        if (!File.Exists(moonPath)) return;

        // 加载并设置背景
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(sxPath));
        avater.style.backgroundImage = new StyleBackground(tex);

        avater.RegisterCallback<ClickEvent>(evt =>
        {
            OnAvatarClicked();
        });
    }

    /// <summary>
    /// 点击头像后
    /// </summary>
    void OnAvatarClicked()
    {
        DateTime mingZhu = new DateTime(mzYear, mzMonth, mzDay);
        //显示用户信息界面
        ShowV(userInput);

        //1，如果用户信息为空，显示用户输入界面
        closeBtn = uiDocument.rootVisualElement.Q<Button>("CloseE");//关闭按钮
        closeBtn.clicked += () =>
        {
            HideV(userInput);
            HideV(calDetail);
            ShowV(ziweiPan);
            DisplayZiWei(mingZhu);
            lastClickTime = Time.time;
        };
    }

    //记事
    void OnNoteChanged(ChangeEvent<string> evt)
    {
        PlayerPrefs.SetString(currentDateKey, evt.newValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 吉时
    /// </summary>
    /// <param name="date"></param>
    void LuckTime(DateTime date)
    {
        var lucks = CalendarData.GetShiChenLuck(date);
        for (int i = 0; i < luckTime.Count && i < lucks.Count; i++)
        {
            var (name, luck) = lucks[i];
            luckTime[i].text = $"{name}{luck}";
            switch (luck)
            {
                case "吉":
                    luckTime[i].style.color = goodColor; break;
                case "平":
                    luckTime[i].style.color = normalColor; break;
                default: break;
            }
        }
    }

    /// <summary>
    /// 六曜字体颜色
    /// </summary>
    /// <param name="liuyao"></param>
    /// <param name="ve"></param>
    string GetLiuYaoColor(string liuyao)
    {
        int hour = DateTime.Now.Hour;
        switch (liuyao)
        {
            case "大安": return "green";
            case "赤口": return (hour > 11 && hour < 13) ? "green" : "black";
            case "先勝": return (hour < 12) ? "green" : "black";
            case "友引": return (hour < 11 || hour > 13) ? "green" : "black";
            case "先負": return (hour > 12) ? "green" : "black";
            default: return "black";
        }
    }

    /// <summary>
    /// 绿色吉黑色凶，红色用于节日不使用
    /// </summary>
    /// <param name="name"></param>
    /// <param name="luck"></param>
    /// <returns></returns>
    string FormatLuckText(string name, string luck)
    {
        return luck == "吉" ? $" <color=green>{name} {luck}</color>" : $" <color=black>{name} {luck}</color>";
    }

    /// <summary>
    /// 紫薇排盘
    /// </summary>
    /// <param name="date"></param>
    void DisplayZiWei(DateTime date)
    {
        //命宫排序对应
        List<VisualElement> ziweiPalaces = new List<VisualElement>
            {
                ziweiPan.Q<VisualElement>("view1"), // 寅
    ziweiPan.Q<VisualElement>("view2"),   // 卯
    ziweiPan.Q<VisualElement>("view3"),   // 辰
    ziweiPan.Q<VisualElement>("view4"), // 巳
    ziweiPan.Q<VisualElement>("view5"),    // 午
    ziweiPan.Q<VisualElement>("view6"),    // 未
    ziweiPan.Q<VisualElement>("view7"),   // 申
    ziweiPan.Q<VisualElement>("view8"),     // 酉
    ziweiPan.Q<VisualElement>("view9"),     // 戌
    ziweiPan.Q<VisualElement>("view10"),   // 亥
    ziweiPan.Q<VisualElement>("view11"),    // 子
    ziweiPan.Q<VisualElement>("view12")     // 丑
            };

        VisualElement vZhong = ziweiPan.Q<VisualElement>("viewZ");
        // 解析时辰为24小时制小时
        int birthHour = ParseHourFromRange(mzTime);
        var palaces = ZiWeiComplete.FillZiWeiChart(date, birthHour, 2025);

        for (int i = 0; i < 12; i++)
        {
            var palace = ziweiPalaces[i];
            var labelList = palace.Query<Label>().First();

            string text = $"{palaces[i].Name} {palaces[i].Branch}{palaces[i].Stem}\n" +
                          $"主星: {string.Join(" ", palaces[i].MainStars)}\n" +
                          $"辅星: {string.Join(" ", palaces[i].SecondaryStars)}\n" +
                          $"神煞: {string.Join(" ", palaces[i].Gods)}";

            if (palaces[i].IsThreePower) text += "\n三方四正吉宫";
            if (palaces[i].IsFlowYear) text += "\n流年宫";

            labelList.text = text;

            // 根据地支确定五行和背景色
            string wuxing = GetWuXingByBranch(palaces[i].Branch);
            Color bgColor = GetColorByWuXing(wuxing);
            palace.style.backgroundColor = bgColor;
        }
    }

    // 地支→五行映射
    static string GetWuXingByBranch(string branch)
    {
        return branch switch
        {
            "寅" or "卯" => "木",
            "巳" or "午" => "火",
            "辰" or "丑" or "未" or "戌" => "土",
            "申" or "酉" => "金",
            "亥" or "子" => "水",
            _ => "未知"
        };
    }

    // 五行→颜色映射
    static Color GetColorByWuXing(string wuXing)
    {
        return wuXing switch
        {
            "木" => new Color(0.4f, 0.8f, 0.4f),   // 绿色
            "火" => new Color(0.9f, 0.3f, 0.3f),   // 红色
            "土" => new Color(0.83f, 0.65f, 0.45f),// 土黄
            "金" => new Color(1.0f, 0.84f, 0.3f),  // 金色
            "水" => new Color(0.26f, 0.65f, 0.96f),// 蓝色
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
    }

    /// <summary>
    /// 选项 模式1和2的主要区别在于显示背景
    /// </summary>
    /// <param name="selected"></param>
    private void ApplySelection(string selected)
    {
        int index = styleMode.choices.IndexOf(selected);

        switch (index)
        {
            case 0: // 第1项
                if (gameOne != null) gameOne.enabled = false;
                stylesScript.ResetEffect();
                stylesScript.InitializeEffect("holidaypic", holiToday);
                break;

            case 1: // 第2项
                if (gameOne != null) gameOne.enabled = false;
                stylesScript.ResetEffect();
                stylesScript.InitializeEffect("monthGirl", "");
                break;

            case 2: // 第3项
                if (gameOne != null) gameOne.enabled = true;
                stylesScript.ResetEffect();
                stylesScript.enabled = false;
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 这个改成活动图片只显示在月历上
    /// </summary>
    /// <param name="holiday"></param>
    async void BackImage(VisualElement et, string holiday)
    {
        string[] files = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "holidaypic"))
.Where(f => f.ToLower().EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            f.ToLower().EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
.ToArray();
        string[] holidayNames = holiday.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
        string fileToLoad = files.FirstOrDefault(f =>
        {
            string name = Path.GetFileNameWithoutExtension(f);
            return holidayNames.Any(h => name.Contains(h.Trim(), StringComparison.OrdinalIgnoreCase));
        }) ?? files[0];
        Texture2D tex = new Texture2D(2, 2);
        byte[] bytes = await File.ReadAllBytesAsync(fileToLoad);
        tex.LoadImage(bytes);
        et.style.backgroundImage = new StyleBackground(tex);
    }

    /// <summary>
    /// 获取月相图片
    /// </summary>
    /// <param name="date"></param>
    int GetMoonPhaseIndex(DateTime date)
    {
        // 基准新月
        DateTime baseNewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
        DateTime utcDate = date.ToUniversalTime();

        const double synodicMonth = 29.530588853;

        // 月龄 = 当前时间 - 基准新月 
        double moonAge = ((utcDate - baseNewMoon).TotalDays) % synodicMonth;
        if (moonAge < 0) moonAge += synodicMonth;

        // 转换为8阶段索引（0=新月, 4=满月）
        int phaseIndex = (int)Math.Floor((moonAge / synodicMonth) * 8 + 0.5) % 8;

        moonPath = Path.Combine(Application.streamingAssetsPath, "moon", $"{phaseIndex + 1}.png");

        if (!File.Exists(moonPath)) return 0;

        // 加载并设置按钮背景
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(moonPath));
        var bg = new StyleBackground(tex);
        foreach (var btn in menuBtn)
            btn.style.backgroundImage = bg;
        return phaseIndex;
    }

    //显示界面
    void ShowV(VisualElement e)
    {
        e.style.display = DisplayStyle.Flex;
        lastClickTime = Time.time;
    }

    //隐藏界面
    void HideV(VisualElement e)
    {
        e.style.display = DisplayStyle.None;
    }

    // 从时辰字符串解析小时
    public static int ParseHourFromRange(string range)
    {
        try
        {
            string[] parts = range.Split('-');
            if (parts.Length > 0)
            {
                string start = parts[0];
                string hourPart = start.Split(':')[0];
                return int.Parse(hourPart);
            }
        }
        catch { }
        return 0; // 默认子时
    }

    public Color bodyColor = Color.red;
    public Color moodColor = Color.green;
    public Color intelColor = Color.blue;
    public Color todayColor = Color.yellow;

    #region 画直线图
    /// <summary>
    /// 画值线图
    /// </summary>
    /// <param name="container"></param>
    /// <param name="birthDate"></param>
    /// <param name="currentDate"></param>
    public void DrawBiorhythmBars(VisualElement container, DateTime birthDate, DateTime selectedDate)
    {
        container.Clear();

        float w = container.resolvedStyle.width > 0 ? container.resolvedStyle.width : 400;
        float h = container.resolvedStyle.height > 0 ? container.resolvedStyle.height : 40;
        float midY = h / 2f;
        float maxHeight = h / 2f * 0.9f;
        float hourStep = w / 24f;

        // 获取当月天数，用于周期计算（28~31）
        int daysInMonth = DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month);

        // 计算每小时的值（波动范围 -1 ~ 1）
        float[] body = new float[24];
        float[] mood = new float[24];
        float[] intel = new float[24];

        for (int hIdx = 0; hIdx < 24; hIdx++)
        {
            DateTime dt = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, hIdx, 0, 0);
            double totalDays = (dt - birthDate).TotalDays;

            // 按周期计算（周期按整个月）
            body[hIdx] = (float)Math.Sin(2 * Math.PI * totalDays / 23);   // 身体周期 23天
            mood[hIdx] = (float)Math.Sin(2 * Math.PI * totalDays / 28);   // 情绪周期 28天
            intel[hIdx] = (float)Math.Sin(2 * Math.PI * totalDays / 33);  // 智力周期 33天
        }

        var graph = new ImmediateVisualElement();
        graph.style.width = w;
        graph.style.height = h;
        container.Add(graph);

        graph.generateVisualContent += ctx =>
        {
            var p = ctx.painter2D;

            // 中线
            p.strokeColor = Color.gray;
            p.lineWidth = 1f;
            p.BeginPath();
            p.MoveTo(new Vector2(0, midY));
            p.LineTo(new Vector2(w, midY));
            p.Stroke();

            // 绘制每小时柱状图
            for (int hIdx = 0; hIdx < 24; hIdx++)
            {
                float[] barValues = { body[hIdx], mood[hIdx], intel[hIdx] };
                Color[] colors = { bodyColor, moodColor, intelColor };

                for (int k = 0; k < 3; k++)
                {
                    float barHeight = barValues[k] * maxHeight;
                    float x = hIdx * hourStep + k * hourStep * 0.25f; // 左中右偏移
                    float y = barHeight >= 0 ? midY - barHeight : midY;
                    float width = hourStep * 0.2f;

                    // 高亮当前小时
                    if (selectedDate.Date == DateTime.Now.Date && hIdx == DateTime.Now.Hour)
                        p.fillColor = todayColor;
                    else
                        p.fillColor = colors[k];

                    p.strokeColor = p.fillColor;

                    p.BeginPath();
                    p.MoveTo(new Vector2(x, y));
                    p.LineTo(new Vector2(x + width, y));
                    p.LineTo(new Vector2(x + width, y + Math.Abs(barHeight)));
                    p.LineTo(new Vector2(x, y + Math.Abs(barHeight)));
                    p.ClosePath();
                    p.Fill();
                }
            }
        };
    }

    void CreateAxisLabels(VisualElement container, int days, float width, float height, DateTime currentDate)
    {
        float step = width / days;
        float midY = height / 2f;

        // X轴天数刻度
        for (int i = 0; i <= days; i += 5)
        {
            var dayLabel = new Label((i - days / 2).ToString())
            {
                style =
                {
                    position = Position.Absolute,
                    left = i * step - 5,
                    top = midY + 8,
                    color = Color.white
                }
            };
            container.Add(dayLabel);
        }

        // 文字显示暂时不用
        /*
        int phaseIndex = GetMoonPhaseIndex(currentDate);
        string[] moonNames = { "新月", "娥眉", "上弦", "盈凸", "满月", "亏凸", "下弦", "残月" };

        var moonLabel = new Label($"月相：{moonNames[phaseIndex % 8]}")
        {
            style =
            {
                position = Position.Absolute,
                left = width - 300,
                top = 5,
                color = Color.blue
            }
        };
        container.Add(moonLabel);
        */
    }

    void DrawBars(Painter2D p, DateTime birth, DateTime date, double period, Color color,
                  float step, float midY, int days, float maxHeight, float xOffset)
    {
        p.fillColor = color;
        p.strokeColor = color;
        p.lineWidth = 1f;
        float barWidth = step * 0.25f;
        float halfCell = step / 2f;

        // 先计算好所有天的值（便于后续扩展）
        float[] values = new float[days];
        for (int i = 0; i < days; i++)
        {
            DateTime d = date.AddDays(i - days / 2);
            int totalDays = (d - birth).Days;
            values[i] = (float)Math.Sin(2 * Math.PI * totalDays / period);
        }

        // 绘制柱体
        for (int i = 0; i < days; i++)
        {
            float value = values[i];
            float barHeight = value * maxHeight;
            float height = Math.Abs(barHeight);
            float centerX = i * step + halfCell;
            float left = centerX + xOffset - barWidth / 2f;
            float top = barHeight >= 0 ? midY - barHeight : midY;

            p.BeginPath();
            p.MoveTo(new Vector2(left, top));
            p.LineTo(new Vector2(left + barWidth, top));
            p.LineTo(new Vector2(left + barWidth, top + height));
            p.LineTo(new Vector2(left, top + height));
            p.ClosePath();
            p.Fill();
        }
    }
    #endregion
}

///
///另一个方法
/// 
/// <summary>
/// 轻量绘制容器
/// </summary>
public class ImmediateVisualElement : VisualElement
{
    public ImmediateVisualElement()
    {
        // 允许使用绘制回调
        generateVisualContent += _ => { };
    }
}
