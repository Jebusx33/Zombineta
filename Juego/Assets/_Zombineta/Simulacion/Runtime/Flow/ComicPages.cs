using System.Collections.Generic;

namespace Zombineta.Flow
{
    /// <summary>La regla de paginas de la cinematica: que vineta arranca una pagina limpia.</summary>
    public static class ComicPages
    {
        public static bool OpensPage(IReadOnlyList<ComicPanel> panels, int index)
        {
            if (panels == null || index < 0 || index >= panels.Count)
                return false;
            return index == 0 || panels[index] == null || panels[index].nuevaPagina;
        }
    }
}
