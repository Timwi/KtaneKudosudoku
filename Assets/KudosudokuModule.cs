using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using Kudosudoku;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>On the Subject of Kudosudoku — created by Timwi</summary>
public class KudosudokuModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;
    public GameObject SquaresParent;

    public KMSelectable[] Squares;

    public Texture[] SnookerBallTextures;
    public Texture[] SnookerBallTexturesCB;
    public Texture[] AstrologyTextures;
    public Texture[] CardSuitTextures;
    public Texture[] MahjongTextures;
    public Texture[] MaritimeFlagTextures;
    public Texture[] ChessPieceTextures;

    public TextMesh LetterTextMesh;
    public TextMesh DigitTextMesh;
    public TextMesh ZoniTextMesh;
    public TextMesh MorseWrong;

    public GameObject SemaphoresParent;
    public GameObject BrailleParent;
    public GameObject BinaryParent;
    public GameObject MorseParent;
    public GameObject ArrowsParent;

    public GameObject TopPanelBacking;
    public GameObject TopPanelCover;
    public GameObject SemaphoresPanel;
    public GameObject BinaryPanel;
    public GameObject RightPanelBacking;
    public GameObject RightPanelCover;
    public GameObject BraillePanel;
    public GameObject LettersPanel;

    public KMSelectable SemaphoresLeftSelectable;
    public KMSelectable SemaphoresRightSelectable;
    public Transform SemaphoresLeftHand;
    public Transform SemaphoresRightHand;
    public Transform SemaphoresLeftFlag;
    public Transform SemaphoresRightFlag;
    public KMSelectable[] BinaryDigits;
    public TextMesh[] BinaryDigitDisplays;
    public KMSelectable[] BrailleDots;
    public KMSelectable[] LetterUpDownButtons;
    public TextMesh LetterDisplay;

    public GameObject ImageTemplate;
    public MeshRenderer MorseLed;
    public Material MorseLedOn;
    public Material MorseLedOff;
    public Material BrailleDotOn;
    public Material BrailleDotOff;
    public Material SquareSolved;
    public Material SquareUnsolved;
    public Material SquareExpectingMorse;
    public Material SquareExpectingMorseDown;
    public Material SquareExpectingTapCode;
    public Material SquareExpectingAnswer;
    public Material SquareExpectingPanelAnswer;

    private MeshRenderer[] _squaresMR;
    private MeshRenderer[] _brailleDotsMR;
    private bool _isCurrentCyclingAnswerCorrect;
    private readonly Stack<GameObject> _unusedImageObjects = new Stack<GameObject>();

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
        Zoni,
        ChessPieces
    }

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int[] _solution;
    private bool _isSolved;
    private Coding[] _codings;
    private bool _colorblind;
    private bool _longPress;
    private KMSelectable _longPressSelectable;
    private Coroutine _longPressCoroutine;
    private Coroutine _delaySubmitCoroutine;
    private Coroutine _waitingToSubmitMorseOrTapCode;
    private Action _delaySubmitAction;

    // Used both for colorblind mode (dark red/blue expecting Morse/Tap Code) and for TP (S1–S4 for sounds)
    private TextMesh _extraText;

    // Used to swap out the texture if color-blind mode is enabled when it’s already visible
    private MeshRenderer _snookerBallGraphic;

    /// <summary>Which square currently has a panel open, a cycling graphic or sound, or active Morse or Tap Code.</summary>
    private int? _activeSquare = null;

    /// <summary>Lists all the times at which the mouse was pressed or released while entering Morse code.</summary>
    private List<float> _morsePressTimes;
    /// <summary>Lists the number of taps between pauses.</summary>
    private List<int> _tapCodeInput;
    /// <summary>While true, the next tap will still be added to the last item in <see cref="_tapCodeInput"/>.</summary>
    private bool _tapCodeLastCodeStillActive;

    // Orientations of the semaphores in the panel, and directions they move when clicked
    private int _leftSemaphore = 0;
    private int _rightSemaphore = 0;
    private bool _leftSemaphoreCw = true;
    private bool _rightSemaphoreCw = false;
    private Coroutine _semaphoreAnimation = null;

    private bool[] _curBinary = new bool[5];
    private bool[] _curBraille = new bool[6];
    private char _curLetter = 'A';
    private float _curArrowRotation = 0;
    private char _morseCharacterToBlink;
    private Coroutine _morseBlinking = null;
    private Coroutine _morseWrong = null;
    private int _activePanelAnimations = 0;
    private sealed class PanelAnimationInfo
    {
        public Transform Cover;
        public int X1, X2, Z1, Z2;
        public Transform Backing;
        public Transform Panel;
        public bool Open;
        public PanelAnimationInfo(Transform cover, int x1, int x2, int z1, int z2, Transform backing, Transform panel, bool open)
        {
            Cover = cover;
            X1 = x1;
            X2 = x2;
            Z1 = z1;
            Z2 = z2;
            Backing = backing;
            Panel = panel;
            Open = open;
        }
    }
    private readonly Queue<PanelAnimationInfo> _topPanelAnimationQueue = new Queue<PanelAnimationInfo>();
    private readonly Queue<PanelAnimationInfo> _rightPanelAnimationQueue = new Queue<PanelAnimationInfo>();

    private readonly char[] _numberNames = new char[4];
    private readonly bool[] _shown = new bool[16];

    // ** STATIC DATA **//

    /// <summary>Lists all 288 possible 4×4 Sudokus.</summary>
    public static readonly int[][] _allSudokus = @"1234341221434321,1234341223414123,1234341241232341,1234341243212143,1234342121434312,1234342143122143,1234431221433421,1234431234212143,1234432121433412,1234432124133142,1234432131422413,1234432134122143,1243341221344321,1243341243212134,1243342121344312,1243342123144132,1243342141322314,1243342143122134,1243431221343421,1243431224313124,1243431231242431,1243431234212134,1243432121343412,1243432134122134,1324241331424231,1324241332414132,1324241341323241,1324241342313142,1324243131424213,1324243142133142,1324421324313142,1324421331422431,1324423121433412,1324423124133142,1324423131422413,1324423134122143,1342241331244231,1342241342313124,1342243131244213,1342243132144123,1342243141233214,1342243142133124,1342421321343421,1342421324313124,1342421331242431,1342421334212134,1342423124133124,1342423131242413,1423231431424231,1423231432414132,1423231441323241,1423231442313142,1423234132144132,1423234141323214,1423321423414132,1423321441322341,1423324121344312,1423324123144132,1423324141322314,1423324143122134,1432231432414123,1432231441233241,1432234131244213,1432234132144123,1432234141233214,1432234142133124,1432321421434321,1432321423414123,1432321441232341,1432321443212143,1432324123144123,1432324141232314,2134341212434321,2134341243211243,2134342112434312,2134342113424213,2134342142131342,2134342143121243,2134431212433421,2134431214233241,2134431232411423,2134431234211243,2134432112433412,2134432134121243,2143341212344321,2143341213244231,2143341242311324,2143341243211234,2143342112344312,2143342143121234,2143431212343421,2143431234211234,2143432112343412,2143432114323214,2143432132141432,2143432134121234,2314142331424231,2314142332414132,2314142341323241,2314142342313142,2314143232414123,2314143241233241,2314412314323241,2314412332411432,2314413212433421,2314413214233241,2314413232411423,2314413234211243,2341142332144132,2341142341323214,2341143231244213,2341143232144123,2341143241233214,2341143242133124,2341412312343412,2341412314323214,2341412332141432,2341412334121234,2341413214233214,2341413232141423,2413132431424231,2413132432414132,2413132441323241,2413132442313142,2413134231244231,2413134242313124,2413312413424231,2413312442311342,2413314212344321,2413314213244231,2413314242311324,2413314243211234,2431132431424213,2431132442133142,2431134231244213,2431134232144123,2431134241233214,2431134242133124,2431312412434312,2431312413424213,2431312442131342,2431312443121243,2431314213244213,2431314242131324,3124241313424231,3124241342311342,3124243112434312,3124243113424213,3124243142131342,3124243143121243,3124421313422431,3124421314322341,3124421323411432,3124421324311342,3124423113422413,3124423124131342,3142241312344321,3142241313244231,3142241342311324,3142241343211234,3142243113244213,3142243142131324,3142421313242431,3142421324311324,3142423113242413,3142423114232314,3142423123141423,3142423124131324,3214142323414132,3214142341322341,3214143221434321,3214143223414123,3214143241232341,3214143243212143,3214412313422431,3214412314322341,3214412323411432,3214412324311342,3214413214232341,3214413223411423,3241142321344312,3241142323144132,3241142341322314,3241142343122134,3241143223144123,3241143241232314,3241412314322314,3241412323141432,3241413213242413,3241413214232314,3241413223141423,3241413224131324,3412123421434321,3412123423414123,3412123441232341,3412123443212143,3412124321344321,3412124343212134,3412213412434321,3412213443211243,3412214312344321,3412214313244231,3412214342311324,3412214343211234,3421123421434312,3421123443122143,3421124321344312,3421124323144132,3421124341322314,3421124343122134,3421213412434312,3421213413424213,3421213442131342,3421213443121243,3421214312344312,3421214343121234,4123231414323241,4123231432411432,4123234112343412,4123234114323214,4123234132141432,4123234134121234,4123321413422431,4123321414322341,4123321423411432,4123321424311342,4123324114322314,4123324123141432,4132231412433421,4132231414233241,4132231432411423,4132231434211243,4132234114233214,4132234132141423,4132321414232341,4132321423411423,4132324113242413,4132324114232314,4132324123141423,4132324124131324,4213132424313142,4213132431422431,4213134221343421,4213134224313124,4213134231242431,4213134234212134,4213312413422431,4213312414322341,4213312423411432,4213312424311342,4213314213242431,4213314224311324,4231132421433412,4231132424133142,4231132431422413,4231132434122143,4231134224133124,4231134231242413,4231312413422413,4231312424131342,4231314213242413,4231314214232314,4231314223141423,4231314224131324,4312123421433421,4312123434212143,4312124321343421,4312124324313124,4312124331242431,4312124334212134,4312213412433421,4312213414233241,4312213432411423,4312213434211243,4312214312343421,4312214334211234,4321123421433412,4321123424133142,4321123431422413,4321123434122143,4321124321343412,4321124334122134,4321213412433412,4321213434121243,4321214312343412,4321214314323214,4321214332141432,4321214334121234"
        .Split(',').Select(str => str.Select(ch => ch - '1').ToArray()).ToArray();
    private static readonly int[] _semaphoreLeftFlagOrientations = new[] { 135, 90, 45, 0, 180, 180, 180, 90, 135, 0, 135, 135, 135, 135, 90, 90, 90, 90, 90, 45, 45, 0, -45, -45, 45, 225 };
    private static readonly int[] _semaphoreRightFlagOrientations = new[] { -180, -180, -180, -180, -45, -90, -135, -225, 45, -90, 0, -45, -90, -135, 45, 0, -45, -90, -135, 0, -45, -135, -90, -135, -90, -90 };
    private static readonly string[] _brailleCodes = "1,12,14,145,15,124,1245,125,24,245,13,123,134,1345,135,1234,12345,1235,234,2345,136,1236,2456,1346,13456,1356".Split(',');
    private static readonly int[][] _tapCodes = "11,12,13,14,15,21,22,23,24,25,13,31,32,33,34,35,41,42,43,44,45,51,52,53,54,55".Split(',').Select(str => str.Select(ch => ch - '0').ToArray()).ToArray();
    private static readonly string[] _simonSamplesSounds = new[] { "Kick", "Snare", "HiHat", "OpenHiHat" };
    private static readonly Dictionary<char, string> _morseCode = new Dictionary<char, string> { { 'A', ".-" }, { 'B', "-..." }, { 'C', "-.-." }, { 'D', "-.." }, { 'E', "." }, { 'F', "..-." }, { 'G', "--." }, { 'H', "...." }, { 'I', ".." }, { 'J', ".---" }, { 'K', "-.-" }, { 'L', ".-.." }, { 'M', "--" }, { 'N', "-." }, { 'O', "---" }, { 'P', ".--." }, { 'Q', "--.-" }, { 'R', ".-." }, { 'S', "..." }, { 'T', "-" }, { 'U', "..-" }, { 'V', "...-" }, { 'W', ".--" }, { 'X', "-..-" }, { 'Y', "-.--" }, { 'Z', "--.." }, { '1', ".----" }, { '2', "..---" }, { '3', "...--" }, { '4', "....-" }, { '5', "....." }, { '6', "-...." }, { '7', "--..." }, { '8', "---.." }, { '9', "----." }, { '0', "-----" } };
    private static readonly string[] _arrowDirectionNames = new[] { "down", "left", "up", "right" };

    // ** ESSENTIALS ** //

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;
        _squaresMR = new MeshRenderer[16];
        _brailleDotsMR = new MeshRenderer[6];
        _colorblind = ColorblindMode.ColorblindModeActive;

        LetterTextMesh.gameObject.SetActive(false);
        DigitTextMesh.gameObject.SetActive(false);
        ZoniTextMesh.gameObject.SetActive(false);
        SemaphoresParent.SetActive(false);
        BrailleParent.SetActive(false);
        BinaryParent.SetActive(false);
        MorseParent.SetActive(false);
        ArrowsParent.SetActive(false);
        ImageTemplate.gameObject.SetActive(false);
        MorseWrong.gameObject.SetActive(false);

        TopPanelCover.SetActive(true);
        TopPanelBacking.SetActive(false);
        SemaphoresPanel.SetActive(false);
        BinaryPanel.SetActive(false);
        RightPanelCover.SetActive(true);
        RightPanelBacking.SetActive(false);
        BraillePanel.SetActive(false);
        LettersPanel.SetActive(false);

        SemaphoresLeftSelectable.OnInteract = startLongPress(SemaphoresLeftSelectable);
        SemaphoresLeftSelectable.OnInteractEnded = releaseLongPress(SemaphoresLeftSelectable, semaphoresLeftHandDown, () => { _leftSemaphoreCw = !_leftSemaphoreCw; semaphoresLeftHandDown(); });
        SemaphoresRightSelectable.OnInteract = startLongPress(SemaphoresRightSelectable);
        SemaphoresRightSelectable.OnInteractEnded = releaseLongPress(SemaphoresRightSelectable, semaphoresRightHandDown, () => { _rightSemaphoreCw = !_rightSemaphoreCw; semaphoresRightHandDown(); });
        for (int i = 0; i < BinaryDigits.Length; i++)
            BinaryDigits[i].OnInteract = binaryDigit(i);
        for (int i = 0; i < 6; i++)
        {
            _brailleDotsMR[i] = BrailleDots[i].GetComponent<MeshRenderer>();
            _brailleDotsMR[i].material = BrailleDotOff;
            BrailleDots[i].OnInteract = brailleDot(i);
        }

        LetterUpDownButtons[0].OnInteract = lettersUpDown(6);
        LetterUpDownButtons[1].OnInteract = lettersUpDown(1);
        LetterUpDownButtons[2].OnInteract = lettersUpDown(-1);
        LetterUpDownButtons[3].OnInteract = lettersUpDown(-6);

        for (int sq = 0; sq < 16; sq++)
        {
            _squaresMR[sq] = Squares[sq].GetComponent<MeshRenderer>();
            _squaresMR[sq].material = SquareUnsolved;
            Squares[sq].OnInteract = squarePress(sq);
        }

        _solution = _allSudokus[Rnd.Range(0, _allSudokus.Length)];
        Debug.LogFormat(@"[Kudosudoku #{0}] Solution:", _moduleId);
        for (int row = 0; row < 4; row++)
            Debug.Log(_solution.Skip(4 * row).Take(4).Select(n => n + 1).JoinString(" "));

        var dist = Bomb.GetSerialNumberNumbers().First();
        if (dist == 0)
            dist = 10;
        _numberNames[0] = Bomb.GetSerialNumberLetters().First();
        for (int i = 1; i < 4; i++)
            _numberNames[i] = (char) ((_numberNames[i - 1] - 'A' + dist) % 26 + 'A');
        Debug.LogFormat(@"[Kudosudoku #{0}] Number names: {1}", _moduleId, _numberNames.Select((ch, ix) => string.Format("{0}={1}", ix + 1, ch)).JoinString(", "));

        _codings = Enum.GetValues(typeof(Coding)).Cast<Coding>().ToArray().Shuffle();

        var potentialGivens = Enumerable.Range(0, 16).ToList().Shuffle();
        // Special case: if both C and K are letter names, Tap Code can’t be a given
        if (_numberNames.Contains('C') && _numberNames.Contains('K'))
            potentialGivens.Remove(Array.IndexOf(_codings, Coding.TapCode));

        // FOR DEBUGGING: prevent a specific coding from being pre-filled
        //potentialGivens.Remove(Array.IndexOf(_codings, Coding.MorseCode));

        var givens = Ut.ReduceRequiredSet(potentialGivens, state =>
                // Special case: if both E and T are letter names, Morse Code must be a given
                !(_numberNames.Contains('E') && _numberNames.Contains('T') && !state.SetToTest.Contains(Array.IndexOf(_codings, Coding.MorseCode))) &&

                // FOR DEBUGGING: force a specific coding to be pre-filled
                //state.SetToTest.Contains(Array.IndexOf(_codings, Coding.Zoni)) &&

                // Make sure that the solution is still unique
                _allSudokus.Count(sudoku => state.SetToTest.All(ix => sudoku[ix] == _solution[ix])) == 1)
            .ToArray();

        Debug.LogFormat(@"[Kudosudoku #{0}] Codings:", _moduleId);
        for (int row = 0; row < 4; row++)
            Debug.Log(Enumerable.Range(0, 4).Select(col => _codings[4 * row + col].ToString()).JoinString(" "));

        foreach (var ix in givens)
            showSquare(ix, initial: true);
        StartCoroutine(processPanelAnimationQueue(_topPanelAnimationQueue));
        StartCoroutine(processPanelAnimationQueue(_rightPanelAnimationQueue));
    }

    private void showSquare(int sq, bool initial)
    {
        if (_shown[sq])
            return;
        _shown[sq] = true;
        if (_shown.All(b => b))
        {
            _isSolved = true;
            Debug.LogFormat(@"[Kudosudoku #{0}] Module solved.", _moduleId);
            Module.HandlePass();
            if (Bomb.GetSolvedModuleNames().Count < Bomb.GetSolvableModuleNames().Count)
                Audio.PlaySoundAtTransform("Disarm", transform);
        }
        else if (!initial)
        {
            Debug.LogFormat(@"[Kudosudoku #{0}] Square {1}{2} correct.", _moduleId, (char) ('A' + sq % 4), (char) ('1' + sq / 4));
            Audio.PlaySoundAtTransform("Correct", Squares[sq].transform);
        }

        var square = Squares[sq].transform;
        _squaresMR[sq].material = SquareSolved;
        switch (_codings[sq])
        {
            case Coding.Letters:
            {
                reparentAndActivate(LetterTextMesh.transform, square);
                LetterTextMesh.text = _numberNames[_solution[sq]].ToString();
                break;
            }

            case Coding.Digits:
            {
                reparentAndActivate(DigitTextMesh.transform, square);
                DigitTextMesh.text = (_solution[sq] + 1).ToString();
                break;
            }

            case Coding.Zoni:
            {
                reparentAndActivate(ZoniTextMesh.transform, square);

                // Usually we’ll show the letter, but if Zoni is an initial given, it has a 1-in-10 chance of showing the digit 1–4 instead.
                ZoniTextMesh.text = (initial && (Rnd.Range(0, 10) == 0) ? (char) (_solution[sq] + '1') : _numberNames[_solution[sq]]).ToString();
                break;
            }

            case Coding.Semaphores:
            {
                reparentAndActivate(SemaphoresParent.transform, square);

                var flag1 = SemaphoresParent.transform.Find("Flag1");
                var left = _semaphoreLeftFlagOrientations[_numberNames[_solution[sq]] - 'A'];
                flag1.localEulerAngles = new Vector3(90, 0, left);
                flag1.localScale = new Vector3(isLeftSemaphoreFlipped(left) ? -1 : 1, 1, 1);

                var flag2 = SemaphoresParent.transform.Find("Flag2");
                var right = _semaphoreRightFlagOrientations[_numberNames[_solution[sq]] - 'A'];
                flag2.localEulerAngles = new Vector3(90, 0, right);
                flag2.localScale = new Vector3(isRightSemaphoreFlipped(right) ? 1 : -1, 1, 1);
                break;
            }

            case Coding.Braille:
            {
                reparentAndActivate(BrailleParent.transform, square);
                for (char bd = '1'; bd <= '6'; bd++)
                    BrailleParent.transform.Find("Dot" + bd).gameObject.SetActive(_brailleCodes[_numberNames[_solution[sq]] - 'A'].Contains(bd));
                break;
            }

            case Coding.Binary:
            {
                reparentAndActivate(BinaryParent.transform, square);
                var name = _numberNames[_solution[sq]] - 'A' + 1;
                var binary = Enumerable.Range(0, 5).Select<int, object>(bit => (name & (1 << bit)) != 0 ? "1" : "0").ToArray();
                BinaryParent.transform.Find("DigitsFront").GetComponent<TextMesh>().text = string.Format("{4}{3}\n{2}{1}{0}", binary);
                break;
            }

            case Coding.Arrows:
            {
                reparentAndActivate(ArrowsParent.transform, square);
                ArrowsParent.transform.localEulerAngles = new Vector3(0, 0, -90 * (_solution[sq] + 1));
                break;
            }

            case Coding.MorseCode:
            {
                reparentAndActivate(MorseParent.transform, square);

                // Usually we’ll blink the letter, but:
                //  • If Morse Code is an initial given, it has a 1-in-5 chance of blinking the digit 1–4 instead.
                //  • If Morse Code is an initial given, would have to blink E or T, and both E and T are number names, definitely blink the digit instead
                _morseCharacterToBlink = initial && (Rnd.Range(0, 5) == 0 || (_numberNames.Contains('E') && _numberNames.Contains('T') && "ET".Contains(_numberNames[_solution[sq]]))) ? (char) (_solution[sq] + '1') : _numberNames[_solution[sq]];
                _morseBlinking = StartCoroutine(blinkMorse());
                break;
            }

            case Coding.TapCode:
            case Coding.SimonSamples:
                // Do nothing, as these are audio-only
                break;

            case Coding.MaritimeFlags:
            {
                var mr = createGraphic(square, 1);
                var flagName = "Flag-" + (initial && Rnd.Range(0, 3) == 0 ? (_solution[sq] + 1).ToString() : _numberNames[_solution[sq]].ToString());
                mr.material.mainTexture = MaritimeFlagTextures.First(t => t.name == flagName);
                break;
            }

            default:
            {
                var textures =
                    _codings[sq] == Coding.Snooker ? (_colorblind ? SnookerBallTexturesCB : SnookerBallTextures) :
                    _codings[sq] == Coding.Astrology ? AstrologyTextures :
                    _codings[sq] == Coding.Mahjong ? MahjongTextures :
                    _codings[sq] == Coding.CardSuits ? CardSuitTextures :
                    _codings[sq] == Coding.ChessPieces ? ChessPieceTextures : null;
                var mr = createGraphic(square, 1);
                mr.material.mainTexture = textures[_solution[sq]];
                if (_codings[sq] == Coding.Snooker)
                    _snookerBallGraphic = mr;
                break;
            }
        }
    }

    // ** CLICK HANDLERS ** //

    private KMSelectable.OnInteractHandler squarePress(int sq)
    {
        return delegate
        {
            if (_shown[sq])
            {
                // Re-play a sound if there is one
                switch (_codings[sq])
                {
                    case Coding.TapCode:
                        StartCoroutine(playTapCode(_tapCodes[_numberNames[_solution[sq]] - 'A'], Squares[sq].transform));
                        break;

                    case Coding.SimonSamples:
                        Audio.PlaySoundAtTransform(_simonSamplesSounds[_solution[sq]], Squares[sq].transform);
                        break;

                    // Allow turning Morse Code on and off
                    case Coding.MorseCode:
                        if (_morseBlinking != null)
                        {
                            StopCoroutine(_morseBlinking);
                            _morseBlinking = null;
                            MorseLed.material = MorseLedOff;
                        }
                        else
                            _morseBlinking = StartCoroutine(blinkMorse());
                        break;
                }
                return false;
            }

            // Another square is still active — submit it
            if (_activeSquare != null && _activeSquare.Value != sq)
            {
                if (_delaySubmitCoroutine != null)
                {
                    StopCoroutine(_delaySubmitCoroutine);
                    _delaySubmitAction();
                    _delaySubmitAction = null;
                    _delaySubmitCoroutine = null;
                }
                else if (_codings[_activeSquare.Value] == Coding.MorseCode)
                    interpretMorseCode(_activeSquare.Value);
                else if (_codings[_activeSquare.Value] == Coding.TapCode)
                    interpretTapCode(_activeSquare.Value);
                else
                    Squares[_activeSquare.Value].OnInteract();
            }

            // User clicked on an unsolved square
            _activeSquare = sq;
            switch (_codings[sq])
            {
                case Coding.MorseCode:
                    // Upon the first click, the square turns “dark red”
                    _squaresMR[sq].material = SquareExpectingMorse;
                    _morsePressTimes = new List<float>();
                    setMorseColorblindText(sq);

                    // Henceforth, the square expects Morse code input
                    Squares[sq].OnInteract = morseMouseDown(sq);
                    Squares[sq].OnInteractEnded = morseMouseUp(sq);
                    _waitingToSubmitMorseOrTapCode = StartCoroutine(waitToInterpretMorseCode(sq));
                    break;

                case Coding.TapCode:
                    // Upon the first click, the square turns “blue”
                    _squaresMR[sq].material = SquareExpectingTapCode;
                    _tapCodeInput = new List<int>();
                    setTapCodeColorblindText(sq);

                    // Henceforth, the square expects Tap Code input
                    Squares[sq].OnInteract = tapCodeMouseDown(sq);
                    break;

                case Coding.Semaphores:
                case Coding.Binary:
                    _topPanelAnimationQueue.Enqueue(new PanelAnimationInfo(TopPanelCover.transform, 0, 0, 0, -7, TopPanelBacking.transform, (_codings[sq] == Coding.Semaphores ? SemaphoresPanel : BinaryPanel).transform, open: true));
                    _squaresMR[sq].material = SquareExpectingPanelAnswer;
                    Squares[sq].OnInteract = _codings[sq] == Coding.Semaphores ? semaphoresSubmit(sq) : binarySubmit(sq);
                    break;

                case Coding.Braille:
                case Coding.Letters:
                    _rightPanelAnimationQueue.Enqueue(new PanelAnimationInfo(RightPanelCover.transform, 0, -7, 0, 0, RightPanelBacking.transform, (_codings[sq] == Coding.Braille ? BraillePanel : LettersPanel).transform, open: true));
                    _squaresMR[sq].material = SquareExpectingPanelAnswer;
                    Squares[sq].OnInteract = _codings[sq] == Coding.Braille ? brailleSubmit(sq) : lettersSubmit(sq);
                    break;

                case Coding.Digits:
                    reparentAndActivate(DigitTextMesh.transform, Squares[sq].transform);
                    startCycle(sq, i =>
                    {
                        Audio.PlaySoundAtTransform("Selection", Squares[sq].transform);
                        DigitTextMesh.text = (i + 1).ToString();
                    }, () => { DigitTextMesh.gameObject.SetActive(false); }, "Digit", 2f, new[] { "1", "2", "3", "4" }, new[] { "1", "2", "3", "4" });
                    break;

                case Coding.MaritimeFlags:
                    // Special case: if one of the number names is Q, we don’t want that flag to be the first in the cycle because it looks too much like the Morse/Tap Code prompt
                    var avoidIndex = _numberNames.Contains('Q') ? Array.IndexOf(_numberNames, 'Q') : (int?) null;
                    startGraphicsCycle(sq, 1, _numberNames.Select(ch => "Flag-" + ch).Select(name => MaritimeFlagTextures.First(t => t.name == name)).ToArray(),
                        "maritime flag", 2f, _numberNames.Select(ch => ch.ToString()).ToArray(), _numberNames.Select(ch => _tpMaritimeFlagNames[ch - 'A']).ToArray(), avoidIndex: avoidIndex);
                    break;

                case Coding.Astrology:
                    startGraphicsCycle(sq, 1, AstrologyTextures, "astrological symbol", 2f, new[] { "fire", "water", "earth", "air" });
                    break;

                case Coding.Snooker:
                    startGraphicsCycle(sq, 1, SnookerBallTextures, "Snooker ball", 2f, new[] { "red", "yellow", "green", "brown" }, cbTextures: SnookerBallTexturesCB);
                    break;

                case Coding.CardSuits:
                    startGraphicsCycle(sq, 1, CardSuitTextures, "card suit", 2f, new[] { "spades", "hearts", "clubs", "diamonds" });
                    break;

                case Coding.Mahjong:
                    startGraphicsCycle(sq, 1, MahjongTextures, "Mahjong tile", 2f, new[] { "plum", "orchid", "chrysanthemum", "bamboo" });
                    break;

                case Coding.ChessPieces:
                    startGraphicsCycle(sq, 1, ChessPieceTextures, "Chess piece", 2f, new[] { "rook", "knight", "bishop", "queen" });
                    break;

                case Coding.Zoni:
                    reparentAndActivate(ZoniTextMesh.transform, Squares[sq].transform);
                    startCycle(sq, i =>
                    {
                        Audio.PlaySoundAtTransform("Selection", Squares[sq].transform);
                        ZoniTextMesh.text = _numberNames[i].ToString();
                    }, () => { ZoniTextMesh.gameObject.SetActive(false); }, "Zoni letter", 2f, _numberNames.Select(ch => ch.ToString()).ToArray(), _numberNames.Select(ch => _tpZoniLetterNames[ch - 'A']).ToArray());
                    break;

                case Coding.SimonSamples:
                    startCycle(sq, i => { Audio.PlaySoundAtTransform(_simonSamplesSounds[i], Squares[sq].transform); }, null, "Simon Samples sample", 2f, _simonSamplesSounds, tpVisibleNames: true);
                    break;

                case Coding.Arrows:
                    reparentAndActivate(ArrowsParent.transform, Squares[sq].transform);
                    _squaresMR[sq].material = SquareExpectingAnswer;
                    var coroutine = StartCoroutine(spinArrow());
                    Squares[sq].OnInteract = delegate
                    {
                        StopCoroutine(coroutine);
                        var input = _curArrowRotation < 45 ? 3 : _curArrowRotation < 135 ? 2 : _curArrowRotation < 225 ? 1 : _curArrowRotation < 315 ? 0 : 3;
                        ArrowsParent.gameObject.SetActive(false);
                        submitAnswer(sq, input, "arrow direction " + _arrowDirectionNames[input], _arrowDirectionNames[_solution[sq]]);
                        return false;
                    };
                    break;
            }
            return false;
        };
    }

    private void setMorseColorblindText(int sq)
    {
        if (_colorblind)
            setTpCbText(sq, "dark\nred", 48);
    }

    private void setTapCodeColorblindText(int sq)
    {
        if (_colorblind)
            setTpCbText(sq, "blue", 48);
    }

    private KMSelectable.OnInteractHandler morseMouseDown(int sq)
    {
        return delegate
        {
            // Disappear color-blind text
            if (_extraText != null)
                _extraText.gameObject.SetActive(false);

            // Check if mouse is already down. This should never happen.
            if (_morsePressTimes.Count % 2 == 1)
                return false;
            _morsePressTimes.Add(Time.time);
            _squaresMR[sq].material = SquareExpectingMorseDown;
            return false;
        };
    }

    private Action morseMouseUp(int i)
    {
        return delegate
        {
            // Check if mouse is already up. This should ideally never happen, but could be because the OnInteractEnded handler is swapped out when the user presses the square the first time:
            if (_morsePressTimes.Count % 2 == 0)
                return;
            _morsePressTimes.Add(Time.time);
            _squaresMR[i].material = SquareExpectingMorse;
        };
    }

    private IEnumerator waitToInterpretMorseCode(int sq)
    {
        while (_morsePressTimes.Count == 0 || _morsePressTimes.Count % 2 == 1 || Time.time - _morsePressTimes.Last() < 2)
            yield return null;

        // User finished entering Morse Code.
        interpretMorseCode(sq);
    }

    private void interpretMorseCode(int sq)
    {
        if (_waitingToSubmitMorseOrTapCode != null)
        {
            StopCoroutine(_waitingToSubmitMorseOrTapCode);
            _waitingToSubmitMorseOrTapCode = null;
        }

        _activeSquare = null;
        Squares[sq].OnInteract = squarePress(sq);
        Squares[sq].OnInteractEnded = null;

        // If there’s only one tap, no matter how long, accept it as input for either E or T.
        string reasonForWrongAnswer;
        if (_morsePressTimes.Count == 2)
        {
            if ("ET".Contains(_numberNames[_solution[sq]]))
                goto correctAnswer;
            reasonForWrongAnswer = "Morse code for E or T";
            MorseWrong.text = "E/T";
            goto wrongAnswer;
        }

        // Calculate the average gap length and deduce dots/dashes from that (same as Morsematics)
        var totalGapTime = 0f;
        for (int t = 1; t < _morsePressTimes.Count - 1; t += 2)
            totalGapTime += _morsePressTimes[t + 1] - _morsePressTimes[t];
        var averageGapTime = totalGapTime / (_morsePressTimes.Count / 2 - 1);
        var morseCode = "";
        for (int t = 0; t < _morsePressTimes.Count; t += 2)
            morseCode += (_morsePressTimes[t + 1] - _morsePressTimes[t]) < 2 * averageGapTime ? "." : "-";
        var character = _morseCode.FirstOrDefault(kvp => kvp.Value == morseCode);
        if (character.Key == _numberNames[_solution[sq]])
            goto correctAnswer;
        reasonForWrongAnswer = character.Key == '\0' ? "an invalid Morse code" : "Morse code for " + character.Key;
        MorseWrong.text = character.Key == '\0' ? "?" : character.Key.ToString();

        wrongAnswer:
        _squaresMR[sq].material = SquareUnsolved;
        _morsePressTimes = null;
        reparentAndActivate(MorseWrong.transform, Squares[sq].transform);
        if (_morseWrong != null)
            StopCoroutine(_morseWrong);
        _morseWrong = StartCoroutine(disappearWrongMorse());
        strikeAndReshuffle(sq, reasonForWrongAnswer, _numberNames[_solution[sq]].ToString());
        return;

        correctAnswer:
        Debug.LogFormat(@"[Kudosudoku #{0}] Square {1}{2}: correct Morse code input ({3}).", _moduleId, (char) ('A' + sq % 4), (char) ('1' + sq / 4), _numberNames[_solution[sq]]);
        showSquare(sq, initial: false);
        _morsePressTimes = null;
    }

    private IEnumerator disappearWrongMorse()
    {
        yield return new WaitForSeconds(2.4f);
        MorseWrong.gameObject.SetActive(false);
        _morseWrong = null;
    }

    private KMSelectable.OnInteractHandler tapCodeMouseDown(int sq)
    {
        return delegate
        {
            // Disappear color-blind text
            if (_extraText != null)
                _extraText.gameObject.SetActive(false);

            if (_tapCodeLastCodeStillActive)
                _tapCodeInput[_tapCodeInput.Count - 1]++;
            else
            {
                _tapCodeLastCodeStillActive = true;
                _tapCodeInput.Add(1);
            }
            if (_waitingToSubmitMorseOrTapCode != null)
                StopCoroutine(_waitingToSubmitMorseOrTapCode);
            _waitingToSubmitMorseOrTapCode = StartCoroutine(acknowledgeTapCode(sq));
            return false;
        };
    }

    private IEnumerator acknowledgeTapCode(int sq)
    {
        yield return new WaitForSeconds(1f);
        Audio.PlaySoundAtTransform("MiniTap", Squares[sq].transform);
        _tapCodeLastCodeStillActive = false;

        if (_tapCodeInput.Count < 2)
            yield break;

        // User finished entering Tap Code.
        interpretTapCode(sq);
    }

    private void interpretTapCode(int sq)
    {
        if (_waitingToSubmitMorseOrTapCode != null)
        {
            StopCoroutine(_waitingToSubmitMorseOrTapCode);
            _waitingToSubmitMorseOrTapCode = null;
        }

        _activeSquare = null;
        Squares[sq].OnInteract = squarePress(sq);

        var expected = _tapCodes[_numberNames[_solution[sq]] - 'A'];
        if (expected.SequenceEqual(_tapCodeInput))
        {
            Debug.LogFormat(@"[Kudosudoku #{0}] Square {1}{2}: correct Tap Code input ({3}).", _moduleId, (char) ('A' + sq % 4), (char) ('1' + sq / 4), _tapCodeInput.JoinString(", "));
            showSquare(sq, initial: false);
        }
        else
        {
            strikeAndReshuffle(sq, string.Format("the Tap Code “{0}”", _tapCodeInput.JoinString(", ")), string.Format("“{0}”", expected.JoinString(", ")));
            _squaresMR[sq].material = SquareUnsolved;
        }

        _tapCodeInput = null;
    }

    private void semaphoresLeftHandDown()
    {
        if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Semaphores)
            return;

        if (_leftSemaphore == (_leftSemaphoreCw ? -45 : 225))
            _leftSemaphoreCw = !_leftSemaphoreCw;
        var oldLeft = _leftSemaphore;
        _leftSemaphore += _leftSemaphoreCw ? -45 : 45;
        startSemaphoreAnimation(oldLeft, _rightSemaphore);
    }

    private void semaphoresRightHandDown()
    {
        if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Semaphores)
            return;

        if (_rightSemaphore == (_rightSemaphoreCw ? -225 : 45))
            _rightSemaphoreCw = !_rightSemaphoreCw;
        var oldRight = _rightSemaphore;
        _rightSemaphore += _rightSemaphoreCw ? -45 : 45;
        startSemaphoreAnimation(_leftSemaphore, oldRight);
    }

    private KMSelectable.OnInteractHandler semaphoresSubmit(int sq)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Semaphores)
                return false;

            var leftExpect = _semaphoreLeftFlagOrientations[_numberNames[_solution[sq]] - 'A'];
            var rightExpect = _semaphoreRightFlagOrientations[_numberNames[_solution[sq]] - 'A'];
            if (_leftSemaphore == leftExpect && _rightSemaphore == rightExpect)
                showSquare(sq, initial: false);
            else
            {
                var orientationNames = new Dictionary<int, string>();
                orientationNames[0] = "N";
                orientationNames[45] = "NW";
                orientationNames[90] = "W";
                orientationNames[135] = orientationNames[-225] = "SW";
                orientationNames[180] = orientationNames[-180] = "S";
                orientationNames[225] = orientationNames[-135] = "SE";
                orientationNames[-90] = "E";
                orientationNames[-45] = "NE";
                strikeAndReshuffle(sq,
                    string.Format("Semaphores {0}.{1}", orientationNames[_leftSemaphore], orientationNames[_rightSemaphore]),
                    string.Format("{0}.{1}", orientationNames[leftExpect], orientationNames[rightExpect]));
            }

            _topPanelAnimationQueue.Enqueue(new PanelAnimationInfo(TopPanelCover.transform, 0, 0, -7, 0, TopPanelBacking.transform, SemaphoresPanel.transform, open: false));
            _activeSquare = null;
            Squares[sq].OnInteract = squarePress(sq);
            return false;
        };
    }

    private KMSelectable.OnInteractHandler binaryDigit(int dgt)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Binary)
                return false;

            _curBinary[dgt] = !_curBinary[dgt];
            BinaryDigitDisplays[dgt].text = _curBinary[dgt] ? "1" : "0";
            return false;
        };
    }

    private KMSelectable.OnInteractHandler binarySubmit(int sq)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Binary)
                return false;

            var expected = Convert.ToString(_numberNames[_solution[sq]] - 'A' + 1, 2).PadLeft(5, '0');
            var provided = _curBinary.Select(b => b ? "1" : "0").JoinString();

            if (provided == expected)
                showSquare(sq, initial: false);
            else
                strikeAndReshuffle(sq, string.Format("Binary {0}", provided), expected);

            _topPanelAnimationQueue.Enqueue(new PanelAnimationInfo(TopPanelCover.transform, 0, 0, -7, 0, TopPanelBacking.transform, BinaryPanel.transform, open: false));
            _activeSquare = null;
            Squares[sq].OnInteract = squarePress(sq);
            return false;
        };
    }

    private KMSelectable.OnInteractHandler brailleDot(int bd)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Braille)
                return false;

            _curBraille[bd] = !_curBraille[bd];
            _brailleDotsMR[bd].material = _curBraille[bd] ? BrailleDotOn : BrailleDotOff;
            return false;
        };
    }

    private KMSelectable.OnInteractHandler brailleSubmit(int sq)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Braille)
                return false;

            var expected = _brailleCodes[_numberNames[_solution[sq]] - 'A'];
            var provided = _curBraille.Select((b, ix) => b ? (ix + 1).ToString() : "").JoinString();

            if (provided == expected)
                showSquare(sq, initial: false);
            else
                strikeAndReshuffle(sq, string.Format("Braille {0}", provided), expected);

            _rightPanelAnimationQueue.Enqueue(new PanelAnimationInfo(RightPanelCover.transform, -7, 0, 0, 0, RightPanelBacking.transform, BraillePanel.transform, open: false));
            _activeSquare = null;
            Squares[sq].OnInteract = squarePress(sq);
            return false;
        };
    }

    private KMSelectable.OnInteractHandler lettersUpDown(int offset)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Letters)
                return false;

            _curLetter = (char) ((_curLetter - 'A' + offset + 26) % 26 + 'A');
            LetterDisplay.text = _curLetter.ToString();
            return false;
        };
    }

    private KMSelectable.OnInteractHandler lettersSubmit(int sq)
    {
        return delegate
        {
            if (_isSolved || _activeSquare == null || _codings[_activeSquare.Value] != Coding.Letters)
                return false;

            if (_curLetter == _numberNames[_solution[sq]])
                showSquare(sq, initial: false);
            else
                strikeAndReshuffle(sq, string.Format("Letter {0}", _curLetter), _numberNames[_solution[sq]].ToString());

            _rightPanelAnimationQueue.Enqueue(new PanelAnimationInfo(RightPanelCover.transform, -7, 0, 0, 0, RightPanelBacking.transform, LettersPanel.transform, open: false));
            _activeSquare = null;
            Squares[sq].OnInteract = squarePress(sq);
            return false;
        };
    }

    // ** ANIMATION/SOUND COROUTINES ** //

    private IEnumerator slidePanelPanel(Transform backing, Transform panel, bool open)
    {
        _activePanelAnimations++;
        yield return null;
        backing.localPosition = new Vector3(0, open ? -1 : 0, 0);
        panel.localPosition = new Vector3(0, open ? -1 : 0, 0);
        backing.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);
        if (open)
            yield return new WaitForSeconds(.25f);
        var durPanel = 1f;
        var elapsed = 0f;
        while (elapsed < durPanel)
        {
            elapsed += Time.deltaTime;
            var e = open
                ? Easing.OutQuad(Mathf.Min(elapsed, durPanel), -1, 0, durPanel)
                : Easing.InQuad(Mathf.Min(elapsed, durPanel), 0, -1, durPanel);
            backing.localPosition = new Vector3(0, e, 0);
            panel.localPosition = new Vector3(0, e, 0);
            yield return null;
        }

        if (!open)
        {
            backing.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
        }
        _activePanelAnimations--;
    }

    private IEnumerator slidePanelCover(Transform cover, int x1, int x2, int z1, int z2, bool open)
    {
        _activePanelAnimations++;
        if (!open)
            yield return new WaitForSeconds(.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, cover);
        yield return null;
        var durCover = .75f;
        var elapsed = 0f;
        while (elapsed < durCover)
        {
            elapsed += Time.deltaTime;
            cover.localPosition = new Vector3(Easing.InOutQuad(Mathf.Min(elapsed, durCover), x1, x2, durCover), 0, Easing.InOutQuad(Mathf.Min(elapsed, durCover), z1, z2, durCover));
            yield return null;
        }
        _activePanelAnimations--;
    }

    private IEnumerator processPanelAnimationQueue(Queue<PanelAnimationInfo> queue)
    {
        yield return null;

        while (true)
        {
            yield return new WaitUntil(() => queue.Count > 0);
            var element = queue.Dequeue();
            StartCoroutine(slidePanelCover(element.Cover, element.X1, element.X2, element.Z1, element.Z2, element.Open));
            StartCoroutine(slidePanelPanel(element.Backing, element.Panel, element.Open));
            yield return new WaitUntil(() => _activePanelAnimations == 0);
        }
    }

    private IEnumerator playTapCode(int[] taps, Transform transf)
    {
        yield return new WaitForSeconds(.1f);
        for (int i = 0; i < taps.Length; i++)
        {
            for (int a = 0; a < taps[i]; a++)
            {
                Audio.PlaySoundAtTransform("Tap", transf);
                yield return new WaitForSeconds(.5f);
            }
            yield return new WaitForSeconds(.5f);
        }
    }

    private IEnumerator blinkMorse()
    {
        yield return new WaitForSeconds(.25f);
        var morse = _morseCode[_morseCharacterToBlink];
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

    private void startSemaphoreAnimation(int oldLeft, int oldRight)
    {
        if (_semaphoreAnimation != null)
            StopCoroutine(_semaphoreAnimation);
        _semaphoreAnimation = StartCoroutine(semaphoreAnimation(oldLeft, oldRight));
    }

    private bool isLeftSemaphoreFlipped(int angle) { return angle < 0 || angle > 180; }
    private bool isRightSemaphoreFlipped(int angle) { return angle > 0 || angle < -180; }

    private IEnumerator semaphoreAnimation(int oldLeft, int oldRight)
    {
        yield return null;

        var duration = .2f;
        var elapsed = 0f;

        float leftStart = oldLeft;
        float rightStart = oldRight;
        float leftFStart = isLeftSemaphoreFlipped(oldLeft) ? 180 : 0;
        float rightFStart = isRightSemaphoreFlipped(oldRight) ? 180 : 0;

        float leftEnd = _leftSemaphore;
        float rightEnd = _rightSemaphore;
        float leftFEnd = isLeftSemaphoreFlipped(_leftSemaphore) ? 180 : 0;
        float rightFEnd = isRightSemaphoreFlipped(_rightSemaphore) ? 180 : 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SemaphoresLeftHand.localEulerAngles = new Vector3(0, 0, Easing.InOutQuad(Mathf.Min(elapsed, duration), leftStart, leftEnd, duration));
            SemaphoresRightHand.localEulerAngles = new Vector3(0, 0, Easing.InOutQuad(Mathf.Min(elapsed, duration), rightStart, rightEnd, duration));
            SemaphoresLeftFlag.localEulerAngles = new Vector3(0, Easing.InOutQuad(Mathf.Min(elapsed, duration), leftFStart, leftFEnd, duration), 0);
            SemaphoresRightFlag.localEulerAngles = new Vector3(0, Easing.InOutQuad(Mathf.Min(elapsed, duration), rightFStart, rightFEnd, duration), 0);
            yield return null;
        }

        _semaphoreAnimation = null;
    }

    private IEnumerator spinArrow()
    {
        _curArrowRotation = Rnd.Range(0f, 360f);
        var direction = Rnd.Range(0, 2) == 0 ? 360 : -360;
        while (true)
        {
            _curArrowRotation = (_curArrowRotation + direction * Time.deltaTime + 360) % 360;
            ArrowsParent.transform.localEulerAngles = new Vector3(0, 0, _curArrowRotation);
            yield return null;
        }
    }

    // ** CYCLES (graphics, sounds) ** //

    private void startCycle(int sq, Action<int> showOption, Action cleanUp, string codingName, float submissionDelay, string[] answerNames, string[] tpAnswerNames = null, bool tpVisibleNames = false, int? avoidIndex = null)
    {
        _squaresMR[sq].material = SquareExpectingAnswer;
        var cycle = Enumerable.Range(0, 4).ToArray().Shuffle();
        while (avoidIndex != null && cycle[0] == avoidIndex.Value)
            cycle.Shuffle();
        _isCurrentCyclingAnswerCorrect = _solution[sq] == cycle[0];
        showOption(cycle[0]);
        var ix = 0;

        _delaySubmitAction = () =>
        {
            if (cleanUp != null)
                cleanUp();
            submitAnswer(sq, cycle[ix], codingName + " " + answerNames[cycle[ix]], answerNames[_solution[sq]]);
            _tpCyclingNames = null;
        };

        _delaySubmitCoroutine = StartCoroutine(submitAfterDelay(submissionDelay));
        if (tpAnswerNames == null)
        {
            tpAnswerNames = new string[4];
            for (int i = 0; i < 4; i++)
                tpAnswerNames[cycle[i]] = "S" + (i + 1);
        }
        _tpCyclingNames = cycle.Select(c => tpAnswerNames[c]).ToArray();
        if (TwitchPlaysActive && tpVisibleNames)
            setTpCbText(sq, tpAnswerNames[cycle[0]], 64);

        Squares[sq].OnInteract = delegate
        {
            if (_delaySubmitCoroutine != null)
                StopCoroutine(_delaySubmitCoroutine);
            ix = (ix + 1) % 4;
            if (TwitchPlaysActive && tpVisibleNames)
                setTpCbText(sq, tpAnswerNames[cycle[ix]], 64);
            _tpCyclingName = tpAnswerNames[cycle[ix]];
            _isCurrentCyclingAnswerCorrect = _solution[sq] == cycle[ix];
            showOption(cycle[ix]);
            _delaySubmitCoroutine = StartCoroutine(submitAfterDelay(submissionDelay));
            return false;
        };
    }

    private void startGraphicsCycle(int sq, float widthRatio, Texture[] textures, string codingName, float submissionDelay, string[] answerNames, string[] tpAnswerNames = null, Texture[] cbTextures = null, int? avoidIndex = null)
    {
        var gr = createGraphic(Squares[sq].transform, widthRatio);
        startCycle(sq,
            i =>
            {
                Audio.PlaySoundAtTransform("Selection", Squares[sq].transform);
                gr.material.mainTexture = _colorblind && cbTextures != null ? cbTextures[i] : textures[i];
            },
            () =>
            {
                gr.gameObject.SetActive(false);
                _unusedImageObjects.Push(gr.gameObject);
            },
            codingName, submissionDelay, answerNames, tpAnswerNames ?? answerNames, avoidIndex: avoidIndex);
    }

    private IEnumerator submitAfterDelay(float submissionDelay)
    {
        yield return new WaitForSeconds(submissionDelay);
        if (_delaySubmitAction != null)
            _delaySubmitAction();
        _delaySubmitAction = null;
        _delaySubmitCoroutine = null;
    }

    // ** OTHER FUNCTIONS ** //

    private void submitAnswer(int sq, int answer, string whatEntered, string expected)
    {
        if (answer == _solution[sq])
            showSquare(sq, initial: false);
        else
            strikeAndReshuffle(sq, whatEntered, expected);
        _activeSquare = null;
        Squares[sq].OnInteract = squarePress(sq);
        if (_extraText != null)
            _extraText.gameObject.SetActive(false);
    }

    private void strikeAndReshuffle(int sq, string whatEntered, string expected)
    {
        Module.HandleStrike();

        Debug.LogFormat(@"[Kudosudoku #{0}] You received a strike on square {1}{2} because you entered {3} while it expected {4}.", _moduleId, (char) ('A' + sq % 4), (char) ('1' + sq / 4), whatEntered, expected);

        _squaresMR[sq].material = SquareUnsolved;

        var oldCodings = _codings.ToArray();
        var oldIndexes = Enumerable.Range(0, 16).Where(ix => !_shown[ix]).ToArray();
        var newIndexes = oldIndexes.ToArray().Shuffle();
        for (int i = 0; i < oldIndexes.Length; i++)
            _codings[newIndexes[i]] = oldCodings[oldIndexes[i]];

        Debug.LogFormat(@"[Kudosudoku #{0}] New codings:", _moduleId);
        for (int row = 0; row < 4; row++)
            Debug.Log(Enumerable.Range(0, 4).Select(col => _codings[4 * row + col].ToString()).JoinString(" "));
    }

    private void reparentAndActivate(Transform obj, Transform newParent)
    {
        if (obj.parent != newParent)
            obj.SetParent(newParent, false);
        obj.gameObject.SetActive(true);
    }

    private MeshRenderer createGraphic(Transform square, float widthRatio)
    {
        var obj = _unusedImageObjects.Count > 0 ? _unusedImageObjects.Pop() : Instantiate(ImageTemplate);
        obj.transform.parent = square;
        obj.transform.localPosition = new Vector3(0, 0, -.001f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = new Vector3(.95f * widthRatio, .95f, 1);
        obj.SetActive(true);
        return obj.GetComponent<MeshRenderer>();
    }

    private KMSelectable.OnInteractHandler startLongPress(KMSelectable selectable)
    {
        return delegate
        {
            if (_longPressCoroutine != null)
                StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = StartCoroutine(longPress(selectable));
            return false;
        };
    }

    private IEnumerator longPress(KMSelectable selectable)
    {
        _longPress = false;
        _longPressSelectable = selectable;
        yield return new WaitForSeconds(.3f);
        _longPress = true;
        Audio.PlaySoundAtTransform("Selection", selectable.transform);
        _longPressCoroutine = null;
    }

    private Action releaseLongPress(KMSelectable selectable, Action shortPress, Action longPress)
    {
        return delegate
        {
            if (_longPressSelectable != selectable)
                return;
            if (_longPressCoroutine != null)
                StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
            if (_longPress)
                longPress();
            else
                shortPress();
        };
    }

    private void setTpCbText(int sq, string text, int fontSize)
    {
        if (_extraText == null)
        {
            _extraText = Instantiate(LetterTextMesh);
            _extraText.name = "TpCbText";
            _extraText.color = Color.white;
        }
        _extraText.text = text;
        _extraText.fontSize = fontSize;
        reparentAndActivate(_extraText.transform, Squares[sq].transform);
        _extraText.transform.localPosition = new Vector3(0, -.04f, -.0001f);
    }

    // ** TWITCH PLAYS ** //

    private static readonly string[] _tpMaritimeFlagNames = new[] {
        // We only need the letters A-Z for submitting an answer in TP.
        "white-blue with cutout",
        "red with cutout",
        "blue-white-red-white-blue horizontal",
        "yellow-blue-yellow horizontal",
        "blue-red horizontal",
        "red diamond on white",
        "yellow-blue vertical stripes",
        "white-red vertical",
        "black dot on yellow",
        "blue-white-blue horizontal",
        "yellow-blue vertical",
        "yellow-black checkerboard",
        "white saltire on blue",
        "blue-white checkerboard",
        "yellow-red diagonal",
        "white square on blue",
        "yellow",
        "yellow cross on red",
        "blue square on white",
        "red-white-blue vertical",
        "red-white checkerboard",
        "red saltire on white",
        "red square on white square on blue",
        "blue cross on white",
        "yellow-red diagonal stripes",
        "yellow-blue-red-black diagonal quadrants"
    };

    private static readonly string[] _tpZoniLetterNames = new[] {
        // We only need the letters A-Z for submitting an answer in TP.
        /* A */ "3 dots triangle inside circle",
        /* B */ "filled circle inside outline with dots on top and bottom",
        /* C */ "filled circle inside left half-circle",
        /* D */ "2 dots vertical inside right half-circle",
        /* E */ "3 dots vertical inside circle",
        /* F */ "left half-circle with dots on each end",
        /* G */ "dot inside circle with gaps at N, E, S and W",
        /* H */ "3 dots horizontal inside circle with gaps at N and S",
        /* I */ "filled circle with dot above and below",
        /* J */ "filled circle with quarter-circles at NW and SE",
        /* K */ "3 dots triangle with half-circle on left",
        /* L */ "filled circle surrounded by 3 unequal arcs",
        /* M */ "3 dots horizontal below top half-circle",
        /* N */ "2 quarter-circles with dots below",
        /* O */ "filled circle",
        /* P */ "filled circle inside left half-circle with dots on top and bottom",
        /* Q */ "three-quarter-circle with dot inside gap at SE",
        /* R */ "circle with gaps at NW, NE, SE and SW",
        /* S */ "circle with gaps at NW and SE",
        /* T */ "3 dots diagonal inside top-left half-circle",
        /* U */ "3 dots diagonal inside bottom-right half-circle",
        /* V */ "filled circle with dots at N, SE and SW",
        /* W */ "3 dots vertical inside circle with gaps at N and S",
        /* X */ "filled circle inside circle with gaps at N, E, S and W",
        /* Y */ "filled circle inside circle",
        /* Z */ "2 dots angled inside circle with gaps at E and W",
    };
    private string[] _tpCyclingNames;
    private string _tpCyclingName;
    private Coroutine _tpRepeatedlyClicking;

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} A1 [tap on a square] | !{0} names [show names for answers currently cycling] | !{0} S1 [stop cycling at answer with that name] | !{0} -.- [Morse code] | !{0} 13 [Tap Code or Braille] | !{0} SW.N [Semaphore (cardinals)] | !{0} 8.12 [Semaphore (clockface)] | !{0} DL U [Semaphore (directions)] | !{0} 01011 [Binary] | !{0} K [Letters] | All code submissions can be prepended by “submit” | !{0} colorblind";
    private bool TwitchPlaysActive = false;
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        TwitchPlaysActive = true;
        Debug.LogFormat(@"<Kudosudoku #{0}> TP: {1}", _moduleId, command);

        Match m;

        if (command.Trim().Equals("colorblind", StringComparison.InvariantCultureIgnoreCase))
        {
            _colorblind = true;

            // Show color-blind text if a square is currently waiting for Morse Code or Tap Code
            if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.MorseCode)
                setMorseColorblindText(_activeSquare.Value);
            if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.TapCode)
                setTapCodeColorblindText(_activeSquare.Value);

            // Switch the texture for any already-visible Snooker balls (“initial: true” prevents an extraneous “square was correct!” log message)
            if (_snookerBallGraphic != null)
                _snookerBallGraphic.material.mainTexture = SnookerBallTexturesCB[_solution[Array.IndexOf(_codings, Coding.Snooker)]];
            yield return null;
        }
        else if ((m = Regex.Match(command, @"^\s*(?:(?:tap|click|press)\s+)?([A-D])\s*([1-4])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";

            var coord = (char.ToUpperInvariant(m.Groups[1].Value[0]) - 'A') + 4 * (m.Groups[2].Value[0] - '1');
            var wasShown = _shown[coord];

            // If there’s already a cycling, just stop it. It’ll be up to the user’s luck if it’s currently at the right spot.
            if (_tpRepeatedlyClicking != null)
            {
                StopCoroutine(_tpRepeatedlyClicking);
                _tpRepeatedlyClicking = null;
            }

            // Tap the square
            yield return "multiple strikes";
            yield return new[] { Squares[coord] };

            // If a square opened up and it’s one of the “submission timeout” squares, we need to repeatedly press it to avoid submitting.
            // However, if we’re already doing that right now and the user just tapped the same square again, then don’t start a second copy of the subroutine
            switch (_codings[coord])
            {
                case Coding.Digits:
                case Coding.MaritimeFlags:
                case Coding.SimonSamples:
                case Coding.Astrology:
                case Coding.Snooker:
                case Coding.CardSuits:
                case Coding.Mahjong:
                case Coding.Zoni:
                case Coding.ChessPieces:
                    if (_activeSquare == coord && !wasShown)
                        _tpRepeatedlyClicking = StartCoroutine(tpRepeatedlyClick(1.25f, Squares[coord]));
                    break;
            }

            yield return "end multiple strikes";
        }
        else if ((m = Regex.Match(command, @"^\s*(?:show\s*)?names\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            if (_activeSquare == null)
            {
                yield return "sendtochaterror No square is currently active.";
                yield break;
            }
            switch (_codings[_activeSquare.Value])
            {
                case Coding.Digits:
                case Coding.MaritimeFlags:
                case Coding.Astrology:
                case Coding.Snooker:
                case Coding.CardSuits:
                case Coding.Mahjong:
                case Coding.Zoni:
                case Coding.ChessPieces:
                    yield return string.Format("sendtochat The names are {0}.", Enumerable.Range(0, 4).Select(i => string.Format("“{0}”", _tpCyclingNames[i])).JoinString(", "));
                    yield break;

                case Coding.SimonSamples:
                    yield return "sendtochat Use the names shown in the square.";
                    yield break;

                case Coding.Arrows:
                    yield return "sendtochat You can use up/down/left/right or north/south/west/east or clockface directions.";
                    yield break;

                default:
                    yield return "sendtochaterror That is not the correct command for submitting an answer to this square.";
                    yield break;
            }
        }
        else if (_activeSquare != null && _tpCyclingNames != null &&
            (((m = Regex.Match(command, @"^\s*(?:stop at|stop on)\s+(.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success
                && _tpCyclingNames.Any(tn => tn.Equals(m.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase)))
            || _tpCyclingNames.Any(tn => tn.Equals(command.Trim(), StringComparison.InvariantCultureIgnoreCase))))
        {
            var value = m.Success ? m.Groups[1].Value : command.Trim();

            switch (_codings[_activeSquare.Value])
            {
                case Coding.Digits:
                case Coding.MaritimeFlags:
                case Coding.SimonSamples:
                case Coding.Astrology:
                case Coding.Snooker:
                case Coding.CardSuits:
                case Coding.Mahjong:
                case Coding.Zoni:
                case Coding.ChessPieces:
                    if (!_tpCyclingNames.Any(cn => cn.Equals(value, StringComparison.InvariantCultureIgnoreCase)))
                        yield return "sendtochaterror I don’t recognize that name.";
                    else
                    {
                        if (_tpRepeatedlyClicking == null)
                        {
                            yield return "sendtochat @{0} Please report this bug to Timwi: “delayed-submission coroutine wasn’t active”. Send along the bomb’s logfile.";
                            yield break;
                        }
                        yield return null;
                        yield return "solve";
                        yield return "strike";
                        StopCoroutine(_tpRepeatedlyClicking);
                        _tpRepeatedlyClicking = null;
                        while (!_tpCyclingName.Equals(value, StringComparison.InvariantCultureIgnoreCase))
                        {
                            yield return new WaitForSeconds(.3f);
                            yield return new[] { Squares[_activeSquare.Value] };
                            yield return "trycancel";
                        }
                    }
                    yield break;

                default:
                    yield return "sendtochaterror @{0} Please report this bug to Timwi: “missing cycling code”. Send along the bomb’s logfile.";
                    yield break;
            }
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.Arrows && (m = Regex.Match(command, @"^\s*(?:(?:click at|click on|stop at|stop on|tap at|tap on)\s+)?(?:(?<n>n|north|up?|t|top|12)|(?<e>e|east|r|right|3)|(?<s>s|south|d|down|b|bottom|6)|(?<w>w|west|l|left|9))\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var desiredDirection = m.Groups["n"].Success ? 2 : m.Groups["s"].Success ? 0 : m.Groups["w"].Success ? 1 : m.Groups["e"].Success ? 3 : -1;
            if (desiredDirection < 0)
            {
                yield return "sendtochaterror @{0} Please report this bug to Timwi: “unrecognized arrow direction”. Send along the bomb’s logfile.";
                yield break;
            }

            yield return null;
            yield return "solve";
            yield return "strike";
            yield return new WaitUntil(() => (_curArrowRotation % 90 < 30 || _curArrowRotation % 90 > 60) && (_curArrowRotation < 45 ? 3 : _curArrowRotation < 135 ? 2 : _curArrowRotation < 225 ? 1 : _curArrowRotation < 315 ? 0 : 3) == desiredDirection);
            yield return new[] { Squares[_activeSquare.Value] };
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.MorseCode && (m = Regex.Match(command, @"^\s*(?:(?:morse|tx|transmit|send|submit|enter|input|play)\s+)?([-.]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            var morse = m.Groups[1].Value;
            var sq = Squares[_activeSquare.Value];
            for (int i = 0; i < morse.Length; i++)
            {
                sq.OnInteract();
                yield return new WaitForSeconds(morse[i] == '.' ? .25f : .75f);
                sq.OnInteractEnded();
                yield return new WaitForSeconds(.25f);
            }
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.TapCode && (m = Regex.Match(command, @"^\s*(?:(?:tap|send|transmit|play|submit|enter|input)\s+)?([1-5][1-5])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            var tapCode = m.Groups[1].Value;
            var sq = Squares[_activeSquare.Value];
            yield return Enumerable.Repeat(sq, tapCode[0] - '0');
            yield return new WaitForSeconds(1.1f);
            yield return Enumerable.Repeat(sq, tapCode[1] - '0');
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.Semaphores && (m = Regex.Match(command, @"^\s*(?:(?:flags?|semaphores?|submit|enter|input)\s+)?(?:(?<l0>n|north|12|u|up)|(?<l_45>ne|north-?east|2|ur|up-?right)|(?<l225>se|south-?east|4|dr|down-?right)|(?<l180>s|south|6|d|down)|(?<l135>sw|south-?west|8|dl|down-?left)|(?<l90>w|west|9|l|left)|(?<l45>nw|north-?west|10|ul|up-?left))[-\.,; ]+(?:(?<r0>n|north|12|u|up)|(?<r_45>ne|north-?east|2|ur|up-?right)|(?<r_90>e|east|3|r|right)|(?<r_135>se|south-?east|4|dr|down-?right)|(?<r_180>s|south|6|d|down)|(?<r_225>sw|south-?west|8|dl|down-?left)|(?<r45>nw|north-?west|10|ul|up-?left))\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            while (!m.Groups[("l" + _leftSemaphore).Replace("-", "_")].Success)
            {
                yield return new[] { SemaphoresLeftSelectable };
                yield return "trycancel";
            }
            while (!m.Groups[("r" + _rightSemaphore).Replace("-", "_")].Success)
            {
                yield return new[] { SemaphoresRightSelectable };
                yield return "trycancel";
            }
            yield return new[] { Squares[_activeSquare.Value] };
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.Binary && (m = Regex.Match(command, @"^\s*(?:(?:binary|bin|digits|submit|enter|input)\s+)?([01]{5})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            for (int i = 0; i < 5; i++)
            {
                if (_curBinary[i] != (m.Groups[1].Value[i] == '1'))
                    yield return new[] { BinaryDigits[i] };
                yield return "trycancel";
            }
            yield return new[] { Squares[_activeSquare.Value] };
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.Letters && (m = Regex.Match(command, @"^\s*(?:(?:letters?|submit|enter|input)\s+)?([A-Z])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            while (!_curLetter.ToString().Equals(m.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return new[] { LetterUpDownButtons[1] };
                yield return "trycancel";
            }
            yield return new[] { Squares[_activeSquare.Value] };
        }
        else if (_activeSquare != null && _codings[_activeSquare.Value] == Coding.Braille && (m = Regex.Match(command, @"^\s*(?:(?:braille|dots|submit|enter|input)\s+)?(?=[1-6])(1?2?3?4?5?6?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            for (int i = 0; i < 6; i++)
            {
                if (_curBraille[i] != m.Groups[1].Value.Contains((char) ('1' + i)))
                    yield return new[] { BrailleDots[i] };
                yield return "trycancel";
            }
            yield return new[] { Squares[_activeSquare.Value] };
        }
    }

    private IEnumerator tpRepeatedlyClick(float delay, KMSelectable button)
    {
        yield return new WaitForSeconds(delay - .1f);
        while (true)
        {
            button.OnInteract();
            yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        var undoneSqs = Enumerable.Range(0, 16).Where(ix => !_shown[ix]).ToList().Shuffle();
        if (_activeSquare != null)
            undoneSqs.Insert(0, _activeSquare.Value);

        foreach (var sq in undoneSqs)
        {
            while (_activePanelAnimations != 0)
                yield return true;

            if (_activeSquare == null)
            {
                yield return new WaitForSeconds(.4f);
                Squares[sq].OnInteract();
                yield return new WaitForSeconds(.3f);
            }

            while (_activePanelAnimations != 0)
                yield return true;

            switch (_codings[sq])
            {
                case Coding.Digits:
                case Coding.SimonSamples:
                case Coding.Astrology:
                case Coding.Snooker:
                case Coding.CardSuits:
                case Coding.Mahjong:
                case Coding.Zoni:
                case Coding.ChessPieces:
                case Coding.MaritimeFlags:
                    while (!_isCurrentCyclingAnswerCorrect)
                    {
                        Squares[sq].OnInteract();
                        yield return new WaitForSeconds(.3f);
                    }
                    while (_delaySubmitCoroutine != null)
                        yield return true;
                    break;

                case Coding.Letters:
                    while (!LetterDisplay.text.Equals(_numberNames[_solution[sq]].ToString()))
                    {
                        LetterUpDownButtons[1].OnInteract();
                        yield return new WaitForSeconds(.1f);
                    }
                    Squares[sq].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;

                case Coding.Arrows:
                    while (_solution[sq] == 3 ? (_curArrowRotation >= 30 && _curArrowRotation <= 330) : (_curArrowRotation <= 240 - 90 * _solution[sq] || _curArrowRotation >= 300 - 90 * _solution[sq]))
                        yield return true;

                    Squares[sq].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;

                case Coding.Binary:
                    var name = _numberNames[_solution[sq]] - 'A' + 1;
                    var binary = Enumerable.Range(0, 5).Select(bit => (name & (1 << (4 - bit))) != 0).ToArray();
                    for (int j = 0; j < 5; j++)
                    {
                        if (_curBinary[j] != binary[j])
                        {
                            BinaryDigits[j].OnInteract();
                            yield return new WaitForSeconds(.1f);
                        }
                    }
                    Squares[sq].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;

                case Coding.MorseCode:
                    var morse = _morseCode[_numberNames[_solution[sq]]];
                    for (int j = 0; j < morse.Length; j++)
                    {
                        Squares[sq].OnInteract();
                        yield return new WaitForSeconds(morse[j].Equals('.') ? .25f : .75f);
                        Squares[sq].OnInteractEnded();
                        yield return new WaitForSeconds(.25f);
                    }
                    while (_morseBlinking == null)
                        yield return true;
                    break;

                case Coding.TapCode:
                    foreach (var tap in _tapCodes[_numberNames[_solution[sq]] - 'A'])
                    {
                        for (int j = 0; j < tap; j++)
                        {
                            Squares[sq].OnInteract();
                            yield return new WaitForSeconds(.1f);
                        }
                        while (_tapCodeLastCodeStillActive)
                            yield return true;
                    }
                    break;

                case Coding.Braille:
                    var braille = _brailleCodes[_numberNames[_solution[sq]] - 'A'];
                    for (int j = 0; j < 6; j++)
                        if (_curBraille[j] != braille.Contains((char) (j + '1')))
                        {
                            BrailleDots[j].OnInteract();
                            yield return new WaitForSeconds(.1f);
                        }
                    Squares[sq].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;

                case Coding.Semaphores:
                    while (_semaphoreLeftFlagOrientations[_numberNames[_solution[sq]] - 'A'] != _leftSemaphore)
                    {
                        SemaphoresLeftSelectable.OnInteract();
                        yield return new WaitForSeconds(.05f);
                        SemaphoresLeftSelectable.OnInteractEnded();
                        yield return new WaitForSeconds(.05f);
                    }
                    while (_semaphoreRightFlagOrientations[_numberNames[_solution[sq]] - 'A'] != _rightSemaphore)
                    {
                        SemaphoresRightSelectable.OnInteract();
                        yield return new WaitForSeconds(.05f);
                        SemaphoresRightSelectable.OnInteractEnded();
                        yield return new WaitForSeconds(.05f);
                    }
                    Squares[sq].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;
            }
        }
    }
}
