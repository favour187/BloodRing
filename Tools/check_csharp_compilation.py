#!/usr/bin/env python3
"""
Blood Ring — C# Structural Lint v2.0 (honest edition)

This script performs STATIC ANALYSIS ONLY — it is NOT a C# compiler.
It checks for common structural issues but CANNOT detect:
  - Missing type references (requires Unity's type resolver)
  - Incorrect method signatures
  - Runtime null references
  - Unity-specific attribute validation
  - Netcode RPC compilation validation

For real compilation verification, build in Unity 2022.3.50f1.
"""

import os
import re
import sys
import glob

def find_cs_files(root):
    """Find all .cs files in the project."""
    return sorted(glob.glob(os.path.join(root, "**", "*.cs"), recursive=True))

def check_brace_balance(content, filepath):
    """Check for balanced braces."""
    issues = []
    opens = content.count('{')
    closes = content.count('}')
    if opens != closes:
        issues.append(f"  ⚠️  Unbalanced braces: {opens} opening vs {closes} closing")
    return issues

def check_using_statements(content, filepath):
    """Check for missing using statements based on usage patterns."""
    issues = []
    
    # Check Unity.Netcode usage (only flag if class extends NetworkBehaviour or uses attributes)
    if 'NetworkBehaviour' in content or '[ServerRpc' in content or '[ClientRpc' in content or 'NetworkVariable' in content:
        if 'using Unity.Netcode' not in content:
            issues.append("  ⚠️  Uses Netcode types but missing 'using Unity.Netcode;'")
    
    # Check UnityEngine.Networking usage
    if 'UnityWebRequest' in content and 'using UnityEngine.Networking' not in content:
        issues.append("  ⚠️  Uses UnityWebRequest but missing 'using UnityEngine.Networking;'")
    
    # Check TMPro usage
    if 'TextMeshPro' in content and 'using TMPro' not in content:
        issues.append("  ⚠️  Uses TextMeshPro but missing 'using TMPro;'")
    
    # Check System.Threading.Tasks usage
    if 'Task<' in content and 'using System.Threading.Tasks' not in content:
        issues.append("  ⚠️  Uses Task but missing 'using System.Threading.Tasks;'")
    
    return issues

def check_class_structure(content, filepath):
    """Check for basic class structure issues."""
    issues = []
    
    # Check for class declaration
    if not re.search(r'(?:public|private|protected|internal|static|abstract|sealed|partial)\s+(?:class|struct|enum|interface)\s+\w+', content):
        if not filepath.endswith('AssemblyInfo.cs'):
            issues.append("  ⚠️  No class/struct/enum/interface declaration found")
    
    return issues

def check_common_issues(content, filepath):
    """Check for common code issues."""
    issues = []
    
    # Check for TODO/FIXME/HACK markers
    for m in re.finditer(r'//\s*(TODO|FIXME|HACK|XXX)\s*[:\s](.+)', content):
        line_num = content[:m.start()].count('\n') + 1
        issues.append(f"  📝 Line {line_num}: {m.group(0).strip()}")
    
    # Check for empty methods (potential stubs)
    for m in re.finditer(r'void\s+\w+\s*\([^)]*\)\s*\{\s*\}', content):
        line_num = content[:m.start()].count('\n') + 1
        method_name = re.search(r'void\s+(\w+)', m.group())
        if method_name:
            issues.append(f"  📝 Line {line_num}: Empty method '{method_name.group(1)}' (stub)")
    
    return issues

def main():
    """Run all checks."""
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    cs_files = find_cs_files(os.path.join(project_root, "Assets"))
    
    print("=" * 80)
    print("  BLOOD RING — C# STRUCTURAL LINT v2.0")
    print("  ⚠️  THIS IS NOT A COMPILER — Verify real builds in Unity 2022.3.50f1")
    print("=" * 80)
    print(f"\nScanning {len(cs_files)} C# files...\n")
    
    total_issues = 0
    files_with_issues = 0
    
    for filepath in cs_files:
        rel_path = os.path.relpath(filepath, project_root)
        try:
            with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
        except Exception as e:
            print(f"❌ {rel_path}: Could not read file: {e}")
            total_issues += 1
            files_with_issues += 1
            continue
        
        issues = []
        issues.extend(check_brace_balance(content, rel_path))
        issues.extend(check_using_statements(content, rel_path))
        issues.extend(check_class_structure(content, rel_path))
        issues.extend(check_common_issues(content, rel_path))
        
        if issues:
            print(f"📄 {rel_path}")
            for issue in issues:
                print(issue)
            print()
            total_issues += len(issues)
            files_with_issues += 1
    
    # Summary
    print("=" * 80)
    if total_issues == 0:
        print(f"  ✅ No structural issues found in {len(cs_files)} files.")
    else:
        print(f"  ⚠️  Found {total_issues} issues in {files_with_issues}/{len(cs_files)} files.")
    
    print()
    print("  REMINDER: This is a lint, not a compiler.")
    print("  To verify compilation, build in Unity 2022.3.50f1:")
    print("    1. Open project in Unity Hub")
    print("    2. Window > General > Console")
    print("    3. Check for red errors")
    print("    4. File > Build Settings > Build")
    print("=" * 80)
    
    return 0 if total_issues == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
