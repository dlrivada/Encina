#!/bin/bash

echo "Provider,Integration,Contract,Property,Load,Total"

for tech in Dapper ADO; do
  for db in SqlServer PostgreSQL MySQL Oracle Sqlite; do
    int=$(find tests/SimpleMediator.$tech.$db.IntegrationTests -name "*.cs" -type f -not -path "*/obj/*" -not -path "*/bin/*" 2>/dev/null | xargs grep "\[Fact\]" 2>/dev/null | wc -l)
    con=$(find tests/SimpleMediator.$tech.$db.ContractTests -name "*.cs" -type f -not -path "*/obj/*" -not -path "*/bin/*" 2>/dev/null | xargs grep "\[Fact\]" 2>/dev/null | wc -l)
    prop=$(find tests/SimpleMediator.$tech.$db.PropertyTests -name "*.cs" -type f -not -path "*/obj/*" -not -path "*/bin/*" 2>/dev/null | xargs grep "\[Theory\]" 2>/dev/null | wc -l)
    load=$(find tests/SimpleMediator.$tech.$db.LoadTests -name "*.cs" -type f -not -path "*/obj/*" -not -path "*/bin/*" 2>/dev/null | xargs grep "\[Fact\]" 2>/dev/null | wc -l)
    total=$((int + con + prop + load))
    if [ $total -gt 0 ]; then
      echo "$tech.$db,$int,$con,$prop,$load,$total"
    fi
  done
done

echo ""
echo "GRAND TOTAL:"
find tests/SimpleMediator.Dapper.*Tests tests/SimpleMediator.ADO.*Tests -name "*.cs" -type f -not -path "*/obj/*" -not -path "*/bin/*" 2>/dev/null | xargs grep -h "\[Fact\]\|\[Theory\]" 2>/dev/null | wc -l
