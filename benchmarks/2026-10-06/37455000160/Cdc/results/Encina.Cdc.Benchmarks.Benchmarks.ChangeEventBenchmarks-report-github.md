```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 26.880 ns | 5.0391 ns | 0.2762 ns |  0.90 | 0.0054 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.447 ns | 0.0835 ns | 0.0046 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.076 ns | 1.1023 ns | 0.0604 ns |  0.47 | 0.0022 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.869 ns | 5.5623 ns | 0.3049 ns |  1.00 | 0.0022 |      56 B |        1.00 |
