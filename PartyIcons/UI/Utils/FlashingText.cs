using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace PartyIcons.UI.Utils;

public class FlashingText
{
    private const int FlashIntervalMs = 500;
    private static readonly Stopwatch Stopwatch = new();

    private readonly Vector4 _flashColor0 = new(0.4f, 0.1f, 0.1f, 1.0f);
    private readonly Vector4 _flashColor1 = new(1.0f, 0.0f, 0.0f, 1.0f);
    private bool _isFlashing;

    private bool IsFlashing
    {
        get => _isFlashing;
        set
        {
            if (_isFlashing != value) {
                _isFlashing = value;

                if (value) {
                    Stopwatch.Start();
                }
                else {
                    Stopwatch.Stop();
                }
            }
        }
    }


    private Vector4 GetColor()
    {
        if (IsFlashing) {
            if (Stopwatch.ElapsedMilliseconds < FlashIntervalMs) {
                return _flashColor1;
            }

            if (Stopwatch.ElapsedMilliseconds > FlashIntervalMs * 2) {
                Stopwatch.Restart();
            }
        }

        return _flashColor0;
    }

    public ImRaii.Color PushColor(bool isFlashing)
    {
        IsFlashing = isFlashing;
        return ImRaii.PushColor(ImGuiCol.Text, GetColor(), IsFlashing);
    }
}