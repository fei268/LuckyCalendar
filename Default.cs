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
using UnityEngine.Video;
using UnityEngine.Networking;

public class Default : MonoBehaviour
{
    //AI说这颜色对一般色盲也友好，且试试
    StyleColor goodColor = new StyleColor(new Color32(0, 114, 178, 255));// 深蓝
    StyleColor normalColor = new StyleColor(new Color32(213, 94, 0, 255));// 橙色

    string langCode;//多语言
    UIDocument uiDocument;//根界面
    VisualElement calDetail;//详细内容区
    VisualElement contents;//主要内容区
    VisualElement calMenu;//中间按钮导航

    VisualElement userInput;//用户信息录入

    VisualTreeAsset dayButtonTemplateAsset;//日历模版
    VisualTreeAsset MingZhuTemplateAsset;//输入模版

    VisualElement dongnan, nan, xinan, dong, zhong, xi, dongbei, bei, xibei, otherelement;//内容区布局
    List<VisualElement> nineElement;

    List<Label> allLabels;
    List<Label> labelWeeks;

    private DropdownField yearDropdown, monthDropdown, dayDropdown, timeDropdown;

    Toggle mzSexToggle;
    private int mzSex, mzYear, mzMonth, mzDay;
    string mzTime;
    Label manLabel;
    Label womenLabel;

    DateTime currentDate;//手动选择时间

    float lastClickTime;

    List<Label> luckTime;//吉时
    List<Label> luckTimeChild;

    Label tipsIco;//记事提示

    TextField noteInput;

    string currentDateKey;

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

    public string resourcesFolder = "monthGirl";
    VideoPlayer videoPlayer;                // 绑定Inspector中的VideoPlayer
    public Material targetMaterial;                // 显示视频/图片的材质球
    public float imageDisplayTime = 6f;            // 图片显示时长
    public RenderTexture renderTexture;            // RenderTexture绑定材质
    GameObject quad;
    private string[] mediaFiles;
    private int currentIndex = 0;

    private bool isCtrl = false;
    private bool isShift = false;
    private KeyCode[] keyMap = { KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K };

    //轮换内容
    List<string> lunContent = new();
    Label labelOther;
    int index = 0;
    float timer = 0f;

    //九宫背景色
    Dictionary<int, Color> starColors = new Dictionary<int, Color>
{
    { 1, new Color(0.4f, 0.8f, 1f, 0.5f) },  // 一白 - 明亮蓝（白系）
    { 2, new Color(0.3f, 0.3f, 0.3f, 0.5f) },// 二黑 - 中灰
    { 3, new Color(0.0f, 0.6f, 0.4f, 0.5f) },// 三碧 - 蓝绿（色盲可区分）
    { 4, new Color(0.0f, 0.4f, 0.2f, 0.5f) },// 四绿 - 深蓝绿色
    { 5, new Color(0.85f, 0.65f, 0.2f, 0.5f) },// 五黄 - 土黄
    { 6, new Color(0.8f, 0.8f, 0.85f, 0.5f) },// 六白 - 白灰
    { 7, new Color(0.9f, 0.45f, 0.2f, 0.5f) }, // 七赤 - 橙红
    { 8, new Color(1f, 0.75f, 0.4f, 0.5f) },   // 八白 - 米黄/沙色
    { 9, new Color(0.6f, 0.3f, 0.7f, 0.5f) }   // 九紫 - 紫色（红紫混合，更色盲友好）
};


    Button closeBtn, openBtn;

    // 十二宫名称（逆时针）
    private static readonly string[] PalaceNames = {
        "命宫","兄弟宫","夫妻宫","子女宫","财帛宫","疾厄宫",
        "迁移宫","仆役宫","官禄宫","田宅宫","福德宫","父母宫"
    };

    // 十四主星简易对照表（此为示例，后续可扩充精确计算规则）
    private static readonly string[] MainStars = {
        "紫微", "天机", "太阳", "武曲", "天同", "廉贞",
        "天府", "太阴", "贪狼", "巨门", "天相", "天梁", "七杀", "破军"
    };

