```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 13.9033 ns | 4.6283 ns | 0.2537 ns |  0.58 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4647 ns | 0.1966 ns | 0.0108 ns |  0.02 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.2432 ns | 2.1664 ns | 0.1187 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 24.0080 ns | 1.1196 ns | 0.0614 ns |  1.00 | 0.0033 |      56 B |        1.00 |
