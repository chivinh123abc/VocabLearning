using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VocabLearning.UI;

namespace VocabLearning.Editor
{
    public class UIGeneratorTool
    {
        [MenuItem("Tools/Create VocabStudy UI Object")]
        public static void CreateUIObject()
        {
            // Tạo một GameObject trống
            GameObject go = new GameObject("VocabStudy_UI");

            // Thêm component UIDocument để hiển thị giao diện UI Toolkit
            UIDocument uiDoc = go.AddComponent<UIDocument>();

            // Tự động tìm và gán PanelSettings có sẵn trong project
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
            if (panelSettings != null)
            {
                uiDoc.panelSettings = panelSettings;
            }
            else
            {
                Debug.LogWarning("Không tìm thấy PanelSettings tại 'Assets/UI Toolkit/PanelSettings.asset'. Bạn có thể tự gán trong Inspector.");
            }

            // Gán file UXML mặc định vào UIDocument (Trường "Source Asset" trong Inspector)
            uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/HomeScreen.uxml");

            // Thêm script điều khiển UI chính
            var controller = go.AddComponent<VocabStudyController>();

            // Tự động gán các VisualTreeAsset cho Controller
            controller.HomeScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/HomeScreen.uxml");
            controller.VocabDetailScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/VocabSetDetailScreen.uxml");
            controller.PracticeModeScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/PracticeModeScreen.uxml");
            controller.QuestScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/QuestScreen.uxml");
            controller.BattleScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/BattleScreen.uxml");
            controller.FriendScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/FriendScreen.uxml");
            controller.ShopScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/ShopScreen.uxml");
            controller.RankingScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/RankingScreen.uxml");
            controller.ProfileScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/ProfileScreen.uxml");
            controller.ResultScreenAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/VocabStudy/ResultScreen.uxml");

            // Cập nhật db.json (Nếu cấu trúc dùng thẻ OverrideJsonDb thì gán, nếu dùng Resources thì bỏ qua bước này hoặc gán)
            controller.OverrideJsonDb = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Mockdata/db.json");

            // Tự động khởi tạo EventSystem nếu Scene chưa có (tránh lỗi không click được button)
            if ( Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // Đăng ký thao tác này vào hệ thống Undo
            Undo.RegisterCreatedObjectUndo(go, "Create UI Object");

            // Tự động chọn object vừa được tạo trong cửa sổ Hierarchy
            Selection.activeObject = go;

            Debug.Log("Đã tạo thành công Object chứa UI Toolkit, tự động gán PanelSettings, UXMLs, Json Database và EventSystem!");
        }
    }
}
