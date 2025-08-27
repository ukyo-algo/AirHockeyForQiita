using UnityEngine;

public class ArcColliderWithPolygon : MonoBehaviour
{
    public float radius = 0.409f; // 1*9/11 = 0.409  (半径に対する、内円の径の比)
    public int segments = 96; // 円弧を構成するセグメント数の1/4
    public float startAngle = 0f; // 開始角度（円弧の開始角度）
    public float endAngle = 90f; // 終了角度（円弧の終了角度）

    void Start()
    {
        // PolygonCollider2Dを設定するときの座標は勝手にローカルになってるらしい
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();

        // 円弧部分の頂点数を計算
        int segmentCount = Mathf.CeilToInt(segments * (endAngle - startAngle) / 360f); 

        Vector2[] points = new Vector2[segmentCount + 1]; // 円弧の頂点segmentCount個を格納するリストを作成

        // 円弧の頂点を計算
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, (float)i / (segmentCount - 1));
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            points[i] = new Vector2(x, y);
        }

        // ポリゴンの始点と終点をつなぐための最後の点を追加
        points[segmentCount] = new Vector2( -Mathf.Sin(Mathf.Deg2Rad * startAngle) * radius, -Mathf.Cos(Mathf.Deg2Rad * endAngle) * radius);

        polygonCollider.SetPath(0, points);// ひとつめの引数は、0番目のポリゴンのぱすですよっていうことを表している
    }
}
