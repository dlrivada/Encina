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
| CreateChangeEvent          | 26.304 ns | 6.1990 ns | 0.3398 ns |  0.90 | 0.0054 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.447 ns | 0.0408 ns | 0.0022 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.327 ns | 1.0877 ns | 0.0596 ns |  0.46 | 0.0022 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.101 ns | 2.0072 ns | 0.1100 ns |  1.00 | 0.0022 |      56 B |        1.00 |
