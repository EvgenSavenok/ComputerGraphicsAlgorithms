using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACG.Core.Objects;

namespace ACG.Core.ObjectRenderer;

public static class BaseRenderer
{
    public static void Render(
        Scene scene, 
        WriteableBitmap? wb, 
        Color foregroundColor,
        Color backgroundColor,
        RenderingType mode)
    {
        if (wb != null)
        {
            WireframeRenderer.ClearBitmap(wb, backgroundColor);
            scene.Camera.ChangeEye();
            scene.UpdateAllModels();

            switch (mode)
            {
                case RenderingType.Wireframe:
                    foreach (var model in scene.Models)
                    {
                        WireframeRenderer.DrawWireframe(model, wb, foregroundColor, scene.Camera);
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported rendering type");
            }

            if (scene.SelectedModel is not null)
                WireframeRenderer.Draw3DSelectionHighlight(scene, scene.SelectedModel, wb, Colors.Aqua);
        }
    }
}