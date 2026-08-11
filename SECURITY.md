# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| 1.0.x   | Yes       |
| < 1.0   | No        |

Only the latest release is supported. Fixes go into a new release rather than
being backported.

## Reporting a vulnerability

**Please do not open a public issue for a security problem.**

Report it privately through GitHub:

1. Go to the [Security tab](https://github.com/Julien-FONTANA/PokerRanges/security)
2. Click **Report a vulnerability**

That opens a private channel visible only to the maintainer. If private
reporting is unavailable to you for any reason, send a direct message to
[@Julien-FONTANA](https://github.com/Julien-FONTANA) on GitHub asking for a
private contact, without describing the issue in that first message.

Please include:

- What the problem is, and what an attacker could achieve with it
- Steps to reproduce, or a proof of concept
- The version you tested, and your operating system

You can expect an acknowledgement within a week. This is a personal project
maintained in spare time, so please be patient with the timeline — but you will
get an answer, and you will be credited in the release notes if you want to be.

## What is in scope

PokerRanges is an offline desktop application. It makes **no network calls**,
handles **no credentials**, and has **no server side**. That narrows the realistic
attack surface considerably. What remains worth reporting:

- **Malicious chart files.** Charts are JSON read from
  `%APPDATA%\PokerRanges\charts\`, a directory the user is invited to edit. A
  crafted file that achieves anything worse than a clean error message — code
  execution, path traversal outside that directory, unbounded resource
  consumption — is a genuine vulnerability.
- **Session and journal files.** Same reasoning for the saved preferences, the
  hand in progress, and the hand journal.
- **The published executable.** Anything about how the self-contained binary is
  produced or loads its dependencies that would let another program on the
  machine inject code into it.

## What is not in scope

- The app crashing on obviously malformed input that the user wrote themselves,
  where the crash is contained and loses nothing but the current hand. That is a
  bug — open a normal issue.
- Poker advice being wrong. That is a correctness issue, sometimes an
  interesting one, but not a security issue. The README lists the known model
  limitations.
- Anything requiring an attacker to already have write access to the user's
  `%APPDATA%` directory *and* nothing else of value on that machine. If they are
  there, the game is already over.
