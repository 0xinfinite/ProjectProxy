
using UnityEngine;

    public class FieldOfView : MonoBehaviour
    {
        [SerializeField] private int numRays;
        //[SerializeField] private Shader depthShader;
        //[SerializeField] private Shader camShader;
        //[SerializeField] private Material mat;
        [SerializeField] private Camera eye;
        [SerializeField] private float fov = 90f;
        //[SerializeField] private float viewDistance = 5f;

        //[SerializeField] private RenderTexture rt;
        //[SerializeField] private float tempValue = 1;
        //[SerializeField] private Vector2 readPos;
        //[SerializeField] private float distanceAtPos;

        private Texture2D pixelReader;
        [SerializeField] private RenderTexture eyeRenderTexture;
    [SerializeField] private int minmapLevel = 0;
    protected Color[] depths;
        // [SerializeField] private RenderTexture resultRT;

        private void Awake()
        {
            CreateTextures();
            SetupEye(eye);
        }

        private void CreateTextures()
        {
            //if(eyeRenderTexture == null)
            //eyeRenderTexture = new RenderTexture(numRays, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            //resultRT = new RenderTexture(numRays, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            eye.targetTexture = eyeRenderTexture;
            numRays = eyeRenderTexture.width;
            pixelReader = new Texture2D(numRays, 1, TextureFormat.RGBAFloat, false);
        }

        private void SetupEye(Camera eye)
        {
            //eye.farClipPlane = viewDistance;
            eye.nearClipPlane = 0.01f;
            eye.targetTexture = eyeRenderTexture;
            eye.aspect = numRays;
            eye.fieldOfView = Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan((fov * Mathf.Deg2Rad) / 2f) / eye.aspect);
        }

        // changed to late update so the movement will have been applied before rendering
        private void LateUpdate()
        {
            RenderEye(eye);
            DebugDrawEye(eye);
        }

        private void RenderEye(Camera eye)
        {
            //var shadowDistance = QualitySettings.shadowDistance;
            //QualitySettings.shadowDistance = 0;

            ////eye.Render();

            //QualitySettings.shadowDistance = shadowDistance;
        }

        private void DebugDrawEye(Camera eye)
        {
            RenderTexture.active = eyeRenderTexture;//resultRT;
            pixelReader.ReadPixels(new Rect(0, 0, numRays, 1), 0, 0);
            pixelReader.Apply();

            // Get all pixels in one call rather than multiple individual calls. Much faster
            depths = pixelReader.GetPixels(minmapLevel);

            // Get right camera's world space position and direction vectors for reuse later
            var start = eye.transform.position;
            var forward = eye.transform.forward;
            var right = eye.transform.right;

            // Calculate width and steps between "rays" at 1 unit depth
            float viewHalfWidth = Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
            float viewWidth = viewHalfWidth * 2f;
            float rayStepSize = viewWidth / (float)(numRays);

            // Calculate the ray step as a world space vector
            var rayStep = right * rayStepSize;

            // Calculate starting ray vector from half of the view width and inset by half a step
            var rayDir = forward - right * (viewHalfWidth - rayStepSize * 0.5f);

            for (int i = 0; i < eyeRenderTexture.width; i++)
            {
                var depth = 1-depths[i].g;

                if (depths[i].g <  0.00001f)
                //depth = eye.farClipPlane;
                {
                    rayDir += rayStep;
                    continue; 
                }

                // Calculate end position by multiplying the ray vector (which has a depth of 1) and
                // the depth from the pixel value, then add to the start position.
                var end = start + rayDir * depth * eye.farClipPlane;

                Debug.DrawLine(start, end, new Color(depths[i].r, Mathf.Lerp(depths[i].g, 0, Mathf.Max(depths[i].r, depths[i].b)), depths[i].b), 0f);

                // Apply step vector
                rayDir += rayStep;
            }

            //distanceAtPos = depths[];
        }
    }

