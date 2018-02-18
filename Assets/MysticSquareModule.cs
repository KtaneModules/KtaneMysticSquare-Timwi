using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MysticSquareModule : MonoBehaviour
{
    public KMSelectable[] ButtonSelectables;
    public Transform[] ButtonObjects;
    public Transform Skull, Knight;

    private int[] _field = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
    private int _skullPos, _knightPos, _winningCondition;
    private bool _isInDanger;
    private bool _isActivated;
    private bool _isSolved;

    private Queue<int> _buttonIndexes = new Queue<int>();
    private Queue<Vector3> _buttonPositions = new Queue<Vector3>();

    private int _moduleId;
    private static int _moduleIdCounter = 1;

    private static int[,] _table = new int[8, 8] {
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
        Skull.gameObject.SetActive(false);
        Knight.gameObject.SetActive(false);
        _moduleId = _moduleIdCounter++;
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

        // Shuffle up the numbers
        int permutations;
        do
        {
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

            // We need an odd permutation, otherwise the puzzle is not solvable.
            permutations = 0;
            for (int i = 0; i <= 8; i++)
                for (int j = 0; j < i; j++)
                    if (_field[i] != 0 && _field[j] != 0 && (_field[i] < _field[j]))
                        permutations++;
        }
        while (permutations % 2 != 0);

        Debug.LogFormat("[Mystic Square #{3}] Field:\n{0}\n{1}\n{2}", "" + _field[0] + _field[1] + _field[2], "" + _field[3] + _field[4] + _field[5], "" + _field[6] + _field[7] + _field[8], _moduleId);

        // Put the game objects in the right places
        for (int i = 0; i < 9; i++)
            if (_field[i] != 0)
                ButtonObjects[_field[i] - 1].localPosition = getButtonPos(i % 3, i / 3);

        // Find the “easier” winning condition
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

        var fieldStr = "?????????".ToCharArray();
        switch (_winningCondition)
        {
            case 0: fieldStr[0] = '1'; fieldStr[2] = '2'; fieldStr[6] = '4'; fieldStr[8] = '3'; break;
            case 1: fieldStr[0] = '1'; fieldStr[2] = '2'; fieldStr[6] = '3'; fieldStr[8] = '4'; break;
            case 2: fieldStr[0] = '1'; fieldStr[2] = '3'; fieldStr[6] = '7'; fieldStr[8] = '5'; break;
            case 3: fieldStr[0] = '1'; fieldStr[2] = '3'; fieldStr[6] = '5'; fieldStr[8] = '7'; break;
            case 10: fieldStr[1] = '1'; fieldStr[5] = '2'; fieldStr[7] = '3'; fieldStr[3] = '4'; break;
            case 11: fieldStr[1] = '1'; fieldStr[5] = '2'; fieldStr[7] = '4'; fieldStr[3] = '3'; break;
            case 12: fieldStr[1] = '2'; fieldStr[5] = '4'; fieldStr[7] = '6'; fieldStr[3] = '8'; break;
            case 13: fieldStr[1] = '2'; fieldStr[5] = '4'; fieldStr[7] = '8'; fieldStr[3] = '6'; break;
            case 20: fieldStr[0] = '1'; fieldStr[4] = '2'; fieldStr[8] = '3'; break;
            case 21: fieldStr[6] = '1'; fieldStr[4] = '2'; fieldStr[2] = '3'; break;
            case 22: fieldStr[0] = '3'; fieldStr[4] = '2'; fieldStr[8] = '1'; break;
            case 23: fieldStr[6] = '3'; fieldStr[4] = '2'; fieldStr[2] = '1'; break;
            case 30: fieldStr[0] = '1'; fieldStr[1] = '2'; fieldStr[2] = '3'; fieldStr[4] = '4'; break;
            case 31: fieldStr[0] = '1'; fieldStr[3] = '2'; fieldStr[6] = '3'; fieldStr[4] = '4'; break;
            case 32: fieldStr[6] = '1'; fieldStr[7] = '2'; fieldStr[8] = '3'; fieldStr[4] = '4'; break;
            case 33: fieldStr[2] = '1'; fieldStr[5] = '2'; fieldStr[8] = '3'; fieldStr[4] = '4'; break;
        }
        Debug.LogFormat("[Mystic Square #{3}] Easy solution:\n{0}\n{1}\n{2}", "" + fieldStr[0] + fieldStr[1] + fieldStr[2], "" + fieldStr[3] + fieldStr[4] + fieldStr[5], "" + fieldStr[6] + fieldStr[7] + fieldStr[8], _moduleId);

        StartCoroutine(MoveButtons());
    }

    private float easeOutSine(float time, float duration, float from, float to)
    {
        return (to - from) * Mathf.Sin(time / duration * (Mathf.PI / 2)) + from;
    }

    private IEnumerator MoveButtons()
    {
        const float duration = .1f;

        while (true)
        {
            while (_buttonIndexes.Count == 0)
                yield return null;

            var ix = _buttonIndexes.Dequeue();
            var oldPos = ButtonObjects[ix].localPosition;
            var newPos = _buttonPositions.Dequeue();
            var elapsed = 0f;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                ButtonObjects[ix].localPosition = new Vector3(
                    easeOutSine(Mathf.Min(elapsed, duration), duration, oldPos.x, newPos.x),
                    easeOutSine(Mathf.Min(elapsed, duration), duration, oldPos.y, newPos.y),
                    easeOutSine(Mathf.Min(elapsed, duration), duration, oldPos.z, newPos.z));
            }
        }
    }

    private Vector3 getButtonPos(int x, int y)
    {
        return new Vector3(-0.002f + 0.044f * x, 0, 0.001f - .043f * y);
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

        // If midfield is empty, skull is under the 7
        if (_field[4] == 0)
        {
            _skullPos = Array.IndexOf(_field, 7);
            Debug.LogFormat("[Mystic Square #{0}] Skull under 7 because center is blank.", _moduleId);
        }
        else
        {
            var lastSerialDigit = serial[5] - 48;
            var useRows = lastSerialDigit != 0 && (lastSerialDigit == _field[0] || lastSerialDigit == _field[2] || lastSerialDigit == _field[4] || lastSerialDigit == _field[6] || lastSerialDigit == _field[8]);
            Debug.LogFormat("[Mystic Square #{2}] Last serial digit: {0}; therefore, use {1}", lastSerialDigit, useRows ? "rows" : "columns", _moduleId);
            _skullPos = Array.IndexOf(_field, 0);
            var skullPath = "blank";
            for (int k = 0; k <= 7; k++)
            {
                var checkN = useRows ? _table[_field[4] - 1, k] : _table[k, _field[4] - 1];
                var ix = Array.IndexOf(_field, checkN);
                int verDif = ix / 3 - _skullPos / 3;
                int horDif = ix % 3 - _skullPos % 3;
                if ((Mathf.Abs(horDif) + Mathf.Abs(verDif)) == 1)
                {
                    _skullPos = ix;
                    skullPath += " → " + _field[ix];
                }
            }
            Debug.LogFormat("[Mystic Square #{0}] Skull path: {1}", _moduleId, skullPath);
        }

        do
            _knightPos = Rnd.Range(0, 9);
        while (_knightPos == _skullPos || _field[_knightPos] == 0);

        Skull.localPosition = new Vector3(.063f - (_skullPos % 3) * .045f, 0, -.028f + .0425f * (_skullPos / 3));
        Knight.localPosition = new Vector3(.063f - (_knightPos % 3) * .045f, 0, -.028f + .0425f * (_knightPos / 3));
        Skull.gameObject.SetActive(true);
        Knight.gameObject.SetActive(true);
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
            case 30: return _field[0] == 1 && _field[1] == 2 && _field[2] == 3 && _field[4] == 4;
            case 31: return _field[0] == 1 && _field[3] == 2 && _field[6] == 3 && _field[4] == 4;
            case 32: return _field[6] == 1 && _field[7] == 2 && _field[8] == 3 && _field[4] == 4;
            case 33: return _field[2] == 1 && _field[5] == 2 && _field[8] == 3 && _field[4] == 4;
        }
        return false;
    }

    void OnPress(int position)
    {
        if (!_isActivated || _isSolved)
            return;
        ButtonSelectables[position].AddInteractionPunch();
        int empty = Array.IndexOf(_field, 0);
        int button = _field[position] - 1;
        if (Math.Abs(position % 3 - empty % 3) + Math.Abs(position / 3 - empty / 3) == 1)
        {
            _field[position] = 0;
            _field[empty] = button + 1;

            _buttonIndexes.Enqueue(button);
            _buttonPositions.Enqueue(getButtonPos(empty % 3, empty / 3));

            // Check for strike or pass
            if (_isInDanger)
            {
                if (_field[_knightPos] == 0)
                {
                    _isInDanger = false;
                    Debug.LogFormat("[Mystic Square #{0}] Found the knight.", _moduleId);
                }
                else if (_field[_skullPos] == 0)
                {
                    Debug.LogFormat("[Mystic Square #{0}] Uncovered the skull before finding the knight.", _moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                }
            }

            if (checkWin())
            {
                Debug.LogFormat("[Mystic Square #{0}] Module solved.", _moduleId);
                GetComponent<KMBombModule>().HandlePass();
                _isSolved = true;
            }
        }
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (!_isActivated || _isSolved)
            yield break;

        var pieces = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length < 2 || pieces[0] != "press")
            yield break;

        var funcs = new List<Func<KMSelectable>>();
        foreach (var piece in pieces.Skip(1))
        {
            switch (piece.Replace("center", "middle").Replace("centre", "middle"))
            {
                case "tl": case "lt": case "topleft": case "lefttop": funcs.Add(() => ButtonSelectables[0]); break;
                case "tm": case "tc": case "mt": case "ct": case "topmiddle": case "middletop": funcs.Add(() => ButtonSelectables[1]); break;
                case "tr": case "rt": case "topright": case "righttop": funcs.Add(() => ButtonSelectables[2]); break;

                case "ml": case "cl": case "lm": case "lc": case "middleleft": case "leftmiddle": funcs.Add(() => ButtonSelectables[3]); break;
                case "mm": case "cm": case "mc": case "cc": case "middle": case "middlemiddle": funcs.Add(() => ButtonSelectables[4]); break;
                case "mr": case "cr": case "rm": case "rc": case "middleright": case "rightmiddle": funcs.Add(() => ButtonSelectables[5]); break;

                case "bl": case "lb": case "bottomleft": case "leftbottom": funcs.Add(() => ButtonSelectables[6]); break;
                case "bm": case "bc": case "mb": case "cb": case "bottommiddle": case "middlebottom": funcs.Add(() => ButtonSelectables[7]); break;
                case "br": case "rb": case "bottomright": case "rightbottom": funcs.Add(() => ButtonSelectables[8]); break;

                case "1": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 1)]); break;
                case "2": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 2)]); break;
                case "3": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 3)]); break;
                case "4": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 4)]); break;
                case "5": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 5)]); break;
                case "6": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 6)]); break;
                case "7": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 7)]); break;
                case "8": funcs.Add(() => ButtonSelectables[Array.IndexOf(_field, 8)]); break;

                default: yield break;
            }
        }

        foreach (var func in funcs)
        {
            var btn = func();
            yield return btn;
            yield return new WaitForSeconds(.1f);
            yield return btn;
        }
    }
}
