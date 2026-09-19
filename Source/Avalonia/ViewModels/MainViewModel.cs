using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace WWSTE.Avalonia.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private const string AllStringsCategory = "All strings";

    private CategoryItemViewModel? _selectedCategory;
    private StringEntryViewModel? _selectedEntry;
    private string _searchText = string.Empty;
    private string _resultCountText = string.Empty;
    private string _selectedSort = "A — Z";
    private string _selectedMask = "*";
    private bool _isCategoryMode = true;
    private bool _documentIsModified;

    public MainViewModel()
    {
        CategoryNames = new ObservableCollection<string>
        {
            "GUI",
            "Mission",
            "Units",
            "Audio",
            "System",
            "Uncategorized",
        };
        SortOptions = new ObservableCollection<string> { "A — Z", "Original" };
        MaskOptions = new ObservableCollection<string> { "*", "GUI:*", "MISSION:*", "UNIT:*", "AUDIO:*", "SYSTEM:*" };

        Entries = new ObservableCollection<StringEntryViewModel>
        {
            new("GUI:MAIN_MENU", "Main Menu", "GUI"),
            new("GUI:NEW_GAME", "New Game", "GUI"),
            new("GUI:LOAD_GAME", "Load Game", "GUI"),
            new("GUI:OPTIONS", "Options", "GUI"),
            new("GUI:QUIT_CONFIRM", "Are you sure you want to return to the desktop?", "GUI"),
            new("MISSION:OBJECTIVE_01", "Establish a base and restore power to the radar.", "Mission"),
            new("MISSION:OBJECTIVE_02", "Locate the abandoned outpost.", "Mission"),
            new("MISSION:COMPLETE", "Mission accomplished", "Mission", "eva_mission_accomplished.wav"),
            new("UNIT:GRIZZLY_NAME", "Grizzly Battle Tank", "Units"),
            new("UNIT:GRIZZLY_DESC", "Versatile main battle tank armed with a 105 mm cannon.", "Units"),
            new("AUDIO:LOW_POWER", "Low power", "Audio", "eva_low_power.wav"),
            new("SYSTEM:PLAYER_JOINED", "{0} has joined the game.", "System"),
        };

        Categories = new ObservableCollection<CategoryItemViewModel>
        {
            new(AllStringsCategory, "*", "≡", "Complete table"),
            new("Interface", "GUI", "▣", "Menus and controls"),
            new("Missions", "Mission", "◆", "Objectives and briefings"),
            new("Units", "Units", "⬡", "Names and descriptions"),
            new("Audio", "Audio", "♪", "Voice and subtitles"),
            new("System", "System", "⚙", "Runtime messages"),
            new("Uncategorized", "Uncategorized", "·", "Without a prefix"),
        };

        AddEntryCommand = new RelayCommand(_ => AddEntry());
        DuplicateEntryCommand = new RelayCommand(_ => DuplicateSelectedEntry(), _ => SelectedEntry is not null);
        DeleteEntryCommand = new RelayCommand(_ => DeleteSelectedEntry(), _ => SelectedEntry is not null);
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !string.IsNullOrEmpty(SearchText));
        ToggleExtraValueCommand = new RelayCommand(_ => ToggleExtraValue(), _ => SelectedEntry is not null);
        ShowCategoriesCommand = new RelayCommand(_ => IsCategoryMode = true);
        ShowAllStringsCommand = new RelayCommand(_ =>
        {
            IsCategoryMode = false;
            SelectedCategory = Categories[0];
        });

        foreach (var entry in Entries)
            entry.PropertyChanged += EntryOnPropertyChanged;

        RefreshCategoryCounts();
        SelectedCategory = Categories[0];
    }

    public ObservableCollection<StringEntryViewModel> Entries { get; }

    public ObservableCollection<StringEntryViewModel> FilteredEntries { get; } = new();

    public ObservableCollection<CategoryItemViewModel> Categories { get; }

    public ObservableCollection<string> CategoryNames { get; }

    public ObservableCollection<string> SortOptions { get; }

    public ObservableCollection<string> MaskOptions { get; }

    public RelayCommand AddEntryCommand { get; }

    public RelayCommand DuplicateEntryCommand { get; }

    public RelayCommand DeleteEntryCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public RelayCommand ToggleExtraValueCommand { get; }

    public RelayCommand ShowCategoriesCommand { get; }

    public RelayCommand ShowAllStringsCommand { get; }

    public CategoryItemViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (ReferenceEquals(_selectedCategory, value))
                return;

            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            ApplyFilters();
        }
    }

    public StringEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (ReferenceEquals(_selectedEntry, value))
                return;

            this.RaiseAndSetIfChanged(ref _selectedEntry, value);
            this.RaisePropertyChanged(nameof(HasSelection));
            this.RaisePropertyChanged(nameof(SelectionPositionText));
            DuplicateEntryCommand.RaiseCanExecuteChanged();
            DeleteEntryCommand.RaiseCanExecuteChanged();
            ToggleExtraValueCommand.RaiseCanExecuteChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            value ??= string.Empty;
            if (_searchText == value)
                return;

            this.RaiseAndSetIfChanged(ref _searchText, value);
            ClearSearchCommand.RaiseCanExecuteChanged();
            ApplyFilters();
        }
    }

    public string ResultCountText
    {
        get => _resultCountText;
        private set => this.RaiseAndSetIfChanged(ref _resultCountText, value);
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set
        {
            value ??= SortOptions[0];
            if (_selectedSort == value)
                return;

            this.RaiseAndSetIfChanged(ref _selectedSort, value);
            ApplyFilters();
        }
    }

    public string SelectedMask
    {
        get => _selectedMask;
        set
        {
            value ??= MaskOptions[0];
            if (_selectedMask == value)
                return;

            this.RaiseAndSetIfChanged(ref _selectedMask, value);
            ApplyFilters();
        }
    }

    public bool IsCategoryMode
    {
        get => _isCategoryMode;
        private set
        {
            if (_isCategoryMode == value)
            {
                // Re-assert the one-way toggle bindings if the active mode is clicked again.
                this.RaisePropertyChanged(nameof(IsCategoryMode));
                this.RaisePropertyChanged(nameof(IsAllStringsMode));
                return;
            }

            this.RaiseAndSetIfChanged(ref _isCategoryMode, value);
            this.RaisePropertyChanged(nameof(IsAllStringsMode));
        }
    }

    public bool IsAllStringsMode => !IsCategoryMode;

    public bool DocumentIsModified
    {
        get => _documentIsModified;
        private set
        {
            if (_documentIsModified == value)
                return;

            this.RaiseAndSetIfChanged(ref _documentIsModified, value);
            this.RaisePropertyChanged(nameof(DocumentTitle));
            this.RaisePropertyChanged(nameof(WindowTitle));
        }
    }

    public bool HasSelection => SelectedEntry is not null;

    public string DocumentTitle => DocumentIsModified ? "ra2md.csf  •" : "ra2md.csf";

    public string WindowTitle => $"Westwood String Table Editor — {DocumentTitle}";

    public string SelectionPositionText
    {
        get
        {
            if (SelectedEntry is null)
                return "No selection";

            var index = FilteredEntries.IndexOf(SelectedEntry);
            return index < 0 ? "No selection" : $"{index + 1} / {FilteredEntries.Count}";
        }
    }

    private void ApplyFilters()
    {
        var previousSelection = SelectedEntry;
        var category = SelectedCategory?.FilterKey ?? "*";
        var query = SearchText.Trim();
        var normalizedQuery = query == "*" ? string.Empty : query.Trim('*');
        var maskPrefix = SelectedMask == "*" ? string.Empty : SelectedMask.TrimEnd('*');

        IEnumerable<StringEntryViewModel> filtered = Entries.Where(entry =>
            (category == "*" || entry.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(maskPrefix) ||
             entry.Name.StartsWith(maskPrefix, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(normalizedQuery) ||
             entry.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
             entry.Value.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
             entry.ExtraValue.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)));

        if (SelectedSort == "A — Z")
            filtered = filtered.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase);

        FilteredEntries.Clear();
        foreach (var entry in filtered)
            FilteredEntries.Add(entry);

        ResultCountText = $"{FilteredEntries.Count} of {Entries.Count}";
        SelectedEntry = previousSelection is not null && FilteredEntries.Contains(previousSelection)
            ? previousSelection
            : FilteredEntries.FirstOrDefault();
        this.RaisePropertyChanged(nameof(SelectionPositionText));
    }

    private void AddEntry()
    {
        var suffix = 1;
        string name;
        do
        {
            name = $"NEW:STRING_{suffix:000}";
            suffix++;
        }
        while (Entries.Any(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        var category = SelectedCategory?.FilterKey;
        if (string.IsNullOrEmpty(category) || category == "*")
            category = "Uncategorized";

        var newEntry = new StringEntryViewModel(name, "New string value", category);
        newEntry.MarkModified();
        newEntry.PropertyChanged += EntryOnPropertyChanged;
        Entries.Insert(0, newEntry);

        DocumentIsModified = true;
        RefreshCategoryCounts();
        SearchText = string.Empty;
        SelectedMask = "*";
        SelectedCategory = Categories[0];
        ApplyFilters();
        SelectedEntry = newEntry;
    }

    private void DuplicateSelectedEntry()
    {
        if (SelectedEntry is null)
            return;

        var baseName = $"{SelectedEntry.Name}_COPY";
        var name = baseName;
        var suffix = 2;
        while (Entries.Any(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            name = $"{baseName}_{suffix++}";

        var duplicate = new StringEntryViewModel(
            name,
            SelectedEntry.Value,
            SelectedEntry.Category,
            SelectedEntry.ExtraValue);
        duplicate.MarkModified();
        duplicate.PropertyChanged += EntryOnPropertyChanged;

        Entries.Insert(0, duplicate);
        DocumentIsModified = true;
        RefreshCategoryCounts();
        SearchText = string.Empty;
        SelectedMask = "*";
        SelectedCategory = Categories[0];
        ApplyFilters();
        SelectedEntry = duplicate;
    }

    private void DeleteSelectedEntry()
    {
        if (SelectedEntry is null)
            return;

        var entry = SelectedEntry;
        entry.PropertyChanged -= EntryOnPropertyChanged;
        Entries.Remove(entry);
        DocumentIsModified = true;
        RefreshCategoryCounts();
        ApplyFilters();
    }

    private void ToggleExtraValue()
    {
        if (SelectedEntry is null)
            return;

        SelectedEntry.ExtraValue = string.IsNullOrWhiteSpace(SelectedEntry.ExtraValue)
            ? "audio_reference.wav"
            : string.Empty;
    }

    private void EntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not StringEntryViewModel entry)
            return;

        if (entry.IsModified)
            DocumentIsModified = true;

        if (e.PropertyName == nameof(StringEntryViewModel.Category))
            RefreshCategoryCounts();

        if (e.PropertyName is nameof(StringEntryViewModel.Name)
            or nameof(StringEntryViewModel.Value)
            or nameof(StringEntryViewModel.ExtraValue)
            or nameof(StringEntryViewModel.Category))
        {
            var categoryFilterIsActive = SelectedCategory?.FilterKey is { } key && key != "*";
            var searchIsActive = !string.IsNullOrWhiteSpace(SearchText);
            var nameAffectsView = e.PropertyName == nameof(StringEntryViewModel.Name) &&
                                  (SelectedMask != "*" || SelectedSort == "A — Z");
            var categoryAffectsView = e.PropertyName == nameof(StringEntryViewModel.Category) &&
                                      categoryFilterIsActive;

            if (searchIsActive || nameAffectsView || categoryAffectsView)
                ApplyFilters();
        }
    }

    private void RefreshCategoryCounts()
    {
        foreach (var category in Categories)
        {
            category.Count = category.FilterKey == "*"
                ? Entries.Count
                : Entries.Count(entry => entry.Category.Equals(category.FilterKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}

public sealed class CategoryItemViewModel : ViewModelBase
{
    private int _count;

    public CategoryItemViewModel(string name, string filterKey, string glyph, string description)
    {
        Name = name;
        FilterKey = filterKey;
        Glyph = glyph;
        Description = description;
    }

    public string Name { get; }

    public string FilterKey { get; }

    public string Glyph { get; }

    public string Description { get; }

    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }
}

public sealed class StringEntryViewModel : ViewModelBase
{
    private string _name;
    private string _value;
    private string _category;
    private string _extraValue;
    private bool _isModified;

    public StringEntryViewModel(string name, string value, string category, string extraValue = "")
    {
        _name = name;
        _value = value;
        _category = category;
        _extraValue = extraValue;
    }

    public string Name
    {
        get => _name;
        set => SetEditableValue(ref _name, value ?? string.Empty, nameof(Name));
    }

    public string Value
    {
        get => _value;
        set
        {
            if (SetEditableValue(ref _value, value ?? string.Empty, nameof(Value)))
                this.RaisePropertyChanged(nameof(CharacterCountText));
        }
    }

    public string Category
    {
        get => _category;
        set => SetEditableValue(ref _category, value ?? string.Empty, nameof(Category));
    }

    public string ExtraValue
    {
        get => _extraValue;
        set
        {
            if (SetEditableValue(ref _extraValue, value ?? string.Empty, nameof(ExtraValue)))
            {
                this.RaisePropertyChanged(nameof(ExtraValueDisplay));
                this.RaisePropertyChanged(nameof(HasExtraValue));
            }
        }
    }

    public bool IsModified
    {
        get => _isModified;
        private set => this.RaiseAndSetIfChanged(ref _isModified, value);
    }

    public bool HasExtraValue => !string.IsNullOrWhiteSpace(ExtraValue);

    public string ExtraValueDisplay => HasExtraValue ? "Extra" : "—";

    public string CharacterCountText => $"{Value.Length} characters";

    public void MarkModified() => IsModified = true;

    private bool SetEditableValue(ref string field, string value, string propertyName)
    {
        if (field == value)
            return false;

        this.RaiseAndSetIfChanged(ref field, value, propertyName);
        IsModified = true;
        return true;
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
