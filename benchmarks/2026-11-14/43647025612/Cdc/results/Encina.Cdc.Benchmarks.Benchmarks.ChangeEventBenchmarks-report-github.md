```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.055 ns | 8.2984 ns | 0.4549 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.402 ns | 0.5776 ns | 0.0317 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.523 ns | 0.8367 ns | 0.0459 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.427 ns | 2.0422 ns | 0.1119 ns |  1.00 | 0.0033 |      56 B |        1.00 |
