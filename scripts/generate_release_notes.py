import subprocess
import os
import re

def get_git_output(command):
    try:
        result = subprocess.run(command, capture_output=True, text=True, check=True)
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Error running git command: {command}\n{e}")
        return ""

def generate_release_notes():
    # Find all merge commits on main
    # We use --merges to find merge commits
    # Format: Hash|Date(iso)|Message
    # Date format: YYYY.MM.DD-HHmm
    log_cmd = [
        "git", "log", "--merges", "--first-parent", "origin/main",
        "--pretty=format:%H|%cd|%s", "--date=format:%Y.%m.%d-%H%M"
    ]

    # If origin/main is not available, try main or HEAD
    # In sandbox, it might be just HEAD if detached
    # Let's try to detect if we are on a branch
    try:
        subprocess.run(["git", "rev-parse", "origin/main"], check=True, capture_output=True)
        branch = "origin/main"
    except:
        branch = "HEAD"
        # If we are detached, we might need to find where main is or just trust HEAD
        # The user said "Treat every merge as a release"
        # So iterating from HEAD backwards through merges is reasonable.

    log_cmd = [
        "git", "log", "--merges", "--first-parent", branch,
        "--pretty=format:%H|%cd|%s", "--date=format:%Y.%m.%d-%H%M"
    ]

    merges_output = get_git_output(log_cmd)
    if not merges_output:
        print("No merge commits found.")
        return

    releases = []

    for line in merges_output.split('\n'):
        if not line: continue
        parts = line.split('|')
        if len(parts) < 3: continue

        commit_hash = parts[0]
        date_str = parts[1]
        message = parts[2]

        # Get the commit details for this merge
        # range: hash^1..hash (commits reachable from hash but not its first parent)
        # This gives us the commits brought in by the merge
        details_cmd = [
            "git", "log", f"{commit_hash}^1..{commit_hash}", "--pretty=format:* %s (%an)"
        ]
        details = get_git_output(details_cmd)

        releases.append({
            "version": date_str,
            "title": message,
            "details": details
        })

    # Generate Markdown
    md_content = "# Release Notes\n\n"

    for release in releases:
        md_content += f"## Release {release['version']}\n\n"
        md_content += f"{release['title']}\n\n"
        if release['details']:
            md_content += f"{release['details']}\n\n"
        else:
            md_content += "* No detailed commit messages found.\n\n"

    # Write to file
    output_path = "docs/RELEASE_NOTES.md"
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        f.write(md_content)

    print(f"Generated {output_path}")

if __name__ == "__main__":
    generate_release_notes()
