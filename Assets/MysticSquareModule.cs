using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MysticSquareModule : MonoBehaviour
{
    public KMSelectable[] buttons;
    public Transform[] visibleButtons;
    public Transform emptyField;

    int[] field = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
    int skull, knight, winCond;
    bool inDanger;

    static int[,] table = new int[8, 8] {
        { 1, 3, 5, 4, 6, 7, 2, 8 },
        { 2, 5, 7, 3, 8, 1, 4, 6 },
        { 6, 4, 8, 1, 7, 3, 5, 2 },
        { 8, 1, 2, 5, 3, 4, 6, 7 },
        { 3, 2, 6, 8, 4, 5, 7, 1 },
        { 7, 6, 1, 2, 5, 8, 3, 4 },
        { 4, 7, 3, 6, 1, 2, 8, 5 },
        { 5, 8, 4, 7, 2, 6, 1, 3 }
    };

    void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;
            if (buttons[i].gameObject.name != "empty")
                buttons[i].OnInteract += delegate { OnPress(j); return false; };
        }

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        inDanger = true;
        skull = 0;
        knight = 0;

        initArray();

        // Find winning condition
        winCond = 0;
        if (topSum() > horMidSum() && topSum() > bottomSum())
            winCond += 0;
        else if (horMidSum() > topSum() && horMidSum() > bottomSum())
            winCond += 10;
        else if (bottomSum() > topSum() && bottomSum() > horMidSum())
            winCond += 20;
        else
            winCond += 30;
        if (leftSum() > verMidSum() && leftSum() > rightSum())
            winCond += 0;
        else if (verMidSum() > leftSum() && verMidSum() > rightSum())
            winCond += 1;
        else if (rightSum() > leftSum() && rightSum() > verMidSum())
            winCond += 2;
        else
            winCond += 3;

        // Set start field
        int[] start = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
        Vector3 dif;

        for (int i = 0; i <= 8; i++)
        {
            for (int j = 0; j <= 8; j++)
            {
                if (start[j] == field[i])
                {
                    if (start[j] == 0)
                    {
                        if (start[i] != 0)
                        {
                            dif = visibleButtons[start[i] - 1].gameObject.transform.position - emptyField.transform.position;
                            visibleButtons[start[i] - 1].gameObject.transform.Translate(-dif, Space.World);
                            emptyField.transform.Translate(dif, Space.World);
                        }
                    }
                    else if (start[i] != 0)
                    {
                        dif = visibleButtons[start[i] - 1].gameObject.transform.position - visibleButtons[start[j] - 1].gameObject.transform.position;
                        visibleButtons[start[i] - 1].gameObject.transform.Translate(-dif, Space.World);
                        visibleButtons[start[j] - 1].gameObject.transform.Translate(dif, Space.World);
                    }
                    else
                    {
                        dif = emptyField.transform.position - visibleButtons[start[j] - 1].gameObject.transform.position;
                        emptyField.transform.Translate(-dif, Space.World);
                        visibleButtons[start[j] - 1].gameObject.transform.Translate(dif, Space.World);
                    }

                    var temp = start[j];
                    start[j] = start[i];
                    start[i] = temp;
                }
            }
        }
    }

    void OnActivate()
    {
        string serial;
        List<string> serialResponses = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
        if (serialResponses.Count == 0)
        {
            // Generate random serial number
            serial = new string(Enumerable.Range(0, 6).Select(i => i == 5 ? "0123456789" : "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ").Select(chs => chs[Rnd.Range(0, chs.Length)]).ToArray());
        }
        else
        {
            Dictionary<string, string> serialDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(serialResponses[0]);
            serial = serialDict["serial"];
        }
        var sNumber = serial[5] - 48;
        Debug.Log("Serial: " + serial + "          sNumber: " + sNumber);

        // Place objectives

        // If midfield is empty, skull is under the 7
        if (field[4] == 0)
        {
            skull = Array.IndexOf(field, 7);
        }
        else
        {
            int checkN;
            for (int k = 0; k <= 7; k++)
            {
                if (sNumber != 0 && (sNumber == field[0] || sNumber == field[2] || sNumber == field[4] || sNumber == field[6] || sNumber == field[8]))
                    checkN = table[(field[4] == 0) ? 1 : (field[4] - 1), k];        // if midfield is empty
                else
                    checkN = table[k, (field[4] == 0) ? 1 : (field[4] - 1)];

                Debug.Log("checkN: " + checkN);
                for (int i = 0; i <= 8; i++)
                {
                    if (field[i] == checkN)
                    {
                        int verDif = i / 3 - skull / 3;
                        int horDif = i % 3 - skull % 3;
                        if ((Mathf.Abs(horDif) + Mathf.Abs(verDif)) == 1)
                        {
                            skull = i;
                            Debug.Log("skull under: " + field[i]);
                        }
                        break;
                    }
                }
            }
        }

        knight = skull;
        while (knight == skull || field[knight] == 0)
            knight = Rnd.Range(0, 9);

        var background = GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.gameObject.name == "RecessedInner");

        //skull texture positioning
        int zOffset = 0;
        int xOffset = 0;
        if (skull < 3)
            zOffset = 2;
        else if (skull < 6)
            zOffset = 1;
        if (skull == 1 || skull == 4 || skull == 7)
            xOffset = 1;
        else if (skull == 2 || skull == 5 || skull == 8)
            xOffset = 2;
        background.materials[2].mainTextureOffset = new Vector2(-1.65f - xOffset * 0.85f, -2f - zOffset * 0.95f);

        //knight texture positioning
        zOffset = 0;
        xOffset = 0;
        if (knight < 3)
            zOffset = 2;
        else if (knight < 6)
            zOffset = 1;
        if (knight == 1 || knight == 4 || knight == 7)
            xOffset = 1;
        else if (knight == 2 || knight == 5 || knight == 8)
            xOffset = 2;
        background.materials[1].mainTextureOffset = new Vector2(-1.65f - xOffset * 0.85f, -2f - zOffset * 0.95f);
    }

    int topSum()
    {
        return field[0] + field[1] + field[2];
    }

    int horMidSum()
    {
        return field[3] + field[4] + field[5];
    }

    int bottomSum()
    {
        return field[6] + field[7] + field[8];
    }

    int leftSum()
    {
        return field[0] + field[3] + field[6];
    }

    int verMidSum()
    {
        return field[1] + field[4] + field[7];
    }

    int rightSum()
    {
        return field[2] + field[5] + field[8];
    }

    bool checkWin()
    {
        if (field.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 }))
            return true;

        switch (winCond)
        {
            case 0: return field[0] == 1 && field[2] == 2 && field[6] == 4 && field[8] == 3;
            case 1: return field[0] == 1 && field[2] == 2 && field[6] == 3 && field[8] == 4;
            case 2: return field[0] == 1 && field[2] == 3 && field[6] == 7 && field[8] == 5;
            case 3: return field[0] == 1 && field[2] == 3 && field[6] == 5 && field[8] == 7;
            case 10: return field[1] == 1 && field[5] == 2 && field[7] == 3 && field[3] == 4;
            case 11: return field[1] == 1 && field[5] == 2 && field[7] == 4 && field[3] == 3;
            case 12: return field[1] == 2 && field[5] == 4 && field[7] == 6 && field[3] == 8;
            case 13: return field[1] == 2 && field[5] == 4 && field[7] == 8 && field[3] == 6;
            case 20: return field[0] == 1 && field[4] == 2 && field[8] == 3;
            case 21: return field[6] == 1 && field[4] == 2 && field[2] == 3;
            case 22: return field[0] == 3 && field[4] == 2 && field[8] == 1;
            case 23: return field[6] == 3 && field[4] == 2 && field[2] == 1;
            case 30: return field[0] == 1 && field[1] == 2 && field[2] == 3 && field[4] == 4 && field[7] == 5;
            case 31: return field[0] == 1 && field[3] == 2 && field[6] == 3 && field[4] == 4 && field[5] == 5;
            case 32: return field[6] == 1 && field[7] == 2 && field[8] == 3 && field[4] == 4 && field[1] == 5;
            case 33: return field[2] == 1 && field[5] == 2 && field[8] == 3 && field[4] == 4 && field[3] == 5;
        }
        return false;
    }

    void initArray()
    {
        for (int i = 0; i <= 8; i++)
        {
            field[i] = 10;
        }
        field[7] = 11;

        bool exists = true;
        int next = 0;


        while (oddPermutation())
        {

            for (int i = 0; i <= 8; i++)
            {
                field[i] = 10;
            }
            field[7] = 11;

            for (int i = 0; i <= 8; i++)
            {
                exists = true;

                int count = 0;

                while (exists == true)
                {
                    count++;
                    exists = false;
                    next = Rnd.Range(0, 9);
                    Debug.Log("next: " + next);
                    for (int j = 0; j <= 8; j++)
                    {
                        if (field[j] == next)
                            exists = true;
                    }
                    if (count > 100)
                    {
                        Debug.Log("abort while");
                        break;
                    }
                }
                field[i] = next;
            }
        }
        Debug.Log("Field: " + field[0] + field[1] + field[2] + field[3] + field[4] + field[5] + field[6] + field[7] + field[8]);
    }

    bool oddPermutation()
    {
        int permutations = 0;
        for (int i = 0; i <= 8; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (field[i] != 0 && field[j] != 0 && (field[i] < field[j]))
                    permutations++;
            }
        }
        Debug.Log("Permutations: " + permutations);
        return (permutations % 2) != 0;
    }

    bool intArrayEquals(int[] a, int[] b)
    {
        if (a.Length == b.Length)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
        return false;
    }


    void OnPress(int position)
    {
        int empty = Array.IndexOf(field, 0);
        int button = field[position] - 1;
        if (Math.Abs(position % 3 - empty % 3) + Math.Abs(position / 3 - empty / 3) == 1)
        {
            field[position] = 0;
            field[empty] = button + 1;
            var dif = visibleButtons[button].gameObject.transform.position - emptyField.transform.position;
            visibleButtons[button].gameObject.transform.Translate(-dif, Space.World);
            emptyField.transform.Translate(dif, Space.World);

            // Check for strike or pass
            if (inDanger)
            {
                if (field[knight] == 0)
                {
                    inDanger = false;
                    Debug.Log("Found the knight.");
                }
                if (field[skull] == 0)
                    GetComponent<KMBombModule>().HandleStrike();
            }
            else if (checkWin())
                GetComponent<KMBombModule>().HandlePass();
        }
    }
}
