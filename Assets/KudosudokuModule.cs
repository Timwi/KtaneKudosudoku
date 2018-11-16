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

    public GameObject TopPanelBacking;
    public GameObject TopPanelCover;
    public GameObject SemaphoresPanel;
    public GameObject BinaryPanel;
    public GameObject RightPanelBacking;
    public GameObject RightPanelCover;
    public GameObject BraillePanel;

    public KMSelectable[] Squares;

    public GameObject ImageTemplate;
    public MeshRenderer MorseLed;
    public Material MorseLedOn;
    public Material MorseLedOff;
    public Material SquareSolved;
    public Material SquareUnsolved;
    public Material SquareExpectingMorse;
    public Material SquareExpectingMorseDown;
    public Material SquareExpectingTapCode;

    private MeshRenderer[] _squaresMR;

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

    // Which squares are currently waiting to have their answers submitted.
    private readonly Coroutine[] _activeSquares = new Coroutine[16];

    /// <summary>
    ///     Which squares currently have a panel open OR active Morse or Tap Code.</summary>
    /// <remarks>
    ///     While this is non-null, all other unsolved squares give a strike.</remarks>
    private int? _specialActiveSquare = null;

    /// <summary>Lists all the times at which the mouse was pressed or released while entering Morse code.</summary>
    private List<float> _morsePressTimes;
    /// <summary>Lists the number of taps between pauses.</summary>
    private List<int> _tapCodeInput;
    /// <summary>While true, the next tap will still be added to the last item in <see cref="_tapCodeInput"/>.</summary>
    private bool _tapCodeLastCodeStillActive;
    private Coroutine _listeningPlaying;

    private readonly char[] _numberNames = new char[4];
    private readonly bool[] _shown = new bool[16];
    private readonly bool[] _theCubeAlternatives = new bool[4];
    private readonly string[] _listeningAlternatives = new string[4];

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
        _squaresMR = new MeshRenderer[16];

        LetterTextMesh.gameObject.SetActive(false);
        DigitTextMesh.gameObject.SetActive(false);
        TheCubeSymbolsTextMesh.gameObject.SetActive(false);
        SemaphoresParent.SetActive(false);
        BrailleParent.SetActive(false);
        BinaryParent.SetActive(false);
        MorseParent.SetActive(false);
        ArrowsParent.SetActive(false);
        ImageTemplate.gameObject.SetActive(false);

        for (int i = 0; i < 16; i++)
        {
            _squaresMR[i] = Squares[i].GetComponent<MeshRenderer>();
            _squaresMR[i].material = SquareUnsolved;
            Squares[i].OnInteract = squarePress(i);
        }

        for (int i = 0; i < 4; i++)
        {
            _theCubeAlternatives[i] = Rnd.Range(0, 2) != 0;
            _listeningAlternatives[i] = _listeningSounds[4 * Rnd.Range(0, 10) + i];
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

        // DEBUGGING
        // potentialGivens.Remove(Array.IndexOf(_codings, Coding.Semaphores));

        var givens = Ut.ReduceRequiredSet(potentialGivens, state =>
                // Special case: if both E and T are letter names, Morse Code must be a given
                !(_numberNames.Contains('E') && _numberNames.Contains('T') && !state.SetToTest.Contains(Array.IndexOf(_codings, Coding.MorseCode))) &&

                // FOR DEBUGGING: force a specific coding to be pre-filled
                // state.SetToTest.Contains(Array.IndexOf(_codings, Coding.ListeningSounds)) &&

                // Make sure that the solution is still unique
                _allSudokus.Count(sudoku => state.SetToTest.All(ix => sudoku[ix] == _solution[ix])) == 1)
            .ToArray();

        Debug.LogFormat(@"[Kudosudoku #{0}] Codings:", _moduleId);
        for (int row = 0; row < 4; row++)
            Debug.Log(Enumerable.Range(0, 4).Select(col => _codings[4 * row + col].ToString()).JoinString(" "));

        foreach (var ix in givens)
            showSquare(ix, initial: true);
    }

    private KMSelectable.OnInteractHandler squarePress(int i)
    {
        return delegate
        {
            if (_isSolved)
                return false;

            if (_specialActiveSquare != null && _specialActiveSquare.Value != i)
            {
                Module.HandleStrike();
                Debug.LogFormat(@"[Kudosudoku #{0}] You received a strike because you pressed on square {1}{2} while square {3}{4} was still waiting for {5} input.",
                    _moduleId, (char) ('A' + i % 4), (char) ('1' + i / 4), (char) ('A' + _specialActiveSquare.Value % 4), (char) ('1' + _specialActiveSquare.Value / 4), _codings[_specialActiveSquare.Value]);
                return false;
            }

            if (_shown[i])
            {
                // Re-play a sound if there is one
                switch (_codings[i])
                {
                    case Coding.TapCode:
                        StartCoroutine(soundTapCode(_tapCodes[_numberNames[_solution[i]] - 'A']));
                        break;

                    case Coding.SimonSamples:
                        Audio.PlaySoundAtTransform(_simonSamplesSounds[_solution[i]], transform);
                        break;

                    case Coding.ListeningSounds:
                        playListeningSound(_listeningAlternatives[_solution[i]]);
                        break;
                }
                return false;
            }

            // User clicked on an unsolved square
            switch (_codings[i])
            {
                case Coding.MorseCode:
                    // Upon the first click, the square turns “dark red”
                    _squaresMR[i].material = SquareExpectingMorse;
                    _morsePressTimes = new List<float>();
                    _specialActiveSquare = i;

                    // Henceforth, the square expects Morse code input
                    Squares[i].OnInteract = morseMouseDown(i);
                    Squares[i].OnInteractEnded = morseMouseUp(i);
                    StartCoroutine(interpretMorseCode(i));
                    break;

                case Coding.TapCode:
                    // Upon the first click, the square turns “blue”
                    _squaresMR[i].material = SquareExpectingTapCode;
                    _tapCodeInput = new List<int>();
                    _specialActiveSquare = i;

                    // Henceforth, the square expects Tap Code input
                    Squares[i].OnInteract = tapCodeMouseDown(i);
                    break;

                case Coding.Semaphores:
                    break;

                case Coding.Letters:
                    break;
                case Coding.Digits:
                    break;
                case Coding.Braille:
                    break;
                case Coding.MaritimeFlags:
                    break;
                case Coding.Binary:
                    break;
                case Coding.SimonSamples:
                    break;
                case Coding.Astrology:
                    break;
                case Coding.Snooker:
                    break;
                case Coding.Arrows:
                    break;
                case Coding.CardSuits:
                    break;
                case Coding.Mahjong:
                    break;
                case Coding.TheCubeSymbols:
                    break;
                case Coding.ListeningSounds:
                    break;
                default:
                    break;
            }
            return false;
        };
    }

    private void playListeningSound(string soundName)
    {
        if (_listeningPlaying != null)
            StopCoroutine(_listeningPlaying);
        _listeningPlaying = StartCoroutine(playListeningSoundCoroutine(soundName));
    }

    private IEnumerator playListeningSoundCoroutine(string soundName)
    {
        Audio.PlaySoundAtTransform("TapePlay", transform);
        yield return new WaitForSeconds(.5f);
        Audio.PlaySoundAtTransform(soundName, transform);
        yield return new WaitForSeconds(4.5f);
        Audio.PlaySoundAtTransform("TapeStop", transform);
        _listeningPlaying = null;
    }

    private KMSelectable.OnInteractHandler morseMouseDown(int i)
    {
        return delegate
        {
            // Check if mouse is already down. This should never happen.
            if (_morsePressTimes.Count % 2 == 1)
                return false;
            _morsePressTimes.Add(Time.time);
            _squaresMR[i].material = SquareExpectingMorseDown;
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

    private IEnumerator interpretMorseCode(int i)
    {
        while (_morsePressTimes.Count == 0 || _morsePressTimes.Count % 2 == 1 || Time.time - _morsePressTimes.Last() < 2)
            yield return null;

        // User finished entering Morse Code.
        _specialActiveSquare = null;
        Squares[i].OnInteract = squarePress(i);
        Squares[i].OnInteractEnded = null;

        // If there’s only one tap, no matter how long, accept it as input for either E or T.
        string reasonForWrongAnswer;
        if (_morsePressTimes.Count == 2)
        {
            if ("ET".Contains(_numberNames[_solution[i]]))
                goto correctAnswer;
            reasonForWrongAnswer = "Morse code for E or T";
            goto wrongAnswer;
        }

        // Calculate the average gap length
        var totalGapTime = 0f;
        for (int t = 1; t < _morsePressTimes.Count - 1; t += 2)
            totalGapTime += _morsePressTimes[t + 1] - _morsePressTimes[t];
        var averageGapTime = totalGapTime / (_morsePressTimes.Count / 2 - 1);
        var morseCode = "";
        for (int t = 0; t < _morsePressTimes.Count; t += 2)
            morseCode += (_morsePressTimes[t + 1] - _morsePressTimes[t]) < 2 * averageGapTime ? "." : "-";
        var character = _morseCode.FirstOrDefault(kvp => kvp.Value == morseCode);
        if (character.Key == _numberNames[_solution[i]])
            goto correctAnswer;
        reasonForWrongAnswer = character.Key == '\0' ? "an invalid Morse code" : "Morse code for " + character.Key;

        wrongAnswer:
        Debug.LogFormat(@"[Kudosudoku #{0}] You received a strike because on square {1}{2} you entered {3} while it expected {4}.",
            _moduleId, (char) ('A' + i % 4), (char) ('1' + i / 4), reasonForWrongAnswer, _numberNames[_solution[i]]);
        _squaresMR[i].material = SquareUnsolved;
        strikeAndReshuffle();
        _morsePressTimes = null;
        yield break;

        correctAnswer:
        Debug.LogFormat(@"[Kudosudoku #{0}] Square {1}{2}: correct Morse code input ({3}).", _moduleId, (char) ('A' + i % 4), (char) ('1' + i / 4), _numberNames[_solution[i]]);
        showSquare(i, initial: false);
        _morsePressTimes = null;
    }

    private KMSelectable.OnInteractHandler tapCodeMouseDown(int i)
    {
        return delegate
        {
            if (_tapCodeLastCodeStillActive)
                _tapCodeInput[_tapCodeInput.Count - 1]++;
            else
            {
                _tapCodeLastCodeStillActive = true;
                _tapCodeInput.Add(1);
            }
            if (_activeSquares[i] != null)
                StopCoroutine(_activeSquares[i]);
            _activeSquares[i] = StartCoroutine(acknowledgeTapCode(i));
            return false;
        };
    }

    private IEnumerator acknowledgeTapCode(int i)
    {
        yield return new WaitForSeconds(1f);
        Audio.PlaySoundAtTransform("MiniTap", transform);
        _tapCodeLastCodeStillActive = false;

        if (_tapCodeInput.Count < 2)
            yield break;

        // User finished entering Tap Code.
        _specialActiveSquare = null;
        Squares[i].OnInteract = squarePress(i);

        var expected = _tapCodes[_numberNames[_solution[i]] - 'A'];
        if (expected.SequenceEqual(_tapCodeInput))
        {
            Debug.LogFormat(@"[Kudosudoku #{0}] Square {1}{2}: correct Tap Code input ({3}).", _moduleId, (char) ('A' + i % 4), (char) ('1' + i / 4), _tapCodeInput.JoinString(", "));
            showSquare(i, initial: false);
        }
        else
        {
            Debug.LogFormat(@"[Kudosudoku #{0}] You received a strike because on square {1}{2} you entered Tap Code “{3}” while it expected “{4}”.",
                _moduleId, (char) ('A' + i % 4), (char) ('1' + i / 4), _tapCodeInput.JoinString(", "), expected.JoinString(", "));
            _squaresMR[i].material = SquareUnsolved;
            strikeAndReshuffle();
        }

        _tapCodeInput = null;
    }

    private void strikeAndReshuffle()
    {
        Module.HandleStrike();

        var oldCodings = _codings.ToArray();
        var oldIndexes = Enumerable.Range(0, 16).Where(ix => !_shown[ix]).ToArray();
        var newIndexes = oldIndexes.ToArray().Shuffle();
        for (int i = 0; i < oldIndexes.Length; i++)
            _codings[newIndexes[i]] = oldCodings[oldIndexes[i]];

        Debug.LogFormat(@"[Kudosudoku #{0}] New codings:", _moduleId);
        for (int row = 0; row < 4; row++)
            Debug.Log(Enumerable.Range(0, 4).Select(col => _codings[4 * row + col].ToString()).JoinString(" "));
    }

    private IEnumerator soundTapCode(int[] taps)
    {
        yield return new WaitForSeconds(.1f);
        for (int i = 0; i < taps.Length; i++)
        {
            for (int a = 0; a < taps[i]; a++)
            {
                Audio.PlaySoundAtTransform("Tap", transform);
                yield return new WaitForSeconds(.5f);
            }
            yield return new WaitForSeconds(.5f);
        }
    }

    private static readonly int[] _semaphoreLeftFlagOrientations = new[] { 135, 90, 45, 0, 180, 180, 180, 90, 135, 0, 135, 135, 135, 135, 90, 90, 90, 90, 90, 45, 45, 0, -45, -45, 45, -135 };
    private static readonly int[] _semaphoreRightFlagOrientations = new[] { -180, -180, -180, -180, -45, -90, -135, 135, 45, -90, 0, -45, -90, -135, 45, 0, -45, -90, -135, 0, -45, -135, -90, -135, -90, -90 };
    private static readonly string[] _brailleCodes = "1,12,14,145,15,124,1245,125,24,245,13,123,134,1345,135,1234,12345,1235,234,2345,136,1236,2456,1346,13456,1356".Split(',');
    private static readonly int[][] _tapCodes = "11,12,13,14,15,21,22,23,24,25,13,31,32,33,34,35,41,42,43,44,45,51,52,53,54,55".Split(',').Select(str => str.Select(ch => ch - '0').ToArray()).ToArray();
    private static readonly string[] _simonSamplesSounds = new[] { "HiHat", "OpenHiHat", "Kick", "Snare" };
    private static readonly string[] _listeningSounds = new[] { "TaxiDispatch", "DialupInternet", "Cow", "PoliceRadioScanner", "ExtractorFan", "CensorshipBleep", "TrainStation", "MedievalWeapons", "Arcade", "DoorClosing", "Casino", "Chainsaw", "Supermarket", "CompressedAir", "SoccerMatch", "ServoMotor", "TawnyOwl", "Waterfall", "SewingMachine", "TearingFabric", "ThrushNightingale", "Zipper", "CarEngine", "VacuumCleaner", "ReloadingGlock19", "BallpointPenWriting", "Oboe", "RattlingIronChain", "Saxophone", "BookPageTurning", "Tuba", "TableTennis", "Marimba", "SqueekyToy", "PhoneRinging", "Helicopter", "TibetanNuns", "FireworkExploding", "ThroatSinging", "GlassShattering" };

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
        _squaresMR[i].material = SquareSolved;
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

                // Usually we’ll blink the letter, but:
                //  • If Morse Code is an initial given, it has a 1-in-3 chance of blinking the digit 1–4 instead.
                //  • If Morse Code is an initial given, would have to blink E or T, and both E and T are number names, definitely blink the digit instead
                var characterToBlink = initial && (Rnd.Range(0, 3) == 0 || (_numberNames.Contains('E') && _numberNames.Contains('T') && "ET".Contains(_numberNames[_solution[i]]))) ? (char) (_solution[i] + '1') : _numberNames[_solution[i]];
                StartCoroutine(blinkMorse(characterToBlink));
                break;
            }

            case Coding.TapCode:
            case Coding.SimonSamples:
            case Coding.ListeningSounds:
                // Do nothing, as these are audio-only
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
