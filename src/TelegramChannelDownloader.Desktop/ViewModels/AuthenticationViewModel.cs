using System.Windows.Input;
using TelegramChannelDownloader.Core.Services;
using TelegramChannelDownloader.Desktop.Commands;
using TelegramChannelDownloader.Desktop.Services;
using TelegramChannelDownloader.Desktop.Utils;
using TelegramChannelDownloader.TelegramApi;
using TelegramChannelDownloader.TelegramApi.Authentication.Models;
using TelegramChannelDownloader.TelegramApi.Configuration;
using AuthenticationStatus = TelegramChannelDownloader.TelegramApi.Authentication.Models.AuthResult;
using UserInfo = TelegramChannelDownloader.TelegramApi.Authentication.Models.TelegramUserInfo;

namespace TelegramChannelDownloader.Desktop.ViewModels;

public class AuthenticationViewModel : ObservableObject
{
    private readonly ITelegramApiClient _telegramApi;
    private readonly IValidationService _validation;
    private readonly IUIService _uiService;

    private string _apiId = string.Empty;
    private string _apiHash = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _verificationCode = string.Empty;
    private string _twoFactorCode = string.Empty;
    private string _connectionStatus = "Not connected";
    private bool _isConnected;
    private bool _isConnecting;
    private bool _isTwoFactorRequired;
    private bool _isPhoneNumberRequired;
    private bool _isVerificationCodeRequired;
    private AuthenticationState _authenticationState = AuthenticationState.Disconnected;
    private UserInfo? _currentUser;

    public AuthenticationViewModel(ITelegramApiClient telegramApi, IValidationService validation, IUIService uiService)
    {
        _telegramApi = telegramApi ?? throw new ArgumentNullException(nameof(telegramApi));
        _validation = validation ?? throw new ArgumentNullException(nameof(validation));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        // Subscribe to authentication status changes
        _telegramApi.AuthenticationStatusChanged += OnAuthenticationStatusChanged;

        // Initialize commands
        ConnectCommand = new AsyncRelayCommand(ExecuteConnectAsync, CanExecuteConnect);
        SubmitPhoneCommand = new AsyncRelayCommand(ExecuteSubmitPhoneAsync, CanExecuteSubmitPhone);
        SubmitCodeCommand = new AsyncRelayCommand(ExecuteSubmitCodeAsync, CanExecuteSubmitCode);
        SubmitTwoFactorCommand = new AsyncRelayCommand(ExecuteSubmitTwoFactorAsync, CanExecuteSubmitTwoFactor);
        DisconnectCommand = new AsyncRelayCommand(ExecuteDisconnectAsync, CanExecuteDisconnect);
    }

    #region Properties

    public string ApiId
    {
        get => _apiId;
        set
        {
            if (SetProperty(ref _apiId, value))
            {
                OnPropertyChanged(nameof(IsApiIdValid));
                OnPropertyChanged(nameof(ApiIdValidationMessage));
            }
        }
    }

    public bool IsApiIdValid
    {
        get
        {
            var result = _validation.ValidateApiId(ApiId);
            return result.IsValid;
        }
    }

