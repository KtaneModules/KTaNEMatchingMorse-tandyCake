using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class MatchingMorseScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable[] buttons;
    public MeshRenderer[] leds;
    public Material unlit, lit, solved;

    private const float flashSpeed = 0.25f;
    private const float solveSpeed = 0.125f;
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private List<char> alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToList();
    private char[] dispChars = new char[9];
    private int? currentSelected = null;
    private string[] sequences = new string[9];
    private int[] pointers = new int[9];
    private Coroutine[] anims = new Coroutine[9];
    private bool[] matched = new bool[9];
    private int jokerPos;

    void Awake ()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 9; i++)
        {
            int ix = i;
            buttons[ix].OnInteract += delegate () { ButtonPress(ix); return false; };
        }
    }

    void Start ()
    {
        GeneratePairs();
        InitiateFlashes();
    }

    void ButtonPress(int pos)
    {
        buttons[pos].AddInteractionPunch(0.5f);
        if (moduleSolved || matched[pos])
            return;
        if (currentSelected == null)
        {
            Audio.PlaySoundAtTransform("midbeep", buttons[pos].transform);
            currentSelected = pos;
            StopCoroutine(anims[pos]);
            leds[pos].material = lit;
        }
        else if (currentSelected == pos)
        {
            Audio.PlaySoundAtTransform("lowbeep", buttons[pos].transform);
            currentSelected = null;
            anims[pos] = StartCoroutine(ButtonFlash(pos));
        }
        else if (dispChars[(int)currentSelected] == dispChars[pos])
        {
            Audio.PlaySoundAtTransform("highbeep", buttons[pos].transform);
            Debug.LogFormat("[Matching Morse #{0}] Successfully matched LED {1} with LED {2}.", moduleId, (int)currentSelected + 1, pos + 1);
            StopCoroutine(anims[pos]);
            leds[(int)currentSelected].material = unlit;
            leds[pos].material = unlit;
            matched[(int)currentSelected] = true;
            matched[pos] = true;
            currentSelected = null;
            if (matched.Count(x => x) >= 8)
                StartCoroutine(Solve());
        }
        else
        {
            Audio.PlaySoundAtTransform("crush", buttons[pos].transform);
            Debug.LogFormat("[Matching Morse #{0}] Attempted to match LED {1}({3}) with LED {2}({4}). Strike!", moduleId, (int)currentSelected + 1, pos + 1, dispChars[(int)currentSelected], dispChars[pos]);
            anims[(int)currentSelected] = StartCoroutine(ButtonFlash((int)currentSelected));
            currentSelected = null;
            Module.HandleStrike();
        }
    }

    void GeneratePairs()
    {
        alphabet.Shuffle();
        for (int i = 0; i < 8; i++)
            dispChars[i] = alphabet[i / 2];
        dispChars[8] = alphabet[5];
        dispChars.Shuffle();
        Debug.LogFormat("[Matching Morse #{0}] The displayed characters are {1} / {2} / {3}.", moduleId, dispChars.Take(3).Join(), dispChars.Skip(3).Take(3).Join(), dispChars.Skip(6).Join());
        for (int i = 0; i < 9; i++)
            if (dispChars.Count(x => x == dispChars[i]) < 2)
                jokerPos = i;
    }
    void InitiateFlashes()
    {
        for (int i = 0; i < 9; i++)
        {
            sequences[i] = MorseData.GenerateSequence(dispChars[i]);
            pointers[i] = Rnd.Range(0, sequences[i].Length);
            anims[i] = StartCoroutine(ButtonFlash(i));
        }
    }
    IEnumerator Solve()
    {
        moduleSolved = true;
        StopCoroutine(anims[jokerPos]);
        leds[jokerPos].material = unlit;
        yield return new WaitForSeconds(2 * solveSpeed);
        if (jokerPos == 4)
        {
            leds[4].material = solved;
            yield return new WaitForSeconds(solveSpeed);
            for (int i = 0; i < 4; i++)
                leds[2 * i + 1].material = solved;
            Audio.PlaySoundAtTransform("solveCrunch", transform);
            yield return new WaitForSeconds(solveSpeed);
            for (int i = 0; i < 5; i++)
                leds[2 * i].material = solved;
            Audio.PlaySoundAtTransform("solveCrunch", transform);
        }
        else
        {
            int[] order = { 0, 1, 2, 5, 8, 7, 6, 3 };
            int pointer = Array.IndexOf(order, jokerPos);
            for (int i = 0; i < 8; i++)
            {
                Audio.PlaySoundAtTransform("solveCrunch", transform);
                leds[order[pointer]].material = solved;
                yield return new WaitForSeconds(solveSpeed);
                pointer++;
                pointer %= 8;
            }
            Audio.PlaySoundAtTransform("solveCrunch", transform);
            leds[4].material = solved;
        }
        yield return new WaitForSeconds(solveSpeed / 2);
        Audio.PlaySoundAtTransform("solveJingle", transform);
        Audio.PlaySoundAtTransform("solve", transform);
        Module.HandlePass();
    }
    IEnumerator ButtonFlash(int pos)
    {
        while (!moduleSolved)
        {
            leds[pos].material = sequences[pos][pointers[pos]] == 'x' ? lit : unlit;
            yield return new WaitForSeconds(flashSpeed);
            pointers[pos]++;
            pointers[pos] %= sequences[pos].Length;
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} match TL BR to match the LEDs in those positions.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        string[] coords = { "TL", "TM", "TR", "ML", "MM", "MR", "BL", "BM", "BR" };
        command = command.Trim().ToUpperInvariant();
        Match m = Regex.Match(command, @"^(?:MATCH|PRESS)((?:\s+[TMB][LMR])+)$");
        if (m.Success)
        {
            foreach (string coor in m.Groups[1].Value.Split(' '))
            {
                buttons[Array.IndexOf(coords, coor)].OnInteract();
                yield return new WaitForSeconds(0.25f);
            }
            if (moduleSolved)
                yield return "solve";
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!moduleSolved)
        {
            if (currentSelected != null)
                buttons[(int)currentSelected].OnInteract();
            for (int i = 0; i < 9; i++)
            {
                if (!matched[i] && i != jokerPos)
                {
                    buttons[i].OnInteract();
                    yield return new WaitForSeconds(0.25f);
                    for (int j = i + 1; j < 9; j++)
                        if (dispChars[j] == dispChars[i])
                        {
                            buttons[j].OnInteract();
                            yield return new WaitForSeconds(0.25f);
                        }
                }
            }
        }
    }
}
