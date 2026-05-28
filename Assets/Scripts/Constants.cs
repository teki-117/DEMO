using UnityEngine;

public class Constants : MonoBehaviour
{
    //文本
    public static string STORY_PATH = "Assets/Resources/story/";
    public static string DEFAULT_STORY_FILE_NAME = "1";
    public static string EXCEL_FILE_EXTENSION = ".xlsx";
    public static int DEFAULT_START_LINE = 1;

    //图片
    public static string AVATAR_PATH = "image/avatar/";
    public static string BACKGROUND_PATH = "image/background/";
    public static string BUTTON_PATH = "image/button/";
    public static string CHARACTER_PATH  = "image/character/";
    public static string IMAGE_LOAD_FAILED = "Failed to load image: ";

    //控制
    public static float DEFAULT_TYPING_SPEED = 0.05f;
    public static float SKIP_MODE_TYPING_SPEED = 0.03f;

    public static string AUTO_ON = "Auto";
    public static string AUTO_OFF = "Auto";
    public static float DEFAULT_AUTO_WATITING_SECONDS = 1f;

    public static string SKIP_ON = "Skip";
    public static string SKIP_OFF = "Skip";
    public static float DEFAULT_SKIP_WAITING_SECONDS = 0.04f;
    //声音
    public static string VOCAL_PATH = "audio/vocal/";
    public static string MUSIC_PATH = "audio/music/";
    public static string AUDIO_LOAD_FAILED = "Failed to load music";

    //打字机
    public static string NO_LOAD_FOUND = "No data found";
    public static string END_OF_STORY = "End of story";
    public static string CHOICE = "choice";
    public static float DEFAULT_WAITING_SECONDS = 1.5f;

    //立绘动画
    public static string APPEAR_AT = "appearAt";
    public static string DISAPPEAR = "disappear";
    public static string MOVE_TO = "moveTo";
    public static int    DURATION_TIME = 1;
    public static string COORDINATE_MISSING = "Coordinate missing";

    public static int DEFAULT_START_INDEX = 0;
    public static int SLOTS_PER_PAGE = 8;
    public static int TOTAL_SLOTS = 40;
    public static string COLON = "：";
    public static string SAVE_GAME = "save_game";
    public static string LOAD_GAME = "load_game";
    public static string EMPTY_SLOT = "empty_slot";

    public static string CAMERA_NOT_FOUND = "Main Camera not found!";
    public static string SAVE_FILE_PATH = "saves";
    public static string SAVE_FILE_EXTENSION = ".json";

    public static string X = "x";

    public static int MAX_LENGTH = 50;
}