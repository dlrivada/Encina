```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.8047 ns | 2.3488 ns | 0.1287 ns |  0.56 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4747 ns | 0.9178 ns | 0.0503 ns |  0.02 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.2873 ns | 5.4139 ns | 0.2968 ns |  0.32 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.2051 ns | 1.6706 ns | 0.0916 ns |  1.00 | 0.0033 |      56 B |        1.00 |
