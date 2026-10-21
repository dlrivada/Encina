```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.726 ns | 4.5504 ns | 0.2494 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.386 ns | 0.0629 ns | 0.0034 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.237 ns | 3.8526 ns | 0.2112 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.120 ns | 1.3851 ns | 0.0759 ns |  1.00 | 0.0033 |      56 B |        1.00 |
