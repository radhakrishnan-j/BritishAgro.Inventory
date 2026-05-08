# UI Layout Guide - Visual Reference

## Categories Page Layout

```
┌─────────────────────────────────────────────────────┐
│  Categories                                         │
│  Create and maintain inventory categories.          │
│                                  [New category ▼]   │
├─────────────────────────────────────────────────────┤
│                                                     │
│  [🔍 Search by category name...]      Total: 45   │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Name        │ Description    │ Status  │ Qty │ Act │
│  ─────────────────────────────────────────────────  │
│ ✓ Grains    │ Cereal & grain │ Active  │ 12 │ ✎ 🗑│
│ ✓ Vegetables│ Fresh produce  │ Active  │  8 │ ✎ 🗑│
│ ✓ Fertilizer│ Farm supplies  │ Inactive│  5 │ ✎ 🗑│
│                                                     │
│ Showing 1 to 10 of 45 items                        │
│ [First] [Previous] [1] [2] [3] [Next] [Last]      │
│                                                     │
└─────────────────────────────────────────────────────┘

When "New category" clicked:
┌───────────────────────────────────────┐
│  Create Category                   [×] │
├───────────────────────────────────────┤
│                                       │
│  Name *                               │
│  [________________]                   │
│                                       │
│  Description                          │
│  [___________________________]         │
│  [___________________________]         │
│                                       │
│  ☑ Active                             │
│                                       │
│                  [Cancel] [Create]    │
│                                       │
└───────────────────────────────────────┘
```

## Products Page Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  Products                                                       │
│  Maintain products, categories, units, and reorder levels.  [+] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [🔍 Search by name/category...]              Total: 156 items │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name      │ Category    │ Unit   │ Reorder │ On hand │ Status │
│  ──────────────────────────────────────────────────────────────│
│ ✓ Wheat   │ [Grains]   │ Bags   │    50   │  450.25 │ Active │
│ ✓ Maize   │ [Grains]   │ Bags   │    30   │  120.00 │ Active │
│ ✓ Lettuce │ [Veg]      │ Boxes  │    20   │   45.50 │ Active │
│                                                                 │
│ Showing 1 to 10 of 156 items                                   │
│ [First] [Previous] [1] [2] ... [Next] [Last]                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Modal Stack (Products):
┌───────────────────────────────────────┐
│  Edit Product                      [×] │
├───────────────────────────────────────┤
│ ✓ Name [Wheat]                        │
│ ✓ Description [Spring wheat...]       │
│ ✓ Category [Grains ▼] + Add category │
│ ✓ Unit [Bags ▼]                       │
│ ✓ Reorder level [50]                  │
│ ✓ ☑ Active                            │
│            [Cancel] [Update]          │
└───────────────────────────────────────┘

Modal Stack (Category from Product):
┌───────────────────────────────────────┐
│  Create Category                   [×] │
├───────────────────────────────────────┤
│ ✓ Name [_________]                    │
│ ✓ Description [_________________]     │
│            [Cancel] [Create]          │
└───────────────────────────────────────┘
```

## Product Usage Page Layout

```
┌──────────────────────────────────┬─────────────────────────────────┐
│     Record Usage (Left Sidebar)  │    Usage History (Main)          │
├──────────────────────────────────┼─────────────────────────────────┤
│                                  │                                 │
│  Product [Select ▼]              │  [🔍 Search product...]   45    │
│                                  │                                 │
│  Date & Time                     │  Product │ Date  │ Qty │ By    │
│  [2024-01-15 T 14:30]            │  ────────────────────────────   │
│                                  │  Wheat   │ 2024  │ 100 │ John  │
│  Issued qty [    ]               │  Maize   │ 2024  │ 50  │ Sarah │
│                                  │  Wheat   │ 2024  │ 75  │ Mike  │
│  Received by [_________]         │                                 │
│                                  │  Showing 1 to 10 of 45 records  │
│  Remarks                         │  [First] [Prev] [1][2] [Next]   │
│  [_________________]             │                                 │
│  [_________________]             │                                 │
│                                  │                                 │
│  [Record usage]                  │                                 │
│                                  │                                 │
└──────────────────────────────────┴─────────────────────────────────┘

