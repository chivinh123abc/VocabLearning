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

        // ----------------------------------------------------
        // 1. Cách gán Action bấm Nút (Practice)
        // ----------------------------------------------------
        // Lấy nút Practice dựa vào class hoặc tên
        Button practiceBtn = root.Q<Button>(className: "mode-btn-practice");
        if (practiceBtn != null)
        {
            practiceBtn.clicked += OnPracticeClicked;
        }

        // ----------------------------------------------------
        // 2. Cách thay đổi Text (Dữ liệu chữ)
        // ----------------------------------------------------
        // Tìm Label theo Class (text-3xl) hoặc Name
        Label titleLabel = root.Q<Label>(className: "text-3xl");
        if (titleLabel != null) titleLabel.text = "Gia đình (Family)";

        // ----------------------------------------------------
        // 3. Cách chèn Hình Ảnh vào một Vùng (Image/VisualElement)
        // ----------------------------------------------------
        // Giả sử có 1 cái hộp bo góc để chứa Avatar, tôi gán tên `name="AvatarBox"` trong UXML
        VisualElement avatarBox = root.Q<VisualElement>("AvatarBox");
        if (avatarBox != null && _avatarImage != null)
        {
            // Thiết lập background image cho thẻ VisualElement thay vì đổi màu
            avatarBox.style.backgroundImage = new StyleBackground(_avatarImage);
        }
    }

    private void OnPracticeClicked()
    {
        Debug.Log("Chuyển sang chế độ Practice Screen...");
    }
}
