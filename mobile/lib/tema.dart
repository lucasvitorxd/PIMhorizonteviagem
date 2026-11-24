// Cores e formatação de moeda compartilhadas pelas telas.
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

const navy = Color(0xff142c36), gold = Color(0xffd6ab59);
final moeda = NumberFormat.currency(locale: 'pt_BR', symbol: r'R$');
