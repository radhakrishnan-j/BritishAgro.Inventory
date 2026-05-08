# UI Redesign - Quick Reference Guide

## Files Modified
1. `Components/Pages/Categories.razor` - Complete redesign
2. `Components/Pages/Products.razor` - Complete redesign
3. `Components/Pages/ProductUsagePage.razor` - Complete redesign
4. `Components/Pages/ProductStocks.razor` - Complete redesign

## Files Created
1. `Components/Shared/PaginationControls.razor` - New pagination component
2. `Components/Shared/GenericModal.razor` - New modal component

## Design Patterns

### Adding Search to a Page
1. Add search state variables:
   ```csharp
   private string searchQuery = string.Empty;
   ```

2. Add search UI with input:
   ```html
   <input type="text" class="form-control border-start-0" 
          placeholder="Search..." 
          @bind="searchQuery" @bind:event="oninput" 
          @onkeyup="OnSearchChanged" />
   ```

3. Add search methods:
   ```csharp
   private void OnSearchChanged()
   {
       currentPage = 1;
       ApplySearch();
   }

   private void ApplySearch()
   {
       if (string.IsNullOrWhiteSpace(searchQuery))
       {
           filteredItems = allItems;
       }
       else
       {
           var query = searchQuery.ToLower();
           filteredItems = allItems
               .Where(i => /* search logic */)
               .ToList();
       }
       UpdatePaginatedItems();
   }
   ```

### Adding Pagination to a Page
1. Add state variables:
   ```csharp
   private int currentPage = 1;
   private int pageSize = 10;
   ```

2. Update data after filtering:
   ```csharp
   private void UpdatePaginatedItems()
   {
       var startIndex = (currentPage - 1) * pageSize;
       paginatedItems = filteredItems.Skip(startIndex).Take(pageSize).ToList();
   }
   ```

3. Add page change handler:
   ```csharp
   private async Task OnPageChanged(int page)
   {
       currentPage = page;
       UpdatePaginatedItems();
   }
   ```

4. Add component to page:
   ```html
   <PaginationControls 
       Items="paginatedItems" 
       CurrentPage="currentPage" 
       PageSize="pageSize" 
       TotalItems="filteredItems.Count" 
       OnPageChanged="OnPageChanged" />
   ```

### Using GenericModal
```html
<GenericModal TModel="MyInputModel" 
    IsOpen="isModalOpen" 
    Title="Create Item" 
    Model="model" 
    SubmitButtonText="Create"
    FormName="my-form" 
    OnClose="CloseModal" 
    OnSubmit="SaveAsync">

    <div class="mb-3">
        <label class="form-label">Name</label>
        <InputText class="form-control" @bind-Value="model.Name" />
        <ValidationMessage For="() => model.Name" class="text-danger" />
    </div>
</GenericModal>
```

## Common Bootstrap Classes Used
- `content-card` - Main card container
- `table-hover` - Hover effect on table rows
- `table-light` - Light header styling
- `text-end` - Right-aligned text
- `badge` - Status badges
- `form-control` - Form inputs
- `btn btn-primary` - Primary button
- `btn-outline-primary` - Secondary button
- `spinner-border` - Loading spinner
- `visually-hidden` - Screen reader only
- `gap-4` - Spacing utility
- `mb-3`, `mb-4` - Margin bottom
- `text-secondary` - Secondary text color
- `input-group` - Grouped inputs (search)

## Icons Used (from Bootstrap Icons)
- `bi bi-search` - Search icon
- `bi bi-pencil` - Edit icon
- `bi bi-trash` - Delete icon
- `bi bi-plus-circle` - Add icon
- `bi bi-check-circle` - Completed/checked icon
- `bi bi-arrow-return-left` - Return/back icon

## Testing Checklist
- [ ] Search filters work correctly
- [ ] Pagination shows correct items
- [ ] Modal opens and closes properly
- [ ] Form validation works in modals
- [ ] Edit operation loads existing data
- [ ] Delete operations work
- [ ] Create operations clear form
- [ ] Loading spinner displays
- [ ] Empty state shows when appropriate
- [ ] Page performs well with 100+ items

## Customization

### Change Items Per Page
Update `pageSize` variable (currently 10):
```csharp
private int pageSize = 20; // Change to show 20 items per page
```

### Change Page Range Display
In `PaginationControls.razor`, modify:
```csharp
@for (int i = Math.Max(1, CurrentPage - 2); i <= Math.Min(TotalPages, CurrentPage + 2); i++)
// Change the -2 and +2 to show more/fewer page numbers
```

### Add Additional Search Fields
In `ApplySearch()` method, add more conditions:
```csharp
.Where(i => i.Name.ToLower().Contains(query) || 
           i.Description.ToLower().Contains(query) ||
           i.Category.Name.ToLower().Contains(query) ||
           i.NewField.ToLower().Contains(query))
```

## Performance Notes
- Pagination reduces DOM size significantly
- Search uses LINQ for efficient filtering
- No unnecessary re-renders
- Modal state is properly managed
- Consider lazy loading for very large datasets
- Consider server-side filtering for 1000+ items

## Accessibility
- All buttons have proper aria labels where needed
- Form labels properly associated with inputs
- Modal has proper accessibility attributes
- Search input has semantic meaning
- Icons have text alternatives

## Browser DevTools Tips
- Check Network tab for performance
- Verify DOM size in Elements tab
- Check Console for any JS errors
- Test responsive layout at different breakpoints
