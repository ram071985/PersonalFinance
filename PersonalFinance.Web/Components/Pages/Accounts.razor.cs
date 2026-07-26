using Microsoft.AspNetCore.Components;
using PersonalFinance.Models;

namespace PersonalFinance.Components.Pages;

public partial class Accounts : ComponentBase
{
    private List<Account> _accounts = new();
    private Account formModel = new();
    private bool _isLoading = true;
    private bool _showForm;
    private bool _isEdit;
    private int _nextId = 1;

    protected override async Task OnInitializedAsync()
    {
        // Simulate network delay – replace with real API call later
        await Task.Delay(600);
        
        // Temporary seed data so the page isn’t empty while we build the UI
        _accounts =
        [
            new Account { Id = _nextId++, Name = "Chase Checking", Type = "Checking", Balance = 4250.75m, Institution = "Chase" },
            new Account { Id = _nextId++, Name = "Ally Savings", Type = "Savings", Balance = 12800.00m, Institution = "Ally" },
            new Account { Id = _nextId++, Name = "Amex Blue Cash", Type = "Credit Card", Balance = -842.30m, Institution = "American Express" }
        ];

        _isLoading = false;
    }

    private void ShowCreateForm()
    {
        formModel = new Account { Type = "Checking" };
        _isEdit = false;
        _showForm = true;
    }

    private void ShowEditForm(Account account)
    {
        // Clone – never mutate the list item until the user saves
        formModel = new Account
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            Institution = account.Institution,
            Notes = account.Notes
        };
        _isEdit = true;
        _showForm = true;
    }

    private void HideForm() => _showForm = false;

    private void HandleSave(Account account)
    {
        if (_isEdit)
        {
            var existing = _accounts.FirstOrDefault(a => a.Id == account.Id);
            if (existing is not null)
            {
                existing.Name = account.Name;
                existing.Type = account.Type;
                existing.Balance = account.Balance;
                existing.Institution = account.Institution;
                existing.Notes = account.Notes;
            }
        }
        else
        {
            account.Id = _nextId++;
            _accounts.Add(account);
        }

        _showForm = false;
    }

    private void Delete(int id) => _accounts.RemoveAll(a => a.Id == id);
}