Return Modal (when clicking action button):
┌───────────────────────────────────┐
│  Create Product Return         [×] │
├───────────────────────────────────┤
│ Product: Wheat                    │
│ Usage Date: 2024-01-15            │
│                                   │
│ Date & Time                       │
│ [2024-01-15 T 14:30]              │
│                                   │
│ Returned Qty [    ]               │
│ (Max available: 100)              │
│                                   │
│ Returned by [________]            │
│                                   │
│ Remarks [________________]        │
│         [_________________]       │
│                                   │
│        [Cancel] [Save Return]     │
│                                   │
└───────────────────────────────────┘
```

## Product Stocks Page Layout

```
┌──────────────────────────────────┬─────────────────────────────────┐
│      Add Stock (Left Sidebar)    │    Stock Lots (Main)            │
├──────────────────────────────────┼─────────────────────────────────┤
│                                  │                                 │
│  Product [Select ▼]              │  [🔍 Search product...]   128   │
│                                  │                                 │
│  Quantity [    ]                 │  Product  │ Category │ Qty │ Date
│                                  │  ────────────────────────────   │
│  Arrival Date & Time             │  Wheat    │ [Grains] │ 450 │ 2024
│  [2024-01-15 T 14:30]            │  Maize    │ [Grains] │ 200 │ 2024
│                                  │  Lettuce  │ [Veg]    │ 120 │ 2024
│  [Add stock]                     │  Wheat    │ [Grains] │ 300 │ 2024
│                                  │                                 │
│                                  │  Showing 1 to 10 of 128 lots    │
│                                  │  [First] [Prev] [1] [2]...      │
│                                  │                                 │
└──────────────────────────────────┴─────────────────────────────────┘
```

## Responsive Layouts

### Mobile (xs/sm - below 768px)
```
┌─────────────────────────┐
│ Categories              │
│ Create and maintain...  │
│        [New category]   │
├─────────────────────────┤
│                         │
│ [🔍 Search...]  45      │
│                         │
│ Name  │ Status │ Action │
│ ──────────────────────  │
│ Wheat │ Active │ ✎ 🗑  │
│ Maize │ Active │ ✎ 🗑  │
│                         │
│ [1] [2] [3] [Next]      │
│                         │
└─────────────────────────┘

Products on Mobile - Stack with scroll:
- Search bar
- Item count
- Table (scrolls right)
- Pagination
```

### Tablet (md - 768px to 991px)
```
┌──────────────────────────────────────────┐
│ Categories                          [+]  │
├──────────────────────────────────────────┤
│  [🔍 Search...]    Count: 45             │
├──────────────────────────────────────────┤
│ Name     │ Description      │ Status  │  │
│ Wheat    │ Spring variety   │ Active  │  │
│ Maize    │ Drought resistant│ Active  │  │
│                                          │
│ Showing 1 to 10 of 45                   │
│ [<] [1] [2] [3] [>]                    │
└──────────────────────────────────────────┘
```

### Desktop (lg/xl - 992px+)
```
Full layouts as shown above with optimal spacing
```

## Color Scheme & Components

### Status Badges
```
Active:   [✓ Active]     (green background, white text)
Inactive: [✗ Inactive]   (gray background, white text)
```

### Category Badges
```
[Grains]      (light gray background, dark text)
[Vegetables]  (light gray background, dark text)
[Supplies]    (light gray background, dark text)
```

### Action Buttons
```
Edit:    ✎ (blue outline)
Delete:  🗑 (red outline)
Return:  ← (blue outline)
Create:  + (inside button text)
Search:  🔍 (in input group)
```

### Form Elements
```
Text input:      [_________________]
Text area:       [___________________]
                 [___________________]
Dropdown:        [Option ▼]
Checkbox:        ☑ Label
Number input:    [    ]
Date/Time:       [2024-01-15 T 14:30]
```

### Loading State
```
        ⟳ Spinner
    Loading items...
```

### Empty State
```
    📭 No items found
    "Create your first category..."
```

### Pagination Controls
```
Showing 1 to 10 of 45 items
[First] [Previous] [1] [2] [3] [Next] [Last]
 (disabled)        (disabled)
```

## Spacing Reference

```
Section to Section:     mb-4 (1.5rem / 24px)
Form Group:             mb-3 (1rem / 16px)
Row Gap:                g-4 (1.5rem / 24px)
Button Gap:             gap-2 (0.5rem / 8px)
Inline Elements:        me-2 (0.5rem margin-right)
Icon to Text:           me-2 (0.5rem / 8px)
```

## Typography

```
Page Title (h1):        Bold, 2.5rem
Section Header (h5):    Bold, 1.25rem
Table Header:           Bold, 0.875rem (uppercase-like)
Table Cell:             Regular, 0.95rem
Label:                  Regular, 0.875rem, darker gray
Help Text:              Regular, 0.75rem, lighter gray
Badge:                  Bold, 0.75rem
```

## Component Hierarchy

```
Page Level
├── SectionHeader
│   ├── Title (h1)
│   ├── Subtitle (p)
│   └── Actions (button group)
├── ContentCard
│   ├── Search Bar
│   ├── Count Display
│   ├── Table
│   │   ├── THead
│   │   └── TBody (rows)
│   └── PaginationControls
│       ├── Item Count
│       └── Page Navigation
├── Sidebar Form (optional)
│   ├── EditForm
│   ├── Fields
│   └── Submit Button
└── Modal (overlay)
    ├── GenericModal
    ├── EditForm
    ├── Fields
    └── Actions
```

## Dark Mode Considerations (Future)

Current design uses light theme. For dark mode:
- Use CSS custom properties for colors
- Update badge colors for contrast
- Adjust table row hover color
- Update modal background
- Ensure WCAG AAA compliance

## Accessibility Features

```
Icons:           title="Edit" for tooltip
Buttons:         aria-label for screenreaders
Forms:           <label for="..."> for inputs
Modals:          role="dialog", tabindex management
Tables:          <thead>/<tbody> semantic markup
Search:          placeholder text + semantic input
Status:          Visual + text labels
Links:           Proper href and title attributes
```

---

This visual guide helps developers understand the new UI structure and can be referenced during development, testing, and future enhancements.
