# IPv6Tool Design System

## Brand Colors

| Token | Value | Usage |
|-------|-------|-------|
| `--bg-header` | `#1a1a2e` | Top status bar background |
| `--bg-header-ok` | `#1a3a28` | Status bar when IPv6 enabled |
| `--bg-header-err` | `#3a1a1a` | Status bar when IPv6 disabled |
| `--bg-body` | `#f5f5fa` | Main form background |
| `--bg-card` | `#ffffff` | Panel / group box background |
| `--bg-footer` | `#f0f0f5` | Footer panel background |
| `--btn-enable` | `#28a05c` | Enable button |
| `--btn-enable-hover` | `#34c476` | Enable button hover |
| `--btn-disable` | `#c0392b` | Disable button |
| `--btn-disable-hover` | `#e0483a` | Disable button hover |
| `--btn-github` | `#24292e` | GitHub button |
| `--text-primary` | `#2c3e50` | Primary text |
| `--text-secondary` | `#7f8c8d` | Secondary text |
| `--text-muted` | `#bdc3c7` | Muted text |
| `--log-bg` | `#1e1e1e` | Console log background |
| `--log-fg` | `#00dc00` | Console log text |

## Spacing (8px grid)

| Token | Pixels | Usage |
|-------|--------|-------|
| `--space-xs` | 4px | Inner padding |
| `--space-sm` | 8px | Tight gaps |
| `--space-md` | 16px | Section padding |
| `--space-lg` | 24px | Group spacing |
| `--space-xl` | 32px | Big gaps |

## Typography

| Role | Font | Size | Weight |
|------|------|------|--------|
| Header status | Microsoft YaHei UI | 18pt | Bold |
| Section title | Microsoft YaHei UI | 10pt | Bold |
| Body text | Microsoft YaHei UI | 9pt | Regular |
| Button label | Microsoft YaHei UI | 12pt | Bold |
| Log output | Consolas | 9pt | Regular |
| Tool button | Microsoft YaHei UI | 9pt | Regular |

## Component Patterns

### Buttons
- **Action buttons** (Enable/Disable): 200×42px, rounded flat, bold 12pt, white text, no border
- **Tool buttons**: 120×28px, standard Windows style
- **Utility buttons**: 100×28px, flat style
- **Disabled state**: opacity 50%, cursor default

### Cards / GroupBoxes
- White background, no border or subtle border
- Section title in bold 10pt
- Inner padding 16px

### Status Indicator
- Full-width dark panel at top
- Center-aligned bold text
- Color shifts: green (ok), red (error), white (neutral)
- Smooth color transitions via Timer
