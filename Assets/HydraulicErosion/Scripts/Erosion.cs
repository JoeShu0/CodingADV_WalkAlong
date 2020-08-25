using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Erosion : MonoBehaviour
{
    [Header("Temp")]
    public Vector2 Position = new Vector2(500.001f, 500.001f); //zx plane

    [Header("Erosion Settings")]
    [Range(0, 1)]
    public float P_inertia = 0.4f;
    [Range(0, 1)]
    public float P_minslope = 0.01f;
    public float P_capacity = 2.0f;
    [Range(0, 1)]
    public float P_evaporation = 0.05f;
    [Range(0, 1)]
    public float P_deposition = 0.1f;
    [Range(0, 1)]
    public float P_erosion = 0.01f;
    public float P_eradius = 4.0f;
    public float P_dradius = 4.0f;
    public int P_maxPatStep = 500;
    public float P_gravity = -9.8f;

    [SerializeField]
    struct WaterDrop
    {
        public Vector2 Pos;
        public float Vel;
        public Vector2 Dir;
        public float Water;
        public float Sediment;
        //public float DyCapacity;
    };
    [SerializeField]
    struct IndexedWeight
    {
        public int Index;
        public float weight;
    };

    public void Erode(ref float[] Height, int HeightMapSize, int DropNum)
    {
        Vector2 DropPosition = new Vector2((float)Random.Range(0, HeightMapSize), (float)Random.Range(0, HeightMapSize));

        WaterDrop wd;
        wd.Pos = DropPosition;
        wd.Vel = 0.1f;
        wd.Dir = new Vector2(1.0f, 0.0f);
        wd.Water = 1.0f;
        wd.Sediment = 0.0f;
        //wd.DyCapacity = 0.0f;

        float MaxRadius = Mathf.Max(P_eradius, P_dradius);

        for (int i = 0; i < P_maxPatStep; i++)
        {
            if (wd.Pos.x + MaxRadius > HeightMapSize || wd.Pos.y + MaxRadius > HeightMapSize
                || wd.Pos.x - MaxRadius < 0 || wd.Pos.y - MaxRadius < 0)
            { break; }

            Vector2 gSlope = CalSlopDir(Height, HeightMapSize, wd.Pos);

            Vector2 Dir_new = (wd.Dir * P_inertia + gSlope * (1 - P_inertia)).normalized;
            Vector2 Pos_new = wd.Pos + Dir_new;
            float Sediment_new = 0.0f;

            //Debug.Log("New Pos is: " + Pos_new);
            

            float H_dif = GetHeight(Height, HeightMapSize, Pos_new) - GetHeight(Height, HeightMapSize, wd.Pos);
            if (H_dif >= 0)
            {
                //We just run over a pit, fill it with sediment
                float droppedSediment = Mathf.Min(H_dif, wd.Sediment);
                List<IndexedWeight> DepositionWeights = GetErosiondWeights(HeightMapSize, wd.Pos, P_dradius);
                ApplyHeightChange(ref Height, DepositionWeights, droppedSediment);
                Sediment_new = wd.Sediment - droppedSediment;
            }
            else
            {
                //down hil
                float CurrentCapacity = Mathf.Max(-H_dif, P_minslope) * wd.Vel * wd.Water * P_capacity;
                if (wd.Sediment > CurrentCapacity)
                {
                    //Carrys to much
                    float droppedSediment = (wd.Sediment - CurrentCapacity) * P_deposition;
                    List<IndexedWeight> DepositionWeights = GetErosiondWeights(HeightMapSize, wd.Pos, P_dradius);
                    ApplyHeightChange(ref Height, DepositionWeights, droppedSediment);
                    Sediment_new = wd.Sediment - droppedSediment;
                }
                else
                {
                    //Not full capacity yet
                    float carrySediment = Mathf.Min((CurrentCapacity - wd.Sediment) * P_erosion, -H_dif);
                    List<IndexedWeight> ErosionWeights = GetErosiondWeights(HeightMapSize, wd.Pos, P_eradius);
                    ApplyHeightChange(ref Height, ErosionWeights, -carrySediment);
                    Sediment_new = wd.Sediment + carrySediment;
                }

            }
            float Vel_new = Mathf.Sqrt(wd.Vel * wd.Vel + H_dif * P_gravity);
            float Water_new = wd.Water * (1 - P_evaporation);
            
            wd.Pos = Pos_new;
            wd.Vel = Vel_new;
            wd.Dir = Dir_new;
            wd.Water = Water_new;
            wd.Sediment = Sediment_new;

            
        }
    }

    float GetHeight(float[] Height, int HeightMapSize, Vector2 Pos)
    {
        int top = Mathf.FloorToInt(Pos.x);
        int left = Mathf.FloorToInt(Pos.y);

        float HTL = Height[top * HeightMapSize + left];
        float HTR = Height[top * HeightMapSize + left + 1];
        float HBL = Height[(top + 1) * HeightMapSize + left];
        float HBR = Height[(top + 1) * HeightMapSize + left + 1];

        return Mathf.Lerp(Mathf.Lerp(HTL, HTR, Pos.y - left), Mathf.Lerp(HBL, HBR, Pos.y - left), Pos.x - top);
    }

    Vector2 CalSlopDir(float[] Height, int HeightMapSize, Vector2 Pos)
    {
        int top = Mathf.FloorToInt(Pos.x);
        int left = Mathf.FloorToInt(Pos.y);

        float HTL = Height[top * HeightMapSize + left];
        float HTR = Height[top * HeightMapSize + left + 1];
        float HBL = Height[(top + 1) * HeightMapSize + left];
        float HBR = Height[(top + 1) * HeightMapSize + left + 1];


        Vector2 Dir =  - new Vector2((HBL - HTL) * (left + 1 - Pos.y) + (HBR - HTR) * (Pos.y - left),
                                  (HTR - HTL) * (top + 1 - Pos.x) + (HBR - HBL) * (Pos.x - top));

        /* 
        float PointHeight = GetHeight(Height, HeightMapSize, Pos);

        Vector3 Point = new Vector3(
                            Pos.x * 0.1f,
                            (PointHeight - 0.5f)*50.0f,
                            Pos.y * 0.1f);
        */
        //Debug.DrawRay(Point, new Vector3(0, 1, 0));

        //Debug.DrawLine(Point,Point + new Vector3(Dir.normalized.x, 0, Dir.normalized.y),Color.cyan);

        return Dir.normalized;
    }

    List<IndexedWeight> GetErosiondWeights(int HeightMapSize, Vector2 Pos, float P_eradius)
    {
        int top = Mathf.FloorToInt(Pos.x - (P_eradius - 1));
        int left = Mathf.FloorToInt(Pos.y - (P_eradius - 1));

        List<IndexedWeight> PointWeights = new List<IndexedWeight>();
        float WeightSum = 0;

        for (int i = top; i < top + P_eradius * 2; i++)
            for (int n = left; n < left + P_eradius * 2; n++)
            {
                float dist = (new Vector2(i, n) - Pos).magnitude;
                if (dist < P_eradius)
                {
                    IndexedWeight IW = new IndexedWeight();
                    IW.Index = i * HeightMapSize + n;
                    IW.weight = P_eradius - dist;
                    WeightSum += IW.weight;
                    PointWeights.Add(IW);
                }
            }

        for (int i = 0; i < PointWeights.Count; i++)
        {
            float Normalizedweight = PointWeights[i].weight / WeightSum;
            IndexedWeight IW = new IndexedWeight();
            IW.Index = PointWeights[i].Index;
            IW.weight = Normalizedweight;
            PointWeights[i] = IW;
        }

        return PointWeights;

    }

    List<IndexedWeight> GetDepositionWeights(int HeightMapSize, Vector2 Pos)
    {
        int top = Mathf.FloorToInt(Pos.x);
        int left = Mathf.FloorToInt(Pos.y);

        List<IndexedWeight> PointWeights = new List<IndexedWeight>();
        float WeightSum = 0;

        for (int i = top; i < top + 1; i++)
            for (int n = left; n < left + 1; n++)
            {
                float dist = (new Vector2(i, n) - Pos).magnitude;
                {
                    IndexedWeight IW = new IndexedWeight();
                    IW.Index = i * HeightMapSize + n;
                    IW.weight = 2.0f - dist;
                    WeightSum += IW.weight;
                    PointWeights.Add(IW);
                }
            }

        for (int i = 0; i < PointWeights.Count; i++)
        {
            float Normalizedweight = PointWeights[i].weight / WeightSum;
            IndexedWeight IW = new IndexedWeight();
            IW.Index = PointWeights[i].Index;
            IW.weight = Normalizedweight;
            PointWeights[i] = IW;
        }

        return PointWeights;
    }

    void ApplyHeightChange(ref float[] Height, List<IndexedWeight> Weights, float Sediment)
    {
        for (int i = 0; i < Weights.Count; i++)
        {
            Height[Weights[i].Index] += Weights[i].weight * Sediment;
        }
    }
}
