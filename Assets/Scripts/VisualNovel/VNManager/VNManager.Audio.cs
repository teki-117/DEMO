public partial class VNManager
{
    private void Audio_PlayVoice(string file) => AudioManager.Instance.PlayVoice(file);

    private void Audio_PlayBGM(string file) => AudioManager.Instance.PlayBackground(file);
}