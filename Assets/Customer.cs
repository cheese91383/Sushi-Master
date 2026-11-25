using UnityEngine;
using UnityEngine.Events;

public enum OrderType { Sashimi, Sushi, Tea }
public enum Quality { Fail, Normal, Perfect }
public enum CustomerState { Arriving, InLine, Ordering, Leaving }

[System.Serializable]
public class Order
{
    public OrderType type;
    public string topping;
    public bool wantsWasabi;
    public bool wantsSoy;
    public int price = 100;

    static readonly string[] sashimiToppings = { "salmon", "tuna", "tamago" };
    static readonly string[] sushiToppings = { "salmon", "tuna", "tamago", "ebi", "unagi", "ika" };

    public static Order CreateRandom()
    {
        var o = new Order();
        o.type = (OrderType)Random.Range(0, 3);
        switch (o.type)
        {
            case OrderType.Sashimi:
                o.topping = Pick(sashimiToppings);
                o.price = 120;
                break;
            case OrderType.Sushi:
                o.topping = Pick(sushiToppings);
                o.wantsWasabi = Random.value < 0.5f;
                o.wantsSoy = Random.value < 0.5f;
                o.price = 150;
                break;
            case OrderType.Tea:
                o.topping = "";
                o.price = 60;
                break;
        }
        return o;
    }

    static string Pick(string[] a) { return a[Random.Range(0, a.Length)]; }
}

public class Customer : MonoBehaviour
{
    public Order order;
    public float patience = 45f;           // 在櫃檯前能等幾秒
    public float moveSpeed = 2f;
    public CustomerState state = CustomerState.Arriving;

    public UnityEvent<Customer, int> onPaid = new UnityEvent<Customer, int>();
    public UnityEvent<Customer> onLeftAngry = new UnityEvent<Customer>();

    float timer;
    Transform target;

    void Update()
    {
        if (target != null)    // 先用直線移動，之後再換成 NavMesh 走路
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, 5f * Time.deltaTime);
        }

        // 排隊時耐心掉一半速度，輪到點餐才全速
        float rate = state == CustomerState.Ordering ? 1f : state == CustomerState.InLine ? 0.5f : 0f;
        if (rate > 0f)
        {
            timer += Time.deltaTime * rate;
            if (timer >= patience) LeaveAngry();
        }
    }

    public void MoveTo(Transform t) { target = t; }

    // 排第 0 位的才是點餐中，其餘是排隊
    public void SetActiveOrder(bool active)
    {
        if (state == CustomerState.Leaving) return;
        state = active ? CustomerState.Ordering : CustomerState.InLine;
    }

    // 三個關卡做完以後呼叫這個，告訴它品質
    public void Serve(Quality q)
    {
        if (state != CustomerState.Ordering) return;
        state = CustomerState.Leaving;
        float ratio = q == Quality.Perfect ? 1f : q == Quality.Normal ? 0.5f : 0f;   // 完美全額、普通一半、失敗沒錢
        onPaid.Invoke(this, Mathf.RoundToInt(order.price * ratio));
    }

    void LeaveAngry()
    {
        if (state == CustomerState.Leaving) return;
        state = CustomerState.Leaving;
        onLeftAngry.Invoke(this);
    }
}