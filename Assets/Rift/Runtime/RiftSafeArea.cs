using System;
using System.Globalization;
using UnityEngine;

namespace RiftHeroines
{
    // Keep the complete 16:9 game inside the device's safe rectangle in either landscape orientation.
    public sealed class RiftSafeArea : MonoBehaviour
    {
        public Rect AppliedArea {get;private set;}
        RectTransform content,parent;
        void Awake(){content=(RectTransform)transform;parent=(RectTransform)transform.parent;}
        void LateUpdate()
        {
            if(Screen.width<=0||Screen.height<=0)return;
            Rect area=Screen.safeArea;
            string argument=RiftGame.Argument("-riftSafeArea");
            if(!Application.isMobilePlatform&&!string.IsNullOrEmpty(argument))
            {
                string[] parts=argument.Split(',');float[] values=new float[4];bool valid=parts.Length==4;
                for(int i=0;valid&&i<4;i++)valid=float.TryParse(parts[i],NumberStyles.Float,CultureInfo.InvariantCulture,out values[i]);
                if(valid&&values[2]>0&&values[3]>0)area=new Rect(values[0],values[1],values[2],values[3]);
            }
            area=Rect.MinMaxRect(Mathf.Clamp(area.xMin,0,Screen.width),Mathf.Clamp(area.yMin,0,Screen.height),Mathf.Clamp(area.xMax,0,Screen.width),Mathf.Clamp(area.yMax,0,Screen.height));
            if(area.width<=0||area.height<=0)area=new Rect(0,0,Screen.width,Screen.height);
            AppliedArea=area;Vector2 size=parent.rect.size;
            float scale=Mathf.Min(size.x*area.width/Screen.width/1920f,size.y*area.height/Screen.height/1080f);
            content.localScale=new Vector3(scale,scale,1);
            content.anchoredPosition=new Vector2((area.center.x/Screen.width-.5f)*size.x,(area.center.y/Screen.height-.5f)*size.y);
        }
    }
}
