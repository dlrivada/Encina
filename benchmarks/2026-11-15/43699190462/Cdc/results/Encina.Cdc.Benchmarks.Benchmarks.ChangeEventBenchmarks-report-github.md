```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.1148 ns | 13.8392 ns | 0.7586 ns |  0.56 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4929 ns |  0.2286 ns | 0.0125 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.5391 ns |  4.2998 ns | 0.2357 ns |  0.34 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 25.1336 ns |  8.4825 ns | 0.4650 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
