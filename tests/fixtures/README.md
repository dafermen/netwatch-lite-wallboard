# Test Fixtures

Store sanitized, deterministic test input here. `wallboard-v0.3.0.json` verifies compatibility when the new automatic recovery field is absent.

Recommended first fixtures:

- A valid v0.3.0 configuration for backward-compatibility checks.
- A minimal configuration with missing optional fields.
- A configuration with boundary numeric values.
- Invalid and truncated JSON samples for failure-path tests.
- Neutral URL examples that do not require private network access.

Fixtures must never contain credentials, cookies, access tokens, internal production URLs, personal data, or machine-specific browser profiles.
