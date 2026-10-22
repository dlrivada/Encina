```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.4309 ns | 14.0735 ns | 0.7714 ns |  0.55 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4836 ns |  0.1381 ns | 0.0076 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.5693 ns |  3.9231 ns | 0.2150 ns |  0.33 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.1714 ns |  4.7379 ns | 0.2597 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
