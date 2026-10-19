using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

public class FieldRecord
{
    public float value;
    public float min = float.MaxValue, max = float.MinValue;
    private double sum = 0;
    private int count = 0;
    public double average;

    public void Update(float v)
    {
        value = v;
        min = MathF.Min(min, value);
        max = MathF.Max(max, value);
        sum += v;
        count++;
        average = sum / count;
    }
}

// 一个字段的 4 个显示 Label，缓存起来，避免每 tick 都 Q<>
public class FieldView
{
    public Label cur, max, min, ave;
    public float lastMax = float.NaN;   // min/max 变了才重写
    public float lastMin = float.NaN;
}

public class ECSvsMono_TestTool : EditorWindow
{
    private VisualElement root;

    //字段
    private FieldRecord deltaTime = new FieldRecord();//帧时间
    private FieldView dtView;
    private FieldRecord FPS = new FieldRecord();//帧率
    private FieldView fpsView;

    private FieldRecord allocPerFrame = new FieldRecord();//本帧分配(KB)
    private FieldView allocView;
    private FieldRecord gcGen0 = new FieldRecord();//GC第0代/帧
    private FieldView gcGen0View;
    private FieldRecord gcGen1 = new FieldRecord();//GC第1代/帧
    private FieldView gcGen1View;
    private FieldRecord gcGen2 = new FieldRecord();//GC第2代/帧
    private FieldView gcGen2View;
    private FieldRecord monoUsed = new FieldRecord();//Mono堆已用(MB)
    private FieldView monoUsedView;
    private FieldRecord monoTotal = new FieldRecord();//Mono堆总大小(MB)
    private FieldView monoTotalView;
    private FieldRecord totalAlloc = new FieldRecord();//累计分配(MB)
    private FieldView totalAllocView;
    private FieldRecord totalReserved = new FieldRecord();//保留内存(MB)
    private FieldView totalReservedView;

    //累计量，用于算每帧差值
    private long lastAlloc;
    private int lastGen0, lastGen1, lastGen2;

    const float MB = 1024f * 1024f;

    [MenuItem("Tools/ECSvsMono_TestTool")]
    public static void Show()
    {
        ECSvsMono_TestTool wnd = GetWindow<ECSvsMono_TestTool>();
        wnd.titleContent = new GUIContent("ECSvsMono_TestTool");
    }

    void OnEnable()
    {
        EditorApplication.update += OnTick;   // 只挂一个
    }

    void OnDisable()
    {
        EditorApplication.update -= OnTick;   // 只摘一个
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        dtView = AddBlock("帧时间");
        fpsView = AddBlock("FPS");
        allocView = AddBlock("本帧分配 KB");
        gcGen0View = AddBlock("GC Gen0/帧");
        gcGen1View = AddBlock("GC Gen1/帧");
        gcGen2View = AddBlock("GC Gen2/帧");
        monoUsedView = AddBlock("Mono堆已用 MB");
        monoTotalView = AddBlock("Mono堆总大小 MB");
        totalAllocView = AddBlock("累计分配 MB");
        totalReservedView = AddBlock("保留内存 MB");

        Button refreshBtn = new Button();
        refreshBtn.style.height = 50;
        refreshBtn.style.width = 150;
        refreshBtn.text = "刷新";
        refreshBtn.clicked += Refresh;
        root.Add(refreshBtn);

        // 初始化累计量，避免第一帧算出一个"从编辑器启动到现在"的巨值
        lastAlloc = Profiler.GetTotalAllocatedMemoryLong();
        lastGen0 = GC.CollectionCount(0);
        lastGen1 = GC.CollectionCount(1);
        lastGen2 = GC.CollectionCount(2);
    }

    private void Refresh()
    {
        deltaTime = new FieldRecord();//帧时间
        FPS = new FieldRecord();//帧率
        allocPerFrame = new FieldRecord();//本帧分配(KB)
        gcGen0 = new FieldRecord();//GC第0代/帧
        gcGen1 = new FieldRecord();//GC第1代/帧
        gcGen2 = new FieldRecord();//GC第2代/帧
        monoUsed = new FieldRecord();//Mono堆已用(MB)
        monoTotal = new FieldRecord();//Mono堆总大小(MB)
        totalAlloc = new FieldRecord();//累计分配(MB)
        totalReserved = new FieldRecord();//保留内存(MB)
    }

    private void OnTick()
    {
        if (root == null || dtView == null) return;   // CreateGUI 之前

        float dt = Time.unscaledDeltaTime;

        deltaTime.Update(dt);
        WriteRecord(dtView, deltaTime);

        FPS.Update(dt > 0f ? 1f / dt : 0f);
        WriteRecord(fpsView, FPS);

        long total = Profiler.GetTotalAllocatedMemoryLong();
        allocPerFrame.Update((total - lastAlloc) / 1024f);
        lastAlloc = total;
        WriteRecord(allocView, allocPerFrame);

        int g0 = GC.CollectionCount(0);
        gcGen0.Update(g0 - lastGen0); lastGen0 = g0;
        WriteRecord(gcGen0View, gcGen0);

        int g1 = GC.CollectionCount(1);
        gcGen1.Update(g1 - lastGen1); lastGen1 = g1;
        WriteRecord(gcGen1View, gcGen1);

        int g2 = GC.CollectionCount(2);
        gcGen2.Update(g2 - lastGen2); lastGen2 = g2;
        WriteRecord(gcGen2View, gcGen2);

        monoUsed.Update(Profiler.GetMonoUsedSizeLong() / MB);
        WriteRecord(monoUsedView, monoUsed);

        monoTotal.Update(Profiler.GetMonoHeapSizeLong() / MB);
        WriteRecord(monoTotalView, monoTotal);

        totalAlloc.Update(Profiler.GetTotalAllocatedMemoryLong() / MB);
        WriteRecord(totalAllocView, totalAlloc);

        totalReserved.Update(Profiler.GetTotalReservedMemoryLong() / MB);
        WriteRecord(totalReservedView, totalReserved);
    }

    private void WriteRecord(FieldView v, FieldRecord rec)
    {
        // value / average 每 tick 都变，直接写
        v.cur.text = rec.value.ToString("F2");
        v.ave.text = rec.average.ToString("F2");

        // min / max 很少变，变了才 ToString + 写
        if (rec.max != v.lastMax) { v.lastMax = rec.max; v.max.text = rec.max.ToString("F2"); }
        if (rec.min != v.lastMin) { v.lastMin = rec.min; v.min.text = rec.min.ToString("F2"); }
    }

    private FieldView AddBlock(string label)
    {
        VisualElement blockUI = new VisualElement();
        blockUI.style.flexDirection = FlexDirection.Row;

        Label labelUI = new Label(label + ":");
        blockUI.Add(labelUI);

        VisualElement fieldUI = new VisualElement();
        fieldUI.style.flexDirection = FlexDirection.Row;
        blockUI.Add(fieldUI);

        FieldView view = new FieldView();
        view.cur = MakeValueLabel();
        view.max = MakeValueLabel();
        view.min = MakeValueLabel();
        view.ave = MakeValueLabel();

        fieldUI.Add(view.cur);
        fieldUI.Add(view.max);
        fieldUI.Add(view.min);
        fieldUI.Add(view.ave);

        root.Add(blockUI);
        return view;
    }

    private Label MakeValueLabel()
    {
        Label l = new Label();
        l.style.unityTextAlign = TextAnchor.MiddleRight;
        return l;
    }
}
