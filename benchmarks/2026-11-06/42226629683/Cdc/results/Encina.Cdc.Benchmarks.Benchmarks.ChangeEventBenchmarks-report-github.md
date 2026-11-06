```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 27.0688 ns | 14.8065 ns | 0.8116 ns |  1.00 |    0.04 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.5108 ns |  0.0866 ns | 0.0047 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  9.5249 ns |  5.2301 ns | 0.2867 ns |  0.35 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.9728 ns | 15.4474 ns | 0.8467 ns |  1.00 |    0.04 | 0.0033 |      56 B |        1.00 |
