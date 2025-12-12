⭐ PsyConnect
A Psychotherapist Session Management System with AI Support & Seamless Online Care

PsyConnect is a modern web application built to help psychotherapists manage their practice more efficiently.
It streamlines session scheduling, online consultations, payments, and client communication — all enhanced with AI-powered tools.

Whether you're conducting in-person therapy or virtual sessions, PsyConnect helps automate administrative tasks so therapists can focus on people, not paperwork.


🚀 Core Functionality

Session Scheduling System — Manage appointments with ease.

SMTP Email Notifications — Automated email confirmations & reminders.

Zoom Integration — Auto-generate secure meeting links for online sessions.

Stripe Payments — Fast, secure, and integrated payment processing.

AI Assistant Chatbot (OpenAI API) — Provides helpful guidance and support for clients.

AI-Generated Admin Summaries (OpenAI API) — Automatic post-session summaries for therapists.

Built with ASP.NET Core MVC using clean architecture principles.


📦 Installation & Usage (For End Users)

These steps are for users who want to run the web application without modifying the source code.

1. Clone the repository
git clone https://github.com/aliialzein/PsyConnect.git
cd PsyConnect

2. Configure environment variables

Add credentials for:

SMTP

Zoom API

Stripe

OpenAI API keys

(Place them inside your appsettings.json file.)

3. Run the application
dotnet run


🛠️ Setup for Contributors (Development Environment)

If you want to contribute or run the project locally for development:

1. Clone the repository
git clone https://github.com/aliialzein/PsyConnect.git
cd PsyConnect

2. Install the correct .NET SDK 9.0.0

3. Apply database migrations
dotnet ef database update

4. Add development environment keys

Update appsettings.Development.json with your API keys and DB config.

5. Launch the development server
dotnet watch run


🤝 Contributor Guidelines

We welcome contributions! To keep things clean and consistent:

Fork the repository and create a feature branch.

Open an issue before starting significant changes.

Use clear and meaningful commit messages (use squash commits if possible).

Submit a pull request using the PR template.

Document any new features or configuration updates.
