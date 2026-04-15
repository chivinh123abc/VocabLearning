using UnityEngine;
using UnityEngine.UIElements; // Namespace bắt buộc cho UI Toolkit

public class MainUIController : MonoBehaviour
{
    // Tham chiếu đến ảnh Sprite bạn kéo thả ở Inspector (từ thư mục Assets/Sprites)
    [SerializeField] private Sprite _avatarImage;

    private UIDocument _doc;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;


        Button practiceBtn = root.Q<Button>(className: "mode-btn-practice");
        if (practiceBtn != null)
        {
            practiceBtn.clicked += OnPracticeClicked;
        }


        Label titleLabel = root.Q<Label>(className: "text-3xl");
        if (titleLabel != null) titleLabel.text = "Gia đình (Family)";

        VisualElement avatarBox = root.Q<VisualElement>("AvatarBox");
        if (avatarBox != null && _avatarImage != null)
        {
            avatarBox.style.backgroundImage = new StyleBackground(_avatarImage);
        }
    }

    private void OnPracticeClicked()
    {
        Debug.Log("Chuyển sang chế độ Practice Screen...");
    }
}
