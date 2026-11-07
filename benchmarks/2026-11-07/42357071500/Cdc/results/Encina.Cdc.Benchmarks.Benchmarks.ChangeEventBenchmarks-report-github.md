```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 14.3773 ns | 10.3174 ns | 0.5655 ns |  0.55 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4897 ns |  0.1075 ns | 0.0059 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.7598 ns |  6.2861 ns | 0.3446 ns |  0.34 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 25.9574 ns |  9.8906 ns | 0.5421 ns |  1.00 |    0.03 | 0.0033 |      56 B |        1.00 |
