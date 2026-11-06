using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiftHeroines
{
    public static class RiftUI
    {
        public static Font Regular, Bold;
        public static readonly Color Ink=Hex("#101323"), Panel=Hex("#141C32"), Muted=Hex("#A6B6CE"), Gold=Hex("#F3D7A0"), Coral=Hex("#FF8A74"), Ice=Hex("#98DCFF");
        static Sprite rounded, disc;
        public static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString(value,out c); return c; }
        public static Sprite Rounded
        {
            get
            {
                if(rounded)return rounded;
                const int n=48; var t=new Texture2D(n,n,TextureFormat.RGBA32,false); var pixels=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++) {
                    float dx=Mathf.Max(0,Mathf.Abs(x+.5f-n*.5f)-14),dy=Mathf.Max(0,Mathf.Abs(y+.5f-n*.5f)-14);
                    pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(10.5f-Mathf.Sqrt(dx*dx+dy*dy)));
                }
                t.SetPixels(pixels);t.Apply(false,true); t.wrapMode=TextureWrapMode.Clamp;
                rounded=Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));return rounded;
            }
        }
        public static Sprite Disc
        {
            get
            {
                if(disc)return disc;
                int n=64;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);var p=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)p[y*n+x]=new Color(1,1,1,Mathf.Clamp01((32-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32)))*.22f));
                t.SetPixels(p);t.Apply(false,true);disc=Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f));return disc;
            }
        }
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color,Sprite sprite=null,bool round=false)
        {
            var r=Rect(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;image.sprite=sprite??(round?Rounded:null);
            if(round)image.type=UnityEngine.UI.Image.Type.Sliced;return image;
        }
        public static Image Surface(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var im=Image(parent,name,x,y,w,h,color,null,true);
            var edge=im.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.6f,.72f,.95f,.18f);edge.effectDistance=new Vector2(1,-1);return im;
        }
        public static Text Text(Transform parent,string name,string text,float x,float y,float w,float h,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.text=text;t.font=bold?Bold:Regular;t.fontSize=size;t.color=color;t.alignment=alignment;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;t.supportRichText=true;return t;
        }
        public static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,Color color,Action action,int size=22)
        {
            var surface=Surface(parent,name,x,y,w,h,color);surface.raycastTarget=true;
            var b=surface.gameObject.AddComponent<Button>();b.targetGraphic=surface;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.72f,.8f,.9f);colors.disabledColor=new Color(.48f,.52f,.62f,.75f);colors.fadeDuration=.08f;b.colors=colors;
            b.onClick.AddListener(()=>action?.Invoke());Text(surface.transform,"Label",caption,8,0,w-16,h,size,Color.white,true,TextAnchor.MiddleCenter);
            surface.gameObject.AddComponent<RiftButtonFeel>();return b;
        }
        public static RiftGlyph Icon(Transform parent,string kind,float x,float y,float w,float h,Color color)
        { var r=Rect(parent,"Icon "+kind,x,y,w,h);var g=r.gameObject.AddComponent<RiftGlyph>();g.Kind=kind;g.color=color;g.raycastTarget=false;return g; }
        public static Image Bar(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var bg=Image(parent,name,x,y,w,h,new Color(.025f,.04f,.08f,.86f),null,true);
            var fill=Image(bg.transform,"Fill",2,2,w-4,h-4,color,null,true);
            fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;return fill;
        }
        public static void Stretch(RectTransform r)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    }

    public sealed class RiftButtonFeel : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public Action Hover;
        RectTransform rect;bool pressed,hover;float scale;
        void Awake(){rect=GetComponent<RectTransform>();}
        void Update(){scale=Mathf.MoveTowards(scale,pressed?-.025f:hover?.015f:0,Time.unscaledDeltaTime*.35f);rect.localScale=Vector3.one*(1+scale);}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnPointerEnter(PointerEventData e){hover=true;Hover?.Invoke();}
        public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
        void OnDisable(){pressed=hover=false;scale=0;if(rect)rect.localScale=Vector3.one;}
    }

    public class RiftGlyph : MaskableGraphic
    {
        public string Kind="spark";
        public float Progress=1;
        protected VertexHelper mesh;
        protected Vector2 center;
        protected float unit;
        protected void Line(Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 n=(b-a).normalized;n=new Vector2(-n.y,n.x)*width*.5f;int i=mesh.currentVertCount;
            Add(a-n,tint);Add(a+n,tint);Add(b+n,tint);Add(b-n,tint);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
        protected void Add(Vector2 p,Color c){mesh.AddVert(center+p*unit,c,Vector2.zero);}
        protected void Polygon(Color c,params Vector2[] points)
        {int i=mesh.currentVertCount;foreach(var p in points)Add(p,c);for(int j=1;j<points.Length-1;j++)mesh.AddTriangle(i,i+j,i+j+1);}
        protected void Ring(float radius,float thickness,Color c,int sides=40,float start=0,float arc=360)
        {Vector2 prev=new Vector2(Mathf.Cos(start*Mathf.Deg2Rad),Mathf.Sin(start*Mathf.Deg2Rad))*radius;for(int i=1;i<=sides;i++){float a=(start+arc*i/sides)*Mathf.Deg2Rad;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;Line(prev,p,thickness,c);prev=p;}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh;vh.Clear();center=rectTransform.rect.center;unit=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            Color c=color;
            switch(Kind)
            {
                case "blade":
                    Polygon(c,new Vector2(-.43f,-.62f),new Vector2(-.23f,-.76f),new Vector2(.65f,.55f),new Vector2(.7f,.88f),new Vector2(.4f,.72f));Line(new Vector2(-.65f,-.22f),new Vector2(-.02f,-.62f),.10f,c);break;
                case "shield":
                    Line(new Vector2(0,.85f),new Vector2(.68f,.52f),.11f,c);Line(new Vector2(.68f,.52f),new Vector2(.48f,-.32f),.11f,c);Line(new Vector2(.48f,-.32f),new Vector2(0,-.85f),.11f,c);Line(new Vector2(0,-.85f),new Vector2(-.48f,-.32f),.11f,c);Line(new Vector2(-.48f,-.32f),new Vector2(-.68f,.52f),.11f,c);Line(new Vector2(-.68f,.52f),new Vector2(0,.85f),.11f,c);Line(new Vector2(0,-.4f),new Vector2(0,.45f),.08f,c);break;
                case "moon":Ring(.7f,.1f,c);Polygon(c,new Vector2(-.1f,-.5f),new Vector2(.45f,.05f),new Vector2(-.05f,.7f),new Vector2(.1f,.1f));break;
                case "mana":Polygon(c,new Vector2(0,.9f),new Vector2(.55f,0),new Vector2(0,-.85f),new Vector2(-.55f,0));break;
                case "heart":Polygon(c,new Vector2(0,-.7f),new Vector2(-.72f,.05f),new Vector2(-.65f,.55f),new Vector2(-.3f,.7f),new Vector2(0,.36f),new Vector2(.3f,.7f),new Vector2(.65f,.55f),new Vector2(.72f,.05f));break;
                case "wind":for(int i=0;i<3;i++){float y=(i-1)*.4f;Line(new Vector2(-.8f,y),new Vector2(.35f+i*.12f,y),.1f,c);Ring(.2f,.1f,c,12,0,160);}break;
                case "target":Ring(.68f,.1f,c);Ring(.25f,.1f,c);Line(new Vector2(-.9f,0),new Vector2(-.4f,0),.1f,c);Line(new Vector2(.4f,0),new Vector2(.9f,0),.1f,c);Line(new Vector2(0,-.9f),new Vector2(0,-.4f),.1f,c);Line(new Vector2(0,.4f),new Vector2(0,.9f),.1f,c);break;
                case "pause":Line(new Vector2(-.28f,-.65f),new Vector2(-.28f,.65f),.23f,c);Line(new Vector2(.28f,-.65f),new Vector2(.28f,.65f),.23f,c);break;
                case "play":Polygon(c,new Vector2(-.45f,-.7f),new Vector2(-.45f,.7f),new Vector2(.7f,0));break;
                case "bond":Ring(.45f,.1f,c,28);Line(new Vector2(-.8f,-.4f),new Vector2(.8f,.4f),.15f,c);break;
                case "flower":for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;Line(Vector2.zero,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.75f,.28f,c);}Ring(.22f,.12f,new Color(1,1,1,c.a));break;
                default:
                    Polygon(c,new Vector2(0,.95f),new Vector2(.23f,.22f),new Vector2(.85f,0),new Vector2(.23f,-.22f),new Vector2(0,-.95f),new Vector2(-.23f,-.22f),new Vector2(-.85f,0),new Vector2(-.23f,.22f));break;
            }
        }
    }

    public sealed class RiftEffectGraphic : RiftGlyph
    {
        public float Age,Duration=.4f;
        public int Variant;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh;vh.Clear();center=rectTransform.rect.center;unit=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            float p=Mathf.Clamp01(Age/Duration), radius=.18f+p*.8f;var c=color;c.a*=1-p;
            if(Variant==6)
            {unit=1;Line(new Vector2(-rectTransform.rect.width*.5f,0),new Vector2(rectTransform.rect.width*.5f,0),3*(1-p),c);return;}
            if(Variant==7)
            {
                for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f+p*.35f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-d.y,d.x);float reach=.45f+p*.4f;Polygon(c,d*(reach+.16f),d*reach+n*.07f,d*(reach-.16f),d*reach-n*.07f);}
                Ring(.25f+p*.4f,.035f,c,24);return;
            }
            if(Variant>=3)
            {
                var core=Color.Lerp(c,Color.white,.7f);core.a=c.a;
                if(Variant==3)
                {
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3+.2f;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Vector2 n=new Vector2(-d.y,d.x);Polygon(core,d*.12f+n*.1f,d*(.5f+p*.45f),d*.12f-n*.1f);}
                    Ring(.25f+p*.35f,.055f*(1-p),c,20);
                }
                else if(Variant==4)
                {
                    Line(new Vector2(-.8f,-.48f),new Vector2(.8f,.48f),.13f*(1-p),core);
                    Line(new Vector2(-.5f,.15f),new Vector2(.5f,-.15f),.055f*(1-p),c);
                    Ring(.45f+p*.35f,.065f*(1-p),c,20,-35,125);
                }
                else
                {
                    for(int i=0;i<4;i++){float a=i*Mathf.PI/2+p*.3f;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Vector2 n=new Vector2(-d.y,d.x);Polygon(c,d*(.3f+p*.3f)+n*.09f,d*(.65f+p*.3f),d*(.3f+p*.3f)-n*.09f,d*(.18f+p*.3f));}
                    Ring(.3f+p*.28f,.04f,c,20);
                }
                return;
            }
            Ring(radius,.08f*(1-p)+.015f,c,32,Variant*30,Variant==2?200:360);
            var white=Color.Lerp(c,Color.white,.7f);white.a=c.a;
            if(Variant==1)
            {
                for(int i=0;i<7;i++){float a=i*2.39996f;Vector2 n=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(n*(radius*.4f),n*(radius+.16f),.025f,white);}
                Line(new Vector2(-radius,-radius*.5f),new Vector2(radius,radius*.5f),.11f*(1-p),white);
            }
            else if(Variant==2) {Ring(radius*.8f,.12f*(1-p),white,24,-30,130);}
            else {for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f+p;Vector2 n=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(n*radius*.6f,n*radius,.06f,c);}}
        }
        public void Refresh(){SetVerticesDirty();}
    }
}
