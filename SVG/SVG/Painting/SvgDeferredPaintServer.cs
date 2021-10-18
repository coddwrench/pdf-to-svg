using System.Linq;
using System.Drawing;

namespace Svg
{
    /// <summary>
    /// A wrapper for a paint server which isn't defined currently in the parse process, but
    /// should be defined by the time the image needs to render.
    /// </summary>
    public class SvgDeferredPaintServer : SvgPaintServer
    {
        private bool _serverLoaded = false;
        private SvgPaintServer _concreteServer;

        public SvgDocument Document { get; set; }
        public string DeferredId { get; set; }

        public SvgDeferredPaintServer() { }
        public SvgDeferredPaintServer(SvgDocument document, string id)
        {
            Document = document;
            DeferredId = id;
        }

        public void EnsureServer(SvgElement styleOwner)
        {
            if (!_serverLoaded)
            {
                if (DeferredId == "currentColor" && styleOwner != null) 
                {
                    var colorElement = (from e in styleOwner.ParentsAndSelf.OfType<SvgElement>()
                                        where e.Color != None && e.Color != SvgColourServer.NotSet && 
                                              e.Color != SvgColourServer.Inherit && e.Color != None
                                        select e).FirstOrDefault();
                    _concreteServer = (colorElement == null ? None : colorElement.Color);
                }
                else 
                {
                    _concreteServer = Document.IdManager.GetElementById(DeferredId) as SvgPaintServer;
                }
                _serverLoaded = true;
            }
        }

        public override Brush GetBrush(SvgVisualElement styleOwner, ISvgRenderer renderer, float opacity, bool forStroke = false)
        {
            EnsureServer(styleOwner);
            return _concreteServer.GetBrush(styleOwner, renderer, opacity, forStroke);
        }

        public override SvgElement DeepCopy()
        {
            return DeepCopy<SvgDeferredPaintServer>();
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgDeferredPaintServer;
            newObj.Document = Document;
            newObj.DeferredId = DeferredId;
            return newObj;
        }

        public override bool Equals(object obj)
        {
            var other = obj as SvgDeferredPaintServer;
            if (other == null)
                return false;

            return Document == other.Document && DeferredId == other.DeferredId;
        }

        public override int GetHashCode()
        {
            if (Document == null || DeferredId == null) return 0;
            return Document.GetHashCode() ^ DeferredId.GetHashCode();
        }

        public override string ToString()
        {
            if (DeferredId == "currentColor")
            {
                return DeferredId;
            }
            else
            {
                return string.Format("url({0})", DeferredId);
            }
        }

        public static T TryGet<T>(SvgPaintServer server, SvgElement parent) where T : SvgPaintServer
        {
            var deferred = server as SvgDeferredPaintServer;
            if (deferred == null)
            {
                return server as T;
            }
            else
            {
                deferred.EnsureServer(parent);
                return deferred._concreteServer as T;
            }
        }
    }
}