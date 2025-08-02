# MyGtdApp

A modern, full-featured GTD (Getting Things Done) board application built with Blazor.  
Organize your tasks, projects, and contexts with an intuitive drag-and-drop interface, hierarchical project trees, and robust data management features.

---

## Features

- **GTD Workflow:** Supports Inbox, Next Actions, Projects, Someday, and Completed columns
- **Project Tree:** Unlimited nested sub-tasks and project hierarchies
- **Drag & Drop:** Move tasks and projects via mouse or touch (mobile-friendly)
- **Quick Add & Edit:** Fast keyboard-driven input and double-click to edit details
- **Context Tags:** Filter and view tasks by contexts (e.g., `@Home`, `@Work`)
- **Data Export/Import:** Backup or restore your tasks as JSON files
- **Responsive UI:** Optimized for desktop, tablet, and mobile devices
- **Persistent Storage:** Data saved in local SQLite database (easily extendable)

---

## Demo

> **Try locally:**  
> See [Quick Start](#quick-start) below.  
>  
> **Sample Data:**  
> On first run, sample tasks from `wwwroot/sample-data/tasks.json` are loaded automatically.

---

## Quick Start

### 1. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) required

### 2. Run Locally

```bash
git clone https://github.com/your-id/MyGtdApp.git
cd MyGtdApp
dotnet run
```

- Open your browser at [http://localhost:61183](http://localhost:61183)

### 3. Database

- Uses SQLite (`App_Data/mygtd.db`) by default
- Sample data is inserted only on the very first run

---

## Usage

### Add Tasks

- Click **+ Add Task** in any column or press Enter
- To add sub-tasks, click the **+** button on a project node

### Move Tasks

- **Mouse:** Drag and drop task cards
- **Touch:** Long-press, then drag (mobile/tablet)
- Supports drag-and-drop within and across project trees

### Complete / Restore Tasks

- Toggle completion via checkbox
- In the **Completed** column, use the "Clear all completed tasks" button to bulk-delete

### Data Export/Import

- Use the **Data Manager** button in the header
- Export: Download all tasks as a JSON file
- Import: Restore tasks from a JSON backup (overwrites current data)

---

## Project Structure

- `Components/` – Blazor UI components (pages, layouts, shared widgets)
- `Models/` – Domain models (TaskItem, enums, JSON helpers)
- `Services/` – Business logic, data services, and database context
- `Repositories/` – Data access layer (SQLite, Entity Framework)
- `wwwroot/` – Static files (CSS, JS, sample data)

---

## Customization

- **Database:**  
  Easily switch to PostgreSQL or other providers by adjusting `Program.cs` and updating dependencies.
- **Styling:**  
  All styles are modular and can be customized in the `Components/*/*.css` and `wwwroot/app.css`.

---

## License

MIT License.  
Feel free to use, modify, and contribute!

---

## Credits

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Icons: [Bootstrap Icons](https://icons.getbootstrap.com/)
- Inspired by GTD methodology (David Allen)

---

**Questions or feedback?**  
Open an issue or pull request on GitHub!

---

You can further enhance this README with screenshots or GIFs for a better first impression.  
Let me know if you'd like a badge section, contribution guidelines, or anything else!
