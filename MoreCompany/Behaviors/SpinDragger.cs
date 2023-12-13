using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace MoreCompany.Behaviors
{
    public class SpinDragger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float speed = 1f;
        Vector2 lastMousePosition;
        bool dragging = false;
        Vector3 rotationalVelocity = Vector3.zero;
        public float dragSpeed = 1f;
        public float airDrag = 0.99f;
        public GameObject target;
        
        

        private void Update()
        {
            if (dragging)
            {
                Vector3 mouseDelta = Mouse.current.position.ReadValue() - lastMousePosition;
                rotationalVelocity += new Vector3(0, -mouseDelta.x, 0) * dragSpeed;
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            
            rotationalVelocity *= airDrag;

            target.transform.Rotate(rotationalVelocity * Time.deltaTime * speed, Space.World);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
            dragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
        }
    }
}