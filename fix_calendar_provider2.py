import re

with open('frontend/lib/features/calendar/presentation/providers/calendar_provider.dart', 'r') as f:
    lines = f.readlines()

output = []
for line in lines:
    if line.startswith("import 'package:jwt_decoder/jwt_decoder.dart';"):
        continue
    if "import '../../../auth/data/auth_session_controller.dart';" in line:
        # Move this to the top of the file
        output.insert(0, line)
        continue
    output.append(line)    

# Remove duplicate imports
final_output = []
seen_imports = set()
for line in output:
    if line.startswith('import'):
        if line in seen_imports:
            continue
        seen_imports.add(line)
        # Put all imports at the very top of final_output
        final_output.insert(0, line)
    else:
        final_output.append(line)

# Let's fix the order properly
imports = [line for line in final_output if line.startswith('import')]
rest = [line for line in final_output if not line.startswith('import')]

with open('frontend/lib/features/calendar/presentation/providers/calendar_provider.dart', 'w') as f:
    f.writelines(imports)
    f.writelines(rest)