    // 五行局对照表
    private static readonly Dictionary<string, string> FiveElementTable = new()
    {
        {"甲子","水二局"},{"乙丑","水二局"},
        {"丙寅","火六局"},{"丁卯","火六局"},
        {"戊辰","土五局"},{"己巳","土五局"},
        {"庚午","金四局"},{"辛未","金四局"},
        {"壬申","水二局"},{"癸酉","水二局"},
        {"甲戌","火六局"},{"乙亥","火六局"},
        {"丙子","水二局"},{"丁丑","水二局"},
        {"戊寅","火六局"},{"己卯","火六局"},
        {"庚辰","土五局"},{"辛巳","土五局"},
        {"壬午","金四局"},{"癸未","金四局"},
        {"甲申","水二局"},{"乙酉","水二局"},
        {"丙戌","火六局"},{"丁亥","火六局"}
    };

    //1，先加载根元素
    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        //多语言测试：
        langCode = "zh";
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

    //2，加载用户输入界面
    #region 出生日期选择
    void OnEnable()
    {
        userInput = uiDocument.rootVisualElement.Q<VisualElement>("UserInput");

        MingZhuTemplateAsset = Resources.Load<VisualTreeAsset>("MingZhu");
        VisualElement clonedMZ = MingZhuTemplateAsset.CloneTree();
        userInput.Add(clonedMZ);

        mzSexToggle = clonedMZ.Q<Toggle>("SexToggle");
        manLabel = mzSexToggle.Q<Label>("ManLabel");
        womenLabel = mzSexToggle.Q<Label>("WomenLabel");

        yearDropdown = clonedMZ.Q<DropdownField>("YearDropdown");
        monthDropdown = clonedMZ.Q<DropdownField>("MonthDropdown");
        dayDropdown = clonedMZ.Q<DropdownField>("DayDropdown");
        timeDropdown = clonedMZ.Q<DropdownField>("TimeDropdown");

        mzSex = PlayerPrefs.GetInt("UserSex", 1);
        mzYear = PlayerPrefs.GetInt("UserYear", 1000);//默认1000
        mzMonth = PlayerPrefs.GetInt("UserMonth", 1);
        mzDay = PlayerPrefs.GetInt("UserDay", 1);
        mzTime = PlayerPrefs.GetString("UserBronTime", "23:00-01:00");

        // 初始化下拉列表
        mzSexToggle.value = (mzSex == 1);
        UpdateSexToggleColor(mzSexToggle.value);

        InitDropdowns(mzYear, mzMonth, mzDay, mzTime);

        // 注册值变化事件
        yearDropdown.RegisterValueChangedCallback(evt => OnDropdownChanged());
        monthDropdown.RegisterValueChangedCallback(evt => OnDropdownChanged());
        dayDropdown.RegisterValueChangedCallback(evt => OnDropdownChanged());
        timeDropdown.RegisterValueChangedCallback(evt => OnDropdownChanged());
        mzSexToggle.RegisterValueChangedCallback(evt =>
        {
            UpdateSexToggleColor(evt.newValue);
            OnDropdownChanged(); // 触发保存
        });
    }

    /// <summary>
    /// 默认年份大于今年，用于错误判断；
    /// </summary>
    void InitDropdowns(int year, int month, int day, string timeRange)
    {
        // 年份 1980-2030
        var years = Enumerable.Range(1960, DateTime.Now.Year - 1960 + 1).Select(y => y.ToString()).ToList();
        yearDropdown.choices = years;
        yearDropdown.value = years.Contains(year.ToString()) ? year.ToString() : "0";

        // 月份 1-12
        var months = Enumerable.Range(1, 12).Select(m => m.ToString()).ToList();
        monthDropdown.choices = months;
        monthDropdown.value = months.Contains(month.ToString()) ? month.ToString() : "1";

        // 日期 1-31
        var days = Enumerable.Range(1, 31).Select(d => d.ToString()).ToList();
        dayDropdown.choices = days;
        dayDropdown.value = days.Contains(day.ToString()) ? day.ToString() : "1";

        var times = ShiChenTable.Select(s =>
        {
            int endHour = (s.startHour + 2) % 24;
            return $"{s.startHour:00}:00-{endHour:00}:00";
        }).ToList();
        timeDropdown.choices = times;
        timeDropdown.value = times.Contains(timeRange) ? timeRange : times[0];
    }

    //点击编辑时触发此
    void OnEditClicked()
    {
        //AutoShow(userInput);
    }
    //选择年月日时触发
    void OnDropdownChanged()
    {
        SaveDate();
    }

