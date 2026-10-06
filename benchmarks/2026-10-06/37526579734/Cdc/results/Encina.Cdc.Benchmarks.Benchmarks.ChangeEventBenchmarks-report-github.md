```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.171 ns | 6.9252 ns | 0.3796 ns |  0.58 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.416 ns | 0.9900 ns | 0.0543 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.528 ns | 4.7516 ns | 0.2604 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.539 ns | 2.6968 ns | 0.1478 ns |  1.00 | 0.0033 |      56 B |        1.00 |
