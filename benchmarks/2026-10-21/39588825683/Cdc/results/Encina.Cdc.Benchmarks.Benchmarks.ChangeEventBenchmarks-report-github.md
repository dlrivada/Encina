```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.2392 ns |  2.6705 ns | 0.1464 ns |  0.57 |    0.01 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4884 ns |  0.1532 ns | 0.0084 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.6487 ns | 13.2429 ns | 0.7259 ns |  0.35 |    0.03 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 24.9118 ns | 10.1428 ns | 0.5560 ns |  1.00 |    0.03 | 0.0033 |      56 B |        1.00 |