    void SaveDate()
    {
        int sex = mzSexToggle.value ? 1 : 0;
        int year = int.Parse(yearDropdown.value);
        int month = int.Parse(monthDropdown.value);
        int day = int.Parse(dayDropdown.value);
        string usertime = timeDropdown.value;

        try
        {
            PlayerPrefs.SetInt("UserSex", sex);
            PlayerPrefs.SetInt("UserYear", year);
            PlayerPrefs.SetInt("UserMonth", month);
            PlayerPrefs.SetInt("UserDay", day);
            PlayerPrefs.SetString("UserBronTime", usertime);
            PlayerPrefs.Save();

            mzSex = sex;
            mzYear = year;
            mzMonth = month;
            mzDay = day;
            mzTime = usertime;

            Debug.Log($"性别{mzSex} 年{mzYear}-{mzMonth}-{mzDay}-{mzTime}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存用户数据失败：{e.Message}");
        }
    }
    /// <summary>
    /// 切换男女颜色
    /// </summary>
    /// <param name="isMan"></param>
    void UpdateSexToggleColor(bool isMan)
    {
        Color blue = new Color(0.2f, 0.4f, 1f);  // 柔和的蓝色
        Color defaultColor = Color.white;        // 默认字体颜色（可按UI风格调整）

        if (isMan)
        {
            manLabel.style.color = blue;
            womenLabel.style.color = defaultColor;
        }
        else
        {
            manLabel.style.color = defaultColor;
            womenLabel.style.color = blue;
        }
    }
    #endregion

    void Start()
    {
        Application.targetFrameRate = 1;

        openBtn = uiDocument.rootVisualElement.Q<Button>("Player");
        calDetail = uiDocument.rootVisualElement.Q<VisualElement>("CalendarContents");
        labelWeeks = uiDocument.rootVisualElement.Q<VisualElement>("WeeksTitle").Query<Label>().ToList();

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
        nan = uiDocument.rootVisualElement.Q<VisualElement>("Nan");
        dongbei = uiDocument.rootVisualElement.Q<VisualElement>("DongBei");
        bei = uiDocument.rootVisualElement.Q<VisualElement>("Bei");
        xibei = uiDocument.rootVisualElement.Q<VisualElement>("XiBei");
        otherelement = uiDocument.rootVisualElement.Q<VisualElement>("Other");

        labelOther = otherelement.Query<Label>("othContent");

        //暂定头像
        GetAvatar();

        openBtn.clicked += () => { userInput.style.display = DisplayStyle.Flex; };

        #region 点击头像
        userInput.style.display = DisplayStyle.None;
        calDetail.style.display = DisplayStyle.None;

        closeBtn = uiDocument.rootVisualElement.Q<Button>("CloseE");//关闭按钮
        if (closeBtn != null)
        {
            closeBtn.clicked += () =>
            {
                userInput.style.display = DisplayStyle.None;

                DateTime mingZhu = new DateTime(mzYear, mzMonth, mzDay);
                bool hasUserData = PlayerPrefs.GetInt("UserYear") > 1911;

                bool allHidden = userInput.style.display == DisplayStyle.None && calDetail.style.display == DisplayStyle.None;
                bool showingDetail = calDetail.style.display == DisplayStyle.Flex;

                if (allHidden)
                {
                    if (hasUserData)
                        ToggleUI(false, mingZhu, 1);   // 显示日历
                    else
                        ToggleUI(true, DateTime.Now, 0); // 显示输入框
                }
                else
                {
                    ToggleUI(showingDetail, showingDetail ? DateTime.Now : mingZhu, showingDetail ? 0 : 1);
                }

                lastClickTime = Time.time;
            };
        }
        #endregion

        //默认生成当前月历
        contents = uiDocument.rootVisualElement.Q<VisualElement>("Contents");
        dayButtonTemplateAsset = Resources.Load<VisualTreeAsset>("DayButtonTemplate");
        if (dayButtonTemplateAsset == null)
        {
            return;
        }

        //获取子元素
        var luckyTimeContainer = uiDocument.rootVisualElement.Q("luckyTime");
        luckTime = luckyTimeContainer.Children()
            .OfType<Label>()
            .ToList();
        // 所有子 Label（不包括上面父级 Label）
        luckTimeChild = luckyTimeContainer.Query<Label>()
            .Where(l => !luckTime.Contains(l))
            .ToList();

        currentDate = DateTime.Now;

        calMenu = uiDocument.rootVisualElement.Q<VisualElement>("CalendarMenu");
        var menuBtn = calMenu.Query<Button>().ToList();

        if (menuBtn.Count == 3)
        {
            menuBtn[0].clicked += () => ChangeMonth(-1);
            menuBtn[1].clicked += () => ChangeMonth(0);
            menuBtn[2].clicked += () => ChangeMonth(1);
        }

        GenerateCalendar(currentDate.Year, currentDate.Month);

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

        #region 视频背景广告
        quad = GameObject.Find("Quad");
        Camera cam = Camera.main;
        float distance = 5f; // Quad到摄像机的距离
        float height = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        quad.transform.localScale = new UnityEngine.Vector3(width, height, 1);
        quad.transform.position = cam.transform.position + cam.transform.forward * distance;
        quad.transform.rotation = cam.transform.rotation; // 面向摄像机

        string path = Path.Combine(Application.streamingAssetsPath, resourcesFolder);
        mediaFiles = Directory.GetFiles(path)
                              .Where(f => f.ToLower().EndsWith(".mp4") ||
                                          f.ToLower().EndsWith(".mov") ||
                                          f.ToLower().EndsWith(".png") ||
                                          f.ToLower().EndsWith(".jpg"))
                              .OrderBy(x => Guid.NewGuid())
                              .ToArray();

        if (videoPlayer == null)
            videoPlayer = GameObject.Find("VideoAD").GetComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane; // 直接渲染到摄像机
        videoPlayer.targetCamera = Camera.main;

        videoPlayer.loopPointReached += OnVideoFinished;

        PlayCurrent();
        #endregion

    }

