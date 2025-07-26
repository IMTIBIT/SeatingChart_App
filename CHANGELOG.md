# SeatingChart App Changelog

## v0.6-improvements-cycle-1 (2025-07-26)

This release focuses on UI/UX refinements, data persistence improvements, and foundational quality‑of‑life updates following the initial functionality milestone. Key changes include:

### New Features

- **UserRoleManager**: Added a singleton manager to track the current user role (Attendant or Admin) with event notifications when the role changes.
- **SeatingUIManager**: Introduced a comprehensive UI manager that handles seat assignment workflows. The new panel supports optional Guest ID and Notes fields, validates required inputs (first name, last name, room number, party size), enforces seat capacity, and disables assignments on out‑of‑service seats. Guest assignments, clear operations and out‑of‑service toggles now trigger immediate layout persistence.
- **AdminToolsManager**: Added an admin tools panel with buttons to reset the layout to its default state and quickly switch between admin and attendant roles for testing purposes. The reset operation deletes saved layouts and resets seat positions and states.
- **Stay Logged In**: Extended `LoginUIManager` with a `Toggle stayLoggedInToggle` to allow attendants to remain in admin mode across sessions. Wrong password attempts now show subtle error feedback with red text.
- **Grid Snapping & Drag Feedback**: Enhanced `SeatController` with optional grid snapping and visual feedback while dragging. Seats snap to grid cells on drag end, scale and tint lighten during dragging, and simple collision avoidance prevents overlapping seats by nudging positions when conflicts are detected.
- **Persistence Enhancements**: The layout now saves whenever a seat is assigned, cleared, marked out‑of‑service, or repositioned. A new `ResetLayout` method in `LayoutManager` deletes existing save files and resets all seats to their default positions and states.

### Improvements

- UI input validation prevents assigning seats unless required fields are populated and party size is within capacity.
- The assignment panel dynamically updates its controls based on the selected seat’s state (occupied, available, out‑of‑service) and the current user role.
- The clear button is hidden when editing an empty seat, reducing visual clutter.
- Out‑of‑service button text updates to indicate whether a seat will be placed out of service or restored.
- `SeatController` now notifies the `LayoutManager` whenever its state changes, ensuring changes are saved immediately.

### Fixes

- Timer resets correctly whenever a new guest is assigned to a seat.
- Added simple collision detection after dragging seats to prevent overlapping positions.
- Prevented duplication of static managers by enforcing singletons in `UserRoleManager` and `LayoutManager`.

### Known Limitations

- The UI still requires manual scene setup to assign references for all new fields and buttons.
- Real‑time network synchronization and data export stubs are not yet implemented. These will be addressed in future cycles.
