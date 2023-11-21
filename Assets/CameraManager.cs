using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public float percentCameraMouseMove = .3f;
    private GameObject player;
    private float playerUnitHeight;
    private float playerUnitWidth;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        Texture2D playerTexture = player.GetComponent<SpriteRenderer>().sprite.texture;
        //TextureImporter playerTextureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(playerTexture)) as TextureImporter;
        playerUnitHeight = playerTexture.height / 10;//playerTextureImporter.spritePixelsPerUnit;
        playerUnitHeight = playerTexture.width / 10;//playerTextureImporter.spritePixelsPerUnit;
    }

    // Update is called once per frame
    void Update()
    {
        followPlayer();
    }

    public void followPlayer()
    {
        Vector3 newCameraLocation = player.transform.position;
        newCameraLocation.z = gameObject.transform.position.z;

        //add in half the players height too since the pivot is at the players feet
        newCameraLocation.y += playerUnitHeight / 2;
        newCameraLocation.x += playerUnitWidth / 2;

        //Move the camera a percentage of the way to the mouse location
        Vector2 mousePos = Input.mousePosition;

        //but don't count outside of screen space
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);

        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        Vector2 vecToMouse = (mousePos - (Vector2)newCameraLocation) * percentCameraMouseMove;

        newCameraLocation += (Vector3)vecToMouse;

        gameObject.transform.position = newCameraLocation;
    }

}
