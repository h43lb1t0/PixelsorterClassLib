# Contributing to PixelsorterClassLib

First off, thank you for considering contributing to PixelsorterClassLib! It's people like you that make this tool better for everyone.

## How Can I Contribute?

### Reporting Bugs
- Ensure the bug was not already reported by searching on GitHub under [Issues](https://github.com/h43lb1t0/PixelsorterClassLib/issues).
- If you're unable to find an open issue addressing the problem, open a new one. Be sure to include a title, a clear description, and as much relevant information as possible (e.g., code samples or an image showing the expected behavior that is not occurring).

### Suggesting Enhancements
- Open a new issue with a clear title and description.
- Explain why this enhancement would be useful to most users and how it aligns with the project's goals.

### Pull Requests
1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature` or `bugfix/FixSomething`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Development Setup
1. Clone the repo: `git clone https://github.com/h43lb1t0/PixelsorterClassLib.git`
2. Ensure you have the appropriate .NET SDK installed.
3. Open the solution in your preferred IDE (Visual Studio, VS Code, Rider, etc.).
4. Build the `PixelsorterClassLib` and `ConsoleApp` to ensure everything compiles correctly.

## Coding Style
- Follow standard C# coding conventions.
- Keep the code clean, and use XML comments for public members (methods, classes) as done currently in the codebase.
- Ensure any added features do not break existing sorting methods or masking logic.
- Write unit tests for any new functionality you add, and ensure all existing tests pass before submitting a Pull Request.

## Dependencies
- If adding new dependencies is absolutely necessary, please prefer those with **MIT** or **Apache 2.0** licenses.
- If a dependency uses a different license, ensure it is compatible with this project and explicitly mention it in your Pull Request.
