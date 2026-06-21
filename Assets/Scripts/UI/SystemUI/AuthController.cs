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
 
                    if (lblSuccess != null) { lblSuccess.text = "Đang xác thực..."; lblSuccess.style.display = DisplayStyle.Flex; }
 
                    // Gọi API Login tới máy chủ Node.js SQL Server
                    VocabLearning.Network.NetworkClient.Instance.Login(user, pass, (success, message, responseData) =>
                    {
                        HideMessages();
                        if (success && responseData != null && responseData.username != null)
                        {
                            _jsonDb.currentUser = responseData; // Ghi đè toàn bộ profile đã chuẩn hóa từ SQL Server
                            if (responseData.quests != null) _jsonDb.quests = responseData.quests;
                            if (responseData.achievements != null) _jsonDb.achievements = responseData.achievements;
                            CheckLevelUp();
                            _jsonDb.jwtToken = responseData.token; // Lưu token vào db cục bộ để duy trì phiên
                            VocabLearning.Network.NetworkClient.Instance.JwtToken = responseData.token; // Đồng bộ token tới Client mạng
                            if (responseData.inventory != null)
                            {
                                _jsonDb.inventory = responseData.inventory; // Đồng bộ kho đồ từ DB lên UI
                            }
 
                            // Chạy kiểm tra nhiệm vụ và điểm danh sau khi đăng nhập thành công
                            CheckDailyQuests();
                            CheckWeeklyLogin();

                            if (responseData.role == "admin")
                            {
                                Debug.Log("[Auth - Network] Đăng nhập Admin thành công. Vào màn hình Home với quyền Admin.");
                            }
                            else
                            {
                                Debug.Log("[Auth - Network] Đăng nhập User thành công. Tiến trình tải hoàn tất.");
                            }
                            LoadScreen(HomeScreenAsset);
                        }
                        else
                        {
                            if (lblError != null) { lblError.text = message; lblError.style.display = DisplayStyle.Flex; }
                        }
                    });
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
 
                    if (lblSuccess != null) { lblSuccess.text = "Đang tạo tài khoản..."; lblSuccess.style.display = DisplayStyle.Flex; }
 
                    // Gọi API đăng ký tới Backend SQL Server
                    VocabLearning.Network.NetworkClient.Instance.Register(user, email, pass, (success, message, responseData) =>
                    {
                        HideMessages();
                        if (success)
                        {
                            if (lblSuccess != null) { lblSuccess.text = "Tạo tài khoản thành công! Vui lòng đăng nhập."; lblSuccess.style.display = DisplayStyle.Flex; }
                            SwitchPanel(panelLogin);
                        }
                        else
                        {
                            if (lblError != null) { lblError.text = message; lblError.style.display = DisplayStyle.Flex; }
                        }
                    });
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
                    string email = inputForgotEmail?.value?.Trim();
                    if (string.IsNullOrEmpty(email))
                    {
                        if (lblError != null) { lblError.text = "Vui lòng nhập email đăng ký tài khoản của bạn!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    btnSendOTP.SetEnabled(false);
                    btnSendOTP.text = "Sending...";
                    if (lblSuccess != null) { lblSuccess.text = "Đang gửi OTP..."; lblSuccess.style.display = DisplayStyle.Flex; }

                    VocabLearning.Network.NetworkClient.Instance.SendOTP(email, (success, msg, res) =>
                    {
                        HideMessages();
                        if (success && res != null && res.success)
                        {
                            if (lblSuccess != null) { lblSuccess.text = "Mã OTP đã được gửi thành công đến hòm thư của bạn!"; lblSuccess.style.display = DisplayStyle.Flex; }
                            
                            // Bắt đầu đếm ngược 60s
                            int countdown = 60;
                            btnSendOTP.text = $"{countdown}s";
                            
                            var timer = btnSendOTP.schedule.Execute(() =>
                            {
                                countdown--;
                                if (countdown <= 0)
                                {
                                    btnSendOTP.text = "Gửi OTP";
                                    btnSendOTP.SetEnabled(true);
                                }
                                else
                                {
                                    btnSendOTP.text = $"{countdown}s";
                                }
                            });
                            timer.Every(1000);
                            timer.Until(() => countdown <= 0);
                        }
                        else
                        {
                            btnSendOTP.SetEnabled(true);
                            btnSendOTP.text = "Gửi OTP";
                            if (lblError != null) { lblError.text = string.IsNullOrEmpty(msg) ? "Gửi mã OTP thất bại!" : msg; lblError.style.display = DisplayStyle.Flex; }
                        }
                    });
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
                    string email = inputForgotEmail?.value?.Trim();
                    string otp = inputForgotOTP?.value?.Trim();
                    string newPass = inputForgotNewPassword?.value?.Trim();
                    string confirmPass = inputForgotConfirmPassword?.value?.Trim();

                    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
                    {
                        if (lblError != null) { lblError.text = "Vui lòng nhập đầy đủ các trường thông tin!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    if (newPass != confirmPass)
                    {
                        if (lblError != null) { lblError.text = "Mật khẩu xác nhận không khớp!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    if (newPass.Length < 4)
                    {
                        if (lblError != null) { lblError.text = "Mật khẩu phải dài tối thiểu 4 ký tự!"; lblError.style.display = DisplayStyle.Flex; }
                        return;
                    }

                    btnRecover.SetEnabled(false);
                    if (lblSuccess != null) { lblSuccess.text = "Đang xác thực và đặt lại mật khẩu..."; lblSuccess.style.display = DisplayStyle.Flex; }

                    VocabLearning.Network.NetworkClient.Instance.ResetPassword(email, otp, newPass, (success, msg, res) =>
                    {
                        btnRecover.SetEnabled(true);
                        HideMessages();
                        if (success && res != null && res.success)
                        {
                            if (lblSuccess != null) { lblSuccess.text = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập."; lblSuccess.style.display = DisplayStyle.Flex; }
                            
                            // Xóa trắng form sau khi thành công
                            if (inputForgotEmail != null) inputForgotEmail.value = "";
                            if (inputForgotOTP != null) inputForgotOTP.value = "";
                            if (inputForgotNewPassword != null) inputForgotNewPassword.value = "";
                            if (inputForgotConfirmPassword != null) inputForgotConfirmPassword.value = "";

                            // Tự động chuyển về login sau 2 giây
                            btnRecover.schedule.Execute(() =>
                            {
                                SwitchPanel(panelLogin);
                            }).StartingIn(2000);
                        }
                        else
                        {
                            if (lblError != null) { lblError.text = string.IsNullOrEmpty(msg) ? "Đặt lại mật khẩu thất bại!" : msg; lblError.style.display = DisplayStyle.Flex; }
                        }
                    });
                };
            }
        }
    }
}
