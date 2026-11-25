using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("關卡設定")]
    public int totalCustomers = 10;
    public int maxWaiting = 3;                       // 櫃檯那位以外，後面最多排幾位
    public float timeLimit = 480f;
    [Range(0.4f, 0.9f)] public float arriveWithin = 0.7f;   // 全部客人要在前 70% 時間內到齊

    [Header("場景參照")]
    public Customer[] customerPrefabs;      // 把所有不同客人的 Prefab 都放進來
    public Transform entrance;
    public Transform[] lineSpots;                    // 0 = 櫃檯前（點餐位），1~3 = 後面排隊

    public int Spawned { get; private set; }
    public int Finished { get; private set; }
    public int Earned { get; private set; }
    public int MaxPossible { get; private set; }
    public float TimeLeft { get; private set; }

    readonly List<Customer> line = new List<Customer>();
    float nextSpawnAt;
    bool running;

    public void StartLevel()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0 || entrance == null || lineSpots == null || lineSpots.Length == 0)
        {
            Debug.LogError("LevelManager：請先指定 Customer Prefabs、Entrance、Line Spots");
            return;
        }
        TimeLeft = timeLimit;
        Spawned = Finished = Earned = MaxPossible = 0;
        running = true;
        ScheduleNext();
    }

    void Update()
    {
        if (!running) return;
        TimeLeft -= Time.deltaTime;
        TrySpawn();
        if (TimeLeft <= 0f || Finished >= totalCustomers) EndLevel();
    }

    // 把「到齊的截止時間」剩下的時間，平均分給還沒來的客人
    void ScheduleNext()
    {
        int remaining = Mathf.Max(totalCustomers - Spawned, 1);
        float elapsed = timeLimit - TimeLeft;
        float window = Mathf.Max(timeLimit * arriveWithin - elapsed, 1f);
        nextSpawnAt = elapsed + window / remaining;
    }

    void TrySpawn()
    {
        if (Spawned >= totalCustomers) return;
        if (timeLimit - TimeLeft < nextSpawnAt) return;
        if (line.Count >= 1 + maxWaiting) return;    // 隊伍滿了，客人在門外多等，空出位置才進來
        SpawnOne();
        ScheduleNext();
    }

    void SpawnOne()
    {
        var c = Instantiate(NextPrefab(), entrance.position, entrance.rotation);
        c.order = Order.CreateRandom();              // 你們再寫：隨機產生點餐內容
        c.onPaid.AddListener(OnCustomerPaid);
        c.onLeftAngry.AddListener(c2 => OnCustomerPaid(c2, 0));
        line.Add(c);
        Spawned++;
        RefreshLinePositions();
    }

    void OnCustomerPaid(Customer c, int paid)
    {
        Earned += paid;
        MaxPossible += c.order.price;
        Finished++;
        line.Remove(c);
        Destroy(c.gameObject, 2f);                   // 離開動畫之後再刪
        RefreshLinePositions();                       // 後面的人往前移一格
    }

    // 排在第 0 位的是正在點餐的，其他的依序站後面
    void RefreshLinePositions()
    {
        for (int i = 0; i < line.Count && i < lineSpots.Length; i++)
        {
            line[i].MoveTo(lineSpots[i]);             // 先用直接瞬移，之後再換成 NavMesh 走路
            line[i].SetActiveOrder(i == 0);           // 只有第 0 位在點餐、開始計算耐心
        }
    }

    // 讓排第一位（正在點餐）的客人收到餐
    public void ServeFront(Quality q)
    {
        if (line.Count > 0) line[0].Serve(q);
    }

    void EndLevel()
    {
        running = false;
        Debug.Log($"結束：賺了 {Earned}，滿分 {MaxPossible}，星級 {Stars()}");
        // 這裡再接結算畫面
    }

    readonly List<Customer> bag = new List<Customer>();

    // 洗牌袋：每種客人都出現過一輪，才會開始重複，十位客人裡不會連著都是同一個
    Customer NextPrefab()
    {
        if (bag.Count == 0)
        {
            bag.AddRange(customerPrefabs);
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = bag[i]; bag[i] = bag[j]; bag[j] = tmp;
            }
        }
        var p = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        return p;
    }

    public int Stars()
    {
        float ratio = MaxPossible > 0 ? (float)Earned / MaxPossible : 0f;
        return ratio >= 0.9f ? 3 : ratio >= 0.6f ? 2 : ratio >= 0.3f ? 1 : 0;
    }
}