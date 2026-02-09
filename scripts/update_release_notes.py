import os
import subprocess
from datetime import datetime

def get_git_output(command):
    try:
        result = subprocess.run(command, capture_output=True, text=True, check=True)
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Error running git command: {command}\n{e}")
        return ""

def update_release_notes():
    # Get version from env or generate
    version = os.environ.get("BUILD_VERSION")
    if not version:
        # Fallback for local testing
        version = datetime.now().strftime("%Y.%m.%d-%H%M")
        print(f"BUILD_VERSION not set, using generated: {version}")

    # Get the latest commit hash
    commit_hash = get_git_output(["git", "rev-parse", "HEAD"])

    # Get commit message subject
    subject = get_git_output(["git", "log", "-1", "--pretty=%s"])

    # Get details
    # Check if merge commit (2 parents)
    parents = get_git_output(["git", "log", "-1", "--pretty=%P"]).split()
    if len(parents) > 1:
        # It's a merge commit. Get changes from parent 1 to HEAD
        # HEAD^1..HEAD
        details = get_git_output(["git", "log", f"{commit_hash}^1..{commit_hash}", "--pretty=format:* %s (%an)"])
    else:
        # It's a direct commit (fast-forward or squash merge without merge commit structure?)
        # Just list the commit itself
        details = f"* {subject} ({get_git_output(['git', 'log', '-1', '--pretty=%an'])})"

    # Read existing file
    file_path = "docs/RELEASE_NOTES.md"
    existing_content = ""
    if os.path.exists(file_path):
        with open(file_path, "r") as f:
            existing_content = f.read()

    # Create new entry
    new_entry = f"## Release {version}\n\n{subject}\n\n{details}\n\n"

    # Insert after title if present
    title = "# Release Notes\n"
    if existing_content.startswith(title):
        # Check if there is a blank line after title
        if existing_content.startswith(title + "\n"):
            # Title + blank line
            split_idx = len(title) + 1
        else:
            # Title only (no blank line? unlikely based on generation script)
            split_idx = len(title)
            new_entry = "\n" + new_entry # Ensure spacing

        final_content = existing_content[:split_idx] + new_entry + existing_content[split_idx:]
    else:
        # No title, prepend everything
        final_content = title + "\n" + new_entry + existing_content

    # Write back
    with open(file_path, "w") as f:
        f.write(final_content)

    print(f"Updated {file_path} with Release {version}")

if __name__ == "__main__":
    update_release_notes()
