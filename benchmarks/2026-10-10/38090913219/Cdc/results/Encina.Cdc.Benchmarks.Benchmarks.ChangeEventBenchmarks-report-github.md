```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.94GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.446 ns | 8.5265 ns | 0.4674 ns |  0.89 | 0.0054 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.466 ns | 0.2465 ns | 0.0135 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.666 ns | 5.6486 ns | 0.3096 ns |  0.44 | 0.0022 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.734 ns | 0.7927 ns | 0.0435 ns |  1.00 | 0.0022 |      56 B |        1.00 |
