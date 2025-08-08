using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class IntroManager : MonoBehaviour
{
    [Header("==== 움직일 이미지 ====")]
    public GameObject floatingImage;            // 위아래로 움직일 이미지
    
    [Header("==== 움직임 설정 ====")]
    [Range(0.1f, 3f)]
    public float moveSpeed = 1f;                // 움직임 속도
    
    [Range(5f, 100f)]
    public float moveHeight = 30f;              // 움직임 범위 (픽셀)
    
    [Header("==== 씬 전환 ====")]
    public string nextSceneName = "main";       // 터치시 이동할 씬 이름
    
    private Vector3 startPosition;              // 시작 위치
    private float timeCounter = 0f;             // 시간 카운터
    
    void Start()
    {
        // 이미지 시작 위치 저장
        if (floatingImage != null)
        {
            startPosition = floatingImage.transform.localPosition;
        }
    }
    
    void Update()
    {
        // 1. 이미지 위아래 움직임 (무한반복)
        MoveImageUpDown();
        
        // 2. 터치 입력 확인 (마우스 클릭도 포함)
        CheckTouchInput();
    }
    
    private void MoveImageUpDown()
    {
        if (floatingImage == null) return;
        
        // 시간 증가
        timeCounter += Time.deltaTime * moveSpeed;
        
        // Sin 함수로 부드러운 위아래 움직임
        float yOffset = Mathf.Sin(timeCounter) * moveHeight;
        
        // 위치 적용
        Vector3 newPosition = startPosition;
        newPosition.y += yOffset;
        floatingImage.transform.localPosition = newPosition;
    }
    
    private void CheckTouchInput()
    {
        // 터치 입력 (모바일) - 새로운 Input System
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            GoToNextScene();
        }
        
        // 마우스 클릭 (PC) - 새로운 Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            GoToNextScene();
        }
    }
    
    private void GoToNextScene()
    {
        Debug.Log($"화면 터치! {nextSceneName} 씬으로 이동합니다.");
        SceneManager.LoadScene(nextSceneName);
    }
}