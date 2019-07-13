using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BoardShape : MonoBehaviour
{
    public int width = 1;
    public int height = 1;
    public bool[] baseShape;
    public bool rotation90 = false;
    public bool rotation180 = false;
    public bool rotation270 = false;
    public int rotation = 0;
    public bool isBlocking = false;
    public bool isUncovered = false;
    public RectTransform rTransform;
    public Animator anim;
    public int Item = 0;
    private void Start()
    {
        anim = GetComponent<Animator>();
        rTransform = GetComponent<RectTransform>();
    }

    //we could use a rotation matrix but it's only for 1 point so we know where it goes
    void RotateTransform()
    {
        int x = 0;
        int y = 0;
        //sprite pivot is bottom left
        if (rotation == 90)
            x = 1;
        else if (rotation == 180)
        {
            x = 1;
            y = 1;
        }
        else if (rotation == 270)
        {
            y = 1;
        }
        var rTransform = gameObject.GetComponent<RectTransform>();
        rTransform.pivot = new Vector2(x, y);
        rTransform.rotation = Quaternion.Euler(0,0,-rotation);
    }

    public bool GetShapePartAtPosition(int x, int y)
    {
        if (x < width && y < height)
            return baseShape[(height - 1 - y) * width + x];
        else
        {
            Debug.LogWarning($"OutOfBounds: x={x}, y={y}; w={width},h={height}");
            return false;
        }
    }
    public void Rotate(int rotation)
    {
        if ((rotation == 90 && rotation90) || (rotation == 180 && rotation180) || (rotation == 270 && rotation270))
        {
            if (rotation == 90)
            {
                baseShape = MatrixRotation(1);
            }
            else if (rotation == 180)
            {
                baseShape = MatrixRotation(2);
            }
            else if (rotation == 270)
            {
                baseShape = MatrixRotation(3);
            }
            if (rotation == 90 || rotation == 270)
            {
                //swap dimensions
                var tmp = width;
                width = height;
                height = tmp;
            }
            this.rotation = (this.rotation + rotation) % 360;
            RotateTransform();
        }
    }

    public void Uncovered()
    {
        isUncovered = true;
        anim.SetTrigger("Blink");
    }

    public void RotateRandom()
    {
        var rand = Random.Range(1, 4);
        Rotate(rand * 90);
    }

    //don't judge i'm terrible with matrix maths
    bool[] MatrixRotation(int quarts)
    {
        var m = width;
        var n = height;
        bool[] ret = new bool[m * n];

        //90 degrees
        if (quarts == 1)
        {
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    ret[j + i * n] = baseShape[i + (n - 1 - j) * m];
                }
            }
        }
        //180
        else if (quarts == 2)
        {
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    ret[i + j * m] = baseShape[(m - 1 - i) + (n - 1 - j) * m];
                }
            }
        }
        //-90
        else if (quarts == 3)
        {
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    ret[j + i * n] = baseShape[(m - 1 - i) + j * m];
                }
            }
        }
        return ret;
    }
}
