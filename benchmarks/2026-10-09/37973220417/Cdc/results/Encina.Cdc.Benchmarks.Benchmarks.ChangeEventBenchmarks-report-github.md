```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.6771 ns | 7.4381 ns | 0.4077 ns |  0.67 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9841 ns | 0.1127 ns | 0.0062 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.9962 ns | 6.4337 ns | 0.3527 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.0193 ns | 1.8823 ns | 0.1032 ns |  1.00 | 0.0033 |      56 B |        1.00 |
