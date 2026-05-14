using UnityEngine;
using UnityEngine.UIElements;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        // --- BINDING CHO AUTH SCREEN ---
        private void BindAuthEvents()
        {
            var panelLogin = _root.Q<VisualElement>("PanelLogin");
            var panelRegister = _root.Q<VisualElement>("PanelRegister");
            var panelForgot = _root.Q<VisualElement>("PanelForgot");

            var lblError = _root.Q<Label>("LblAuthError");
            var lblSuccess = _root.Q<Label>("LblAuthSuccess");

            // Helper để ẩn lỗi
            void HideMessages()
            {
                if (lblError != null) lblError.style.display = DisplayStyle.None;
                if (lblSuccess != null) lblSuccess.style.display = DisplayStyle.None;
            }

            // Chuyển panel
            void SwitchPanel(VisualElement showPanel)
            {
                HideMessages();
                if (panelLogin != null) panelLogin.style.display = DisplayStyle.None;
                if (panelRegister != null) panelRegister.style.display = DisplayStyle.None;
                if (panelForgot != null) panelForgot.style.display = DisplayStyle.None;
                if (showPanel != null) showPanel.style.display = DisplayStyle.Flex;
            }

            // --- LOGIN PANEL EVENTS ---
            var btnLogin = _root.Q<Button>("BtnLogin");
            var btnGoToRegister = _root.Q<Button>("BtnGoToRegister");
            var btnGoToForgot = _root.Q<Button>("BtnGoToForgot");
            var inputLoginUsername = _root.Q<TextField>("InputLoginUsername");
            var inputLoginPassword = _root.Q<TextField>("InputLoginPassword");
            var btnToggleLoginPassword = _root.Q<Button>("BtnToggleLoginPassword");

            // Bắt buộc ẩn mật khẩu ngay khi khởi tạo
            if (inputLoginPassword != null) inputLoginPassword.isPasswordField = true;

            if (btnGoToRegister != null) btnGoToRegister.clicked += () => SwitchPanel(panelRegister);
            if (btnGoToForgot != null) btnGoToForgot.clicked += () => SwitchPanel(panelForgot);

            if (btnToggleLoginPassword != null && inputLoginPassword != null)
            {
                btnToggleLoginPassword.clicked += () =>
                {
                    inputLoginPassword.isPasswordField = !inputLoginPassword.isPasswordField;
                    btnToggleLoginPassword.text = inputLoginPassword.isPasswordField ? "Show" : "Hide";
                };
            }

            if (btnLogin != null)
            {
                btnLogin.clicked += () =>
                {
                    HideMessages();
                    string user = inputLoginUsername?.value;
                    string pass = inputLoginPassword?.value;

                    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                    {
                        if (lblError != null) { lblError.text = "Username and Password cannot be empty!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    // Validate
                    bool isValid = false;
                    VocabLearning.Data.UserJson matchedUser = null;

                    if (_jsonDb.registeredUsers != null && _jsonDb.registeredUsers.Count > 0)
                    {
                        matchedUser = _jsonDb.registeredUsers.Find(u => u.username == user && u.password == pass);
                        if (matchedUser != null) isValid = true;
                    }
                    else if (_jsonDb.currentUser != null)
                    {
                        // Fallback logic for mock db
                        if (user == _jsonDb.currentUser.username && (string.IsNullOrEmpty(_jsonDb.currentUser.password) || pass == _jsonDb.currentUser.password))
                        {
                            isValid = true;
                            matchedUser = _jsonDb.currentUser;
                        }
                    }

                    if (isValid)
                    {
                        _jsonDb.currentUser = matchedUser; // Cập nhật user hiện tại

                        // Chạy check quest và điểm danh SAU KHI đăng nhập thành công
                        CheckDailyQuests();
                        CheckWeeklyLogin();

                        LoadScreen(HomeScreenAsset); // Thành công -> Vào game
                    }
                    else
                    {
                        if (lblError != null) { lblError.text = "Invalid username or password!"; lblError.style.display = DisplayStyle.Flex; }
                    }
                };
            }

            // --- REGISTER PANEL EVENTS ---
            var btnRegister = _root.Q<Button>("BtnRegister");
            var btnBackToLoginFromReg = _root.Q<Button>("BtnBackToLoginFromReg");
            var inputRegUsername = _root.Q<TextField>("InputRegUsername");
            var inputRegEmail = _root.Q<TextField>("InputRegEmail");
            var inputRegPassword = _root.Q<TextField>("InputRegPassword");
            var btnToggleRegPassword = _root.Q<Button>("BtnToggleRegPassword");

            // Bắt buộc ẩn mật khẩu ngay khi khởi tạo
            if (inputRegPassword != null) inputRegPassword.isPasswordField = true;

            if (btnBackToLoginFromReg != null) btnBackToLoginFromReg.clicked += () => SwitchPanel(panelLogin);

            if (btnToggleRegPassword != null && inputRegPassword != null)
            {
                btnToggleRegPassword.clicked += () =>
                {
                    inputRegPassword.isPasswordField = !inputRegPassword.isPasswordField;
                    btnToggleRegPassword.text = inputRegPassword.isPasswordField ? "Show" : "Hide";
                };
            }

            if (btnRegister != null)
            {
                btnRegister.clicked += () =>
                {
                    HideMessages();
                    string user = inputRegUsername?.value;
                    string email = inputRegEmail?.value;
                    string pass = inputRegPassword?.value;

                    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                    {
                        if (lblError != null) { lblError.text = "Username and Password are required!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    if (_jsonDb.registeredUsers == null) _jsonDb.registeredUsers = new System.Collections.Generic.List<VocabLearning.Data.UserJson>();

                    // Kiểm tra trùng
                    if (_jsonDb.registeredUsers.Exists(u => u.username == user))
                    {
                        if (lblError != null) { lblError.text = "Username already exists!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    // Tạo user mới (Copy dữ liệu từ currentUser nếu muốn có sẵn data để test)
                    VocabLearning.Data.UserJson newUser = null;
                    if (_jsonDb.currentUser != null)
                    {
                        string originalJson = UnityEngine.JsonUtility.ToJson(_jsonDb.currentUser);
                        newUser = UnityEngine.JsonUtility.FromJson<VocabLearning.Data.UserJson>(originalJson);
                    }
                    else
                    {
                        newUser = new VocabLearning.Data.UserJson();
                    }

                    newUser.id = System.Guid.NewGuid().ToString();
                    newUser.username = user;
                    newUser.email = email;
                    newUser.password = pass;

                    _jsonDb.registeredUsers.Add(newUser);

                    if (lblSuccess != null) { lblSuccess.text = "Account created successfully! Please login."; lblSuccess.style.display = DisplayStyle.Flex; }
                    SwitchPanel(panelLogin);
                };
            }

            // --- FORGOT PASSWORD PANEL EVENTS ---
            var btnRecover = _root.Q<Button>("BtnRecover");
            var btnBackToLoginFromForgot = _root.Q<Button>("BtnBackToLoginFromForgot");
            var inputForgotEmail = _root.Q<TextField>("InputForgotEmail");
            var inputForgotOTP = _root.Q<TextField>("InputForgotOTP");
            var inputForgotNewPassword = _root.Q<TextField>("InputForgotNewPassword");
            var inputForgotConfirmPassword = _root.Q<TextField>("InputForgotConfirmPassword");
            var btnSendOTP = _root.Q<Button>("BtnSendOTP");

            // Bắt buộc ẩn mật khẩu ngay khi khởi tạo
            if (inputForgotNewPassword != null) inputForgotNewPassword.isPasswordField = true;
            if (inputForgotConfirmPassword != null) inputForgotConfirmPassword.isPasswordField = true;

            if (btnBackToLoginFromForgot != null) btnBackToLoginFromForgot.clicked += () => SwitchPanel(panelLogin);

            if (btnSendOTP != null)
            {
                btnSendOTP.clicked += () =>
                {
                    HideMessages();
                    string email = inputForgotEmail?.value;
                    if (string.IsNullOrEmpty(email))
                    {
                        if (lblError != null) { lblError.text = "Please enter your email first to receive OTP!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }
                    if (lblSuccess != null) { lblSuccess.text = "OTP Code has been sent to your email!"; lblSuccess.style.display = DisplayStyle.Flex; }
                };
            }

            var btnToggleForgotNewPassword = _root.Q<Button>("BtnToggleForgotNewPassword");
            if (btnToggleForgotNewPassword != null && inputForgotNewPassword != null)
            {
                btnToggleForgotNewPassword.clicked += () =>
                {
                    inputForgotNewPassword.isPasswordField = !inputForgotNewPassword.isPasswordField;
                    btnToggleForgotNewPassword.text = inputForgotNewPassword.isPasswordField ? "Show" : "Hide";
                };
            }

            var btnToggleForgotConfirmPassword = _root.Q<Button>("BtnToggleForgotConfirmPassword");
            if (btnToggleForgotConfirmPassword != null && inputForgotConfirmPassword != null)
            {
                btnToggleForgotConfirmPassword.clicked += () =>
                {
                    inputForgotConfirmPassword.isPasswordField = !inputForgotConfirmPassword.isPasswordField;
                    btnToggleForgotConfirmPassword.text = inputForgotConfirmPassword.isPasswordField ? "Show" : "Hide";
                };
            }

            if (btnRecover != null)
            {
                btnRecover.clicked += () =>
                {
                    HideMessages();
                    string email = inputForgotEmail?.value;
                    string otp = inputForgotOTP?.value;
                    string newPass = inputForgotNewPassword?.value;
                    string confirmPass = inputForgotConfirmPassword?.value;

                    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
                    {
                        if (lblError != null) { lblError.text = "Please fill in all fields!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    if (newPass != confirmPass)
                    {
                        if (lblError != null) { lblError.text = "Passwords do not match!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    // Dummy UI response cho tính năng reset mật khẩu
                    if (lblSuccess != null)
                    {
                        lblSuccess.text = "Your password has been successfully reset! You can now login.";
                        lblSuccess.style.display = DisplayStyle.Flex;
                    }

                    // Xóa trắng form sau khi thành công
                    if (inputForgotEmail != null) inputForgotEmail.value = "";
                    if (inputForgotOTP != null) inputForgotOTP.value = "";
                    if (inputForgotNewPassword != null) inputForgotNewPassword.value = "";
                    if (inputForgotConfirmPassword != null) inputForgotConfirmPassword.value = "";
                };
            }
        }
    }
}
