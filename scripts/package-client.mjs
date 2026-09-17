import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = path.join(root, 'PackageSource/com.gamers.client');
const sample = path.join(root, 'Assets/Samples/Gamers Client Helper/1.0.0/Reference Integration');
const target = path.join(source, 'Samples~/ReferenceIntegration');

// The imported demo is the editable sample. Publish exactly that directory,
// preserving metadata, so reimporting cannot revert to an older implementation.
fs.rmSync(target, { recursive: true, force: true });
fs.cpSync(sample, target, { recursive: true });
for (const name of ['integration-guide.md', 'api-reference.md']) {
  fs.copyFileSync(path.join(root, 'docs', name), path.join(source, 'Documentation~', name));
}
const version = JSON.parse(fs.readFileSync(path.join(source, 'package.json'), 'utf8')).version;
const stage = fs.mkdtempSync(path.join(os.tmpdir(), 'gamers-package-'));
try {
  fs.cpSync(source, path.join(stage, 'package'), { recursive: true });
  const output = path.join(root, `com.gamers.client-${version}.tgz`);
  execFileSync('tar', ['--exclude=.DS_Store', '-czf', output, '-C', stage, 'package'], {
    env: { ...process.env, COPYFILE_DISABLE: '1' },
  });
  console.log(output);
} finally {
  fs.rmSync(stage, { recursive: true, force: true });
}
