```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.6833 ns | 12.7596 ns | 0.6994 ns |  0.54 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.5856 ns |  0.7181 ns | 0.0394 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.7658 ns |  3.5732 ns | 0.1959 ns |  0.32 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.1999 ns | 12.8560 ns | 0.7047 ns |  1.00 |    0.03 | 0.0033 |      56 B |        1.00 |
