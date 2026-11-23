```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.6979 ns |  9.1877 ns | 0.5036 ns |  0.58 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4699 ns |  0.3379 ns | 0.0185 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  9.3490 ns | 29.1403 ns | 1.5973 ns |  0.37 |    0.06 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 25.1405 ns |  5.7789 ns | 0.3168 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
