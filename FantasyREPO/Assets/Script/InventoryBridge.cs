using System;
using System.Collections;
using System.Reflection;

public static class InventoryBridge
{
    /// <summary>
    /// itemID 완전일치 또는 "Copper"처럼 prefix 로 시작하면 매칭으로 간주.
    /// 프로젝트 인벤토리 구조를 건드리지 않고, 가능한 경우의 수를
    /// 반사(reflection)로 탐색해서 개수를 셉니다.
    /// </summary>
    public static int Count(string keyOrPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyOrPrefix)) return 0;

        try
        {
            // 1) 타입 찾기: "Inventory" 싱글톤을 가정(없어도 컴파일 에러 없음)
            var invType = Type.GetType("Inventory");
            if (invType == null) return 0;

            // 2) Instance / instance / s_instance 등에서 싱글톤 얻기
            object inv = GetStatic(invType, "Instance")
                       ?? GetStatic(invType, "instance")
                       ?? GetStatic(invType, "s_instance");
            if (inv == null) return 0;

            // 3) 인벤토리 측에 준비된 API가 있으면 그것부터 사용
            //    priority: Count(string) -> GetItemCount(string) -> CountByPrefix(string)
            var m = invType.GetMethod("Count", new[] { typeof(string) })
                 ?? invType.GetMethod("GetItemCount", new[] { typeof(string) })
                 ?? invType.GetMethod("CountByPrefix", new[] { typeof(string) });
            if (m != null)
                return SafeToInt(m.Invoke(inv, new object[] { keyOrPrefix }));

            // 4) 컬렉션 필드/프로퍼티를 훑어서 itemID/amount 를 가진 원소들을 합산
            int sum = 0;

            void AccumFrom(object container)
            {
                if (container == null) return;
                if (container is IEnumerable e)
                {
                    foreach (var it in e)
                        sum += AmountIfMatch(it, keyOrPrefix);
                }
            }

            // 흔한 이름들 먼저 시도
            AccumFrom(GetInst(inv, "items"));
            AccumFrom(GetInst(inv, "bag"));
            AccumFrom(GetInst(inv, "bagSlots"));
            AccumFrom(GetInst(inv, "slots"));
            AccumFrom(GetInst(inv, "quickSlots"));
            AccumFrom(GetInst(inv, "equipment"));

            // 그래도 0이면 공개된 모든 IEnumerable 필드/프로퍼티를 스캔(비용 적음)
            if (sum == 0)
            {
                foreach (var f in invType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (typeof(IEnumerable).IsAssignableFrom(f.FieldType))
                        AccumFrom(f.GetValue(inv));

                foreach (var p in invType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.CanRead)
                        AccumFrom(p.GetValue(inv, null));
            }

            return sum;
        }
        catch { return 0; }
    }

    // ---------- helpers ----------

    static object GetStatic(Type t, string name) =>
        t.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) ??
        t.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null, null);

    static object GetInst(object o, string name)
    {
        var t = o.GetType();
        return t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(o) ??
               t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(o, null);
    }

    static int SafeToInt(object v)
    {
        if (v == null) return 0;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var n)) return n;
        return 0;
    }

    /// <summary>
    /// 원소(it)가 itemID(string)와 amount(int)를 가지고 있으면,
    /// keyOrPrefix와 매칭되는 양만큼 반환.
    /// itemID 프로퍼티/필드 이름 후보: itemID, id, key
    /// amount 후보: amount, count, stack, quantity
    /// </summary>
    static int AmountIfMatch(object it, string keyOrPrefix)
    {
        if (it == null) return 0;
        var t = it.GetType();

        // 1) 바로 itemID/id/key가 붙어있는 경우
        string id = TryGetString(it, "itemID")
                 ?? TryGetString(it, "id")
                 ?? TryGetString(it, "key");

        // 2) 슬롯 형태: item / data / itemData 같은 참조 안쪽에 itemID가 있는 경우
        if (string.IsNullOrEmpty(id))
        {
            object inner = TryGetObject(it, "item")
                        ?? TryGetObject(it, "data")
                        ?? TryGetObject(it, "itemData")
                        ?? TryGetObject(it, "reference");
            if (inner != null)
            {
                id = TryGetString(inner, "itemID")
                  ?? TryGetString(inner, "id")
                  ?? TryGetString(inner, "key");
            }
        }

        if (string.IsNullOrEmpty(id)) return 0;

        bool match = id.Equals(keyOrPrefix, StringComparison.OrdinalIgnoreCase) ||
                     id.StartsWith(keyOrPrefix, StringComparison.OrdinalIgnoreCase);
        if (!match) return 0;

        // 3) 수량: 슬롯에 amount/count/stack/quantity가 있으면 그 값, 없으면 1
        object amtObj = TryGetObject(it, "amount")
                     ?? TryGetObject(it, "count")
                     ?? TryGetObject(it, "stack")
                     ?? TryGetObject(it, "quantity");

        return SafeToInt(amtObj ?? 1);
    }

    // --- 소도구 ---
    static string TryGetString(object o, string name)
    {
        var t = o.GetType();
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) { var v = f.GetValue(o); return v?.ToString(); }
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanRead) { var v = p.GetValue(o, null); return v?.ToString(); }
        return null;
    }
    static object TryGetObject(object o, string name)
    {
        var t = o.GetType();
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) return f.GetValue(o);
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanRead) return p.GetValue(o, null);
        return null;
    }
}
