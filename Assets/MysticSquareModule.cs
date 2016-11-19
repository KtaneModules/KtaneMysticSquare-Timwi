using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MysticSquareModule : MonoBehaviour
{
    public KMSelectable[] ButtonSelectables;
    public Transform[] ButtonObjects;
    public Transform EmptyField, Skull, Knight;

    int[] _field = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
    int _skullPos, _knightPos, _winningCondition;
    bool _isInDanger;
    bool _isActivated;

    static int[,] _table = new int[8, 8] {
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
        for (int i = 0; i < ButtonSelectables.Length; i++)
        {
            int j = i;
            ButtonSelectables[i].OnInteract += delegate { OnPress(j); return false; };
        }

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        _isInDanger = true;
        _isActivated = false;
        _skullPos = 0;
        _knightPos = 0;

        initArray();

        // Find winning condition
        _winningCondition = 0;
        if (topSum() > horMidSum() && topSum() > bottomSum())
            _winningCondition += 0;
        else if (horMidSum() > topSum() && horMidSum() > bottomSum())
            _winningCondition += 10;
        else if (bottomSum() > topSum() && bottomSum() > horMidSum())
            _winningCondition += 20;
        else
            _winningCondition += 30;
        if (leftSum() > verMidSum() && leftSum() > rightSum())
            _winningCondition += 0;
        else if (verMidSum() > leftSum() && verMidSum() > rightSum())
            _winningCondition += 1;
        else if (rightSum() > leftSum() && rightSum() > verMidSum())
            _winningCondition += 2;
        else
            _winningCondition += 3;

        // Set start field
        int[] start = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
        Vector3 dif;

        for (int i = 0; i <= 8; i++)
        {
            for (int j = 0; j <= 8; j++)
            {
                if (start[j] == _field[i])
                {
                    if (start[j] == 0)
                    {
                        if (start[i] != 0)
                        {
                            dif = ButtonObjects[start[i] - 1].gameObject.transform.position - EmptyField.transform.position;
                            ButtonObjects[start[i] - 1].gameObject.transform.Translate(-dif, Space.World);
                            EmptyField.transform.Translate(dif, Space.World);
                        }
                    }
                    else if (start[i] != 0)
                    {
                        dif = ButtonObjects[start[i] - 1].gameObject.transform.position - ButtonObjects[start[j] - 1].gameObject.transform.position;
                        ButtonObjects[start[i] - 1].gameObject.transform.Translate(-dif, Space.World);
                        ButtonObjects[start[j] - 1].gameObject.transform.Translate(dif, Space.World);
                    }
                    else
                    {
                        dif = EmptyField.transform.position - ButtonObjects[start[j] - 1].gameObject.transform.position;
                        EmptyField.transform.Translate(-dif, Space.World);
                        ButtonObjects[start[j] - 1].gameObject.transform.Translate(dif, Space.World);
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
        _isActivated = true;
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
        var lastSerialDigit = serial[5] - 48;
        Debug.Log("Serial: " + serial + "          lastSerialDigit: " + lastSerialDigit);

        // Place objectives
        var useRows = lastSerialDigit != 0 && (lastSerialDigit == _field[0] || lastSerialDigit == _field[2] || lastSerialDigit == _field[4] || lastSerialDigit == _field[6] || lastSerialDigit == _field[8]);

        // If midfield is empty, skull is under the 7
        if (_field[4] == 0)
        {
            _skullPos = Array.IndexOf(_field, 7);
        }
        else
        {
            for (int k = 0; k <= 7; k++)
            {
                var checkN = useRows ? _table[_field[4] - 1, k] : _table[k, _field[4] - 1];
                var ix = Array.IndexOf(_field, checkN);
                int verDif = ix / 3 - _skullPos / 3;
                int horDif = ix % 3 - _skullPos % 3;
                if ((Mathf.Abs(horDif) + Mathf.Abs(verDif)) == 1)
                    _skullPos = ix;
            }
        }
        Debug.Log("Skull under: " + _field[_skullPos]);

        do
            _knightPos = Rnd.Range(0, 9);
        while (_knightPos == _skullPos || _field[_knightPos] == 0);

        Skull.localPosition = new Vector3(.063f - (_skullPos % 3) * .045f, 0, -.028f + .0425f * (_skullPos / 3));
        Knight.localPosition = new Vector3(.063f - (_knightPos % 3) * .045f, 0, -.028f + .0425f * (_knightPos / 3));
    }

    int topSum()
    {
        return _field[0] + _field[1] + _field[2];
    }

    int horMidSum()
    {
        return _field[3] + _field[4] + _field[5];
    }

    int bottomSum()
    {
        return _field[6] + _field[7] + _field[8];
    }

    int leftSum()
    {
        return _field[0] + _field[3] + _field[6];
    }

    int verMidSum()
    {
        return _field[1] + _field[4] + _field[7];
    }

    int rightSum()
    {
        return _field[2] + _field[5] + _field[8];
    }

    bool checkWin()
    {
        if (_field.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 }))
            return true;

        switch (_winningCondition)
        {
            case 0: return _field[0] == 1 && _field[2] == 2 && _field[6] == 4 && _field[8] == 3;
            case 1: return _field[0] == 1 && _field[2] == 2 && _field[6] == 3 && _field[8] == 4;
            case 2: return _field[0] == 1 && _field[2] == 3 && _field[6] == 7 && _field[8] == 5;
            case 3: return _field[0] == 1 && _field[2] == 3 && _field[6] == 5 && _field[8] == 7;
            case 10: return _field[1] == 1 && _field[5] == 2 && _field[7] == 3 && _field[3] == 4;
            case 11: return _field[1] == 1 && _field[5] == 2 && _field[7] == 4 && _field[3] == 3;
            case 12: return _field[1] == 2 && _field[5] == 4 && _field[7] == 6 && _field[3] == 8;
            case 13: return _field[1] == 2 && _field[5] == 4 && _field[7] == 8 && _field[3] == 6;
            case 20: return _field[0] == 1 && _field[4] == 2 && _field[8] == 3;
            case 21: return _field[6] == 1 && _field[4] == 2 && _field[2] == 3;
            case 22: return _field[0] == 3 && _field[4] == 2 && _field[8] == 1;
            case 23: return _field[6] == 3 && _field[4] == 2 && _field[2] == 1;
            case 30: return _field[0] == 1 && _field[1] == 2 && _field[2] == 3 && _field[4] == 4 && _field[7] == 5;
            case 31: return _field[0] == 1 && _field[3] == 2 && _field[6] == 3 && _field[4] == 4 && _field[5] == 5;
            case 32: return _field[6] == 1 && _field[7] == 2 && _field[8] == 3 && _field[4] == 4 && _field[1] == 5;
            case 33: return _field[2] == 1 && _field[5] == 2 && _field[8] == 3 && _field[4] == 4 && _field[3] == 5;
        }
        return false;
    }

    void initArray()
    {
        int permutations;
        do
        {
            Debug.Log("Shuffling buttons...");
            _field = Enumerable.Range(0, 9).ToArray();
            for (int j = _field.Length; j >= 1; j--)
            {
                int item = Rnd.Range(0, j);
                if (item < j - 1)
                {
                    var t = _field[item];
                    _field[item] = _field[j - 1];
                    _field[j - 1] = t;
                }
            }

            permutations = 0;
            for (int i = 0; i <= 8; i++)
                for (int j = 0; j < i; j++)
                    if (_field[i] != 0 && _field[j] != 0 && (_field[i] < _field[j]))
                        permutations++;
        }
        while (permutations % 2 != 0);
        Debug.Log("Field: " + _field[0] + _field[1] + _field[2] + _field[3] + _field[4] + _field[5] + _field[6] + _field[7] + _field[8]);
    }

    void OnPress(int position)
    {
        if (!_isActivated)
            return;
        int empty = Array.IndexOf(_field, 0);
        int button = _field[position] - 1;
        if (Math.Abs(position % 3 - empty % 3) + Math.Abs(position / 3 - empty / 3) == 1)
        {
            _field[position] = 0;
            _field[empty] = button + 1;
            var dif = ButtonObjects[button].gameObject.transform.position - EmptyField.transform.position;
            ButtonObjects[button].gameObject.transform.Translate(-dif, Space.World);
            EmptyField.transform.Translate(dif, Space.World);

            // Check for strike or pass
            if (_isInDanger)
            {
                if (_field[_knightPos] == 0)
                {
                    _isInDanger = false;
                    Debug.Log("Found the knight.");
                }
                if (_field[_skullPos] == 0)
                    GetComponent<KMBombModule>().HandleStrike();
            }
            else if (checkWin())
                GetComponent<KMBombModule>().HandlePass();
        }
    }
}
