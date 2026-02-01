# PetSitterApp Setup Guide

## Overview
PetSitterApp is a Blazor WebAssembly Progressive Web Application (PWA) designed for pet sitters to track customers, pets, appointments, and payments. It features offline support using IndexedDB and synchronizes data with the user's Google Account (Google Sheets and Google Calendar).

## Prerequisites
*   .NET 10.0 SDK
*   A Google Cloud Project

## Google Cloud Configuration

To enable the Google Sync features, you must configure a project in the Google Cloud Console.

### 1. Create a Project
1.  Go to the [Google Cloud Console](https://console.cloud.google.com/).
2.  Create a new project (e.g., "PetSitterApp").

### 2. Enable APIs
1.  Navigate to **APIs & Services** > **Library**.
2.  Search for and enable the following APIs:
    *   **Google Sheets API**
    *   **Google Calendar API**

### 3. Configure OAuth Consent Screen
1.  Navigate to **APIs & Services** > **OAuth consent screen**.
2.  Select **External** (unless you are a Google Workspace user and want to restrict it to your organization) and click **Create**.
3.  Fill in the required App Information (App name, User support email, etc.).
4.  **Scopes**: Add the following scopes:
    *   `https://www.googleapis.com/auth/spreadsheets` (See, edit, create, and delete all your Google Sheets spreadsheets)
    *   `https://www.googleapis.com/auth/calendar` (See, edit, share, and permanently delete all the calendars you can access using Google Calendar)
    *   `email`
    *   `profile`
    *   `openid`
5.  **Test Users**: If your app is in "Testing" mode, add the email addresses of the Google accounts you intend to use with the app.

### 4. Create Credentials
1.  Navigate to **APIs & Services** > **Credentials**.
2.  Click **Create Credentials** > **OAuth client ID**.
3.  **Application type**: Select **Single Page Application** (or Web Application).
4.  **Authorized JavaScript origins**: Add the URL where your app will be hosted.
    *   For local development, add: `https://localhost:5001` (and/or `http://localhost:5000` depending on your launch settings).
    *   For production, add your production domain (e.g., `https://my-petsitter-app.web.app`).
5.  **Authorized redirect URIs**: Add the origin followed by `/authentication/login-callback`.
    *   Example: `https://localhost:5001/authentication/login-callback`
6.  Click **Create**.
7.  Copy the **Client ID**.

## Application Configuration

1.  Open the file `PetSitterApp/wwwroot/appsettings.json`.
2.  Replace `YOUR_CLIENT_ID.apps.googleusercontent.com` with the Client ID you copied in the previous step.

```json
{
  "Google": {
    "ClientId": "YOUR_ACTUAL_CLIENT_ID.apps.googleusercontent.com",
    "DefaultScopes": "https://www.googleapis.com/auth/spreadsheets https://www.googleapis.com/auth/calendar"
  }
}
```

## Running the Application

1.  Open a terminal in the project root.
2.  Run the application:
    ```bash
    dotnet run --project PetSitterApp/PetSitterApp.csproj
    ```
3.  Open your browser to the URL shown (usually `http://localhost:5000` or `https://localhost:5001`).

## How to Use

1.  **Login**: Go to the **Settings** page and click "Log in with Google".
2.  **Sync**: After logging in, click "Sync Now" on the Settings page. This will:
    *   Create a new Google Sheet named "PetSitterApp Data" if it doesn't exist.
    *   Push your local data (Customers, Pets, Appointments, Payments) to the sheet.
    *   Sync your appointments to your primary Google Calendar.
3.  **Offline**: You can continue to add/edit records while offline. The changes will be saved locally. Remember to Sync again when you are back online.
