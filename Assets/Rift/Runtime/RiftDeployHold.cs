using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed class RiftDeployHold : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public RiftGame Game;public int Hero;
        public void OnPointerDown(PointerEventData e)
        {if(e.button==PointerEventData.InputButton.Left&&GetComponent<Button>().interactable)Game.BeginDeploymentHold(Hero,true);}
        public void OnPointerUp(PointerEventData e){Game.EndDeploymentHold(Hero);}
        public void OnPointerExit(PointerEventData e){Game.EndDeploymentHold(Hero);}
        void OnDisable(){if(Game)Game.EndDeploymentHold(Hero);}
    }
}
