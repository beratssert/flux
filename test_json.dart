import 'dart:convert';

void main() {
  String noZ = "2026-04-06T06:00:00";
  print(DateTime.parse(noZ));
  print(DateTime.parse(noZ).toLocal());
  print(DateTime.parse(noZ + "Z").toLocal());
}