    void Update()
    {
        //过时隐藏
        if (Time.time - lastClickTime > 50f)
        {
            if (userInput.style.display == DisplayStyle.Flex || calDetail.style.display == DisplayStyle.Flex)
            {
                userInput.style.display = DisplayStyle.None;
                calDetail.style.display = DisplayStyle.None;
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

        //按键音
        isCtrl = Input.GetKey(KeyCode.LeftControl);
        isShift = Input.GetKey(KeyCode.LeftShift);
        //按下按键的时候禁用输入框
        noteInput.SetEnabled(!(isCtrl || isShift));

        for (int i = 0; i < keyMap.Length; i++)
        {
            if (Input.GetKeyDown(keyMap[i]))
            {
                int col = i; // 列索引
                string notePrefix = "中音";

                if (isCtrl)
                {
                    notePrefix = "低音";
                }
                else if (isShift)
                {
                    notePrefix = "高音";
                }
                else
                {
                    notePrefix = "中音";
                }
                string fileName = $"{notePrefix}_{col + 1}";
                AudioClip clip = Resources.Load<AudioClip>($"piano_notes/{fileName}");
                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, UnityEngine.Vector3.zero);
                }
            }
        }

        //滚动内容
        timer += Time.deltaTime;
        if (timer > 10f)
        {
            timer = 0;
            index = (index + 1) % lunContent.Count;
            labelOther.text = lunContent[index];
        }

    }

    /// <summary>
    /// 初始化默认月份的日历
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
        string[] jieqi24 = CalendarData.Get24JieQi(year, month);

        //底部背景：
        //allLabels理论上固定，暂时写死；
        //由于UI中排版问题，label顺序不一致，这里数字部分如下（0开始）：1，3，4，7，8（中），9，12，13，15
        //逆飞顺序，UI中NineStarMonth的VisualElement顺序：5-1-4-3-8-2-7-6-9
        int zIndex = CalendarData.GetYearStar(firstDay).StarNo;
        bool isForward = CalendarData.GetYearStar(firstDay).isForward;

        // Label 索引顺序
        int[] labelIndices = { 8, 1, 7, 4, 13, 3, 12, 9, 15 };
        
        for (int i = 0; i < labelIndices.Length; i++)
        {
            int offset = isForward ? i : -i;  // 顺飞加，逆飞减
            allLabels[labelIndices[i]].text = Wrap9(zIndex, offset).ToString();
        }

        //时间显示
        LuckTime(today);

        //星期显示
        int dayNumber = (int)today.DayOfWeek;
        labelWeeks[dayNumber].style.backgroundColor = new StyleColor(new Color(0f, 1.0f, 0f));

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

                string jpHoliday = CalendarData.GetJapanHoliday(selectedDate);
                var chineseCal = CalendarData.GetChineseCalendar(selectedDate);

