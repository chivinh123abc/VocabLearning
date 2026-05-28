using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VocabLearning.Network
{
    public class NetworkClient : MonoBehaviour
    {
        private static NetworkClient _instance;
        public static NetworkClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("NetworkClient");
                    _instance = go.AddComponent<NetworkClient>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private const string BaseUrl = "http://localhost:5000/api";

        public delegate void NetworkCallback<T>(bool success, string message, T data);

        // API Đăng nhập
        public void Login(string username, string password, NetworkCallback<VocabLearning.Data.UserJson> callback)
        {
            StartCoroutine(PostRequestCoroutine<VocabLearning.Data.UserJson>("/auth/login", $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}", callback));
        }

        // API Đăng ký
        public void Register(string username, string email, string password, NetworkCallback<VocabLearning.Data.UserJson> callback)
        {
            string jsonPayload = $"{{\"username\":\"{username}\",\"email\":\"{email}\",\"password\":\"{password}\"}}";
            StartCoroutine(PostRequestCoroutine<VocabLearning.Data.UserJson>("/auth/register", jsonPayload, callback));
        }

        // API Đồng bộ Tiến trình (Save Game)
        public void SyncUserData(VocabLearning.Data.UserJson user, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(user);
            StartCoroutine(PostRequestCoroutine<string>("/user/sync", jsonPayload, callback));
        }

        // API Lấy dữ liệu toàn cục tĩnh
        public void GetGlobals(NetworkCallback<GlobalsResponse> callback)
        {
            StartCoroutine(GetRequestCoroutine<GlobalsResponse>("/globals", callback));
        }

        // API Admin: Thêm từ vựng mới
        public void AdminAddWord(VocabLearning.Data.WordJson word, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(word);
            StartCoroutine(PostRequestCoroutine<string>("/admin/words", jsonPayload, callback));
        }

        // API Admin: Sửa từ vựng
        public void AdminUpdateWord(VocabLearning.Data.WordJson word, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(word);
            StartCoroutine(PutRequestCoroutine<string>($"/admin/words/{word.id}", jsonPayload, callback));
        }

        // API Admin: Xóa từ vựng
        public void AdminDeleteWord(string wordId, NetworkCallback<string> callback)
        {
            StartCoroutine(DeleteRequestCoroutine<string>($"/admin/words/{wordId}", callback));
        }

        // API Admin: Thêm bộ từ vựng mới
        public void AdminAddVocabSet(VocabLearning.Data.VocabSetJson vocabSet, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(vocabSet);
            StartCoroutine(PostRequestCoroutine<string>("/admin/vocabsets", jsonPayload, callback));
        }

        // API Admin: Sửa bộ từ vựng
        public void AdminUpdateVocabSet(VocabLearning.Data.VocabSetJson vocabSet, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(vocabSet);
            StartCoroutine(PutRequestCoroutine<string>($"/admin/vocabsets/{vocabSet.id}", jsonPayload, callback));
        }

        // API Admin: Xóa bộ từ vựng
        public void AdminDeleteVocabSet(string setId, NetworkCallback<string> callback)
        {
            StartCoroutine(DeleteRequestCoroutine<string>($"/admin/vocabsets/{setId}", callback));
        }

        // --- COROUTINE TRUY VẤN MẠNG ---

        private IEnumerator PostRequestCoroutine<T>(string endpoint, string jsonPayload, NetworkCallback<T> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(BaseUrl + endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                ProcessResponse(request, callback);
            }
        }

        private IEnumerator PutRequestCoroutine<T>(string endpoint, string jsonPayload, NetworkCallback<T> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(BaseUrl + endpoint, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                ProcessResponse(request, callback);
            }
        }

        private IEnumerator DeleteRequestCoroutine<T>(string endpoint, NetworkCallback<T> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(BaseUrl + endpoint, "DELETE"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                ProcessResponse(request, callback);
            }
        }

        private IEnumerator GetRequestCoroutine<T>(string endpoint, NetworkCallback<T> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(BaseUrl + endpoint))
            {
                yield return request.SendWebRequest();

                ProcessResponse(request, callback);
            }
        }

        private void ProcessResponse<T>(UnityWebRequest request, NetworkCallback<T> callback)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    // Nếu kiểu T là string, trả thẳng chuỗi phản hồi
                    if (typeof(T) == typeof(string))
                    {
                        callback?.Invoke(true, "Thao tác thành công!", (T)(object)jsonResponse);
                    }
                    else
                    {
                        T parsedObj = JsonUtility.FromJson<T>(jsonResponse);
                        callback?.Invoke(true, "Thành công!", parsedObj);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Network] Lỗi parse dữ liệu JSON: {ex.Message}. Response: {jsonResponse}");
                    callback?.Invoke(false, $"Lỗi xử lý phản hồi: {ex.Message}", default);
                }
            }
            else
            {
                string errMessage = "Lỗi kết nối mạng!";
                if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    try
                    {
                        ErrorResponse errObj = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                        errMessage = errObj.message;
                    }
                    catch
                    {
                        errMessage = request.error;
                    }
                }
                else
                {
                    errMessage = request.error;
                }

                Debug.LogError($"[Network] API Lỗi: {request.url} | {errMessage}");
                callback?.Invoke(false, errMessage, default);
            }
        }
    }

    // Các lớp hỗ trợ parse JSON từ phản hồi API
    [System.Serializable]
    public class ErrorResponse
    {
        public bool success;
        public string message;
    }

    [System.Serializable]
    public class GlobalsResponse
    {
        public bool success;
        public System.Collections.Generic.List<VocabLearning.Data.WordJson> words;
        public System.Collections.Generic.List<VocabLearning.Data.VocabSetJson> vocabSets;
        public System.Collections.Generic.List<VocabLearning.Data.AchievementJson> achievements;
        public System.Collections.Generic.List<VocabLearning.Data.QuestJson> questPool;
        public System.Collections.Generic.List<VocabLearning.Data.ShopItemJson> shopItems;
        public System.Collections.Generic.List<VocabLearning.Data.UserJson> leaderboardUsers;
    }
}
