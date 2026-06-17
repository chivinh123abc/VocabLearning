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

        private string _baseUrl = "http://localhost:5000/api";
        private bool _configLoaded = false;

        public string BaseUrl
        {
            get
            {
                if (!_configLoaded)
                {
                    LoadConfig();
                }
                return _baseUrl;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadConfig();
        }

        private void LoadConfig()
        {
            _configLoaded = true;
            string configPath = System.IO.Path.Combine(Application.dataPath, "..", ".env");
            try
            {
                if (System.IO.File.Exists(configPath))
                {
                    string[] lines = System.IO.File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                            continue;

                        if (trimmed.StartsWith("ServerUrl="))
                        {
                            string url = trimmed.Substring("ServerUrl=".Length).Trim();
                            if (!string.IsNullOrEmpty(url))
                            {
                                _baseUrl = url;
                                Debug.Log($"[NetworkClient] Loaded ServerUrl from .env: {_baseUrl}");
                                return;
                            }
                        }
                        else if (trimmed.Contains("://") && (trimmed.StartsWith("http") || trimmed.StartsWith("https")))
                        {
                            _baseUrl = trimmed;
                            Debug.Log($"[NetworkClient] Loaded raw URL from .env: {_baseUrl}");
                            return;
                        }
                    }
                    Debug.Log($"[NetworkClient] .env found but no valid URL detected. Using default: {_baseUrl}");
                }
                else
                {
                    string defaultContent = 
                        "# Cau hinh ket noi server LAN PvP cho game VocabLearning\n" +
                        "# Hay thay doi dia chi IP duoi day khop voi IP may chay server (backend)\n" +
                        "ServerUrl=http://192.168.1.80:5000/api\n";
                    System.IO.File.WriteAllText(configPath, defaultContent, System.Text.Encoding.UTF8);
                    Debug.Log($"[NetworkClient] Created default .env at: {configPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkClient] Error reading/writing .env: {ex.Message}");
            }
        }

        public string JwtToken { get; set; } // Token bảo mật JWT dùng cho các API yêu cầu xác thực

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

        // API Gửi mã OTP khôi phục mật khẩu
        public void SendOTP(string email, NetworkCallback<ForgotPasswordResponse> callback)
        {
            string jsonPayload = $"{{\"email\":\"{email}\"}}";
            StartCoroutine(PostRequestCoroutine<ForgotPasswordResponse>("/auth/forgot-password", jsonPayload, callback));
        }

        // API Xác thực OTP và đặt mật khẩu mới
        public void ResetPassword(string email, string otp, string newPassword, NetworkCallback<ForgotPasswordResponse> callback)
        {
            string jsonPayload = $"{{\"email\":\"{email}\",\"otp\":\"{otp}\",\"newPassword\":\"{newPassword}\"}}";
            StartCoroutine(PostRequestCoroutine<ForgotPasswordResponse>("/auth/reset-password", jsonPayload, callback));
        }

        // API Đồng bộ Tiến trình (Save Game)
        public void SyncUserData(VocabLearning.Data.UserJson user, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(user);
            StartCoroutine(PostRequestCoroutine<string>("/user/sync", jsonPayload, callback));
        }

        // API Đổi tên hiển thị (Change Username)
        public void ChangeUsername(string newUsername, NetworkCallback<ChangeUsernameResponse> callback)
        {
            string jsonPayload = $"{{\"newUsername\":\"{newUsername}\"}}";
            StartCoroutine(PostRequestCoroutine<ChangeUsernameResponse>("/user/change-username", jsonPayload, callback));
        }

        // API Lấy dữ liệu toàn cục tĩnh
        public void GetGlobals(NetworkCallback<GlobalsResponse> callback)
        {
            StartCoroutine(GetRequestCoroutine<GlobalsResponse>("/globals", callback));
        }

        // API Lấy Thông tin Tiến độ & Profile đầy đủ của User từ DB
        public void GetUserProfile(NetworkCallback<UserProfileResponse> callback)
        {
            StartCoroutine(GetRequestCoroutine<UserProfileResponse>("/user/profile", callback));
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

        // API Admin: Lấy tất cả người dùng
        public void AdminGetUsers(NetworkCallback<AdminUsersResponse> callback)
        {
            StartCoroutine(GetRequestCoroutine<AdminUsersResponse>("/admin/users", callback));
        }

        // API Admin: Cập nhật thông số & trạng thái người dùng
        public void AdminUpdateUser(VocabLearning.Data.UserJson user, NetworkCallback<string> callback)
        {
            string jsonPayload = JsonUtility.ToJson(user);
            StartCoroutine(PutRequestCoroutine<string>($"/admin/users/{user.id}", jsonPayload, callback));
        }

        // API Admin: Xóa tài khoản người dùng
        public void AdminDeleteUser(string userId, NetworkCallback<string> callback)
        {
            StartCoroutine(DeleteRequestCoroutine<string>($"/admin/users/{userId}", callback));
        }

        // --- LAN PvP Matchmaking & Battle Room APIs ---

        // API Đăng ký ghép trận
        public void Matchmake(string userId, string username, int rankPoints, string avatar, NetworkCallback<MatchmakeStatusResponse> callback)
        {
            MatchmakePayload payload = new MatchmakePayload
            {
                userId = userId,
                username = username,
                rankPoints = rankPoints,
                avatar = avatar
            };
            string jsonPayload = JsonUtility.ToJson(payload);
            StartCoroutine(PostRequestCoroutine<MatchmakeStatusResponse>("/battle/matchmake", jsonPayload, callback));
        }

        // API Kiểm tra trạng thái tìm trận
        public void GetMatchmakeStatus(string userId, NetworkCallback<MatchmakeStatusResponse> callback)
        {
            StartCoroutine(GetRequestCoroutine<MatchmakeStatusResponse>($"/battle/matchmake/status/{userId}", callback));
        }

        // API Hủy tìm trận
        public void CancelMatchmake(string userId, NetworkCallback<string> callback)
        {
            CancelMatchmakePayload payload = new CancelMatchmakePayload { userId = userId };
            string jsonPayload = JsonUtility.ToJson(payload);
            StartCoroutine(PostRequestCoroutine<string>("/battle/matchmake/cancel", jsonPayload, callback));
        }

        // API Lấy thông tin trạng thái phòng chơi
        public void GetRoomState(string roomId, NetworkCallback<PvPRoomResponse> callback)
        {
            StartCoroutine(GetRequestCoroutine<PvPRoomResponse>($"/battle/room/{roomId}", callback));
        }

        // API Gửi đáp án lên server
        public void SubmitBattleAnswer(string roomId, string userId, int roundIndex, string answerText, float answerTime, NetworkCallback<PvPRoomResponse> callback)
        {
            SubmitAnswerPayload payload = new SubmitAnswerPayload
            {
                userId = userId,
                roundIndex = roundIndex,
                answerText = answerText,
                answerTime = answerTime
            };
            string jsonPayload = JsonUtility.ToJson(payload);
            StartCoroutine(PostRequestCoroutine<PvPRoomResponse>($"/battle/room/{roomId}/answer", jsonPayload, callback));
        }

        // API Thoát hoặc đầu hàng phòng đấu
        public void LeaveBattleRoom(string roomId, string userId, NetworkCallback<PvPRoomResponse> callback)
        {
            LeaveRoomPayload payload = new LeaveRoomPayload { userId = userId };
            string jsonPayload = JsonUtility.ToJson(payload);
            StartCoroutine(PostRequestCoroutine<PvPRoomResponse>($"/battle/room/{roomId}/leave", jsonPayload, callback));
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

                if (!string.IsNullOrEmpty(JwtToken))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + JwtToken);
                }

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

                if (!string.IsNullOrEmpty(JwtToken))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + JwtToken);
                }

                yield return request.SendWebRequest();

                ProcessResponse(request, callback);
            }
        }

        private IEnumerator DeleteRequestCoroutine<T>(string endpoint, NetworkCallback<T> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(BaseUrl + endpoint, "DELETE"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();

                if (!string.IsNullOrEmpty(JwtToken))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + JwtToken);
                }

                yield return request.SendWebRequest();

                ProcessResponse(request, callback);
            }
        }

        private IEnumerator GetRequestCoroutine<T>(string endpoint, NetworkCallback<T> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(BaseUrl + endpoint))
            {
                if (!string.IsNullOrEmpty(JwtToken))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + JwtToken);
                }

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

    [System.Serializable]
    public class AdminUsersResponse
    {
        public bool success;
        public System.Collections.Generic.List<VocabLearning.Data.UserJson> users;
    }

    [System.Serializable]
    public class UserProfileResponse
    {
        public bool success;
        public string message;
        public VocabLearning.Data.UserJson user;
    }

    [System.Serializable]
    public class ChangeUsernameResponse
    {
        public bool success;
        public string message;
        public string username;
        public string displayName;
    }

    [System.Serializable]
    public class ForgotPasswordResponse
    {
        public bool success;
        public string message;
    }

    [System.Serializable]
    public class MatchmakePayload
    {
        public string userId;
        public string username;
        public int rankPoints;
        public string avatar;
    }

    [System.Serializable]
    public class MatchmakeStatusResponse
    {
        public bool success;
        public string status; // "searching", "matched", "idle"
        public string roomId;
    }

    [System.Serializable]
    public class CancelMatchmakePayload
    {
        public string userId;
    }

    [System.Serializable]
    public class PvPPlayerState
    {
        public string userId;
        public string username;
        public int rankPoints;
        public int hp;
        public bool answered;
        public bool isCorrect;
        public float answerTime;
        public string answerText;
        public string avatar;
    }

    [System.Serializable]
    public class PvPRoomState
    {
        public string roomId;
        public System.Collections.Generic.List<PvPPlayerState> players;
        public System.Collections.Generic.List<VocabLearning.Data.WordJson> wordPool;
        public int currentRoundIndex;
        public string status; // "playing", "finished"
        public string winnerId;
    }

    [System.Serializable]
    public class PvPRoomResponse
    {
        public bool success;
        public PvPRoomState room;
    }

    [System.Serializable]
    public class SubmitAnswerPayload
    {
        public string userId;
        public int roundIndex;
        public string answerText;
        public float answerTime;
    }

    [System.Serializable]
    public class LeaveRoomPayload
    {
        public string userId;
    }
}