                labels[0].text = day.ToString(); // 数字1号
                if (!string.IsNullOrEmpty(jpHoliday))
                {
                    labels[0].style.color = Color.red;
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
                labels[3].text = liuyao; //大安

                int hour = DateTime.Now.Hour;
                bool setGreen = false;
                switch (liuyao)
                {
                    case "大安":
                        setGreen = true;
                        break;
                    case "赤口":
                        if (hour > 11 && hour < 13) setGreen = true;
                        break;
                    case "先勝":
                        if (hour < 12) setGreen = true;
                        break;
                    case "友引":
                        if (hour < 11 || hour > 13) setGreen = true;
                        break;
                    case "先負":
                        if (hour > 12) setGreen = true;
                        break;
                }

                if (setGreen) labels[3].style.color = Color.green;

                if (selectedDate.Date == today.Date)
                {
                    newButton.style.backgroundColor = new StyleColor(new Color(0f, 1.0f, 0f)); // 绿色
                    labels[0].style.color = Color.white; // 让日期文字变白色，突出显示
                }

                int row = (day + startOffset - 1) / 7; // 计算当前行，0-based
                int col = (day + startOffset - 1) % 7; // 当前列

                newButton.clicked += () =>
                {
                    //这里需要做判断，等程序完善
                    string[] prefix = { "低音", "中音", "高音" };
                    string fileName = "";

                    if (row == 1)
                    {
                        fileName = $"{prefix[0]}_{col + 1}"; // 低音
                    }
                    else if (row == 2)
                    {
                        fileName = $"{prefix[1]}_{col + 1}"; // 中音，不需要按键
                    }
                    else if (row == 3)
                    {
                        fileName = $"{prefix[2]}_{col + 1}"; // 高音
                    }

                    AudioClip clip = Resources.Load<AudioClip>($"piano_notes/{fileName}");
                    if (clip != null)
                    {
                        AudioSource.PlayClipAtPoint(clip, UnityEngine.Vector3.zero);
                    }

                    ClickMessage(selectedDate);
                };
            }

