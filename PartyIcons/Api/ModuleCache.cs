using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public static unsafe class ModuleCache
{
    public static void Initialize()
    {
        Service.ClientState.Logout += ResetRaptureAtkModule;
    }

    public static void Dispose()
    {
        Service.ClientState.Logout -= ResetRaptureAtkModule;
    }

    private static RaptureAtkModule* _raptureAtkModulePtr;

    public static RaptureAtkModule* RaptureAtkModulePtr
    {
        get
        {
            if (_raptureAtkModulePtr == null) {
                _raptureAtkModulePtr = Framework.Instance()->GetUiModule()->GetRaptureAtkModule();
            }

            return _raptureAtkModulePtr;
        }
    }

    private static void ResetRaptureAtkModule() => _raptureAtkModulePtr = null;
}