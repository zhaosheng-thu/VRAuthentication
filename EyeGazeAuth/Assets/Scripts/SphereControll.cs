using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereControll : MonoBehaviour
{
    private GameObject[] spheres;  // 用于存储要展示球体的数组
    private int currentIndex = 0;  // 当前显示的球体索引
    private int indexOfPin = 1;
    private List<List<int>> password = new();
    private bool begin, init;
    private AuthController trackingScript;
    private float timeWait = 0.3f;

    private List<GameObject> lineObjects = new(); // 生成的轨迹的对象
    private List<LineRenderer> drawnLines = new List<LineRenderer>(); // 划线

    void Start()
    {
        List<int> ps1 = new List<int> { 6, 7, 8, 5, 2 };
        List<int> ps2 = new List<int> { 6, 7, 8, 5 };
        List<int> ps3 = new List<int> { 6, 7, 8, 4 };
        List<int> ps4 = new List<int> { 6, 7, 4, 5 };
        List<int> ps5 = new List<int> { 6, 4, 2, 5 };
        List<int> ps6 = new List<int> { 0, 4, 8, 5, 2 };
        List<int> ps7 = new List<int> { 6, 3, 0, 1, 2 };
        List<int> ps8 = new List<int> { 8, 4, 0, 1, 2 };
        List<int> ps9 = new List<int> { 6, 7, 8, 4, 0 };
        List<int> ps10 = new List<int> { 3, 0, 1, 2 };
        List<int> ps11 = new List<int> { 6, 7, 4, 1, 2 };
        List<int> ps12 = new List<int> { 7, 4, 1, 2 };
        List<int> ps13 = new List<int> { 6, 7, 8, 4, 0, 1, 2 };
        List<int> ps14 = new List<int> { 6, 4, 2, 5, 8 };
        List<int> ps15 = new List<int> { 7, 3, 0, 1, 2, 5 };
        List<int> ps16 = new List<int> { 6, 7, 8, 5, 2, 1, 0 };
        List<int> ps17 = new List<int> { 6, 4, 2, 1 };
        List<int> ps18 = new List<int> { 4, 8, 5, 2, 3 };
        password.Add(ps1);
        password.Add(ps2);
        password.Add(ps3);
        password.Add(ps4);
        password.Add(ps5);
        password.Add(ps6);
        password.Add(ps7);
        password.Add(ps8);
        password.Add(ps9);
        password.Add(ps10);
        password.Add(ps11);
        password.Add(ps12);
        password.Add(ps13);
        password.Add(ps14);
        password.Add(ps15);
        password.Add(ps16);
        password.Add(ps17);
        password.Add(ps18);

        trackingScript = GameObject.Find("Controller").GetComponent<AuthController>();//获取controller
        this.begin = trackingScript.Begin;
        this.init = true;
        indexOfPin = trackingScript.indexOfPIN; // 初始为0
        
    }

    private void Update()
    {
        this.begin = trackingScript.Begin;
        indexOfPin = trackingScript.indexOfPIN;
        if (!begin && init)
        {
            Init();
            //foreach (var s in spheres)
            //{
            //    s.SetActive(false);
            //}
            // 获取第一个球体的Renderer组件
            Renderer currentRenderer = spheres[0].GetComponent<Renderer>();
            currentRenderer.material.color = Color.green;
            //spheres[0].SetActive(true);
            Debug.Log("envoke" + indexOfPin);
            // 启动定时器，每隔 调用ShowNextSphere方法
            InvokeRepeating("ShowNextSphere", timeWait, timeWait);
            init = false;
        }
        else if (begin)
        {
            if (!init) // 防止反复调用
            {
                // 清除所有划线用的的物体
                if (lineObjects == null) return;
                foreach (GameObject lineObject in lineObjects)
                {
                    Destroy(lineObject);
                }
                // 清空列表
                lineObjects.Clear();
                drawnLines.Clear();
            }
            init = true;
            CancelInvoke("ShowNextSphere");
            CancelInvoke("RestartShow");
        }
    }

    void ShowNextSphere()
    {

        //spheres[currentIndex].SetActive(false);
        Renderer currentRenderer = spheres[currentIndex].GetComponent<Renderer>();
        currentRenderer.material.color = Color.white;
        // 遍历完毕，准备下次
        if (currentIndex + 1 == spheres.Length)
        {
            // 清除所有划线用的的物体
            if (lineObjects == null) return;
            foreach (GameObject lineObject in lineObjects)
            {
                Destroy(lineObject);
            }
            // 清空列表
            lineObjects.Clear();
            drawnLines.Clear();
            // 停止定时器
            CancelInvoke("ShowNextSphere");
            // 等待  秒后重新开始
            Invoke("RestartShow", 2f);
        }
        if (begin)
        {
            CancelInvoke("RestartShow");
            CancelInvoke("ShowNextSphere");
        }
        currentIndex = (currentIndex + 1) % (spheres.Length);
        //Debug.Log("current" + currentIndex);
        //spheres[currentIndex].SetActive(true);
        // 获取当前球体的Renderer组件
        currentRenderer = spheres[currentIndex].GetComponent<Renderer>();
        currentRenderer.material.color = Color.green;
        // 划线
        if (currentIndex > 1) DrawLineBetweenSpheres(spheres[currentIndex - 1], spheres[currentIndex]);
    }

    void RestartShow()
    {
        // 重新开始定时器
        currentIndex = 0;
        //spheres[currentIndex].SetActive(true);
        //// 获取当前球体的Renderer组件
        //Renderer currentRenderer = spheres[currentIndex].GetComponent<Renderer>();
        //currentRenderer.material.color = Color.gray;
        InvokeRepeating("ShowNextSphere", timeWait, timeWait);
    }

    void Init()
    {
        List<int> psNow = password[indexOfPin];
        spheres = new GameObject[psNow.Count + 1]; // 第一个放空
        spheres[0] = transform.GetChild(0).gameObject;
        for (int i = 1; i < psNow.Count + 1; i++)
        {
            spheres[i] = transform.GetChild(psNow[i - 1] + 1).gameObject; // 寻找对应的球体
        }
    }

    // 划线
    void DrawLineBetweenSpheres(GameObject sphere1, GameObject sphere2)
    {
        // 创建一个新的空物体，将其作为 LineRenderer 的容器
        GameObject lineObject = new GameObject("LineRendererObject");
        lineObjects.Add(lineObject);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // 设置 LineRenderer 的相关属性，比如颜色、宽度等
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        lineRenderer.SetPosition(0, sphere1.transform.position);
        lineRenderer.SetPosition(1, sphere2.transform.position);
        // 创建一个新的材质
        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.color = Color.green;
        lineRenderer.material = newMaterial;

        // 将新创建的 LineRenderer 添加到列表中
        drawnLines.Add(lineRenderer);
    }

}