    public string ApiIdValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(ApiId)) return string.Empty;
            var result = _validation.ValidateApiId(ApiId);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public string ApiHash
    {
        get => _apiHash;
        set
        {
            if (SetProperty(ref _apiHash, value))
            {
                OnPropertyChanged(nameof(IsApiHashValid));
                OnPropertyChanged(nameof(ApiHashValidationMessage));
            }
        }
    }

    public bool IsApiHashValid
    {
        get
        {
            var result = _validation.ValidateApiHash(ApiHash);
            return result.IsValid;
        }
    }

    public string ApiHashValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(ApiHash)) return string.Empty;
            var result = _validation.ValidateApiHash(ApiHash);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (SetProperty(ref _phoneNumber, value))
            {
                OnPropertyChanged(nameof(IsPhoneNumberValid));
                OnPropertyChanged(nameof(PhoneNumberValidationMessage));
            }
        }
    }

    public bool IsPhoneNumberValid
    {
        get
        {
            var result = _validation.ValidatePhoneNumber(PhoneNumber);
            return result.IsValid;
        }
    }

    public string PhoneNumberValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(PhoneNumber)) return string.Empty;
            var result = _validation.ValidatePhoneNumber(PhoneNumber);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public string VerificationCode
    {
        get => _verificationCode;
        set
        {
            if (SetProperty(ref _verificationCode, value))
            {
                OnPropertyChanged(nameof(IsVerificationCodeValid));
                OnPropertyChanged(nameof(VerificationCodeValidationMessage));
            }
        }
    }

    public bool IsVerificationCodeValid
    {
        get
        {
            var result = _validation.ValidateVerificationCode(VerificationCode);
            return result.IsValid;
        }
    }

    public string VerificationCodeValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(VerificationCode)) return string.Empty;
            var result = _validation.ValidateVerificationCode(VerificationCode);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public string TwoFactorCode
    {
        get => _twoFactorCode;
        set
        {
            if (SetProperty(ref _twoFactorCode, value))
            {
                OnPropertyChanged(nameof(IsTwoFactorCodeValid));
                OnPropertyChanged(nameof(TwoFactorCodeValidationMessage));
            }
        }
    }

    public bool IsTwoFactorCodeValid
    {
        get
        {
            var result = _validation.ValidateTwoFactorCode(TwoFactorCode);
            return result.IsValid;
        }
    }

    public string TwoFactorCodeValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(TwoFactorCode)) return string.Empty;
            var result = _validation.ValidateTwoFactorCode(TwoFactorCode);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetProperty(ref _isConnecting, value);
    }

    public bool IsTwoFactorRequired
    {
        get => _isTwoFactorRequired;
        set => SetProperty(ref _isTwoFactorRequired, value);
    }

    public bool IsPhoneNumberRequired
    {
        get => _isPhoneNumberRequired;
        set => SetProperty(ref _isPhoneNumberRequired, value);
    }

    public bool IsVerificationCodeRequired
    {
        get => _isVerificationCodeRequired;
        set => SetProperty(ref _isVerificationCodeRequired, value);
    }

    public AuthenticationState AuthenticationState
    {
        get => _authenticationState;
        set => SetProperty(ref _authenticationState, value);
    }

    public UserInfo? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    #endregion

    #region Commands

    public ICommand ConnectCommand { get; }
    public ICommand SubmitPhoneCommand { get; }
    public ICommand SubmitCodeCommand { get; }
    public ICommand SubmitTwoFactorCommand { get; }
    public ICommand DisconnectCommand { get; }

    #endregion

    #region Events

    public event EventHandler<bool>? AuthenticationStateChanged;

    #endregion

    #region Command Implementations

    private bool CanExecuteConnect() => !IsConnecting && IsApiIdValid && IsApiHashValid;

    private async Task ExecuteConnectAsync()
    {
        try
        {
            IsConnecting = true;
            ConnectionStatus = "Connecting...";

            if (!int.TryParse(ApiId, out var apiIdInt))
            {
                await _uiService.ShowErrorAsync("Invalid API ID", "Please enter a valid numeric API ID.");
                return;
            }

            var config = new TelegramApiConfig
            {
                ApiId = apiIdInt,
                ApiHash = ApiHash
            };

            await _telegramApi.InitializeAsync(config);
            ConnectionStatus = "Connection initialized. Ready for authentication.";
        }
        catch (Exception ex)
        {
            await _uiService.ShowErrorAsync("Connection Failed", $"Failed to connect: {ex.Message}");
            ConnectionStatus = "Connection failed";
            AuthenticationState = AuthenticationState.ConnectionError;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteSubmitPhone() => !IsConnecting && IsPhoneNumberValid && 
                                           AuthenticationState == AuthenticationState.WaitingForPhoneNumber;

    private async Task ExecuteSubmitPhoneAsync()
    {
        try
        {
            IsConnecting = true;
            await _telegramApi.AuthenticatePhoneAsync(PhoneNumber);
        }
        catch (Exception ex)
        {
            await _uiService.ShowErrorAsync("Phone Submission Failed", $"Failed to submit phone number: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteSubmitCode() => !IsConnecting && IsVerificationCodeValid && 
                                          AuthenticationState == AuthenticationState.WaitingForVerificationCode;

    private async Task ExecuteSubmitCodeAsync()
    {
        try
        {
            IsConnecting = true;
            await _telegramApi.VerifyCodeAsync(VerificationCode);
        }
        catch (Exception ex)
        {
            await _uiService.ShowErrorAsync("Code Verification Failed", $"Failed to verify code: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteSubmitTwoFactor() => !IsConnecting && IsTwoFactorCodeValid && 
                                               AuthenticationState == AuthenticationState.WaitingForTwoFactorAuth;

    private async Task ExecuteSubmitTwoFactorAsync()
    {
        try
        {
            IsConnecting = true;
            await _telegramApi.VerifyTwoFactorAuthAsync(TwoFactorCode);
        }
        catch (Exception ex)
        {
            await _uiService.ShowErrorAsync("Two-Factor Authentication Failed", $"Failed to verify two-factor code: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteDisconnect() => IsConnected && !IsConnecting;

    private async Task ExecuteDisconnectAsync()
    {
        try
        {
            var confirmed = await _uiService.ShowConfirmationAsync("Disconnect", 
                "Are you sure you want to disconnect? You will need to re-authenticate.");
            
            if (!confirmed) return;

            IsConnecting = true;
            await _telegramApi.DisconnectAsync();
            
            // Reset authentication state
            ConnectionStatus = "Not connected";
            IsConnected = false;
            AuthenticationState = AuthenticationState.Disconnected;
            CurrentUser = null;
            
            // Clear input fields
            VerificationCode = string.Empty;
            TwoFactorCode = string.Empty;
        }
        catch (Exception ex)
        {
            await _uiService.ShowErrorAsync("Disconnect Failed", $"Failed to disconnect: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    #endregion

    #region Event Handlers

    private void OnAuthenticationStatusChanged(object? sender, AuthStatusChangedEventArgs e)
    {
        var status = e.CurrentStatus;
        
        // Update authentication state
        AuthenticationState = status.State;
        IsConnected = status.IsConnected;
        IsConnecting = status.IsAuthenticating;
        ConnectionStatus = status.Message;
        CurrentUser = status.User;

        if (status.User != null)
        {
            ConnectionStatus = $"Connected as {status.User.DisplayName}";
        }

        // Update UI field visibility based on authentication state
        IsPhoneNumberRequired = status.State == AuthenticationState.WaitingForPhoneNumber;
        IsVerificationCodeRequired = status.State == AuthenticationState.WaitingForVerificationCode;
        IsTwoFactorRequired = status.State == AuthenticationState.WaitingForTwoFactorAuth;

        // Clear input fields when appropriate
        if (status.State == AuthenticationState.WaitingForPhoneNumber)
        {
            VerificationCode = string.Empty;
            TwoFactorCode = string.Empty;
        }
        else if (status.State == AuthenticationState.WaitingForVerificationCode)
        {
            TwoFactorCode = string.Empty;
        }

        // Notify listeners about authentication state changes
        AuthenticationStateChanged?.Invoke(this, IsConnected);
    }

    #endregion
}