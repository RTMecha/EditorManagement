using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EditorManagement.Functions.Components
{
    public class MouseController : MonoBehaviour
    {
        public MyGameActions gameActions;

        private void Awake()
        {
            gameActions = new MyGameActions();
        }

        private void Update()
        {
            if (System.Windows.Forms.Cursor.Position != new System.Drawing.Point((int)transform.position.x, (int)transform.position.y))
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)transform.position.x, (int)transform.position.y);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (EventSystem.current != null  && EventSystem.current.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<Button>())
                        EventSystem.current.currentSelectedGameObject.GetComponent<Button>().OnSubmit(null);

                    if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                        EventSystem.current.currentSelectedGameObject.GetComponent<InputField>().ActivateInputField();
                }
            }

            float x = gameActions.Move.Vector.x;
            float y = gameActions.Move.Vector.y;

            transform.position += new Vector3(x, y, 0f);
        }
    }
}
