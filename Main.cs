using Core.Logging;
using Game.Orchestration;
using MonoMod.RuntimeDetour;
using ShapezShifter.Kit;
using ShapezShifter.SharpDetour;

namespace Expr;

public class Main : IMod
{
    internal static readonly ModFolderLocator Res = ModDirectoryLocator.CreateLocator<Main>().SubLocator("Resources");

    public Main(ILogger logger)
    {
    }

    public void Dispose()
    {
    }
}
