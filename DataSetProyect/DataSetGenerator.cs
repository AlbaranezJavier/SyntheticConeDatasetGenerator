using Boo.Lang;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/*
 Generates the data corresponding to an image to form a data set, when the p key is pressed.
 Each camera stores an independent image with its own data.
 Variables
 type: cone_big_orange = 0, cone_small_orange = 1, cone_blue = 2, cone_yellow = 3
 box2d: (pixelx1, pixely1), (pixelx2, pixely2) origin up-left
 locate(x,y,z): from camera
 yew(x,y,z): x(front), y(around)
 size(x,y,z):
     */

public class DataSetGenerator : MonoBehaviour
{
    public bool debugging = false;
    public Camera leftCam, rightCam;
    public ConeGenerator targets;
    public string pathFile = "Assets/DocDataset/";
    public string folderName = "...";

    private int imageIndex;
    private List<string> rowsData;

    private string pathDataset, pathLeftImages, pathRightImages, csvPathLeftCam, csvPathRightCam; //Folder paths

    //Constants
    private const string headerFile = "id type pixelx1 pixely1 pixelx2 pixely2 locatex locatey locatez yewx yewy yewz sizew sizeh sizel";
    private const string folderLeftImages = "/LeftImages";
    private const string folderRightImages = "/RightImages";
    private const string leftCSV = "/LeftCamera.csv";
    private const string rightCSV = "/RightCamera.csv";
    private NumberFormatInfo formatInfo = new NumberFormatInfo { 
        NumberDecimalSeparator = ".",
        NumberDecimalDigits = 3
    };

    private void Start()
    {
        //storage folders are managed and their addresses are saved
        pathDataset = pathFile + folderName;
        pathLeftImages = pathDataset + folderLeftImages;
        pathRightImages = pathDataset + folderRightImages;
        csvPathLeftCam = pathDataset + leftCSV;
        csvPathRightCam = pathDataset + rightCSV;
        imageIndex = InitializeFile(pathDataset, pathLeftImages, pathRightImages, csvPathLeftCam, csvPathRightCam, headerFile);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P)) {
            bool validLeft = SaveData(leftCam, csvPathLeftCam, imageIndex);
            bool validRight = SaveData(rightCam, csvPathRightCam, imageIndex);
            if (validLeft || validRight)
            {
                StartCoroutine(SaveScreens(pathLeftImages, pathRightImages, imageIndex));
                imageIndex++;
            }
        }

        if(debugging)
        {
            PrintLines(leftCam);
            PrintLines(rightCam);
        }
    }

    /*
     Saves the image of the camera
         */
    private IEnumerator SaveScreens(string leftPath, string rightPath, int index)
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        tex.Apply();
        byte[] bytes = tex.EncodeToJPG();
        File.WriteAllBytes(leftPath + "/" + index.ToString() + ".jpg", bytes);
        Destroy(tex);

        leftCam.rect = new Rect(0f, 0f, 1f, 0f);

        yield return new WaitForEndOfFrame();
        tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        tex.Apply();
        bytes = tex.EncodeToJPG();
        File.WriteAllBytes(rightPath + "/" + index.ToString() + ".jpg", bytes);
        Destroy(tex);

        leftCam.rect = new Rect(0f, 0f, 1f, 1f);
    }

    /*
     It initializes the specified file with the indicated head, if it does not exist it creates it
         */
    private static int InitializeFile(string path, string leftImages, string rightImages, string leftCSV, string rightCSV, string _headerFile)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(leftImages);
            Directory.CreateDirectory(rightImages);
           
            string[] createHeader = { _headerFile };
            File.WriteAllLines(leftCSV, createHeader, Encoding.UTF8);
            File.WriteAllLines(rightCSV, createHeader, Encoding.UTF8);
        }
        int imageIndex = 0;
        while (File.Exists(leftImages + "/" + imageIndex.ToString() + ".jpg")) imageIndex++;
        return imageIndex;
    }

    /*
     Stores data collected and processed by a camera.
         */
    private bool SaveData(Camera cam, string csvPath, int index)
    {
        try
        {
            Vector4 boundingbox2d;
            Vector3 locate, yew, size;
            int type;
            rowsData = new List<string>();
            //for each object generated 
            foreach (GameObject obj in targets.objectList)
            {
                boundingbox2d = GetBoundingBox(cam, obj.transform, out bool visible, false);
                if (visible)
                {
                    type = obj.transform.GetComponent<ObjectProperties>().type;
                    locate = cam.transform.InverseTransformPoint(obj.transform.position);
                    yew = (Quaternion.Inverse(cam.transform.rotation) * obj.transform.GetComponent<ObjectProperties>().GetYew()).eulerAngles;
                    size = obj.transform.GetComponent<ObjectProperties>().GetSize();
                    rowsData.Add(string.Format(formatInfo, "{0} {1} {2} {3} {4} {5} {6:N} {7:N} {8:N} {9:N} {10:N} {11:N} {12:N} {13:N} {14:N}",
                        index, type, boundingbox2d.x, boundingbox2d.y, boundingbox2d.z, boundingbox2d.w,
                        locate.x, locate.y, locate.z, yew.x, yew.y, yew.z, size.x, size.y, size.z));
                }
            }
            //if an object has been detected, save data and image
            if (rowsData.Count > 0)
            {
                File.AppendAllLines(csvPath, rowsData);
                return true;
            } else
            {
                return false;
            }
        }
        catch (UnassignedReferenceException) { return false; }
    }

    private void PrintLines(Camera cam)
    {
        try
        {
            Vector4 boundingbox2d;
            rowsData = new List<string>();
            //for each object generated 
            foreach (GameObject obj in targets.objectList)
            {
                boundingbox2d = GetBoundingBox(cam, obj.transform, out bool visible, true);
            }

        }
        catch (UnassignedReferenceException) { }
    }

    /*
     Generates the bounding box 2d of an object with respect to a camera.
     Returns a four position vector with the two points that make up the rectangle and
     if that object has finally been detected
         */
    private Vector4 GetBoundingBox(Camera cam, Transform parent, out bool visible, bool _debugging)
    {
        RaycastHit hit;
        int layerMask = 1 << 8;
        Vector2 point1 = new Vector2(Screen.width, Screen.height);
        Vector2 point2 = new Vector2(0f, 0f);
        Vector3 screenPoint;
        visible = false;

        foreach (Transform ch in parent)
        {
            //Discard those points that are not within the frustrum
            screenPoint = cam.WorldToViewportPoint(ch.position);
            if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
            {
                //Check if he has any obstacles in between
                if (Physics.Raycast(ch.position, cam.transform.position - ch.position, out hit, Vector3.Distance(ch.position, cam.transform.position), ~layerMask))
                {
                    //if (_debugging) Debug.DrawRay(ch.position, cam.transform.position - ch.position, Color.red);
                }
                else
                {
                    visible = true;
                    if (_debugging) Debug.DrawRay(ch.position, cam.transform.position - ch.position, Color.white);
                    //Generates the bounding box
                    Vector2 pointTemp = cam.WorldToScreenPoint(ch.position);
                    if (pointTemp.x < point1.x) { point1.x = (int)pointTemp.x; }
                    if (Screen.height - pointTemp.y < point1.y) { point1.y = (int)(Screen.height - pointTemp.y); }
                    if (pointTemp.x > point2.x) { point2.x = (int)pointTemp.x; }
                    if (Screen.height - pointTemp.y > point2.y) { point2.y = (int)(Screen.height - pointTemp.y); }
                }
            }
        }
        return new Vector4(point1.x, point1.y, point2.x, point2.y);
    }
}
