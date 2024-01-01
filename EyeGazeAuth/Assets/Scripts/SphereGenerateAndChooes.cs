using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SphereGenerateAndChooes : MonoBehaviour
{
    [SerializeField]
    private float radius = 0.1f;
    [SerializeField]
    private float gazeDurationThreshold = 0.1f; // 设置目光停留的阈值时间
    [SerializeField]
    private float gazeRadiusThreshold = 4f;
    private float gazeTimer = 0f; // 用于计时目光停留的时间
    private float highHead = 1.20f;

    private AuthController trackingScript;
    private GazeVisualization gazeVisualization;
    private bool Begin, oldBegin;
    private bool isUp; // 标志
    float[] angles = new float[] { 0, 10, 20, 30, 40, 50 };
    //float[] angles = new float[] { 0, 8, 16, 24, 32, 40, 48 };
    private GameObject[,] spheres; // 用于存储生成的球体对象
    private List<GameObject> selectedSpheres = new();// 已经选择的小球序列
    private List<GameObject> lineObjects = new(); // 生成的轨迹的对象
    private List<LineRenderer> drawnLines = new List<LineRenderer>();
    List<int> order = new(); // 输入的PIN的order

    private Transform t;
    private float distance;
    // 3*3 大小 以及 对应的PIN顺序
    private int index = 1;
    List<float> dis = new List<float>();
    private int numRows = 3; // 3 rows for a 3x3 grid
    private int numCols = 3; // 3 columns for a 3x3 grid
    private int cpyRow, cpyCol;
    Vector3[] points;
    // Start is called before the first frame update
    private void Awake()
    {
        t = GameObject.Find("GazeCalibration").transform;
        trackingScript = GameObject.Find("Controller").GetComponent<AuthController>();
        gazeVisualization = GameObject.Find("Controller").GetComponent<GazeVisualization>();
        gazeRadiusThreshold = Mathf.Tan(Mathf.Deg2Rad * gazeRadiusThreshold / 2); // 计算FOV值
    }
    void Start()
    {
        if (trackingScript)
        {
            Begin = trackingScript.Begin;
            index = trackingScript.indexOfSize;
        }

        foreach (var a in angles)
        {
            dis.Add(Mathf.Tan(Mathf.Deg2Rad * a) * t.position.z);
            //Debug.Log(Mathf.Tan(Mathf.Deg2Rad * a) * t.position.z);
        }

        // 初始化 spheres 数组
        spheres = new GameObject[numRows, numCols];
        // 第一次初始化(此时index为0)
        distance = dis[index];
        // 3*3中心点
        Vector3 center = new Vector3(0, highHead, t.position.z);
        // 计算其他八个点的坐标
        points = new Vector3[]
        {
            new Vector3(center.x - distance, center.y + distance, center.z),
            new Vector3(center.x, center.y + distance, center.z),
            new Vector3(center.x + distance, center.y + distance, center.z),
            new Vector3(center.x - distance, center.y, center.z),
            new Vector3(center.x + distance, center.y, center.z),
            new Vector3(center.x - distance, center.y - distance, center.z),
            new Vector3(center.x, center.y - distance, center.z),
            new Vector3(center.x + distance, center.y - distance, center.z),
            center
         };
        // 生成布局
        GenerateSphere(distance, center);
    }

    // Update is called once per frame
    void Update()
    {
        // 开始
        Begin = trackingScript.Begin;
        index = trackingScript.indexOfSize;
        if (Begin != oldBegin) ResetSpheresColor();
        if (!Begin) // 未开始
        {
            cpyCol = -1;
            cpyRow = -1; // 直接将这两个值置为不可能的值
            return;
        }
        if (trackingScript.GetEyeClose() > 0.7f) // 闭眼防止误触
        {
            cpyCol = -1;
            cpyRow = -1;
            return;
        }

        // 得到注视点
        Vector3 gazePoint = gazeVisualization.GetGazePos();
        //Debug.Log("gaze" + gazePoint);
        if (!HitTesting(gazePoint))
        {
            // hitTesting失败，返回
            return;
        }
        //hitTest成功，计算对应的序号
        int selectedRow = Mathf.Clamp(Mathf.FloorToInt((gazePoint.y - highHead + numRows * distance / 2) / distance), 0, numRows - 1);
        int selectedCol = Mathf.Clamp(Mathf.FloorToInt((gazePoint.x - t.position.x + numCols * distance / 2) / distance), 0, numCols - 1);
        //Debug.Log("row" + selectedRow + "cal" + selectedCol + "dis" + distance);
        HandleSelection(selectedRow, selectedCol);
        // 更新状态
        cpyCol = selectedCol;
        cpyRow = selectedRow;
    }

    private void LateUpdate()
    {
        isUp = trackingScript.GetIsUp();
        oldBegin = Begin;
    }

    public void GenerateSphere(float distance, Vector3 center)
    {
        List<Vector3[,]> gridPoints = new List<Vector3[,]>();
        float deltaX = distance;
        float deltaY = distance;
        Debug.Log("dis" + distance);
        Vector3[,] currentGrid = new Vector3[numRows, numCols];

        // Generate grid points
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                // Calculate the position of the point relative to the center
                float offsetX = (col - (numCols - 1) / 2.0f) * deltaX;
                float offsetY = (row - (numRows - 1) / 2.0f) * deltaY;

                // Calculate the final position of the point
                Vector3 pointPosition = center + new Vector3(offsetX, offsetY, 0);
                currentGrid[row, col] = pointPosition;
            }
        }
        gridPoints.Add(currentGrid);

        // 渲染的点
        foreach (var g in gridPoints)
        {
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    spheres[row, col] = AddSphere(g[row, col], radius, true);
                }
            }
        }
        // 碰装检测的hittest
        foreach (var g in gridPoints)
        {
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    AddSphere(g[row, col], radius, false);
                }
            }
        }
    }

    public GameObject AddSphere(Vector3 pos, float r, bool render)
    {
        // 指定球体的半径
        float radius = r;
        Vector3 position = pos;
        // 对于hittest,改变大小 计算这个点到人的距离
        if (!render)
        {
            float distanceOfSphere = Vector3.Distance(pos, new Vector3(0, (float)1.15, 0));
            radius = distanceOfSphere * gazeRadiusThreshold * 2;
        }
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = t; // 设置父物体
        sphere.transform.localScale = new Vector3(radius, radius, radius);
        sphere.transform.position = position;
        // 白色
        sphere.GetComponent<Renderer>().material.color = Color.white;
        //
        // Renderer组件，使其不可见
        if (!render)
        {
            sphere.GetComponent<Renderer>().enabled = false;
        }

        return sphere;
    }

    // 处理选中状态的方法
    private void HandleSelection(int row, int col)
    {
        if (row == cpyRow && col == cpyCol)
        {
            gazeTimer += Time.deltaTime;

            //Debug.Log("selec" + selectedSpheres.Count);
            if ((gazeTimer >= gazeDurationThreshold && selectedSpheres.Count > 0) || (selectedSpheres.Count == 0 && gazeTimer > 0.15f))
            {
                // 特判第一次选择
                if (selectedSpheres.Count == 0)
                {
                    // 将选中的球体标记为选中状态
                    spheres[row, col].GetComponent<Renderer>().material.color = Color.red;
                    // 在选中的list添加
                    selectedSpheres.Add(spheres[row, col]);
                    order.Add(row * 3 + col);
                }
                // 这个点要选择
                else
                {
                    if (IsChoosed(row, col))
                    {
                        return;
                    }
                    // 处理如果中间未选的状态
                    Vector3 start = selectedSpheres[^1].transform.position;
                    Vector3 end = spheres[row, col].transform.position;
                    int midRow, midCol;
                    // 有中简点未选择
                    if (AddMidpoint(start, end, out midRow, out midCol))
                    {
                        Choose(midRow, midCol);
                    }
                    // 选择此时触发选择的点
                    Choose(row, col);
                }
            }

        }
        // 目光不在同一区域，重置计时器
        else
        {
            gazeTimer = 0f;
        }
    }

    // 重置
    private void ResetSpheresColor()
    {
        foreach (var sphere in spheres)
        {
            sphere.GetComponent<Renderer>().material.color = Color.white;
        }

        // 清除所有以 "LineRendererObject" 命名的物体
        if (lineObjects == null) return;
        foreach (GameObject lineObject in lineObjects)
        {
            Destroy(lineObject);
        }

        lineObjects.Clear();
        drawnLines.Clear();
        selectedSpheres.Clear();
        if (order.Count > 2)
        {
            string nameStr = trackingScript.Name + "-" + trackingScript.indexOfSize.ToString() + "-" + trackingScript.indexOfPIN.ToString()
            + "-" + trackingScript.time.ToString();
            string filePath = @"E:\Desktop\data\VRAuth1\P" + trackingScript.Name.Split("-")[1] + @"\PINEntry_" + nameStr + ".txt";
            string OrderedPPIN = "";
            foreach (var o in order) OrderedPPIN = OrderedPPIN + "-" + o.ToString();
            File.WriteAllText(filePath, OrderedPPIN);
        }
        order.Clear();
    }

    private void ResetSpheresLayer()
    {
        foreach (var sphere in spheres)
        {
            Destroy(sphere);
        }
        // 初始化 spheres 数组
        spheres = new GameObject[numRows, numCols];
        // 3*3中心点
        Vector3 center = new Vector3(0, highHead, t.position.z);
        // 初始化
        distance = dis[index];
        GenerateSphere(distance, center);
        selectedSpheres.Clear();

        // 清除所有以 "LineRendererObject" 命名的物体
        if (lineObjects == null) return;
        foreach (GameObject lineObject in lineObjects)
        {
            Destroy(lineObject);
        }

        lineObjects.Clear();
        // 清空列表
        drawnLines.Clear();
    }

    // 划线
    void DrawLineBetweenSpheres(GameObject sphere1, GameObject sphere2)
    {
        // 创建一个新的空物体，将其作为 LineRenderer 的容器
        GameObject lineObject = new GameObject("LineRendererObject");
        lineObjects.Add(lineObject);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // 设置 LineRenderer 的相关属性，比如颜色、宽度等
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        lineRenderer.SetPosition(0, sphere1.transform.position);
        lineRenderer.SetPosition(1, sphere2.transform.position);
        // 创建一个新的材质
        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.color = Color.red;
        lineRenderer.material = newMaterial;

        // 将新创建的 LineRenderer 添加到列表中
        drawnLines.Add(lineRenderer);
    }

    public bool HitTesting(Vector3 gazePoint)
    {
        //foreach (Vector3 point in points)
        //{
        //    // 计算屏幕空间中的点到输入位置的距离
        //    float distanceToInput = Vector2.Distance(new Vector2(gazePoint.x, gazePoint.y), new Vector2(point.x, point.y));
        //    //Debug.Log("distance-Input" + distanceToInput + "dis" + distance);

        //    // 进行命中测试， 计算阈值（tan)
        //    if (distanceToInput <= gazeRadiusThreshold)
        //    {
        //        return true;
        //    }
        //}
        //return false;
        if (gazePoint.x + gazePoint.y + gazePoint.z != 0) return true;
        return false;
    }

    // 判断是否在中点位置
    public bool AddMidpoint(Vector3 start, Vector3 end, out int midRow, out int midCol)
    {
        Vector3 midpoint = (start + end) / 2f;
        // 遍历sphere
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                // 这个点在线上但未选择
                if (Vector3.Distance(midpoint, spheres[row, col].transform.position) < 0.001f)
                {
                    midRow = row;
                    midCol = col;
                    return true;
                }
            }
        }
        midRow = -1;
        midCol = -1;
        return false;
    }

    // 判断一个点是否输入过
    public bool IsChoosed(int row, int col)
    {
        // 防止一个点重复输入
        foreach (var s in selectedSpheres)
        {
            if (Vector3.Distance(spheres[row, col].transform.position, s.transform.position) < 0.0001f)
                return true;
        }
        return false;
    }

    public void Choose(int row, int col)
    {
        // 将选中的球体标记为选中状态
        spheres[row, col].GetComponent<Renderer>().material.color = Color.red;
        // 在选中的list添加
        selectedSpheres.Add(spheres[row, col]);
        order.Add(row * 3 + col);
        //Debug.Log("row" + row + "col" + col);
        // 划线
        if (selectedSpheres.Count < 15)
        {
            DrawLineBetweenSpheres(selectedSpheres[^1], selectedSpheres[^2]);

        }
    }

}
