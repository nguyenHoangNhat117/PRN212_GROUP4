using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels;

/// <summary>
/// Nền chung cho các ViewModel của F11–F15: trạng thái bận + thông báo.
/// </summary>
public abstract class BusyViewModelBase : ViewModelBase
{
    private bool busy;
    private string message = "";

    protected bool IsBusy => busy;

    public bool CanInteract => !busy;

    public string Message
    {
        get => message;
        protected set => SetProperty(ref message, value);
    }

    protected void SetBusy(bool value)
    {
        busy = value;
        OnPropertyChanged(nameof(CanInteract));
        OnBusyChanged();
    }

    /// <summary>Gọi RaiseCanExecuteChanged cho các command tại đây.</summary>
    protected virtual void OnBusyChanged()
    {
    }

    protected void ShowError(Exception ex)
    {
        Message = ex.Message;
    }
}