            // 添加到父容器
            contents.Add(newButton);
        }
    }

    /// <summary>
    /// 按钮点击
    /// </summary>
    /// <param name="evt"></param>
    private void ClickMessage(DateTime selectedDate)
    {
        ToggleUI(false, selectedDate, 0);

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
    /// 日历详细显示区
    /// </summary>
    /// <param name="date"></param>
    /// <param name="type">0日历 1个人</param>
    void DisplayCalendar(DateTime date, int type)
    {
        var result = CalendarData.GetChineseCalendar(date);

        //九宫背景：查日历为日九宫，查自己为时九宫
        #region 九宫背景
        // 获取日或时飞星信息
        if (type == 0) // 日
        {
            var dayStarInfo = CalendarData.GetDayStar(date); // 返回 (StarNo, isForward)
            int zIndex = dayStarInfo.StarNo;
            bool isForward = dayStarInfo.isForward;

            RenderNineStar(zIndex, isForward);
        }
        else // 时
        {
            var hourStarInfo = CalendarData.GetHourStar(date); // 返回 (hourStar, hourStars, isForward)
            int zIndex = hourStarInfo.StarNo;
            bool isForward = hourStarInfo.isForward;

            RenderNineStar(zIndex, isForward);
        }
        #endregion

        if (type == 0)
        {
            LuckTime(date);

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

            //2-1 治水
            Label labelNan1 = nan.Q<VisualElement>("Nan1").Query<Label>();
            labelNan1.text = $"{CalendarData.GetLongNiu(date)}";

            //2-2 煞
            var sha = CalendarData.GetShaFang(result.GanZhiYear, result.GanZhiDay);
            Label labelNan2 = nan.Q<VisualElement>("Nan2").Query<Label>();
            labelNan2.text = $"岁煞{sha.SuiSha}";
            labelNan2.text += $"\n劫煞{sha.JieSha}";
            labelNan2.text += $"\n灾煞{sha.ZaiSha}";

            //3,节日
            string holidayJP = CalendarData.GetJapanHoliday(date);
            var huangheiDao = CalendarData.GetHuangDaoShen(date);
            Label labelXiNan = xinan.Query<Label>().ToList()[0];
            labelXiNan.text = $"{holidayJP}";
            labelXiNan.text += $"\n{huangheiDao.Name}[{huangheiDao.Type}]";

            //4-1 星座 28宿
            Label labelDong1 = dong.Q<VisualElement>("Dong1").Query<Label>();
            var xingZuo = CalendarData.GetZodiacSign(date);
            var liuYao = CalendarData.Get6Yao(result.LunarDate);
            labelDong1.text = $"{xingZuo.Name}";
            labelDong1.text += $"\n{liuYao.Name} {liuYao.JiXiong}";

            //4-2 六曜 十二建
            Label labelDong2 = dong.Q<VisualElement>("Dong2").Query<Label>();
            var twelveGod = CalendarData.GetTwelveGodInfo(date);
            labelDong2.text = $"{CalendarData.Get28Xiu(date).Name} {CalendarData.Get28Xiu(date).Luck}";
            labelDong2.text += $"\n{twelveGod.Name} {twelveGod.Jx}";

            //5 八字 纳音 九星 林下之猪 东西四命
            List<Label> labelZhong = zhong.Query<Label>().ToList();
            labelZhong[0].text = $"{result.GanZhiYear} {CalendarData.GetNaYin(result.GanZhiYear)} {CalendarData.GetNineStarLuck("year", date).StarName}";
            labelZhong[0].text += $"\n{result.GanZhiMonth} {CalendarData.GetNaYin(result.GanZhiMonth)} {CalendarData.GetNineStarLuck("month", date).StarName}";
            labelZhong[0].text += $"\n<color=red>{result.GanZhiDay}</color> {CalendarData.GetNaYin(result.GanZhiDay)} {CalendarData.GetNineStarLuck("day", date).StarName}";
            labelZhong[0].text += $"\n{result.GanZhiTime} {CalendarData.GetNaYin(result.GanZhiTime)} {CalendarData.GetNineStarLuck("hour", date).StarName}";
            labelZhong[1].text = $"命主：{result.GanZhiDay.Substring(0, 1)}{CalendarData.GetFive(result.GanZhiDay.Substring(0, 1))}";
            labelZhong[1].text += $" {CalendarData.GetShengXiao(date)} {CalendarData.GetMingGua(date)}";

            string[] bazi = new string[] { result.GanZhiYear, result.GanZhiMonth, result.GanZhiDay, result.GanZhiTime };
            string wuxingCount = "";
            foreach (var item in CalendarData.CountFiveElements(bazi))
            {
                wuxingCount += $"{item.Key}:{item.Value} ";
            }
            labelZhong[1].text += $"\n{wuxingCount}";
            //$"{CalendarData.GetDetailedComment(CalendarData.CountFiveElements(bazi))}" //八字批注


            //6-1 神方位
            List<Label> labelXi = xi.Query<Label>().ToList();

            var shenWei = CalendarData.GetFiveShenDirections(date);

            labelXi[0].text = $"喜神 {shenWei.XiShen}";
            labelXi[0].text += $"\n贵神 {shenWei.GuiShen}";
            labelXi[0].text += $"\n财神 {shenWei.CaiShen}";

            //6-2
            labelXi[1].text = $"鹤神 {shenWei.HeShen}";
            labelXi[1].text += $"\n胎神 {shenWei.TaiShen}";

            //7，忌
            Label labelDongBei = dongbei.Query<Label>().ToList()[0];
            labelDongBei.text = $"{CalendarData.GetTwelveGodInfo(date).Ji}";
            labelDongBei.text += $"{CalendarData.Get28Xiu(date).Ji}";

            //8-1 三合
            List<Label> labelBei = bei.Query<Label>().ToList();
            labelBei[0].text = $"三合：\n{CalendarData.GetSanHe(result.GanZhiDay)}";
            labelBei[0].text += $"\n六合：\n{CalendarData.GetLiuHe(result.GanZhiDay)}";

            //8-2 邢冲害空
            labelBei[1].text = $"{CalendarData.GetChong(result.GanZhiDay)}";
            labelBei[1].text += $" {CalendarData.GetHai(result.GanZhiDay)}";
            labelBei[1].text += $"\n{CalendarData.GetXing(result.GanZhiDay)}";
            labelBei[1].text += $"\n年空 {CalendarData.GetKongWang(result.GanZhiYear)}";
            labelBei[1].text += $"\n月空 {CalendarData.GetKongWang(result.GanZhiMonth)}";
            labelBei[1].text += $"\n日空 {CalendarData.GetKongWang(result.GanZhiDay)}";

            //9,宜
            Label labelXiBei = xibei.Query<Label>().ToList()[0];
            string yg13 = CalendarData.GetYangGong13(date);
            if (yg13 != "")
            {
                labelXiBei.text = yg13;
            }
            else
            {
                labelXiBei.text = $"{CalendarData.GetTwelveGodInfo(date).Yi}";
                labelXiBei.text += $"{CalendarData.Get28Xiu(date).Yi}";
            }

            //10 显示不下这么多，轮播
            string[] jieqi24 = CalendarData.Get24JieQi(date.Year, date.Month);

            lunContent.Add($"{jieqi24[0]}:{jieqi24[1]} {jieqi24[2]}:{jieqi24[3]}");//24节气时间
            lunContent.Add($"{CalendarData.Get28Xiu(date).Sheng}");//28宿生人
            lunContent.Add($"{CalendarData.CalculateWeight(date, mzTime)}");//称骨算命
            lunContent.Add($"{CalendarData.GetBaiJi(result.GanZhiDay)}");//彭祖百忌

            labelOther.text = lunContent[0];

        }
        else
        {
            LuckTime(date);

            //命宫排序对应
            List<VisualElement> ziweiPalaces = new List<VisualElement>
            {
                calDetail.Q<VisualElement>("DongNan"),
                calDetail.Q<VisualElement>("Dong1"),
                calDetail.Q<VisualElement>("Dong2"),
                calDetail.Q<VisualElement>("DongBei"),
                calDetail.Q<VisualElement>("Bei1"),
                calDetail.Q<VisualElement>("Bei2"),
                calDetail.Q<VisualElement>("XiBei"),
                calDetail.Q<VisualElement>("Xi2"),
                calDetail.Q<VisualElement>("Xi1"),
                calDetail.Q<VisualElement>("XiNan"),
                calDetail.Q<VisualElement>("Nan2"),
                calDetail.Q<VisualElement>("Nan1")
            };
            // 解析时辰为24小时制小时
            int hour = ParseHourFromRange(mzTime);
            var res = FillZiWeiPalaces(ziweiPalaces, mzMonth, hour, result.GanZhiYear);

            Debug.Log($"命宫索引={res.Index}, 紫微在={res.ZiWei}, 五行局={res.Ju}");
        }
    }

    //记事
    void OnNoteChanged(ChangeEvent<string> evt)
    {
        PlayerPrefs.SetString(currentDateKey, evt.newValue);
        PlayerPrefs.Save();
    }

    //上一月下一月
    void ChangeMonth(int offset)
    {
        currentDate = offset switch
        {
            -1 => new DateTime(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month, 1),
            0 => DateTime.Now,
            1 => new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1),
            _ => currentDate
        };
        GenerateCalendar(currentDate.Year, currentDate.Month);
        ClickMessage(currentDate);
    }

    #region 视频广告 待改成整点时候放视频带声音
    private void PlayCurrent()
    {
        if (mediaFiles.Length == 0) return;

        string file = mediaFiles[currentIndex];
        string ext = Path.GetExtension(file).ToLower();

        CancelInvoke(nameof(NextMedia));

        if (ext == ".mp4" || ext == ".mov")
        {
            // 播放视频
            StopAllCoroutines();
            quad.SetActive(false);

            videoPlayer.url = file;
            videoPlayer.Play();
        }
        else
        {
            // 显示图片
            videoPlayer.Stop();
            quad.SetActive(true);

            StartCoroutine(ShowImage(file));
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        NextMedia();
    }

    private void NextMedia()
    {
        currentIndex++;
        if (currentIndex >= mediaFiles.Length)
        {
            currentIndex = 0;
        }
        PlayCurrent();
    }

    private System.Collections.IEnumerator ShowImage(string file)
    {
        byte[] bytes = File.ReadAllBytes(file);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);

        targetMaterial.mainTexture = tex;

        yield return new WaitForSeconds(imageDisplayTime);

        NextMedia();
    }
    #endregion

    //获取头像
    async void GetAvatar()
    {
        string[] files = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "sxpic"))
                              .Where(f => f.ToLower().EndsWith(".png")).ToArray();
        if (files.Length == 0) return;

        string ganZhi = "";
        ganZhi = CalendarData.GetChineseCalendar(DateTime.Now).GanZhiDay.Substring(1, 1);

        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Contains(ganZhi))
            {
                byte[] bytes = await File.ReadAllBytesAsync(files[i]);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                if (tex != null) openBtn.style.backgroundImage = new StyleBackground(tex);
                break;
            }
        }
    }

    /// <summary>
    /// 隐藏/显示界面
    /// </summary>
    /// <param name="showUserInput"></param>
    /// <param name="date"></param>
    /// <param name="type"></param>
    void ToggleUI(bool showUserInput, DateTime date, int type)
    {
        userInput.style.display = showUserInput ? DisplayStyle.Flex : DisplayStyle.None;
        calDetail.style.display = showUserInput ? DisplayStyle.None : DisplayStyle.Flex;
        DisplayCalendar(date, type);
        lastClickTime = Time.time;
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
                    luckTime[i].style.color= goodColor; break;
                case "平":
                    luckTime[i].style.color= normalColor; break;
                default: break;
            }
        }
    }

    /// <summary>
    /// 循环九宫数字 1~9
    /// </summary>
    /// <param name="start">起始数字</param>
    /// <param name="offset">偏移，正数顺飞，负数逆飞</param>
    /// <returns>循环后的数字</returns>
    int Wrap9(int start, int offset)
    {
        int value = start + offset;
        value = ((value - 1) % 9 + 9) % 9 + 1; // 保证 1~9 循环
        return value;
    }

    //九宫背景颜色
    private void RenderNineStar(int zIndex, bool isForward)
    {
        int[] elementShun = { 4, 8, 5, 6, 1, 7, 2, 3, 0 };
        int[] elementNi = { 4, 0, 3, 2, 7, 1, 6, 5, 8 };
        int[] elementOrder = isForward ? elementShun : elementNi;

        for (int i = 0; i < elementOrder.Length; i++)
        {
            int offset = isForward ? i : -i;
            int starNum = Wrap9(zIndex, offset);

            if (starColors.TryGetValue(starNum, out var color))
            {
                nineElement[elementOrder[i]].style.backgroundColor = color;
            }
        }
    }


    #region 紫薇斗数
    /// <summary>
    /// 将紫微斗数十二宫名称和主星填入12个VisualElement
    /// </summary>
    public static (int Index, string ZiWei, string Ju) FillZiWeiPalaces(
        List<VisualElement> palaces, int lunarMonth, int hour, string yearGanzhi)
    {
        if (palaces == null || palaces.Count != 12)
        {
            Debug.LogError("必须包含12个宫位元素");
            return (0, "", "");
        }

        // ✅ 清空所有 Label
        foreach (var palace in palaces)
        {
            var labels = palace.Query<Label>().ToList();
            foreach (var label in labels)
            {
                label.text = string.Empty;
            }
        }

        // ① 计算命宫索引
        int mingIndex = GetMingGongIndex(lunarMonth, hour);

        // ② 五行局
        string fiveElementBureau = GetFiveElementBureau(yearGanzhi);

        // ③ 紫微星位置
        int ziweiIndex = GetZiWeiIndex(mingIndex, fiveElementBureau);

        // ④ 从紫微星开始依序安14主星（逆时针）
        for (int i = 0; i < MainStars.Length; i++)
        {
            int palaceIdx = (ziweiIndex + i) % 12;
            var label = palaces[palaceIdx].Q<Label>();
            if (label != null)
            {
                label.text += (label.text == "" ? "" : "\n") + MainStars[i];
            }
        }

        // ⑤ 填入宫名（逆时针，从命宫开始）
        for (int i = 0; i < 12; i++)
        {
            int palaceIdx = (mingIndex + i) % 12;
            var label = palaces[i].Q<Label>();
            if (label != null)
            {
                label.text = $"{PalaceNames[palaceIdx]}\n{label.text}";
            }
        }

        Debug.Log($"命宫：{PalaceNames[mingIndex]}，紫微星在：{PalaceNames[ziweiIndex]}，五行局：{fiveElementBureau}");

        return (mingIndex, PalaceNames[ziweiIndex], fiveElementBureau);
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

    // 计算命宫索引
    public static int GetMingGongIndex(int lunarMonth, int hour)
    {
        int[] hourToBranch = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12 };
        int branchIndex = hourToBranch[Mathf.Clamp(hour, 0, 23)];
        int mingNumber = (lunarMonth + branchIndex) % 12;
        if (mingNumber == 0) mingNumber = 12;
        return mingNumber - 1;
    }

    // 获取五行局
    public static string GetFiveElementBureau(string yearGanzhi)
    {
        if (FiveElementTable.TryGetValue(yearGanzhi, out var bureau))
            return bureau;
        return "未知局";
    }

    // 紫微星落宫（简化偏移）
    public static int GetZiWeiIndex(int mingIndex, string fiveElementBureau)
    {
        int offset = fiveElementBureau switch
        {
            var s when s.Contains("水") => 2,
            var s when s.Contains("木") => 4,
            var s when s.Contains("火") => 6,
            var s when s.Contains("土") => 8,
            var s when s.Contains("金") => 10,
            _ => 0
        };
        return (mingIndex + offset) % 12;
    }
    #endregion
}
