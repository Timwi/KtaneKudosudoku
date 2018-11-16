using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using Kudosudoku;
using UnityEditor;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>On the Subject of Kudosudoku — created by Timwi</summary>
public class KudosudokuModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public GameObject SquaresParent;

    public Texture[] SnookerBallTextures;
    public Texture[] AstrologyTextures;
    public Texture[] CardSuitTextures;
    public Texture[] MahjongTextures;
    public Texture[] MaritimeFlagTextures;

    public TextMesh LetterTextMesh;
    public TextMesh DigitTextMesh;
    public TextMesh TheCubeSymbolsTextMesh;

    public GameObject SemaphoresParent;
    public GameObject BrailleParent;
    public GameObject BinaryParent;
    public GameObject MorseParent;
    public GameObject ArrowsParent;

    public KMSelectable[] Squares;

    public GameObject ImageTemplate;
    public MeshRenderer MorseLed;
    public Material MorseLedOn;
    public Material MorseLedOff;

    public enum Coding
    {
        Letters,
        Digits,
        MorseCode,
        Semaphores,
        Braille,
        MaritimeFlags,
        TapCode,
        Binary,
        SimonSamples,
        Astrology,
        Snooker,
        Arrows,
        CardSuits,
        Mahjong,
        TheCubeSymbols,
        ListeningSounds
    }

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int[] _solution;
    private bool _isSolved;
    private Coding[] _codings;

    private readonly char[] _numberNames = new char[4];
    private readonly bool[] _shown = new bool[16];
    private readonly bool[] _theCubeAlternatives = new bool[4];
    private readonly int[] _listeningAlternatives = new int[4];

    [MenuItem("A/B")]
    public static void Clean()
    {
        var m = FindObjectOfType<KudosudokuModule>();
        Debug.Log(m.name);
    }

    /// <summary>Lists all 288 possible 4×4 Sudokus.</summary>
    public static int[][] _allSudokus = @"1234341221434321,1234341223414123,1234341241232341,1234341243212143,1234342121434312,1234342143122143,1234431221433421,1234431234212143,1234432121433412,1234432124133142,1234432131422413,1234432134122143,1243341221344321,1243341243212134,1243342121344312,1243342123144132,1243342141322314,1243342143122134,1243431221343421,1243431224313124,1243431231242431,1243431234212134,1243432121343412,1243432134122134,1324241331424231,1324241332414132,1324241341323241,1324241342313142,1324243131424213,1324243142133142,1324421324313142,1324421331422431,1324423121433412,1324423124133142,1324423131422413,1324423134122143,1342241331244231,1342241342313124,1342243131244213,1342243132144123,1342243141233214,1342243142133124,1342421321343421,1342421324313124,1342421331242431,1342421334212134,1342423124133124,1342423131242413,1423231431424231,1423231432414132,1423231441323241,1423231442313142,1423234132144132,1423234141323214,1423321423414132,1423321441322341,1423324121344312,1423324123144132,1423324141322314,1423324143122134,1432231432414123,1432231441233241,1432234131244213,1432234132144123,1432234141233214,1432234142133124,1432321421434321,1432321423414123,1432321441232341,1432321443212143,1432324123144123,1432324141232314,2134341212434321,2134341243211243,2134342112434312,2134342113424213,2134342142131342,2134342143121243,2134431212433421,2134431214233241,2134431232411423,2134431234211243,2134432112433412,2134432134121243,2143341212344321,2143341213244231,2143341242311324,2143341243211234,2143342112344312,2143342143121234,2143431212343421,2143431234211234,2143432112343412,2143432114323214,2143432132141432,2143432134121234,2314142331424231,2314142332414132,2314142341323241,2314142342313142,2314143232414123,2314143241233241,2314412314323241,2314412332411432,2314413212433421,2314413214233241,2314413232411423,2314413234211243,2341142332144132,2341142341323214,2341143231244213,2341143232144123,2341143241233214,2341143242133124,2341412312343412,2341412314323214,2341412332141432,2341412334121234,2341413214233214,2341413232141423,2413132431424231,2413132432414132,2413132441323241,2413132442313142,2413134231244231,2413134242313124,2413312413424231,2413312442311342,2413314212344321,2413314213244231,2413314242311324,2413314243211234,2431132431424213,2431132442133142,2431134231244213,2431134232144123,2431134241233214,2431134242133124,2431312412434312,2431312413424213,2431312442131342,2431312443121243,2431314213244213,2431314242131324,3124241313424231,3124241342311342,3124243112434312,3124243113424213,3124243142131342,3124243143121243,3124421313422431,3124421314322341,3124421323411432,3124421324311342,3124423113422413,3124423124131342,3142241312344321,3142241313244231,3142241342311324,3142241343211234,3142243113244213,3142243142131324,3142421313242431,3142421324311324,3142423113242413,3142423114232314,3142423123141423,3142423124131324,3214142323414132,3214142341322341,3214143221434321,3214143223414123,3214143241232341,3214143243212143,3214412313422431,3214412314322341,3214412323411432,3214412324311342,3214413214232341,3214413223411423,3241142321344312,3241142323144132,3241142341322314,3241142343122134,3241143223144123,3241143241232314,3241412314322314,3241412323141432,3241413213242413,3241413214232314,3241413223141423,3241413224131324,3412123421434321,3412123423414123,3412123441232341,3412123443212143,3412124321344321,3412124343212134,3412213412434321,3412213443211243,3412214312344321,3412214313244231,3412214342311324,3412214343211234,3421123421434312,3421123443122143,3421124321344312,3421124323144132,3421124341322314,3421124343122134,3421213412434312,3421213413424213,3421213442131342,3421213443121243,3421214312344312,3421214343121234,4123231414323241,4123231432411432,4123234112343412,4123234114323214,4123234132141432,4123234134121234,4123321413422431,4123321414322341,4123321423411432,4123321424311342,4123324114322314,4123324123141432,4132231412433421,4132231414233241,4132231432411423,4132231434211243,4132234114233214,4132234132141423,4132321414232341,4132321423411423,4132324113242413,4132324114232314,4132324123141423,4132324124131324,4213132424313142,4213132431422431,4213134221343421,4213134224313124,4213134231242431,4213134234212134,4213312413422431,4213312414322341,4213312423411432,4213312424311342,4213314213242431,4213314224311324,4231132421433412,4231132424133142,4231132431422413,4231132434122143,4231134224133124,4231134231242413,4231312413422413,4231312424131342,4231314213242413,4231314214232314,4231314223141423,4231314224131324,4312123421433421,4312123434212143,4312124321343421,4312124324313124,4312124331242431,4312124334212134,4312213412433421,4312213414233241,4312213432411423,4312213434211243,4312214312343421,4312214334211234,4321123421433412,4321123424133142,4321123431422413,4321123434122143,4321124321343412,4321124334122134,4321213412433412,4321213434121243,4321214312343412,4321214314323214,4321214332141432,4321214334121234"
        .Split(',').Select(str => str.Select(ch => ch - '1').ToArray()).ToArray();

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;

        LetterTextMesh.gameObject.SetActive(false);
        DigitTextMesh.gameObject.SetActive(false);
        TheCubeSymbolsTextMesh.gameObject.SetActive(false);
        SemaphoresParent.SetActive(false);
        BrailleParent.SetActive(false);
        BinaryParent.SetActive(false);
        MorseParent.SetActive(false);
        ArrowsParent.SetActive(false);
        ImageTemplate.gameObject.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            _theCubeAlternatives[i] = Rnd.Range(0, 2) != 0;
            _listeningAlternatives[i] = 4 * Rnd.Range(0, 10) + i;
        }

        _solution = _allSudokus[Rnd.Range(0, _allSudokus.Length)];
        Debug.LogFormat(@"[Kudosudoku #{0}] Solution:", _moduleId);
        for (int i = 0; i < 4; i++)
            Debug.Log(_solution.Skip(4 * i).Take(4).JoinString(" "));

        var dist = Bomb.GetSerialNumberNumbers().First();
        _numberNames[0] = Bomb.GetSerialNumberLetters().First();
        for (int i = 1; i < 4; i++)
            _numberNames[i] = (char) ((_numberNames[i - 1] - 'A' + dist) % 26 + 'A');
        Debug.LogFormat(@"[Kudosudoku #{0}] Number names: {1}", _moduleId, _numberNames.Select((ch, ix) => string.Format("{0}={1}", ix + 1, ch)).JoinString(", "));

        var coords = Enumerable.Range(0, 16).ToList().Shuffle();
        var givens = Ut.ReduceRequiredSet(coords, state => _allSudokus.Count(sudoku => state.SetToTest.All(ix => sudoku[ix] == _solution[ix])) == 1).ToArray();

        _codings = Enum.GetValues(typeof(Coding)).Cast<Coding>().ToArray().Shuffle();

        // Special case: if both C and K are letter names, Tap Code can’t be a given
        if (_numberNames.Contains('C') && _numberNames.Contains('K'))
            while (givens.Contains(Array.IndexOf(_codings, Coding.TapCode)))
                _codings.Shuffle();

        foreach (var ix in givens)
            showSquare(ix, initial: true);

        // FOR DEBUGGING
        for (int i = 0; i < 16; i++)
            showSquare(i, initial: true);
    }

    private static readonly int[] _semaphoreLeftFlagOrientations = new[] { 135, 90, 45, 0, 180, 180, 180, 90, 135, 0, 135, 135, 135, 135, 90, 90, 90, 90, 90, 45, 45, 0, -45, -45, 45, -135 };
    private static readonly int[] _semaphoreRightFlagOrientations = new[] { -180, -180, -180, -180, -45, -90, -135, 135, 45, -90, 0, -45, -90, -135, 45, 0, -45, -90, -135, 0, -45, -135, -90, -135, -90, -90 };
    private static readonly string[] _brailleCodes = "1,12,14,145,15,124,1245,125,24,245,13,123,134,1345,135,1234,12345,1235,234,2345,136,1236,2456,1346,13456,1356".Split(',');

    private void showSquare(int i, bool initial)
    {
        if (_shown[i])
            return;
        _shown[i] = true;
        if (_shown.All(b => b))
        {
            _isSolved = true;
            Module.HandlePass();
        }

        var square = Squares[i].transform;
        switch (_codings[i])
        {
            case Coding.Letters:
            {
                reparentAndActivate(LetterTextMesh.transform, square);
                LetterTextMesh.text = _numberNames[_solution[i]].ToString();
                break;
            }

            case Coding.Digits:
            {
                reparentAndActivate(DigitTextMesh.transform, square);
                DigitTextMesh.text = (_solution[i] + 1).ToString();
                break;
            }

            case Coding.TheCubeSymbols:
            {
                reparentAndActivate(TheCubeSymbolsTextMesh.transform, square);
                TheCubeSymbolsTextMesh.text = ((char) ('A' + _solution[i] + (_theCubeAlternatives[_solution[i]] ? 10 : 0))).ToString();
                break;
            }

            case Coding.Semaphores:
            {
                reparentAndActivate(SemaphoresParent.transform, square);

                var flag1 = SemaphoresParent.transform.Find("Flag1");
                var left = _semaphoreLeftFlagOrientations[_numberNames[_solution[i]] - 'A'];
                flag1.localEulerAngles = new Vector3(90, 0, left);
                flag1.localScale = new Vector3(left < 0 ? -1 : 1, 1, 1);

                var flag2 = SemaphoresParent.transform.Find("Flag2");
                var right = _semaphoreRightFlagOrientations[_numberNames[_solution[i]] - 'A'];
                flag2.localEulerAngles = new Vector3(90, 0, right);
                flag2.localScale = new Vector3(right > 0 ? 1 : -1, 1, 1);
                break;
            }

            case Coding.Braille:
            {
                reparentAndActivate(BrailleParent.transform, square);
                for (char bd = '1'; bd <= '6'; bd++)
                    BrailleParent.transform.Find("Dot" + bd).gameObject.SetActive(_brailleCodes[_numberNames[_solution[i]] - 'A'].Contains(bd));
                break;
            }

            case Coding.Binary:
            {
                reparentAndActivate(BinaryParent.transform, square);
                var name = _numberNames[_solution[i]] - 'A' + 1;
                var binary = Enumerable.Range(0, 5).Select<int, object>(bit => (name & (1 << bit)) != 0 ? "1" : "0").ToArray();
                BinaryParent.transform.Find("DigitsFront").GetComponent<TextMesh>().text = string.Format("{4}{3}\n{2}{1}{0}", binary);
                break;
            }

            case Coding.Arrows:
            {
                reparentAndActivate(ArrowsParent.transform, square);
                ArrowsParent.transform.localEulerAngles = new Vector3(0, 0, -90 * _solution[i]);
                break;
            }

            case Coding.MorseCode:
            {
                reparentAndActivate(MorseParent.transform, square);
                var characterToBlink = initial && Rnd.Range(0, 3) == 0 ? (char) (_solution[i] + '1') : _numberNames[_solution[i]];
                StartCoroutine(blinkMorse(characterToBlink));
                break;
            }

            case Coding.TapCode:
                break;
            case Coding.SimonSamples:
                break;
            case Coding.ListeningSounds:
                break;

            case Coding.MaritimeFlags:
            {
                var mr = createGraphic(square, 1);
                var flagName = "Flag-" + (initial && Rnd.Range(0, 3) == 0 ? (_solution[i] + 1).ToString() : _numberNames[_solution[i]].ToString());
                mr.material.mainTexture = MaritimeFlagTextures.First(t => t.name == flagName);
                break;
            }

            default:
            {
                var textures =
                    _codings[i] == Coding.Snooker ? SnookerBallTextures :
                    _codings[i] == Coding.Astrology ? AstrologyTextures :
                    _codings[i] == Coding.Mahjong ? MahjongTextures :
                    _codings[i] == Coding.CardSuits ? CardSuitTextures : null;
                var mr = createGraphic(square, _codings[i] == Coding.Mahjong ? 109f / 169f : 1);
                mr.material.mainTexture = textures[_solution[i]];
                break;
            }
        }
    }

    private static readonly Dictionary<char, string> _morseCode = new Dictionary<char, string>
    {
        { 'A', ".-" },
        { 'B', "-..." },
        { 'C', "-.-." },
        { 'D', "-.." },
        { 'E', "." },
        { 'F', "..-." },
        { 'G', "--." },
        { 'H', "...." },
        { 'I', ".." },
        { 'J', ".---" },
        { 'K', "-.-" },
        { 'L', ".-.." },
        { 'M', "--" },
        { 'N', "-." },
        { 'O', "---" },
        { 'P', ".--." },
        { 'Q', "--.-" },
        { 'R', ".-." },
        { 'S', "..." },
        { 'T', "-" },
        { 'U', "..-" },
        { 'V', "...-" },
        { 'W', ".--" },
        { 'X', "-..-" },
        { 'Y', "-.--" },
        { 'Z', "--.." },
        { '1', ".----" },
        { '2', "..---" },
        { '3', "...--" },
        { '4', "....-" },
        { '5', "....." },
        { '6', "-...." },
        { '7', "--..." },
        { '8', "---.." },
        { '9', "----." },
        { '0', "-----" }
    };

    private IEnumerator blinkMorse(char ch)
    {
        var morse = _morseCode[ch];
        const float dotLength = .3f;
        while (true)
        {
            for (int i = 0; i < morse.Length; i++)
            {
                MorseLed.material = MorseLedOn;
                yield return new WaitForSeconds((morse[i] == '.' ? 1 : 3) * dotLength);
                MorseLed.material = MorseLedOff;
                yield return new WaitForSeconds(dotLength);
            }
            yield return new WaitForSeconds(4 * dotLength);
        }
    }

    private void reparentAndActivate(Transform obj, Transform newParent)
    {
        if (obj.parent != newParent)
            obj.SetParent(newParent, false);
        obj.gameObject.SetActive(true);
    }

    private MeshRenderer createGraphic(Transform square, float widthRatio)
    {
        var obj = Instantiate(ImageTemplate);
        obj.transform.parent = square;
        obj.transform.localPosition = new Vector3(0, 0, -.001f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = new Vector3(.95f * widthRatio, .95f, 1);
        obj.SetActive(true);
        return obj.GetComponent<MeshRenderer>();
    }
}
